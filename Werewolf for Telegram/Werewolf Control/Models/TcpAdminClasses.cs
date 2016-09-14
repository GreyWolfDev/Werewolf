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
        public string BotName { get; set; }
        public int NumGames { get; set; }
        public int NumPlayers { get; set; }
        public TimeSpan Uptime { get; set; }
        public List<NodeResponseInfo> Nodes { get; set; }
        public long MessagesProcPerSecond { get; set; }
        public long MessagesPerSecondIn { get; set; }
        public long MessagesPerSecondOut { get; set; }
        public string Status { get; set; }
        public int MaxGames { get; set; }
        public DateTime MaxGamesTime { get; set; }
    }

    public class GetNodeInfo
    {
        public string JType { get; set; } = "GetNodeInfo";
        public Guid ClientId { get; set; }
    }

    public class NodeResponseInfo
    {
        public string JType { get; set; } = "NodeResponseInfo";
        public Guid ClientId { get; set; }
        public int CurrentGames { get; set; }
        public int CurrentPlayers { get; set; }
        public TimeSpan Uptime { get; set; }
        public List<GameListInfo> Games { get; set; } = new List<GameListInfo>();
        public string Version { get; set; }
        public bool ShuttingDown { get; set; }
        public int MessagesSent { get; set; }
    }

    public class GetGameInfo
    {
        public string JType { get; set; } = "GetGameInfo";
        public long GroupId { get; set; }
        public Guid ClientId { get; set; }
    }

    public class StopNodeRequest
    {
        public string JType { get; set; } = "StopNodeRequest";
        public Guid ClientId { get; set; }
    }
}
