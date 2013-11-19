using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Data;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;


namespace DPS
{
    [ApiVersion(1, 14)]
    public class DPS : TerrariaPlugin
    {
        public override Version Version { get { return new Version(1, 1, 0); } }

        public override string Name { get { return "DPS"; } }

        public override string Author { get { return "Antagonist"; } }

        public override string Description { get { return "A DPS Measuring plugin"; } }

        public DPS(Main game)
            : base(game)
        {
            Order = 335;
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            }
            base.Dispose(disposing);
        }

        public string SyntaxErrorPrefix = "Invalid syntax! Proper usage: ";
        public string NoPermissionError = "You do not have permission to use this command.";

        public static DPSPlayer[] DPSPlayers = new DPSPlayer[256];

        public void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("dps.cmd", DPSCMD, "dps"));
        }

        public void OnGetData(GetDataEventArgs args)
        {
            try
            {
                if (args.MsgID == PacketTypes.NpcStrike)
                {
                    using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
                    {
                        var reader = new BinaryReader(data);

                        var NPCID = reader.ReadInt16();
                        var Damage = reader.ReadInt16();
                        var Knockback = reader.ReadSingle();
                        var Direction = reader.ReadByte();
                        var Crit = reader.ReadBoolean();

                        TSPlayer player = TShock.Players[args.Msg.whoAmI];
                        DPSPlayer dpsplayer = DPSPlayers[player.Index];
                        //player.SendInfoMessage(NPCID + " " + Damage + " " + Knockback + " " + Direction + " " + Crit);
                        dpsplayer.totaldamage += Damage;
                        dpsplayer.dpstotal += Damage;
                        dpsplayer.attackamount += 1;

                        if (!dpsplayer.countingdps)
                        {
                            dpsplayer.countingdps = true;
                            dpsplayer.lasttime = DateTime.Now;
                            dpsplayer.timespan = 0;
                            //commented this instead of removing it, just for testing purposes
                            //var newthread = new DamageThread(args);
                            //Thread thread = new Thread(new ThreadStart(newthread.CalcDPS));
                            //thread.Start();
                        }
                        dpsplayer.timespan += (DateTime.Now - dpsplayer.lasttime).TotalSeconds;
                        dpsplayer.lasttime = DateTime.Now;

                        if (dpsplayer.timespan >= dpsplayer.notifyinterval && (DateTime.Now - dpsplayer.lasttime).TotalSeconds <= 2 * dpsplayer.notifyinterval)
                        {
                            int dps = Convert.ToInt32(dpsplayer.dpstotal / dpsplayer.timespan);
                            if (dps > 0)
                            {
                                dpsplayer.dps = dps;
                            }
                            if (dpsplayer.notify)
                            {
                                dpsplayer.dpsstats = String.Format("Damage Summary: [DPS: {0}, Dmg/Attk: {1}, Total Attks: {2}, Total Dmg: {3}]", dpsplayer.dps, dpsplayer.totaldamage / dpsplayer.attackamount, dpsplayer.attackamount, dpsplayer.totaldamage);
                                player.SendSuccessMessage(dpsplayer.dpsstats);
                            }
                            dpsplayer.dpstotal = 0;
                            dpsplayer.countingdps = false;
                            dpsplayer.seconds = 0;
                            dpsplayer.timespan = 0;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.ConsoleError("DPS OnGetData Error: " + e.Message);
            }
        }

        public void OnJoin(JoinEventArgs args)
        {
            DPSPlayers[args.Who] = new DPSPlayer(args.Who);
        }
        public void OnLeave(LeaveEventArgs args)
        {
            DPSPlayers[args.Who] = null;
        }

        public void DPSCMD(CommandArgs args)
        {
            try
            {
                TSPlayer player = args.Player;
                DPSPlayer dpsplayer = DPSPlayers[player.Index];

                if (args.Parameters.Count == 1)
                {
                    var option = args.Parameters[0].ToLower();

                    if (option == "show")
                    {
                        if (dpsplayer.timespan < 1)
                        {
                            dpsplayer.timespan = 1;
                        }
                        dpsplayer.dpsstats = String.Format("Damage Summary: [DPS: {0}, Dmg/Attk: {1}, Total Attks: {2}, Total Dmg: {3}]", dpsplayer.dps, dpsplayer.attackamount != 0 ? dpsplayer.totaldamage / dpsplayer.attackamount : dpsplayer.totaldamage, dpsplayer.attackamount, dpsplayer.totaldamage);
                        player.SendSuccessMessage(dpsplayer.dpsstats);
                        return;
                    }
                    if (option == "notify")
                    {
                        dpsplayer.notify = dpsplayer.notify == false;
                        player.SendSuccessMessage(dpsplayer.notify ? "You will now receive DPS notifications." : "You will no longer receive DPS notifications.");
                        return;
                    }
                    if (option == "notifyinterval")
                    {
                        player.SendErrorMessage(SyntaxErrorPrefix + "/dps notifyinterval <new interval>");
                        return;
                    }
                    else if (option == "reset")
                    {
                        dpsplayer.totaldamage = 0;
                        dpsplayer.attackamount = 0;
                        player.SendSuccessMessage(String.Format("Your DPS stats have been reset."));
                        return;
                    }
                    else if (option == "brag")
                    {
                        TSPlayer.All.SendInfoMessage(String.Format("{0}: My DPS is {1}.", player.Name, dpsplayer.dps));
                        return;
                    }
                }
                else if (args.Parameters.Count == 2)
                {
                    var option = args.Parameters[0].ToLower();
                    var option2 = args.Parameters[1].ToLower();

                    if (option == "notifyinterval")
                    {
                        int interval;
                        if (!int.TryParse(args.Parameters[1], out interval))
                        {
                            player.SendErrorMessage(SyntaxErrorPrefix + "/dps notifyinterval <new interval>");
                            return;
                        }
                        dpsplayer.notifyinterval = Convert.ToInt32(args.Parameters[1]);
                        player.SendSuccessMessage(String.Format("You will now receive DPS notifications every {0} seconds.", dpsplayer.notifyinterval));
                        return;
                    }
                }
                player.SendErrorMessage(SyntaxErrorPrefix + "/dps <show/notify/notifyinterval/reset/brag>");
                return;
            }
            catch (Exception e)
            {
                Log.ConsoleError("DPS CMD Error: " + e.Message);
            }
        }
    }
    public class DamageThread
    {
        GetDataEventArgs args;
        TSPlayer player;
        DPSPlayer dpsplayer;
        public DamageThread(GetDataEventArgs args)
        {
            this.args = args;
            this.player = TShock.Players[args.Msg.whoAmI];
            this.dpsplayer = DPS.DPSPlayers[args.Msg.whoAmI];
        }
        public void CalcDPS()
        {
            try
            {
                dpsplayer.seconds = 0;
                while (dpsplayer.countingdps && !Netplay.disconnect && dpsplayer.seconds++ < 5)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (player == null)
                    {
                        Log.ConsoleError("Player left before notified of DPS.");
                        return;
                    }
                    if (dpsplayer.timespan < 1)
                    {
                        dpsplayer.timespan = 1;
                    }
                    int dps = Convert.ToInt32(dpsplayer.dpstotal / dpsplayer.timespan);
                    if (dps > 0)
                    {
                        dpsplayer.dps = dps;
                    }
                    if (dpsplayer.seconds > 4)
                    {
                        dpsplayer.countingdps = false;
                    }
                }
                if (dpsplayer.notify)
                {
                    dpsplayer.dpsstats = String.Format("Damage Summary: [DPS: {0}, Dmg/Attk: {1}, Total Attks: {2}, Total Dmg: {3}]", dpsplayer.dps, dpsplayer.totaldamage / dpsplayer.attackamount, dpsplayer.attackamount, dpsplayer.totaldamage);
                    player.SendSuccessMessage(dpsplayer.dpsstats);
                }
                dpsplayer.dpstotal = 0;
                dpsplayer.countingdps = false;
                dpsplayer.seconds = 0;
                dpsplayer.timespan = 0;
            }
            catch (Exception e)
            {
                Log.ConsoleError("CalcDPS Error: " + e.Message);
                DPS.DPSPlayers[args.Index].countingdps = false;
            }
        }
    }
}