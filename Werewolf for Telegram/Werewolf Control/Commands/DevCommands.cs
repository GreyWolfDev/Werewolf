﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Handler;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Attributes.Command(Trigger = "dumpgifs", DevOnly = true)]
        public static void DumpGifs(Update u, string[] args)
        {
            var gifLists = new[]
            {
                "VillagerDieImages", "WolfWin", "WolvesWin", "VillagersWin", "NoWinner", "StartGame", "StartChaosGame",
                "TannerWin", "CultWins", "SerialKillerWins", "LoversWin"
            };

            foreach (var name in gifLists)
            {
                var field = typeof(Settings).GetField(name);
                var list = field.GetValue(null) as List<string>;
                foreach (var g in list)
                {
                    try
                    {
                        var r = Bot.Api.SendDocument(u.Message.Chat.Id, g, name + " - " + g).Result;
                    }
                    catch (AggregateException e)
                    {
                        Send(g + " - " + e.InnerExceptions.FirstOrDefault()?.Message, u.Message.Chat.Id);
                    }
                    Thread.Sleep(1000);
                }
                Thread.Sleep(5000);
            }
        }

        [Attributes.Command(Trigger = "maintenance", DevOnly = true)]
        public static void Maintenenace(Update u, string[] args)
        {
            //stop accepting all new games.
            Program.MaintMode = !Program.MaintMode;
            Send($"Maintenance Mode: {Program.MaintMode}", u.Message.Chat.Id);
        }

        [Attributes.Command(Trigger = "winchart", DevOnly = true)]
        public static void WinChart(Update update, string[] args)
        {
            Charting.TeamWinChart(args[1], update);
        }

        [Attributes.Command(Trigger = "learngif", DevOnly = true)]
        public static void LearnGif(Update update, string[] args)
        {
            UpdateHandler.SendGifIds = !UpdateHandler.SendGifIds;
            Bot.Send($"GIF learning = {UpdateHandler.SendGifIds}", update.Message.Chat.Id);
        }

        [Attributes.Command(Trigger = "asplode", DevOnly = true)]
        public static void Asplode(Update u, string[] args)
        {
            //yep, just an alias, for giggles.  Hey, developers gotta have some fun, right?
            Update(u, args);
        }

        [Attributes.Command(Trigger = "update", DevOnly = true)]
        public static void Update(Update update, string[] args)
        {
            if (update.Message.Date > DateTime.UtcNow.AddSeconds(-3))
            {
                Process.Start(Path.Combine(Bot.RootDirectory, "Resources\\update.exe"), update.Message.Chat.Id.ToString());
                Bot.Running = false;
                Program.Running = false;
                Bot.Api.StopReceiving();
                Thread.Sleep(500);
                using (var db = new WWContext())
                {
                    var bot =
#if DEBUG
                        4;
#elif BETA
                        3;
#elif RELEASE
                        1;
#elif RELEASE2
                        2;
#endif
                    db.BotStatus.Find(bot).BotStatus = "Updating";
                    db.SaveChanges();
                }
                Environment.Exit(1);
            }
        }

        [Attributes.Command(Trigger = "broadcast", DevOnly = true)]
        public static void Broadcast(Update u, string[] args)
        {
            foreach (var n in Bot.Nodes.Select(x => x.Games.ToList()))
            {
                foreach (var g in n)
                {
                    Bot.Send(args[1], g.GroupId, parseMode: ParseMode.Markdown);
                }
            }
        }

        [Attributes.Command(Trigger = "killgame", GlobalAdminOnly = true, InGroupOnly = true)]
        public static void KillGame(Update u, string[] args)
        {
            var game = Bot.GetGroupNodeAndGame(u.Message.Chat.Id);
            game?.Kill();
        }

        [Attributes.Command(Trigger = "stopnode", GlobalAdminOnly = true)]
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

        [Attributes.Command(Trigger = "replacenodes", DevOnly = true)]
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

        [Attributes.Command(Trigger = "playtime", DevOnly = true)]
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

        [Attributes.Command(Trigger = "getroles", DevOnly = true)]
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

        [Attributes.Command(Trigger = "skipvote", DevOnly = true)]
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

        private static List<IRole> GetRoleList(int playerCount, bool allowCult = true, bool allowTanner = true, bool allowFool = true)
        {
            var rolesToAssign = new List<IRole>();
            //need to set the max wolves so game doesn't end immediately - 25% max wolf population
            //25% was too much, max it at 5 wolves.
            for (int i = 0; i < Math.Max(playerCount / 5, 1); i++)
                rolesToAssign.Add(IRole.Wolf);
            //add remaining roles to 'card pile'
            foreach (var role in Enum.GetValues(typeof(IRole)).Cast<IRole>())
            {
                switch (role)
                {
                    case IRole.Wolf:
                        break;
                    case IRole.CultistHunter:
                    case IRole.Cultist:
                        if (allowCult != false && playerCount > 10)
                            rolesToAssign.Add(role);
                        break;
                    case IRole.Tanner:
                        if (allowTanner != false)
                            rolesToAssign.Add(role);
                        break;
                    case IRole.Fool:
                        if (allowFool != false)
                            rolesToAssign.Add(role);
                        break;
                    default:
                        rolesToAssign.Add(role);
                        break;
                }
            }

            //add a couple more masons
            rolesToAssign.Add(IRole.Mason);
            rolesToAssign.Add(IRole.Mason);
            //for smaller games, all roles will be available and chosen randomly.  For large games, it will be about the
            //same as it was before....

            //if player count > role count, add another cultist into the mix
            if (rolesToAssign.Any(x => x== IRole.CultistHunter))
            {
                rolesToAssign.Add(IRole.Cultist);
                rolesToAssign.Add(IRole.Cultist);
            }
            //now fill rest of the slots with villagers (for large games)
            for(int i = 0; i < playerCount / 4; i++)
                rolesToAssign.Add(IRole.Villager);
            return rolesToAssign;
        }

        [Attributes.Command(Trigger = "test", DevOnly = true)]
        public static void Test(Update update, string[] args)
        {
            var balanced = false;
            var count = int.Parse(args[1]);
            List<IRole> rolesToAssign = new List<IRole>();
            int villageStrength = 0, enemyStrength = 0;
            var attempts = 0;
            var nonVgRoles = new[] { IRole.Cultist, IRole.SerialKiller, IRole.Tanner, IRole.Wolf };
            while (!balanced)
            {
                attempts++;
                if (attempts >= 20)
                    break;
                rolesToAssign = GetRoleList(count);
                rolesToAssign.Shuffle();
                rolesToAssign = rolesToAssign.Take(count).ToList();
                //check the balance
                
                villageStrength =
                    rolesToAssign.Where(x => !nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));
                var wolfStrength = rolesToAssign.Where(x => x == IRole.Wolf).Sum(x => x.GetStrength(rolesToAssign));
                var skStrength = rolesToAssign.Where(x => x == IRole.SerialKiller)
                    .Sum(x => x.GetStrength(rolesToAssign));
                var cultStrength = rolesToAssign.Where(x => x == IRole.Cultist).Sum(x => x.GetStrength(rolesToAssign));

                //check balance
                var varianceAllowed = (count / 5) + 3;
                enemyStrength = (wolfStrength + skStrength + cultStrength);
                balanced = (Math.Abs(villageStrength - enemyStrength) <= varianceAllowed);


            }

            var msg = $"Attempts: {attempts}\n";
            if (balanced)
            {
                msg = $"Total Village strength: {villageStrength}\nTotal Enemy strength: {enemyStrength}\n\n";
                msg +=
                    $"Village team:\n{rolesToAssign.Where(x => !nonVgRoles.Contains(x)).OrderBy(x => x).Select(x => x.ToString()).Aggregate((a, b) => a + "\n" + b)}\n\n";
                msg +=
                    $"Enemy teams:\n{rolesToAssign.Where(x => nonVgRoles.Contains(x)).OrderBy(x => x).Select(x => x.ToString()).Aggregate((a, b) => a + "\n" + b)}";
            }
            else
            {
                msg += "Unbalanced :(";
            }

            Send(msg, update.Message.Chat.Id);
        }

        [Attributes.Command(Trigger = "sql", DevOnly = true)]
        public static void Sql(Update u, string[] args)
        {
            if (args.Length == 1)
            {
                Send("You must enter a sql command...", u.Message.Chat.Id);
            }
            using (var db = new WWContext())
            {
                var sql = args[1];
                var conn = db.Database.Connection;
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                string raw = "";
                using (var comm = conn.CreateCommand())
                {
                    comm.CommandText = sql;
                    var reader = comm.ExecuteReader();
                    var result = "";
                    if (reader.HasRows)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                            raw += reader.GetName(i) + " - ";
                        result += raw + Environment.NewLine;
                        raw = "";
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                raw += reader[i] + " - ";
                            }
                            result += raw + Environment.NewLine;
                            raw = "";
                        }
                    }
                    Send(result + reader.RecordsAffected + " records affected", u.Message.Chat.Id);
                }
            }
        }

        [Attributes.Command(Trigger = "reloadenglish", DevOnly = true)]
        public static void ReloadEnglish(Update update, string[] args)
        {
            Bot.English = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));
        }

        [Attributes.Command(Trigger = "leavegroup", DevOnly = true)]
        public static void LeaveGroup(Update update, string[] args)
        {
            Send("Para said I can't play with you guys anymore, you are a bad influence! *runs out the door*", long.Parse(args[1]))
                .ContinueWith((result) =>
                {
                    Bot.Api.LeaveChat(args[1]);
                });
        }

        [Attributes.Command(Trigger = "clearcount", DevOnly = true)]
        public static void ClearCount(Update u, string[] args)
        {
            UpdateHandler.UserMessages.Clear();
        }

        [Attributes.Command(Trigger = "notifyspam", DevOnly = true)]
        public static void NotifySpam(Update u, string[] args)
        {
            Send("Please don't spam me like that", long.Parse(args[1]));
        }

        [Attributes.Command(Trigger = "notifyban", DevOnly = true)]
        public static void NotifyBan(Update u, string[] args)
        {
            Send("You have been banned.  You may appeal your ban in @werewolfbanappeal", long.Parse(args[1]));
        }

        [Attributes.Command(Trigger = "whois", DevOnly = true)]
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
        [Attributes.Command(Trigger = "getcommands", DevOnly = true)]
        public static void GetCommands(Update u, string[] args)
        {
            var target = int.Parse(args[1]);
            var reply = UpdateHandler.UserMessages[target].Messages.Aggregate("", (a, b) => a + "\n" + b.Command);
            Send(reply, u.Message.Chat.Id);
        }

        [Attributes.Command(Trigger = "updatestatus", GlobalAdminOnly = true)]
        public static void UpdateStatus(Update u, string[] args)
        {
            var menu = new InlineKeyboardMarkup(new[] { "Bot 1", "Bot 2", "Beta Bot", "Test Bot" }.Select(x => new InlineKeyboardButton(x, $"status|{u.Message.From.Id}|{x}|null")).ToArray());

            Bot.Api.SendTextMessage(u.Message.From.Id, "Which bot?",
                replyMarkup: menu);
            if (u.Message.Chat.Type != ChatType.Private)
                Send(GetLocaleString("SentPrivate", GetLanguage(u.Message.From.Id)), u.Message.Chat.Id);
        }

        [Attributes.Command(Trigger = "getbans", GlobalAdminOnly = true)]
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

                reply = UpdateHandler.BanList.Where(x => x.Expires < new DateTime(3000, 1, 1)).OrderBy(x => x.Expires).
                    Aggregate(reply, (current, ban) => 
                    current + $"{ban.TelegramId} - {ban.Name.FormatHTML()}: {ban.Reason}".ToBold() + 
                    $"\n{"Expires: " + TimeZoneInfo.ConvertTimeToUtc(ban.Expires, TimeZoneInfo.Local).ToString("u") + "\n"}");

                Send(reply, u.Message.Chat.Id);

                reply = "";
                reply = UpdateHandler.BanList.Where(x => x.Expires >= new DateTime(3000, 1, 1)).OrderBy(x => x.Expires).
                    Aggregate(reply, (current, ban) =>
                    current + $"{ban.TelegramId} - {ban.Name.FormatHTML()}: {ban.Reason}".ToBold() + "\n");
                Send(reply, u.Message.Chat.Id);
            }
        }

        [Attributes.Command(Trigger = "permban", DevOnly = true)]
        public static void PermBan(Update u, string[] args)
        {


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
            if (args.Length < 2 || String.IsNullOrEmpty(args[1]))
                return;
            //now check for ids
            var toBan = new List<int>();
            var did = 0;
            var banReason = "";
            foreach (var arg in args[1].Split(' '))
            {
                if (int.TryParse(arg, out did))
                {
                    toBan.Add(did);
                }
                else
                {
                    banReason += arg + " ";
                }
            }
            banReason = banReason.Trim();
            if (toBan.Count > 0)
            {
                foreach (var uid in toBan)
                {
                    using (var db = new WWContext())
                    {
                        var player = db.Players.FirstOrDefault(x => x.TelegramId == uid);
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
                            Send($"User {player.Name} (@{player.UserName}) has been banned", u.Message.Chat.Id);
                        }
                    }
                }
            }

        }

        [Attributes.Command(Trigger = "remban", GlobalAdminOnly = true)]
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

        [Attributes.Command(Trigger = "cleanmain", GlobalAdminOnly = true)]
        public static void CleanMain(Update u, string[] args)
        {
            using (var sw = new StreamWriter(Path.Combine(Bot.RootDirectory, "..\\kick.log")))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                //now, check the json file
                var json = new StreamReader("c:\\bot\\users.json").ReadToEnd();
                var users = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                sw.WriteLine($"Beginning json kick process.  Found {users.Count} users json file");
                Send($"Beginning json kick process.  Found {users.Count} users in json file", u.Message.Chat.Id);
                var i = 0;
                var removed = 0;
                using (var db = new WWContext())
                    foreach (var user in users)
                    {
                        i++;
                        sw.Flush();
                        sw.Write($"\n{i}: ");
                        try
                        {
                            var id = int.Parse(user.Key);
                            var time = DateTime.Parse(user.Value);
                            if (time >= DateTime.Now.AddDays(-14)) //two weeks
                                continue;

                            //check their status first, so we don't make db calls for someone not even in the chat.
                            var status = Bot.Api.GetChatMember(Settings.PrimaryChatId, id).Result.Status;
                            if (status != ChatMemberStatus.Member)
                                continue;

                            //get the last time they played a game
                            var p = db.Players.FirstOrDefault(x => x.TelegramId == id);

                            //get latest game player, check within 2 weeks
                            var gp = p?.GamePlayers.Join(db.Games.Where(x => x.GroupId == Settings.PrimaryChatId), x => x.GameId, y => y.Id, (gamePlayer, game) => new { game.TimeStarted, game.Id }).OrderByDescending(x => x.Id).FirstOrDefault();
                            if (gp != null)
                            {
                                if (gp.TimeStarted >= DateTime.Now.AddDays(-14))
                                    continue;
                            }

                            //at this point, they have been in the group at least 2 weeks, haven't played in the group in the past 2 weeks, and are a member.  Time to kick.
                            try
                            {
                                //first, check if the user is in the group
                                sw.Write($"{status}");
                                if (status != ChatMemberStatus.Member) //user is not in group, skip
                                    continue;
                                //kick
                                Bot.Api.KickChatMember(Settings.PrimaryChatId, id);
                                removed++;
                                sw.Write($" | Removed ({p?.Name ?? id.ToString()})");
                                //get their status
                                status = Bot.Api.GetChatMember(Settings.PrimaryChatId, id).Result.Status;
                                while (status == ChatMemberStatus.Member) //loop
                                {
                                    //wait for database to report status is kicked.
                                    status = Bot.Api.GetChatMember(Settings.PrimaryChatId, id).Result.Status;
                                    Thread.Sleep(500);
                                }
                                //status is now kicked (as it should be)
                                var attempts = 0;
                                sw.Write(" | Unbanning-");
                                while (status != ChatMemberStatus.Left) //unban until status is left
                                {
                                    attempts++;
                                    sw.Write($" {status} ");
                                    sw.Flush();
                                    Bot.Api.UnbanChatMember(Settings.PrimaryChatId, id);
                                    Thread.Sleep(500);
                                    status = Bot.Api.GetChatMember(Settings.PrimaryChatId, id).Result.Status;
                                }
                                //yay unbanned
                                sw.Write($" | Unbanned ({attempts} attempts)");
                                //let them know
                                Send(
                                    "You have been removed from the main chat as you have not played in that group in the 2 weeks.  You are always welcome to rejoin!",
                                    id);
                            }
                            catch (AggregateException ex)
                            {
                                var e = ex.InnerExceptions.First();
                                if (e.Message.Contains("User not found"))
                                    sw.Write($"Not Found - {p.Name}");
                                else if (e.Message.Contains("USER_ID_INVALID"))
                                    sw.Write($"Account Closed - {p.Name}");
                                else
                                {
                                    sw.Write(e.Message);
                                    //sw.WriteLine(e.StackTrace);
                                }
                            }
                            catch (Exception e)
                            {
                                sw.WriteLine(e.Message);
                                sw.WriteLine(e.StackTrace);
                                // ignored
                            }
                            sw.Flush();
                            //sleep 4 seconds to avoid API rate limiting
                            Thread.Sleep(100);

                        }
                        catch
                        {
                            // ignored
                        }

                    }

                //fun times ahead!
                //get our list of inactive users
                Console.ForegroundColor = ConsoleColor.Cyan;
                List<v_InactivePlayersMain> inactive;
                using (var db = new WWContext())
                {
                    inactive = db.v_InactivePlayersMain.OrderByDescending(x => x.last).ToList();
                }
                Send($"Checking {inactive.Count} users from database. {removed} from json file", u.Message.Chat.Id);
                var timeStarted = DateTime.Now;

                sw.WriteLine($"Beginning kick process.  Found {inactive.Count} users in database");
                i = 0;
                foreach (var p in inactive)
                {
                    i++;
                    sw.Write($"\n{i}: ");
                    try
                    {
                        //first, check if the user is in the group
                        var status = Bot.Api.GetChatMember(Settings.PrimaryChatId, p.TelegramId).Result.Status;
                        sw.Write($"{status}");
                        if (status != ChatMemberStatus.Member) //user is not in group, skip
                            continue;
                        //kick
                        Bot.Api.KickChatMember(Settings.PrimaryChatId, p.TelegramId);
                        removed++;
                        sw.Write($" | Removed ({p.Name})");
                        //get their status
                        status = Bot.Api.GetChatMember(Settings.PrimaryChatId, p.TelegramId).Result.Status;
                        while (status == ChatMemberStatus.Member) //loop
                        {
                            //wait for database to report status is kicked.
                            status = Bot.Api.GetChatMember(Settings.PrimaryChatId, p.TelegramId).Result.Status;
                            Thread.Sleep(500);
                        }
                        //status is now kicked (as it should be)
                        var attempts = 0;
                        sw.Write(" | Unbanning-");
                        while (status != ChatMemberStatus.Left) //unban until status is left
                        {
                            attempts++;
                            sw.Write($" {status} ");
                            sw.Flush();
                            Bot.Api.UnbanChatMember(Settings.PrimaryChatId, p.TelegramId);
                            Thread.Sleep(500);
                            status = Bot.Api.GetChatMember(Settings.PrimaryChatId, p.TelegramId).Result.Status;
                        }
                        //yay unbanned
                        sw.Write($" | Unbanned ({attempts} attempts)");
                        //let them know
                        Send(
                            "You have been removed from the main chat as you have not played in that group in the 2 weeks.  You are always welcome to rejoin!",
                            p.TelegramId);
                    }
                    catch (AggregateException ex)
                    {
                        var e = ex.InnerExceptions.First();
                        if (e.Message.Contains("User not found"))
                            sw.Write($"Not Found - {p.Name}");
                        else if (e.Message.Contains("USER_ID_INVALID"))
                            sw.Write($"Account Closed - {p.Name}");
                        else
                        {
                            sw.Write(e.Message);
                            //sw.WriteLine(e.StackTrace);
                        }
                    }
                    catch (Exception e)
                    {
                        sw.WriteLine(e.Message);
                        sw.WriteLine(e.StackTrace);
                        // ignored
                    }
                    sw.Flush();
                    //sleep 4 seconds to avoid API rate limiting
                    Thread.Sleep(100);
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                Send(
                    $"@{u.Message.From.Username} I have removed {removed} users from the main group.\nTime to process: {DateTime.Now - timeStarted}",
                    u.Message.Chat.Id);




            }
        }

        [Attributes.Command(Trigger = "remgrp", GlobalAdminOnly = true)]
        public static void RemGrp(Update u, string[] args)
        {
            var link = args[1];
            if (String.IsNullOrEmpty(link))
            {
                Send("Use /remgrp <link>", u.Message.Chat.Id);
                return;
            }
            //grouplink should be the argument
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupLink == link);
                if (grp != null)
                {

                    try
                    {
                        var result = Bot.Api.LeaveChat(grp.GroupId).Result;
                        Send($"Bot removed from group: " + result, u.Message.Chat.Id);
                    }
                    catch
                    {

                    }
                    grp.GroupLink = null;
                    db.SaveChanges();
                    Send($"Group {grp.Name} removed from /grouplist", u.Message.Chat.Id);
                }
            }
        }
    }


}
