using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Werewolf_Control.Attributes;
using Werewolf_Control.Handler;
using Werewolf_Control.Helpers;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Command(Trigger = "dumpgifs", DevOnly = true)]
        public static void DumpGifs(Update u, string[] args)
        {

            foreach (var g in Settings.VillagerDieImages)
            {
                try
                {
                    var r = Bot.Api.SendDocument(u.Message.Chat.Id, g, "VillagerDieImages - " + g).Result;
                }
                catch (AggregateException e)
                {
                    Send(g + " - " + e.InnerExceptions.FirstOrDefault().Message, u.Message.Chat.Id);
                }
                Thread.Sleep(1000);
            }
            Thread.Sleep(5000);
            //foreach (var g in Settings.WolfWin)
            //{
            //    Bot.Api.SendDocument(u.Message.Chat.Id, g, "WolfWin - " + g);
            //    Thread.Sleep(1000);
            //}
            //Thread.Sleep(5000);
            //foreach (var g in Settings.WolvesWin)
            //{
            //    Bot.Api.SendDocument(u.Message.Chat.Id, g, "WolvesWin - " + g);
            //    Thread.Sleep(1000);
            //}
            //Thread.Sleep(5000);
            //foreach (var g in Settings.VillagersWin)
            //{
            //    Bot.Api.SendDocument(u.Message.Chat.Id, g, "VillagersWin - " + g);
            //    Thread.Sleep(1000);
            //}
            //Thread.Sleep(5000);
            //foreach (var g in Settings.NoWinner)
            //{
            //    Bot.Api.SendDocument(u.Message.Chat.Id, g, "NoWinner - " + g);
            //    Thread.Sleep(1000);
            //}
            //Thread.Sleep(5000);
            //foreach (var g in Settings.StartGame)
            //{
            //    Bot.Api.SendDocument(u.Message.Chat.Id, g, "StartGame - " + g);
            //    Thread.Sleep(1000);
            //}
            //Thread.Sleep(5000);
            foreach (var g in Settings.StartChaosGame)
            {
                try
                {
                    var r = Bot.Api.SendDocument(u.Message.Chat.Id, g, "StartChaosGame - " + g).Result;
                }
                catch (AggregateException e)
                {
                    Send(g + " - " + e.InnerExceptions.FirstOrDefault().Message, u.Message.Chat.Id);
                }
                Thread.Sleep(1000);
            }
            //Thread.Sleep(5000);
            //foreach (var g in Settings.TannerWin)
            //{
            //    Bot.Api.SendDocument(u.Message.Chat.Id, g, "TannerWin - " + g);
            //    Thread.Sleep(1000);
            //}
            //Thread.Sleep(5000);
            //foreach (var g in Settings.CultWins)
            //{
            //    Bot.Api.SendDocument(u.Message.Chat.Id, g, "CultWins - " + g);
            //    Thread.Sleep(1000);
            //}
            //Thread.Sleep(5000);
            //foreach (var g in Settings.SerialKillerWins)
            //{
            //    Bot.Api.SendDocument(u.Message.Chat.Id, g, "SerialKillerWins - " + g);
            //    Thread.Sleep(1000);
            //}
            //Thread.Sleep(5000);
            //foreach (var g in Settings.LoversWin)
            //{
            //    Bot.Api.SendDocument(u.Message.Chat.Id, g, "LoversWin - " + g);
            //    Thread.Sleep(1000);
            //}

        }

        [Command(Trigger = "winchart", DevOnly = true)]
        public static void WinChart(Update update, string[] args)
        {
            Charting.TeamWinChart(args[1], update);
        }

        [Command(Trigger = "learngif", DevOnly = true)]
        public static void LearnGif(Update update, string[] args)
        {
            UpdateHandler.SendGifIds = !UpdateHandler.SendGifIds;
            Bot.Send($"GIF learning = {UpdateHandler.SendGifIds}", update.Message.Chat.Id);
        }

        [Command(Trigger = "update", DevOnly = true)]
        public static void Update(Update update, string[] args)
        {
            if (update.Message.Date > DateTime.UtcNow.AddSeconds(-3))
            {
                Process.Start(Path.Combine(Bot.RootDirectory, "Resources\\update.exe"));
                Bot.Running = false;
                Program.Running = false;
                Thread.Sleep(500);
                Environment.Exit(1);
            }
        }

        [Command(Trigger = "killgame", GlobalAdminOnly = true, InGroupOnly = true)]
        public static void KillGame(Update u, string[] args)
        {
            var game = Bot.GetGroupNodeAndGame(u.Message.Chat.Id);
            game?.Kill();
        }

        [Command(Trigger = "stopnode", GlobalAdminOnly = true)]
        public static void StopNode(Update u, string[] args)
        {
            //get the node
            try
            {
                var nodeid = args[1];
                var node = Bot.Nodes.FirstOrDefault(x => x.ClientId == Guid.Parse(nodeid));
                node?.ShutDown();
                if (node != null)
                    Send($"Node {node.ClientId} will stop accepting games", u.Message.Chat.Id);
                else
                    Send("No node with that ID found.", u.Message.Chat.Id);
            }
            catch
            {
                Send("/stopnode <node guid>", u.Message.Chat.Id);
            }

        }

        //[Command(Trigger = "sendonline", DevOnly = true)]
        //public static void SendOnline(Update update, string[] args)
        //{
        //    new Task(Bot.SendOnline).Start();
        //}

        [Command(Trigger = "replacenodes", DevOnly = true)]
        public static void ReplaceNodes(Update update, string[] args)
        {
            foreach (var n in Bot.Nodes)
                n.ShutDown();
            //get version
            var baseDirectory = Path.Combine(Bot.RootDirectory, ".."); //go up one directory
            var currentChoice = new NodeChoice();
            foreach (var dir in Directory.GetDirectories(baseDirectory, "*Node*"))
            {
                //get the node exe in this directory
                var file = Directory.GetFiles(dir, "Werewolf Node.exe").First();
                Version fvi = System.Version.Parse(FileVersionInfo.GetVersionInfo(file).FileVersion);
                if (fvi > currentChoice.Version)
                {
                    currentChoice.Path = file;
                    currentChoice.Version = fvi;
                }
            }

            Bot.Send($"Replacing nodes with latest version: {currentChoice.Version}", update.Message.Chat.Id);
        }

        [Command(Trigger = "playtime", DevOnly = true)]
        public static void PlayTime(Update update, string[] args)
        {
            if (args.Length > 1)
            {
                var playerCount = int.Parse(args[1]);
                using (var db = new WWContext())
                {
                    var counts = db.getPlayTime(playerCount).First();
                    var msg =
                        $"(In minutes)\nMin: {counts.Minimum}\nMax: {counts.Maximum}\nAverage: {counts.Average}";
                    Bot.Send(msg, update.Message.Chat.Id);
                }
            }
        }

        [Command(Trigger = "getroles", DevOnly = true)]
        public static void GetRoles(Update update, string[] args)
        {
            if (args.Length > 1)
            {
                var groupName = args.Skip(1).Aggregate((a, b) => a + " " + b).Trim();
                using (var db = new WWContext())
                {
                    var roles = db.getRoles(groupName);
                    var msg = roles.Aggregate("", (current, r) => current + $"{r.name}: {r.role}\n");
                    Bot.Send(msg, update.Message.Chat.Id);
                }
            }
        }

        [Command(Trigger = "skipvote", DevOnly = true)]
        public static void SkipVote(Update update, string[] args)
        {
            var node = GetPlayerNode(update.Message.From.Id);
            var game = GetGroupNodeAndGame(update.Message.Chat.Id);
            if (game != null || node != null)
            {
                //try grabbing the game again...
                if (node != null && game == null)
                    game =
                        node.Games.FirstOrDefault(
                            x => x.Users.Contains(update.Message.From.Id));
                //try again.....
                if (game == null)
                    game = GetGroupNodeAndGame(update.Message.Chat.Id);
                //player is not in game, they need to join, if they can
                game?.SkipVote();
            }
        }

        [Command(Trigger = "test", DevOnly = true)]
        public static void Test(Update update, string[] args)
        {
            //get the user
            using (var db = new WWContext())
            {
                var p = db.Players.FirstOrDefault(x => x.TelegramId == 114006743);
                Send(p.Name, update.Message.Chat.Id);
            }
        }

        [Command(Trigger = "reloadenglish", DevOnly = true)]
        public static void ReloadEnglish(Update update, string[] args)
        {
            Bot.English = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));
        }

        [Command(Trigger = "leavegroup", DevOnly = true)]
        public static void LeaveGroup(Update update, string[] args)
        {
            Send("Para said I can't play with you guys anymore, you are a bad influence! *runs out the door*", long.Parse(args[1]))
                .ContinueWith((result) =>
                {
                    Bot.Api.LeaveChat(args[1]);
                });
        }

        [Command(Trigger = "clearcount", DevOnly = true)]
        public static void ClearCount(Update u, string[] args)
        {
            UpdateHandler.UserMessages.Clear();
        }

        [Command(Trigger = "notifyspam", DevOnly = true)]
        public static void NotifySpam(Update u, string[] args)
        {
            Send("Please don't spam me like that", long.Parse(args[1]));
        }

        [Command(Trigger = "notifyban", DevOnly = true)]
        public static void NotifyBan(Update u, string[] args)
        {
            Send("You have been banned.  You may appeal your ban in @werewolfsupport", long.Parse(args[1]));
        }

        [Command(Trigger = "whois", DevOnly = true)]
        public static void WhoIs(Update u, string[] args)
        {
            using (var db = new WWContext())
            {
                var search = int.Parse(args[1].Trim());
                var p = db.Players.FirstOrDefault(x => x.TelegramId == search);
                if (p != null)
                    Send($"User: {p.Name}\nUserName: @{p.UserName}", u.Message.Chat.Id);
            }
        }
        [Command(Trigger = "getcommands", DevOnly = true)]
        public static void GetCommands(Update u, string[] args)
        {
            var target = int.Parse(args[1]);
            var reply = UpdateHandler.UserMessages[target].Messages.Aggregate("", (a, b) => a + "\n" + b.Command);
            Send(reply, u.Message.Chat.Id);
        }

        [Command(Trigger = "getbans", GlobalAdminOnly = true)]
        public static void GetBans(Update u, string[] args)
        {
            using (var db = new WWContext())
            {
                var reply = "Spam Ban List:\n";
                foreach (var id in UpdateHandler.SpamBanList)
                {
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                    if (p != null) //it really shouldn't be...
                    {
                        reply += $"{id} - {p.Name} @{p.UserName}\n";
                    }
                }
                reply += "\nGlobal Bans in Database\n";

                reply = UpdateHandler.BanList.OrderBy(x => x.Expires).Aggregate(reply, (current, ban) => current + $"{ban.TelegramId} - {ban.Name.FormatHTML()}: {ban.Reason}".ToBold() + $"\n{(ban.Expires < new DateTime(3000, 1, 1) ? "Expires: " + TimeZoneInfo.ConvertTimeToUtc(ban.Expires, TimeZoneInfo.Local).ToString("u") + "\n" : "")}");

                Send(reply, u.Message.Chat.Id);

            }
        }

        [Command(Trigger = "permban", DevOnly = true)]
        public static void PermBan(Update u, string[] args)
        {
            var tosmite = new List<int>();

            foreach (var e in u.Message.Entities)
            {
                switch (e.Type)
                {
                    case MessageEntityType.Mention:
                        //get user
                        var username = u.Message.Text.Substring(e.Offset + 1, e.Length - 1);
                        using (var db = new WWContext())
                        {
                            var player = db.Players.FirstOrDefault(x => x.UserName == username);
                            if (player != null)
                            {
                                var game = Bot.GetGroupNodeAndGame(u.Message.Chat.Id);
                                game?.SmitePlayer(player.TelegramId);
                                //add the ban
                                var ban = new GlobalBan
                                {
                                    Expires = (DateTime)SqlDateTime.MaxValue,
                                    Reason = args[1].Split(' ').Skip(1).Aggregate((a, b) => a + " " + b), //skip the players name
                                    TelegramId = player.TelegramId,
                                    BanDate = DateTime.Now,
                                    BannedBy = u.Message.From.FirstName,
                                    Name = player.Name
                                };
                                db.GlobalBans.Add(ban);
                                UpdateHandler.BanList.Add(ban);
                                db.SaveChanges();
                                Send("User has been banned", u.Message.Chat.Id);
                            }
                        }
                        break;
                    case MessageEntityType.TextMention:
                        using (var db = new WWContext())
                        {
                            var player = db.Players.FirstOrDefault(x => x.TelegramId == e.User.Id);
                            if (player != null)
                            {
                                var game = Bot.GetGroupNodeAndGame(u.Message.Chat.Id);
                                game?.SmitePlayer(player.TelegramId);
                                //add the ban
                                var ban = new GlobalBan
                                {
                                    Expires = (DateTime)SqlDateTime.MaxValue,
                                    Reason = args[1].Split(' ').Skip(1).Aggregate((a, b) => a + " " + b), //skip the players name
                                    TelegramId = player.TelegramId,
                                    BanDate = DateTime.Now,
                                    BannedBy = u.Message.From.FirstName,
                                    Name = player.Name
                                };
                                db.GlobalBans.Add(ban);
                                UpdateHandler.BanList.Add(ban);
                                db.SaveChanges();
                                Send("User has been banned", u.Message.Chat.Id);
                            }
                        }
                        break;
                }
            }
        }

        [Command(Trigger = "remban", GlobalAdminOnly = true)]
        public static void RemoveBan(Update u, string[] args)
        {
            var tosmite = new List<int>();

            foreach (var e in u.Message.Entities)
            {
                switch (e.Type)
                {
                    case MessageEntityType.Mention:
                        //get user
                        var username = u.Message.Text.Substring(e.Offset + 1, e.Length - 1);
                        using (var db = new WWContext())
                        {
                            var player = db.Players.FirstOrDefault(x => x.UserName == username);
                            if (player != null)
                            {
                                var ban = db.GlobalBans.FirstOrDefault(x => x.TelegramId == player.TelegramId);
                                if (ban != null)
                                {
                                    var localban = UpdateHandler.BanList.FirstOrDefault(x => x.Id == ban.Id);
                                    if (localban != null)
                                        UpdateHandler.BanList.Remove(localban);
                                    db.GlobalBans.Remove(ban);
                                    db.SaveChanges();
                                    Send("User has been unbanned.", u.Message.Chat.Id);
                                }
                            }
                        }
                        break;
                    case MessageEntityType.TextMention:
                        using (var db = new WWContext())
                        {
                            var player = db.Players.FirstOrDefault(x => x.TelegramId == e.User.Id);
                            if (player != null)
                            {
                                var ban = db.GlobalBans.FirstOrDefault(x => x.TelegramId == player.TelegramId);
                                if (ban != null)
                                {
                                    var localban = UpdateHandler.BanList.FirstOrDefault(x => x.Id == ban.Id);
                                    if (localban != null)
                                        UpdateHandler.BanList.Remove(localban);
                                    db.GlobalBans.Remove(ban);
                                    db.SaveChanges();
                                    Send("User has been unbanned.", u.Message.Chat.Id);
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
