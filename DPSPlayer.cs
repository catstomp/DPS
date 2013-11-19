using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace DPS
{
    public class DPSPlayer
    {
		public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public bool notify { get; set; }
        public int totaldamage { get; set; }
        public int dps { get; set; }
        public double timespan { get; set; }
        public DateTime prevtime { get; set; }
        public int prevdmg { get; set; }
        public int attackamount { get; set; }
        public string dpsstats { get; set; }
        public int notifyinterval { get; set; }

		public DPSPlayer(int index)
        {
            this.Index = index;
            this.notify = false;
            this.totaldamage = 0;
            this.timespan = 0;
            this.prevtime = DateTime.Now;
            this.prevdmg = 0;
            this.attackamount = 0;
            this.dpsstats = "";
            this.notifyinterval = 5;
        }
    }
}
