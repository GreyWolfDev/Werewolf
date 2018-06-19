using System;

namespace Werewolf_Node.Models
{
    public class GetGameInfo
    {
        public string JType { get; set; } = "GetGameInfo";
        public long GroupId { get; set; }
        public Guid ClientId { get; set; }
    }
}
