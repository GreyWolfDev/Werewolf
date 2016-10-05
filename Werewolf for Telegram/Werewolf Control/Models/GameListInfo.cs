using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Models
{
    public class GameListInfo
    {
        public long GroupId { get; set; }
        public string GroupName { get; set; }
        public int NumPlayers { get; set; }
        public int PlayersAlive { get; set; }
        public GameState State { get; set; }
    }
}
