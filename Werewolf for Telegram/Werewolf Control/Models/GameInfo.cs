﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Werewolf_Control.Helpers;

namespace Werewolf_Control.Models
{
    public class GameInfo
    {
        public HashSet<int> Users { get; set; } = new HashSet<int>();
        public long GroupId { get; set; }
        public Guid Guid { get; set; }
        public string Language { get; set; }
        public string ChatGroup { get; set; }
        public GameState State { get; set; }
        public HashSet<IPlayer> Players { get; set; } = new HashSet<IPlayer>();
        public int PlayerCount { get; set; }
        public Guid NodeId { get; set; }

        public void AddPlayer(Update update)
        {
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            if (n == null) return;
            //var g = n.Games.FirstOrDefault(x => x.GroupId == update.Message.Chat.Id);
            //g?.
            Users.Add(update.Message.From.Id);
            var json = JsonConvert.SerializeObject(new PlayerJoinInfo { User = update.Message.From, GroupId = GroupId });
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
            var n = Bot.Nodes.FirstOrDefault(x => x.ClientId == NodeId);
            if (n == null) return;

            Users.Remove(update.Message.From.Id);
            var json = JsonConvert.SerializeObject(new PlayerFleeInfo { User = update.Message.From, GroupId = update.Message.Chat.Id });
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
    }

    public enum GameState
    {
        Joining, Running, Dead
    }
}
