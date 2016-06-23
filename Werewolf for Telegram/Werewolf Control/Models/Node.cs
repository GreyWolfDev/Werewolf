using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Werewolf_Control.Helpers;

namespace Werewolf_Control.Models
{
    public class Node
    {
        public TcpClient TcpClient { get; set; }
        public bool ShuttingDown { get; set; }
        public Guid ClientId { get; set; }
        public int CurrentGames { get; set; }
        public int TotalGames { get; set; }
        public int CurrentPlayers { get; set; }
        public int TotalPlayers { get; set; }
        public int ThreadCount { get; set; }
        public int DuplicateGamesRemoved { get; set; }
        public TimeSpan Uptime { get; set; }
        public HashSet<GameInfo> Games { get; set; } = new HashSet<GameInfo>();
        public string Version { get; set; }

        public void StartGame(Update update, bool chaos = false)
        {
            var info = new GameStartInfo
            {
                Chat = update.Message.Chat,
                Chaos = chaos,
                User = update.Message.From
            };
            this.Broadcast(JsonConvert.SerializeObject(info));
            this.Games.Add(new GameInfo
            {
                ChatGroup = info.Chat.Title,
                Language = "English",
                GroupId = info.Chat.Id,
                NodeId = ClientId,
                Users = new HashSet<int> {update.Message.From.Id},
                State = GameState.Joining
            });
        }

        public void EndGame(GameEndInfo gei)
        {
            CurrentGames--;
            CurrentPlayers -= gei.PlayerCount;
            Games.RemoveWhere(x => x.GroupId == gei.GroupId);
        }

        public void SendReply(Update update)
        {
            var info = JsonConvert.SerializeObject(new ReplyInfo {Update = update});
            this.Broadcast(info);
        }

        public void ShutDown()
        {
            ShuttingDown = true;
            this.Broadcast(JsonConvert.SerializeObject(new UpdateNodeInfo()));
        }

        public void SendReply(CallbackQuery query)
        {
            var info = JsonConvert.SerializeObject(new CallbackInfo {Query = query});
            this.Broadcast(info);
        }
    }
}
