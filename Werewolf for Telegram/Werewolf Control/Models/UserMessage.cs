using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Models
{
    class UserMessage
    {
        public DateTime Time { get; set; }
        public string Command { get; set; }

        public UserMessage(string command)
        {
            Time = DateTime.Now;
            Command = command;
        }
    }
}
