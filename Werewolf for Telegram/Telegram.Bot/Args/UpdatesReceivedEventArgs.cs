using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Telegram.Bot.Args
{
    public class UpdatesReceivedEventArgs : EventArgs
    {
        public int UpdateCount { get; set; }
        public DateTime TimeReceived => DateTime.Now;

        internal UpdatesReceivedEventArgs(int count)
        {
            UpdateCount = count;
        }
    }
}
