using System;
using System.Collections.Generic;

namespace Werewolf_Node.Models
{
    public class GameInfo
    {
        public HashSet<int> Users { get; set; } = new HashSet<int>();  //update this to users alive
        public long GroupId { get; set; }
        public int Guid { get; set; }
        public string Language { get; set; }
        public string ChatGroup { get; set; }
        public GameState State { get; set; }
        public int NodeId { get; set; } = Program.ClientId;
        public HashSet<IPlayer> Players { get; set; } = new HashSet<IPlayer>();
        public int PlayerCount { get; set; }
    }

    public enum GameState
    {
        Joining, Running, Dead
    }
}
