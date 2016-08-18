using System;
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
                Process.Start(Path.Combine(Bot.RootDirectory, "Resources\\update.exe"), update.Message.Chat.Id.ToString());
                Bot.Running = false;
                Program.Running = false;
                Thread.Sleep(500);
                Environment.Exit(1);
            }
        }

        [Command(Trigger = "broadcast", DevOnly = true)]
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
            //test writing to database
            //using (var db = new WWContext())
            //{
            //    var p = db.Players.FirstOrDefault(x => x.TelegramId == UpdateHandler.Para);
            //    if (p != null)
            //    {
            //        var flags = (Achievements) p.Achievements;
            //        flags = flags | Achievements.GunnerSaves;
                   
            //        p.Achievements = (long)flags;
            //    }
            //    db.SaveChanges();
            //    Send($"{p.Achievements}", update.Message.Chat.Id);
            //}
        }

        [Command(Trigger = "sql", DevOnly = true)]
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
            Send("You have been banned.  You may appeal your ban in @werewolfbanappeal", long.Parse(args[1]));
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

        [Command(Trigger = "cleanmain", GlobalAdminOnly = true)]
        public static void CleanMain(Update u, string[] args)
        {
            using (var sw = new StreamWriter(Path.Combine(Bot.RootDirectory, "..\\kick.log")))
            {
                
                //fun times ahead!
                //get our list of inactive users
                Console.ForegroundColor = ConsoleColor.Cyan;
                List<v_InactivePlayersMain> inactive;
                using (var db = new WWContext())
                {
                    inactive = db.v_InactivePlayersMain.OrderByDescending(x => x.last).ToList();
                }
                Send($"Checking {inactive.Count} users", u.Message.Chat.Id);
                var timeStarted = DateTime.Now;
                var removed = 0;
                sw.WriteLine($"Beginning kick process.  Found {inactive.Count} users in database");
                var i = 0;
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

        [Command(Trigger = "remgrp", GlobalAdminOnly = true)]
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
