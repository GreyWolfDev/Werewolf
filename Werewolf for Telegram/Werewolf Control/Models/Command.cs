using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Werewolf_Control.Helpers;

namespace Werewolf_Control.Models
{
    class Command
    {
        public string Trigger { get; set; }
        public bool GroupAdminOnly { get; set; }
        public bool GlobalAdminOnly { get; set; }
        public bool DevOnly { get; set; }
        public bool Blockable { get; set; }
        public Bot.ChatCommandMethod Method { get; set; }
        public bool InGroupOnly { get; set; }
    }
}
