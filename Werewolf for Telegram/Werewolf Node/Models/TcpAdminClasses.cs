using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Node.Models
{
    public class GetGameInfo
    {
        public string JType { get; set; } = "GetGameInfo";
        public long GroupId { get; set; }
        public Guid ClientId { get; set; }
    }
}
