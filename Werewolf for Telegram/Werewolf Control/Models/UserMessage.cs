using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Werewolf_Control.Models
{
    class UserMessage
    {
        public DateTime Time { get; set; }
        public string Command { get; set; }
        public bool Replied { get; set; }

        public UserMessage(Message m)
        {
            Time = m.Date;
            Command = m.Text;
        }
    }
}
