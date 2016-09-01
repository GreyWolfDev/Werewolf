using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Args
{
    public class StatusChangeEventArgs : EventArgs
    {
        public Status Status { get; private set; }

        internal StatusChangeEventArgs(Status status)
        {
            Status = status;
        }
    }
}
