using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Werewolf_Control.Helpers;

namespace Werewolf_Control.Models
{
    public class GameInfo
    {
        public HashSet<int> Users { get; set; } = new HashSet<int>();
        public long GroupId { get; set; }
        public int Guid { get; set; }
        public string Language { get; set; }
        public string ChatGroup { get; set; }
        public GameState State { get; set; }
        public HashSet<IPlayer> Players { get; set; } = new HashSet<IPlayer>();
        public int PlayerCount { get; set; }
        public int NodeId { get; set; }

        public void AddPlayer(Update update)
        {
            AddPlayer(update.Message.From);
        }

        public void AddPlayer(User user)
        {
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            if (n == null) return;
            Users.Add(user.Id);
            var json = JsonConvert.SerializeObject(new PlayerJoinInfo { User = user, GroupId = GroupId });
            n.Broadcast(json);
        }

        public void ForceStart()
        {
            var json = JsonConvert.SerializeObject(new ForceStartInfo { GroupId = GroupId });
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            n?.Broadcast(json);
        }

        public void ShowPlayers()
        {
            var json = JsonConvert.SerializeObject(new PlayerListRequestInfo { GroupId = GroupId });
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            n?.Broadcast(json);
        }

        public void RemovePlayer(Update update)
        {
            RemovePlayer(update.Message.From);
        }

        public void RemovePlayer(User user)
        {
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            if (n == null) return;

            Users.Remove(user.Id);
            var json = JsonConvert.SerializeObject(new PlayerFleeInfo { User = user, GroupId = GroupId });
            n.Broadcast(json);
        }

        public void LoadLanguage(string fileName)
        {
            var json = JsonConvert.SerializeObject(new LoadLangInfo { GroupId = GroupId, FileName = fileName });
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            n?.Broadcast(json);
        }

        public void SmitePlayer(int id)
        {
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            if (n == null) return;
            Users.Remove(id);
            var json = JsonConvert.SerializeObject(new PlayerSmiteInfo { GroupId = GroupId, UserId = id });
            n.Broadcast(json);
        }

        public void SkipVote()
        {
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            var json = JsonConvert.SerializeObject(new SkipVoteInfo { GroupId = GroupId });
            n?.Broadcast(json);
        }

        public void Kill()
        {
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            var json = JsonConvert.SerializeObject(new GameKillInfo() { GroupId = GroupId });
            n?.Broadcast(json);
        }

        public void ExtendTime(long id, bool admin, int seconds)
        {
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            if (n == null) return;
            var json = JsonConvert.SerializeObject(new ExtendTimeInfo() { GroupId = GroupId , Admin = admin, User = id, Seconds = seconds});
            n?.Broadcast(json);
        }

        public void ShowJoinButton()
        {
            var json = JsonConvert.SerializeObject(new JoinButtonRequestInfo { GroupId = GroupId });
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            n?.Broadcast(json);
        }

        public void UpdateOriginalPinnedMsg(int messageId)
        {
            var json = JsonConvert.SerializeObject(new OriginalPinnedMsgUpdateInfo { GroupId = GroupId, MessageId = messageId });
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            n?.Broadcast(json);
        }
    }

    public enum GameState
    {
        Joining, Running, Dead
    }
}
