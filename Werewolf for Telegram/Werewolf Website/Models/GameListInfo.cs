using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Werewolf_Website.Models
{
    public class GameListInfo
    {
        public long GroupId { get; set; }
        public string GroupName { get; set; }
        public int NumPlayers { get; set; }
        public int PlayersAlive { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public GameState State { get; set; }
    }
}