using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Models
{
    public class GetStatusInfo
    {
        public string JType { get; set; } = "GetStatusInfo";
    }

    public class StatusResponseInfo
    {
        public string JType { get; set; } = "StatusResponseInfo";
        public int NumGames { get; set; }
        public int NumPlayers { get; set; }
        public TimeSpan Uptime { get; set; }
        public List<Guid> NodeIds { get; set; }
        public long MessagesPerSecondIn { get; set; }
        public long MessagesPerSecondOut { get; set; }
        public string Status { get; set; }
        public int MaxGames { get; set; }
        public DateTime MaxGamesTime { get; set; }
    }

}
