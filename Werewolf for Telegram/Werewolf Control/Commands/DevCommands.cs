using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml.Linq;
using Database;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Handler;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;
using Werewolf_Control.Attributes;
using File = System.IO.File;
using Group = Database.Group;
using RegHelper = Werewolf_Control.Helpers.RegHelper;

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
                        var r = Bot.Api.SendDocumentAsync(u.Message.Chat.Id, g, name + " - " + g).Result;
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

        [Attributes.Command(Trigger = "bangroup", DevOnly = true)]
        public static void BanGroup(Update u, string[] args)
        {
            long groupid = 0;
            if (long.TryParse(args[1], out groupid))
            {
                using (var db = new WWContext())
                {
                    var g = db.Groups.FirstOrDefault(x => x.GroupId == groupid);
                    if (g != null)
                    {
                        g.CreatedBy = "BAN";
                        db.SaveChanges();
                        Bot.Api.LeaveChatAsync(groupid);
                        Send($"{g.Name} has been banned.", u.Message.Chat.Id);
                    }
                }
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
                //Thread.Sleep(500);
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
#if !BETA
            foreach (var n in Bot.Nodes.Select(x => x.Games.ToList()))
            {
                foreach (var g in n)
                {
                    Bot.Send(args[1], g.GroupId, parseMode: ParseMode.Markdown);
                }
            }
#else
            foreach (var g in BetaGroups)
            {
                try
                {
                    var success = Bot.Send(args[1], g, parseMode: ParseMode.Markdown).Result;
                }
                catch (AggregateException e)
                {
                    Bot.Send("Couldn't send to " + g + ".\n"+ e.InnerExceptions[0].Message, UpdateHelper.Devs[1]);
                }
            }
#endif
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
                var node = Bot.Nodes.FirstOrDefault(x => x.ClientId == nodeid);
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

        [Attributes.Command(Trigger = "killnode", GlobalAdminOnly = true)]
        public static void KillNode(Update u, string[] args)
        {
            //get the node
            try
            {
                var nodeid = args[1];
                var node = Bot.Nodes.FirstOrDefault(x => x.ClientId == nodeid);
                node?.ShutDown(true);
                if (node != null)
                    Send($"Node {node.ClientId} will shut down", u.Message.Chat.Id);
                else
                    Send("No node with that ID found.", u.Message.Chat.Id);
            }
            catch
            {
                Send("/killnode <node guid>", u.Message.Chat.Id);
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

        //private static List<IRole> GetRoleList(int playerCount, bool allowCult = true, bool allowTanner = true, bool allowFool = true)
        //{
        //    var rolesToAssign = new List<IRole>();
        //    //need to set the max wolves so game doesn't end immediately - 25% max wolf population
        //    //25% was too much, max it at 5 wolves.
        //    for (int i = 0; i < Math.Max(playerCount / 5, 1); i++)
        //        rolesToAssign.Add(IRole.Wolf);
        //    //add remaining roles to 'card pile'
        //    foreach (var role in Enum.GetValues(typeof(IRole)).Cast<IRole>())
        //    {
        //        switch (role)
        //        {
        //            case IRole.Wolf:
        //            case IRole.Faithful: //never start a game with faithfuls.
        //                break;
        //            case IRole.CultistHunter:
        //            case IRole.Preacher:
        //            case IRole.Cultist:
        //                if (allowCult != false && playerCount > 10)
        //                    rolesToAssign.Add(role);
        //                break;
        //            case IRole.Tanner:
        //                if (allowTanner != false)
        //                    rolesToAssign.Add(role);
        //                break;
        //            case IRole.Fool:
        //                if (allowFool != false)
        //                    rolesToAssign.Add(role);
        //                break;
        //            case IRole.WolfCub:
        //            case IRole.AlphaWolf: //don't add more wolves, just replace
        //                rolesToAssign.Add(role);
        //                rolesToAssign.Remove(IRole.Wolf);
        //                break;
        //            default:
        //                rolesToAssign.Add(role);
        //                break;
        //        }
        //    }

        //    //add a couple more masons
        //    rolesToAssign.Add(IRole.Mason);
        //    rolesToAssign.Add(IRole.Mason);
        //    //for smaller games, all roles will be available and chosen randomly.  For large games, it will be about the
        //    //same as it was before....


        //    if (rolesToAssign.Any(x => x == IRole.CultistHunter || x == IRole.Preacher))
        //    {
        //        rolesToAssign.Add(IRole.Cultist);
        //        rolesToAssign.Add(IRole.Cultist);
        //    }
        //    //now fill rest of the slots with villagers (for large games)
        //    for (int i = 0; i < playerCount / 4; i++)
        //        rolesToAssign.Add(IRole.Villager);
        //    return rolesToAssign;
        //}

        public class BalancedGameAttempt
        {
            public bool Balanced { get; set; }
            public int AttemptsMade { get; set; }
        }

        private static Dictionary<int, List<BalancedGameAttempt>> BalancedAttempts;

        //[Attributes.Command(Trigger = "createbalance", DevOnly = true)]
        //public static void CreateBalancedGames(Update u, string[] args)
        //{
        //    //get parameters
        //    int count = 0;

        //    if (!int.TryParse(args[1], out count))
        //    {
        //        Send("use !createbalance <playercount>", u.Message.Chat.Id);
        //        return;

        //    }



        //    var balanced = false;

        //    List<IRole> rolesToAssign = new List<IRole>();
        //    int villageStrength = 0, enemyStrength = 0;
        //    var attempts = 0;
        //    var nonVgRoles = new[] { IRole.Cultist, IRole.SerialKiller, IRole.Tanner, IRole.Wolf, IRole.AlphaWolf, IRole.Sorcerer, IRole.WolfCub };
        //    while (!balanced)
        //    {
        //        attempts++;
        //        if (attempts >= 200)
        //            break;
        //        rolesToAssign = GetRoleList(count);
        //        rolesToAssign.Shuffle();
        //        rolesToAssign = rolesToAssign.Take(count).ToList();
        //        if (rolesToAssign.Contains(IRole.Sorcerer) &
        //            !rolesToAssign.Any(x => new[] { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub }.Contains(x)))
        //            //can't have a sorcerer without wolves.  That's silly
        //            continue;

        //        //check the balance

        //        villageStrength =
        //        rolesToAssign.Where(x => !nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));
        //        enemyStrength =
        //            rolesToAssign.Where(x => nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));

        //        //check balance
        //        var varianceAllowed = (count / 5) + 3;

        //        balanced = (Math.Abs(villageStrength - enemyStrength) <= varianceAllowed);


        //    }



        //    var msg = $"Attempts: {attempts}\n";
        //    if (balanced)
        //    {
        //        msg += $"Total Village strength: {villageStrength}\nTotal Enemy strength: {enemyStrength}\n\n";
        //        msg +=
        //            $"Village team:\n{rolesToAssign.Where(x => !nonVgRoles.Contains(x)).OrderBy(x => x).Select(x => x.ToString()).Aggregate((a, b) => a + "\n" + b)}\n\n";
        //        msg +=
        //            $"Enemy teams:\n{rolesToAssign.Where(x => nonVgRoles.Contains(x)).OrderBy(x => x).Select(x => x.ToString()).Aggregate((a, b) => a + "\n" + b)}";
        //    }
        //    else
        //    {
        //        msg += "Unbalanced :(";
        //    }
        //    Send(msg, u.Message.Chat.Id);
        //}

        [Attributes.Command(Trigger = "test", DevOnly = true)]
        public static void Test(Update update, string[] args)
        {
        }

        [Attributes.Command(Trigger = "checkgroups", DevOnly = true)]
        public static void CheckGroupList(Update update, string[] args)
        {
            using (var db = new WWContext())
            {
                Bot.Send("Please hold, searching....", update.Message.Chat.Id);

                var groups = db.Groups.Where(x => x.Preferred != false && x.GroupLink != null && x.GroupId != Settings.MainChatId && x.GroupId != Settings.VeteranChatId)
                    .Select(x => new PossibleGroup() { GroupId = x.GroupId, GroupLink = x.GroupLink, MemberCount = x.MemberCount ?? 0, Name = x.Name }).ToList();

                var ofcSpells = new[] { "official", "offciail", "official", "oficial", "offical" };
                var wuffSpells = new[] { "wolf", "wuff", "wulf", "lupus" };

                for (var i = groups.Count - 1; i >= 0; i--)
                {
                    var g = groups[i];
                    //check for official
                    if (ofcSpells.Any(x => g.Name.Contains(x)))
                    {
                        if (wuffSpells.Any(x => g.Name.Contains(x)))
                            continue;
                    }
                    groups.RemoveAt(i);

                }

                //groups = groups.Where(x => x.Name.Unidecode().IndexOf("Werewolf",StringComparison.InvariantCultureIgnoreCase) != -1).ToList();
                if (groups.Any())
                    Bot.Send(
                        groups.Aggregate("Groups detected having variations of Werewolf and Official in name:\n",
                            (a, b) => a + $"\n{b.OriginalName} - {b.GroupId} - {b.GroupLink}"), update.Message.Chat.Id);
                else
                    Bot.Send("No groups found with variations on Werewolf and Official in name.",
                        update.Message.Chat.Id);


            }
        }

        internal class PossibleGroup
        {
            private string _name;

            public string Name
            {
                get => _name;
                set
                {
                    OriginalName = value;
                    _name = value.Unidecode().ToLower();
                }
            }
            public string GroupLink { get; set; }
            public string OriginalName { get; set; }
            public long GroupId { get; set; }
            public int MemberCount { get; set; }

            public PossibleGroup(Group g)
            {
                Name = g.Name.Unidecode().ToLower();
                OriginalName = g.Name;
                GroupLink = g.GroupLink;
                GroupId = g.GroupId;
                MemberCount = g.MemberCount ?? 0;
            }

            public PossibleGroup()
            {

            }
        }

        [Attributes.Command(Trigger = "sql", DevOnly = true)]
        public static void Sql(Update u, string[] args)
        {
            if (args.Length == 1)
            {
                Send("You must enter a sql command...", u.Message.Chat.Id);
                return;
            }
            using (var db = new WWContext())
            {
                var conn = db.Database.Connection;
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                string raw = "";

                var queries = args[1].Split(';');
                foreach (var sql in queries)
                {
                    using (var comm = conn.CreateCommand())
                    {
                        comm.CommandText = sql;
                        var reader = comm.ExecuteReader();
                        var result = "";
                        if (reader.HasRows)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                                raw += reader.GetName(i) + (i == reader.FieldCount - 1 ? "" : " - ");
                            result += raw + Environment.NewLine;
                            raw = "";
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                    raw += (reader.IsDBNull(i) ? "<i>NULL</i>" : reader[i]) + (i == reader.FieldCount - 1 ? "" : " - ");
                                result += raw + Environment.NewLine;
                                raw = "";
                            }
                        }
                        result += reader.RecordsAffected == -1 ? "" : (reader.RecordsAffected + " records affected");
                        result = !String.IsNullOrEmpty(result) ? result : (sql.ToLower().StartsWith("select") ? "Nothing found" : "Done.");
                        Send(result, u.Message.Chat.Id);
                    }
                }
            }
        }

        [Attributes.Command(Trigger = "reloadenglish", DevOnly = true)]
        public static void ReloadEnglish(Update update, string[] args)
        {
            Bot.English = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));
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

        [Attributes.Command(Trigger = "adddonation", GlobalAdminOnly = true)]
        public static void AddDonation(Update u, string[] args)
        {
#if !BETA
            using (var db = new WWContext())
            {
                var p = u.GetTarget(db);
                if (p != null && p.Id != u.Message.From.Id)
                {
                    if (p.DonationLevel == null)
                        p.DonationLevel = 0;
                    //get the amount to add
                    var a = args[1].Split(' ');
                    var amtStr = a[a.Length - 1];
                    int amt;
                    if (int.TryParse(amtStr, out amt))
                    {
                        if (amt < 101)
                        {
                            bool wasLocked = p.DonationLevel < 10;
                            p.DonationLevel += amt;
                            if (p.DonationLevel >= 10)
                                p.GifPurchased = true;
                            db.SaveChanges();
                            Send($"{p.Name} (@{p.UserName}) donation level is now {p.DonationLevel}", u.Message.Chat.Id);
                            var msg = "";
                            if (wasLocked)
                            {
                                msg = "GIF Pack unlocked.  Your current donation level is " + p.DonationLevel;
                            }
                            else
                            {
                                msg = "Your donation level has been updated.  New level: " + p.DonationLevel;
                            }
                            try
                            {
                                var sent = Send(msg, p.TelegramId).Result;
                                Send($"User was notified!", u.Message.Chat.Id);
                            }
                            catch
                            {
                                Send("Unable to notify user (this bot not started by the user).  Please reach out to them.", u.Message.Chat.Id);
                            }
                        }
                    }
                    else
                    {
                        Send($"Unable to parse donation amount: {amtStr}", u.Message.Chat.Id);
                    }
                }
                else
                    Send($"Unable to determine user to add donation level to.", u.Message.Chat.Id);

            }
#endif
        }

        [Attributes.Command(Trigger = "updatestatus", GlobalAdminOnly = true)]
        public static void UpdateStatus(Update u, string[] args)
        {
            var menu = new InlineKeyboardMarkup(new[] { "Bot 1", "Bot 2", "Beta Bot", "Test Bot" }.Select(x => new InlineKeyboardCallbackButton(x, $"status|{u.Message.From.Id}|{x}|null")).ToArray());

            Bot.Api.SendTextMessageAsync(u.Message.From.Id, "Which bot?",
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

        [Attributes.Command(Trigger = "permban", GlobalAdminOnly = true)]
        public static void PermBan(Update u, string[] args)
        {
#if !BETA
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
                                    BanDate = DateTime.UtcNow,
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
                                    BanDate = DateTime.UtcNow,
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
                                BanDate = DateTime.UtcNow,
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
#endif
        }

        [Attributes.Command(Trigger = "remban", GlobalAdminOnly = true)]
        public static void RemoveBan(Update u, string[] args)
        {
#if !BETA
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
            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                foreach (var userid in args[1].Split(' '))
                {
                    if (!int.TryParse(userid, out int id)) continue;

                    using (var db = new WWContext())
                    {
                        var ban = db.GlobalBans.FirstOrDefault(x => x.TelegramId == id);
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
            }
#endif
        }

        [Attributes.Command(Trigger = "cleanmain", GlobalAdminOnly = true)]
        public static void CleanMain(Update u, string[] args)
        {
            var skip = 0;
            if (args.Length > 0)
                int.TryParse(args[1], out skip);
            if (skip > 0)
                Send($"Skipping the first {skip} users", u.Message.Chat.Id);
            using (var sw = new StreamWriter(Path.Combine(Bot.RootDirectory, "..\\kick.log")))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                //now, check the json file
                var timeStarted = DateTime.UtcNow;
                Send("Getting users from main chat, please wait...", u.Message.Chat.Id);
                ChannelInfo channel = null;
                try
                {
                    channel = CLI.GetChatInfo("WereWuff - The Game", u.Message.Chat.Id).Result;
                }
                catch (AggregateException e)
                {
                    Send(e.InnerExceptions[0].Message + "\n" + e.InnerExceptions[0].StackTrace, u.Message.Chat.Id);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    Send(e.Message + "\n" + e.StackTrace, u.Message.Chat.Id);
                }
                if (channel == null) return;
                var users = channel.Users.Skip(skip).ToList();
                Send($"Beginning kick process.  Found {users.Count} users in the group", u.Message.Chat.Id);
                var i = 0;
                var removed = 0;
                using (var db = new WWContext())
                    foreach (var usr in users)
                    {
                        var id = usr.id;
                        i++;
                        sw.Flush();
                        sw.Write($"\n{i}|{id}|{usr.deleted}|");

                        try
                        {
                            ChatMemberStatus status;
                            try
                            {
                                //check their status first, so we don't make db calls for someone not even in the chat.
                                status = Bot.Api.GetChatMemberAsync(Settings.PrimaryChatId, id).Result.Status;
                            }
                            catch (AggregateException e)
                            {
                                if (e.InnerExceptions[0].Message.Contains("User not found"))
                                {
                                    sw.Write("NF|");
                                    status = ChatMemberStatus.Member;
                                }
                                else
                                    throw;
                            }

                            if (status != ChatMemberStatus.Member)
                                continue;
                            //get the last time they played a game
                            var p = db.Players.FirstOrDefault(x => x.TelegramId == id);

                            //get latest game player, check within 2 weeks
                            var gp =
                                p?.GamePlayers.Join(db.Games.Where(x => x.GroupId == Settings.PrimaryChatId),
                                        x => x.GameId, y => y.Id, (gamePlayer, game) => new { game.TimeStarted, game.Id })
                                    .OrderByDescending(x => x.Id)
                                    .FirstOrDefault();
                            var lastPlayed = DateTime.MinValue;
                            if (gp != null)
                            {
                                lastPlayed = gp.TimeStarted.Value;
                                if (gp.TimeStarted >= DateTime.UtcNow.AddDays(-14))
                                    continue;
                            }

                            //at this point, they have been in the group at least 2 weeks, haven't played in the group in the past 2 weeks, and are a member.  Time to kick.
                            try
                            {
                                //first, check if the user is in the group
                                //sw.Write($"{status}");
                                if (status != ChatMemberStatus.Member) //user is not in group, skip
                                    continue;
                                //kick
                                Bot.Api.KickChatMemberAsync(Settings.PrimaryChatId, id);
                                removed++;
                                sw.Write($"Removed ({p?.Name ?? id.ToString()})");

                                //get their status

                                while (status == ChatMemberStatus.Member) //loop
                                {
                                    //wait for database to report status is kicked.
                                    try
                                    {
                                        status = Bot.Api.GetChatMemberAsync(Settings.PrimaryChatId, id).Result.Status;
                                    }
                                    catch (AggregateException e)
                                    {
                                        if (e.InnerExceptions[0].Message.Contains("User not found"))
                                        {
                                            status = ChatMemberStatus.Kicked;
                                        }
                                        else
                                            throw;
                                    }
                                    Thread.Sleep(500);
                                }
                                //status is now kicked (as it should be)
                                var attempts = 0;
                                sw.Write("|Unbanning-");
                                while (status != ChatMemberStatus.Left) //unban until status is left
                                {
                                    attempts++;
                                    sw.Write($"{status}");
                                    sw.Flush();
                                    Bot.Api.UnbanChatMemberAsync(Settings.PrimaryChatId, id);
                                    Thread.Sleep(500);
                                    try
                                    {
                                        status = Bot.Api.GetChatMemberAsync(Settings.PrimaryChatId, id).Result.Status;
                                    }
                                    catch (AggregateException e)
                                    {
                                        if (e.InnerExceptions[0].Message.Contains("User not found"))
                                        {
                                            status = ChatMemberStatus.Left;
                                        }
                                        else
                                            throw;
                                    }
                                }
                                //yay unbanned
                                sw.Write($"|Unbanned({attempts} attempts)");
                                //let them know
                                Send("You have been removed from the main chat as you have not played in that group in the 2 weeks.  You are always welcome to rejoin!", id);
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
                        catch (AggregateException e)
                        {
                            sw.Write(e.InnerExceptions[0].Message);
                        }
                        catch (Exception e)
                        {
                            while (e.InnerException != null)
                                e = e.InnerException;
                            sw.Write($"{e.Message}");
                        }

                    }


                Console.ForegroundColor = ConsoleColor.Gray;
                Send(
                    $"@{u.Message.From.Username} I have removed {removed} users from the main group.\nTime to process: {DateTime.UtcNow - timeStarted}",
                    u.Message.Chat.Id);




            }
        }



        [Attributes.Command(Trigger = "leavegroup", GlobalAdminOnly = true)]
        public static void LeaveGroup(Update update, string[] args)
        {
            if (String.IsNullOrEmpty(args[1]))
            {
                Send("Use /leavegroup <id|link|username>", update.Message.Chat.Id);
                return;
            }

            Database.Group grp;
            using (var db = new WWContext())
                grp = GetGroup(args[1], db);
            var grpid = grp?.GroupId ?? (long)0;
            var grpname = grp?.Name ?? "";


            if (grpid != 0)
            {
                try
                {
                    Send("Para said I can't play with you guys anymore, you are a bad influence! *runs out the door*", grpid)
                        .ContinueWith((result) =>
                        {
                            Bot.Api.LeaveChatAsync(grpid);
                        });
                }
                catch (Exception e)
                {
                    Send("An error occurred.\n" + e.Message, update.Message.Chat.Id);
                    return;
                }
                var msg = "Bot successfully left from group";
                msg += String.IsNullOrEmpty(grpname) ? $" {grpname}." : ".";
                Send(msg, update.Message.Chat.Id);
            }
            else
                Send("Couldn't find the group. Is the id/link valid?", update.Message.Chat.Id);
            return;
        }

        [Attributes.Command(Trigger = "preferred", GlobalAdminOnly = true)]
        public static void Preferred(Update update, string[] args)
        {
#if !BETA
            var group = args[1];
            if (String.IsNullOrEmpty(args[1]))
            {
                Bot.Send("Usage: `/preferred <link|username|groupid>`", update.Message.Chat.Id, parseMode: ParseMode.Markdown);
                return;
            }
            using (var db = new WWContext())
            {
                Database.Group grp = GetGroup(group, db);
                if (grp == null)
                {
                    Send("Group not found.", update.Message.Chat.Id);
                    return;
                }
                //get the languages which they played, make a menu out of it
                var rankings = db.GroupRanking.Where(x => x.GroupId == grp.Id && !x.Language.EndsWith("BaseAllVariants")).ToList();
                var menu = rankings.Select(x => new[] {
                    new InlineKeyboardCallbackButton(x.Language, $"pf|{grp.GroupId}|{x.Language}|i"),
                    new InlineKeyboardCallbackButton(x.Show == false ? "☑️" : "✅", $"pf|{grp.GroupId}|{x.Language}|t")
                }).ToList();
                //add a button at the beginning and at the end
                menu.Insert(0, new[] {
                    new InlineKeyboardCallbackButton("Global", $"pf|{grp.GroupId}|null|i"),
                    new InlineKeyboardCallbackButton(grp.Preferred == false ? "☑️" : "✅", $"pf|{grp.GroupId}|null|t")
                });
                menu.Add(new[] { new InlineKeyboardCallbackButton("Done", "done") });
                //send everything
                Send(
                    $"{grp.GroupId} | " + (grp.GroupLink == null ? grp.Name : $" <a href=\"{grp.GroupLink}\">{grp.Name}</a>") +
                    "\n\nSelect the languages under which the group is allowed to appear in grouplist.\nNote that the first option, if disabled, overrides all the others.",
                    update.Message.Chat.Id, customMenu: new InlineKeyboardMarkup(menu.ToArray())
                );
                return;
            }
#endif
        }

        [Attributes.Command(Trigger = "ohaider", DevOnly = true)]
        public static void OHaiDer(Update u, string[] args)
        {
            try
            {
                if (u.Message.Chat.Type != ChatType.Private)
                {
                    Send("You must run this command in PM!!", u.Message.Chat.Id);
                    return;
                }
                //#if !DEBUG
                //                if (Bot.Me.Username != "werewolfbot")
                //                {
                //                    Send("Please run this command on @werewolfbot only", u.Message.Chat.Id);
                //                    return;
                //                }
                //#endif
                int id = 0;
                if (int.TryParse(args[1], out id))
                {
                    //get the user from the database
                    using (var ww = new WWContext())
                    {
                        var user = ww.Players.FirstOrDefault(x => x.TelegramId == id);
                        if (user != null)
                        {
                            //create a menu for this
                            var buttons = new[] { new InlineKeyboardCallbackButton("Yes", "ohai|yes|" + user.Id), new InlineKeyboardCallbackButton("No", "ohai|no") };
                            Send($"Update OHAIDER Achievement using player {user.Name}?", u.Message.Chat.Id,
                                customMenu: new InlineKeyboardMarkup(buttons));
                        }
                        else
                        {
                            Send("Unable to find player account with that id", u.Message.Chat.Id);
                        }
                    }
                }
                else
                {
                    Send("Invalid ID", u.Message.Chat.Id);
                }
            }
            catch (Exception e)
            {
                Send("Unable to update OHAIDER: " + e.Message, u.Message.Chat.Id);
            }

        }

        [Attributes.Command(Trigger = "clearlogs", DevOnly = true)]
        public static void ClearLogs(Update u, string[] args)
        {
            var LogPath = Path.Combine(Bot.RootDirectory, "..\\Logs\\");
            var files = new[] { "NodeFatalError.log", "error.log", "tcperror.log", "apireceiveerror.log", "getUpdates.log" };
            foreach (var file in files)
            {
                try
                {
                    System.IO.File.Delete(LogPath + file);
                }
                catch
                {
                    Thread.Sleep(50);
                    try
                    {
                        System.IO.File.Delete(LogPath + file);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        [Attributes.Command(Trigger = "getlogs", DevOnly = true)]
        public static void GetLogs(Update u, string[] args)
        {
            try
            {
                var LogPath = Path.Combine(Bot.RootDirectory, "..\\Logs\\");

                var path = LogPath + "errors.zip";
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                var someFileExists = false;
                using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    var files = new[] { "NodeFatalError.log", "error.log", "tcperror.log", "apireceiveerror.log", $"getUpdates {DateTime.UtcNow.ToString("MMM-dd-yyyy")}.log" };

                    foreach (var file in files)
                    {
                        var fp = LogPath + file;
                        if (!File.Exists(fp)) continue;
                        someFileExists = true;
                        zip.CreateEntryFromFile(fp, file, CompressionLevel.Optimal);
                    }
                }
                //now send the file
                if (someFileExists)
                {
                    var fs = new FileStream(path, FileMode.Open);
                    Bot.Api.SendDocumentAsync(u.Message.Chat.Id, new FileToSend("errors.zip", fs));
                }
            }
            catch (Exception e)
            {
                Bot.Send(e.Message, u.Message.Chat.Id);
            }
        }

        [Attributes.Command(Trigger = "movelang", DevOnly = true)]
        public static void MoveLang(Update u, string[] args)
        {
            //TODO:
            //1. Ask for the langfile to move
            //2. Ask for new filename and langnode
            //3. Create the new langfile automagically
            var match = new Regex(@"([\s\S]*).xml ([\s\S]*).xml").Match(args[1] ?? "");
            if (!match.Success || match.Groups.Count != 3)
            {
                Bot.Send("Fail. Use !movelang <oldfilename>.xml <newfilename>.xml", u.Message.Chat.Id, parseMode: ParseMode.Markdown);
                return;
            }
            var oldfilename = match.Groups[1].Value;
            var newfilename = match.Groups[2].Value;
            var langs = Directory.GetFiles(Bot.LanguageDirectory).Where(x => x.EndsWith(".xml")).Select(x => new LangFile(x)).ToList();
            var oldlang = langs.FirstOrDefault(x => x.FileName == oldfilename);
            var newlang = langs.FirstOrDefault(x => x.FileName == newfilename);
            if (oldlang == null || newlang == null)
            {
                Send("No langfile found. Make sure those langfiles exist!", u.Message.Chat.Id);
                return;
            }

            string msg = $"OLD FILE\n_Name:_ {oldlang.Name}\n_Base:_ {oldlang.Base}\n_Variant:_ {oldlang.Variant}\n_Last updated:_ {oldlang.LatestUpdate.ToString("MMM dd")}\n\n";
            msg += $"NEW FILE\n_Name:_ {newlang.Name}\n_Base:_ {newlang.Base}\n_Variant:_ {newlang.Variant}\n_Last updated:_ {newlang.LatestUpdate.ToString("MMM dd")}\n\n";
            msg += "Are you sure?";

            var buttons = new[] { new InlineKeyboardCallbackButton("Yes", $"movelang|yes|{oldfilename}|{newfilename}"), new InlineKeyboardCallbackButton("No", $"movelang|no") };
            Bot.Send(msg, u.Message.Chat.Id, customMenu: new InlineKeyboardMarkup(buttons), parseMode: ParseMode.Markdown);
        }

        [Attributes.Command(Trigger = "resetlink", GlobalAdminOnly = true)]
        public static void ResetLink(Update u, string[] args)
        {
            var link = args[1];
            if (String.IsNullOrEmpty(link))
            {
                Send("Use /resetlink <link|id|username>. This will reset the link of the group, without affecting the Preferred status.", u.Message.Chat.Id);
            }
            else
            {
                using (var db = new WWContext())
                {
                    Database.Group grp = GetGroup(link, db);

                    if (grp == null)
                        Send($"Group not found.", u.Message.Chat.Id);
                    else
                    {
                        grp.GroupLink = null;
                        db.SaveChanges();
                        Send($"The link for {grp.Name} has been reset.", u.Message.Chat.Id);
                    }
                }
            }
            return;
        }


        [Attributes.Command(Trigger = "commitlangs", DevOnly = true)]
        public static void CommitLangs(Update u, string[] args)
        {
            var msg = "Processing...";
            var msgid = Send(msg, u.Message.Chat.Id).Result.MessageId;
            try
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        FileName = @"C:\Werewolf Source\Werewolf\Werewolf for Telegram\Languages\commit.bat",
                        Arguments = $"\"Syncing langfiles from Telegram\"",
                        WorkingDirectory = @"C:\Werewolf Source\Werewolf\Werewolf for Telegram\Languages",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                p.Start();
                msg += "\nStarted the process. Reading output from git...";
                Bot.Edit(u.Message.Chat.Id, msgid, msg);

                var output = "";
                while (!p.StandardOutput.EndOfStream)
                    output += p.StandardOutput.ReadLine() + Environment.NewLine;
                while (!p.StandardError.EndOfStream)
                    output += p.StandardError.ReadLine() + Environment.NewLine;

                msg += "\nValidating the output...";
                Bot.Edit(u.Message.Chat.Id, msgid, msg);

                //validate the output
                if (output.Contains("failed"))
                {
                    msg += "\n<b>Failed to commit files. See control output for information</b>";
                    Console.WriteLine(output);
                }
                else if (output.Contains("nothing to commit"))
                {
                    msg += "\n<b>Nothing to commit.</b>";
                }
                else
                {
                    //try to grab the commit
                    var regex = new Regex("(\\[master .*])");
                    var match = regex.Match(output);
                    var commit = "";
                    if (match.Success)
                    {
                        commit = match.Value.Replace("[master ", "").Replace("]", "");
                    }
                    msg += $"\n<b>Files committed successfully.</b> {(String.IsNullOrEmpty(commit) ? "" : $"<a href=\"https://github.com/GreyWolfDev/Werewolf/commit/" + commit + $"\">{commit}</a>")}";
                }
            }
            catch (Exception e)
            {
                msg += e.Message;
            }

            Bot.Edit(u.Message.Chat.Id, msgid, msg);
        }

        [Attributes.Command(Trigger = "user", GlobalAdminOnly = true)]
        public static void User(Update u, string[] args)
        {
            using (var db = new WWContext())
            {

                var t = u.GetTarget(db);
                if (t == null)
                {
                    Send("Not Found", u.Message.Chat.Id);
                    return;
                }
                var result = $"<b>{t.Name.FormatHTML()}</b>\n";
                if (!String.IsNullOrEmpty(t.UserName))
                    result += $"@{t.UserName}\n";
                result += $"------------------\nGames Played: {t.GamePlayers.Count}\nLanguage: {t.Language.FormatHTML()}\nDonation Level: {t.DonationLevel ?? 0}\n";
                if (t.GamePlayers.Any())
                    result += $"Played first game: {t.GamePlayers.OrderBy(x => x.Id).First().Game.TimeStarted}\n";
                var spamBans = t.TempBanCount;
                if (spamBans > 0)
                {
                    result += $"Player has been temp banned {t.TempBanCount} times\n";
                }
                //check if currently banned
                var banned = db.GlobalBans.FirstOrDefault(x => x.TelegramId == t.TelegramId);
                if (banned != null)
                {
                    result += $"\n------------------\n<b>PLAYER IS CURRENTLY BANNED</b>\nReason: {banned.Reason}\nBanned on: {banned.BanDate}\nBanned by: {banned.BannedBy}\n";
                    if (banned.Expires < DateTime.UtcNow.AddYears(1))
                    {
                        var expiry = (banned.Expires - DateTime.UtcNow);
                        result += $"Ban will be lifted in {expiry.Days} days, {expiry.Hours} hours, and {expiry.Minutes} minutes\n";
                    }
                    else
                        result += $"This ban is permanent.\n";
                }
                else if ((spamBans ?? 0) == 0)
                {
                    result += "Player is clean, no werewolf bans on record.\n";
                }

                Bot.Api.SendTextMessageAsync(u.Message.Chat.Id, result, parseMode: ParseMode.Html);
            }
        }

        [Attributes.Command(Trigger = "unlockbeta", GlobalAdminOnly = true)]
        public static void UnlockBeta(Update u, string[] args)
        {
#if BETA
            if (!Program.BetaUnlocked)
            {
                Program.BetaUnlocked = true;
                foreach(var id in new[] { u.Message.Chat.Id, -1001094155678 }.Distinct())
                    Bot.Send($"<b>Beta has been unlocked for all groups by {u.Message.From.FirstName.FormatHTML()}!</b>", id);
                
            }
            else
            {
                Bot.Send("Beta was already unlocked for all groups!", u.Message.Chat.Id);
            }
#endif
        }

        [Attributes.Command(Trigger = "lockbeta", GlobalAdminOnly = true)]
        public static void LockBeta(Update u, string[] args)
        {
#if BETA
            if (Program.BetaUnlocked)
            {
                Program.BetaUnlocked = false;
                foreach (var id in new[] { u.Message.Chat.Id, -1001094155678 }.Distinct())
                    Bot.Send($"<b>Beta has been locked for non-betagroups by {u.Message.From.FirstName.FormatHTML()}!</b>", id);

            }
            else
            {
                Bot.Send("Beta was already locked for non-betagroups!", u.Message.Chat.Id);
            }
#endif
        }

    }


}
