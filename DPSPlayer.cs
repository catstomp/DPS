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
        public int prevtotaldamage { get; set; }
        public int dpstotal { get; set; }
        public int dps { get; set; }
        public bool countingdps { get; set; }
        public int seconds { get; set; }
        public double timespan { get; set; }
        public DateTime lasttime { get; set; }
        public int attackamount { get; set; }
        public string dpsstats { get; set; }

		public DPSPlayer(int index)
        {
            this.Index = index;
            this.notify = false;
            this.totaldamage = 0;
            this.prevtotaldamage = 0;
            this.dpstotal = 0;
            this.countingdps = false;
            this.seconds = 0;
            this.timespan = 0;
            this.lasttime = DateTime.Now;
            this.attackamount = 0;
            this.dpsstats = "";
        }
    }
}
