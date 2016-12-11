using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.Design;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using Database;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
namespace Werewolf_Control.Handler
{

    internal static class UpdateHandler
    {
        internal static Dictionary<int, SpamDetector> UserMessages = new Dictionary<int, SpamDetector>();

        internal static HashSet<int> SpamBanList = new HashSet<int>
        {

        };
        internal static List<GlobalBan> BanList = new List<GlobalBan>();

        internal static bool SendGifIds = false;
        public static void UpdateReceived(object sender, UpdateEventArgs e)
        {
            new Task(() => { HandleUpdate(e.Update); }).Start();
        }

        private static void AddCount(int id, string command)
        {
            try
            {
                if (!UserMessages.ContainsKey(id))
                    UserMessages.Add(id, new SpamDetector { Messages = new HashSet<UserMessage>() });
                UserMessages[id].Messages.Add(new UserMessage(command));
            }
            catch
            {
                // ignored
            }
        }

        internal static void BanMonitor()
        {
            while (true)
            {
                try
                {
                    //first load up the ban list
                    using (var db = new WWContext())
                    {
                        foreach (var id in SpamBanList)
                        {
                            var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                            var name = p?.Name;
                            var count = p?.TempBanCount ?? 0;
                            count++;
                            if (p != null)
                                p.TempBanCount = count; //update the count

                            var expireTime = DateTime.Now;
                            switch (count)
                            {
                                case 1:
                                    expireTime = expireTime.AddHours(12);
                                    break;
                                case 2:
                                    expireTime = expireTime.AddDays(1);
                                    break;
                                case 3:
                                    expireTime = expireTime.AddDays(3);
                                    break;
                                default: //perm ban
                                    expireTime = (DateTime)SqlDateTime.MaxValue;
                                    break;

                            }
                            db.GlobalBans.Add(new GlobalBan
                            {
                                BannedBy = "Moderator",
                                Expires = expireTime,
                                TelegramId = id,
                                Reason = "Spam / Flood",
                                BanDate = DateTime.Now,
                                Name = name
                            });
                        }
                        SpamBanList.Clear();
                        db.SaveChanges();

                        //now refresh the list
                        var list = db.GlobalBans.ToList();
#if RELEASE2
                        for (var i = list.Count - 1; i >= 0; i--)
                        {
                            if (list[i].Expires > DateTime.Now) continue;
                            db.GlobalBans.Remove(db.GlobalBans.Find(list[i].Id));
                            list.RemoveAt(i);
                        }
                        db.SaveChanges();
#endif

                        BanList = list;
                    }
                }
                catch
                {
                    // ignored
                }

                //refresh every 20 minutes
                Thread.Sleep(TimeSpan.FromMinutes(1));
            }
        }

        internal static void SpamDetection()
        {
            while (true)
            {
                try
                {
                    var temp = UserMessages.ToDictionary(entry => entry.Key, entry => entry.Value);
                    //clone the dictionary
                    foreach (var key in temp.Keys.ToList())
                    {
                        try
                        {
                            //drop older messages (1 minute)
                            temp[key].Messages.RemoveWhere(x => x.Time < DateTime.Now.AddMinutes(-1));

                            //comment this out - if we remove it, it doesn't keep the warns
                            //if (temp[key].Messages.Count == 0)
                            //{
                            //    temp.Remove(key);
                            //    continue;
                            //}
                            //now count, notify if limit hit
                            if (temp[key].Messages.Count() >= 20) // 20 in a minute
                            {
                                temp[key].Warns++;
                                if (temp[key].Warns < 2 && temp[key].Messages.Count < 40)
                                {
                                    Send($"Please do not spam me. Next time is automated ban.", key);
                                    //Send($"User {key} has been warned for spamming: {temp[key].Warns}\n{temp[key].Messages.GroupBy(x => x.Command).Aggregate("", (a, b) => a + "\n" + b.Count() + " " + b.Key)}",
                                    //    Para);
                                    continue;
                                }
                                if ((temp[key].Warns >= 3 || temp[key].Messages.Count >= 40) & !temp[key].NotifiedAdmin)
                                {
                                    //Send(
                                    //    $"User {key} has been banned for spamming: {temp[key].Warns}\n{temp[key].Messages.GroupBy(x => x.Command).Aggregate("", (a, b) => a + "\n" + b.Count() + " " + b.Key)}",
                                    //    Para);
                                    temp[key].NotifiedAdmin = true;
                                    //ban
                                    SpamBanList.Add(key);
                                    var count = 0;
                                    using (var db = new WWContext())
                                    {
                                        count = db.Players.FirstOrDefault(x => x.TelegramId == key).TempBanCount ?? 0;
                                    }
                                    var unban = "";
                                    switch (count)
                                    {
                                        case 0:
                                            unban = "12 hours";
                                            break;
                                        case 1:
                                            unban = "24 hours";
                                            break;
                                        case 2:
                                            unban = "3 days";
                                            break;
                                        default:
                                            unban =
                                                "Permanent. You have reached the max limit of temp bans for spamming.";
                                            break;
                                    }
                                    Send("You have been banned for spamming.  Your ban period is: " + unban,
                                        key);
                                }

                                temp[key].Messages.Clear();
                            }
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine(e.Message);
                        }
                    }
                    UserMessages = temp;
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.Message);
                }
                Thread.Sleep(2000);
            }
        }

        internal static void HandleUpdate(Update update)
        {
            {
                Bot.MessagesProcessed++;

                //ignore previous messages
                if ((update.Message?.Date ?? DateTime.MinValue) < Bot.StartTime.AddSeconds(-10))
                    return; //toss it

                var id = update.Message.Chat.Id;

#if DEBUG
                //if (update.Message.Chat.Title != "Werewolf Translators Group" && !String.IsNullOrEmpty(update.Message.Chat.Title) && update.Message.Chat.Title != "Werewolf Mod / Dev chat (SFW CUZ YOUNGENS)" && update.Message.Chat.Title != "Werewolf Translators Group (SFW cuz YOUNGENS)")
                //{
                //    try
                //    {
                //        Bot.Api.LeaveChat(update.Message.Chat.Id);
                //    }
                //    catch
                //    {
                //        // ignored
                //    }
                //}
#endif

                //let's make sure it is a bot command, as we shouldn't see anything else....
                //if (update.Message.Type != MessageType.ServiceMessage &&
                //    update.Message.Type != MessageType.UnknownMessage && update.Message.Chat.Type != ChatType.Private)
                //{
                //    if (
                //        update.Message.Entities.All(
                //            x => x.Type != MessageEntityType.BotCommand && x.Type != MessageEntityType.Mention))
                //    {
                //        var admins = Bot.Api.GetChatAdministrators(update.Message.Chat.Id).Result.ToList();


                //        var adminlist = admins.Aggregate("", (a, b) => a + Environment.NewLine + "@" + b.User.Username);
                //        //I shouldn't have seen this message!
                //        Send(
                //            "Privacy mode has been enabled, but has not updated for this group.  In order for it to be updated, I need to leave and be added back.  Admin, please add me back to this group!\n" +
                //            adminlist.FormatHTML(),
                //            update.Message.Chat.Id);

                //        try
                //        {
                //            using (var db = new WWContext())
                //            {
                //                var grps = db.Groups.Where(x => x.GroupId == id);
                //                if (!grps.Any())
                //                {
                //                    return;
                //                }
                //                foreach (var g in grps)
                //                {
                //                    g.BotInGroup = false;
                //                    g.UserName = update.Message.Chat.Username;
                //                    g.Name = update.Message.Chat.Title;
                //                }
                //                db.SaveChanges();
                //            }
                //        }
                //        catch
                //        {
                //            // ignored
                //        }

                //        Bot.Api.LeaveChat(update.Message.Chat.Id);
                //    }
                //}


                //Settings.Main.LogText += update?.Message?.Text + Environment.NewLine;
                bool block = new[] { Settings.SupportChatId, Settings.PersianSupportChatId }.Contains(id);

#if !DEBUG
                try
#endif
                {
                    Group grp;
                    switch (update.Message.Type)
                    {
                        case MessageType.UnknownMessage:
                            break;
                        case MessageType.TextMessage:
                            if (update.Message.Text.StartsWith("!") || update.Message.Text.StartsWith("/"))
                            {

                                if (BanList.Any(x => x.TelegramId == (update.Message?.From?.Id ?? 0)) || SpamBanList.Contains(update.Message?.From?.Id ?? 0))
                                {
                                    return;
                                }
                                if (update.Message.Chat.Type == ChatType.Private)
                                    AddCount(update.Message.From.Id, update.Message.Text);
                                var args = GetParameters(update.Message.Text);
                                args[0] = args[0].ToLower().Replace("@" + Bot.Me.Username.ToLower(), "");

                                if (args[0].StartsWith("about"))
                                {
                                    var reply = Commands.GetAbout(update, args);
                                    if (reply != null)
                                    {
                                        try
                                        {
                                            var result = Send(reply, update.Message.From.Id).Result;
                                            if (update.Message.Chat.Type != ChatType.Private)
                                                Send(GetLocaleString("SentPrivate", GetLanguage(update.Message.From.Id)), update.Message.Chat.Id);

                                        }
                                        catch (Exception e)
                                        {
                                            Commands.RequestPM(update.Message.Chat.Id);
                                        }
                                    }
                                    return;
                                }

                                //check for the command

                                #region More optimized code, but slow as hell

                                var command = Bot.Commands.FirstOrDefault(
                                        x =>
                                            String.Equals(x.Trigger, args[0],
                                                StringComparison.InvariantCultureIgnoreCase));
                                if (command != null)
                                {
                                    //check that we should run the command
                                    if (block && command.Blockable)
                                    {
                                        Send(id == Settings.SupportChatId
                                                ? "No games in support chat!"
                                                : "اینجا گروه پشتیبانیه نه بازی، لطفا دکمه استارت رو نزنید.", id);
                                        return;
                                    }
                                    if (command.DevOnly & !UpdateHelper.Devs.Contains(update.Message.From.Id))
                                    {
                                        Send(GetLocaleString("NotPara", GetLanguage(id)), id);
                                        return;
                                    }
                                    if (command.GlobalAdminOnly)
                                    {
                                        if (!UpdateHelper.IsGlobalAdmin(update.Message.From.Id))
                                        {
                                            Send(GetLocaleString("NotGlobalAdmin", GetLanguage(id)), id);
                                            return;
                                        }
                                    }
                                    if (command.GroupAdminOnly & !UpdateHelper.IsGroupAdmin(update) & !UpdateHelper.Devs.Contains(update.Message.From.Id) & !UpdateHelper.IsGlobalAdmin(update.Message.From.Id))
                                    {
                                        Send(GetLocaleString("GroupAdminOnly", GetLanguage(update.Message.Chat.Id)), id);
                                        return;
                                    }
                                    if (command.InGroupOnly & update.Message.Chat.Type == ChatType.Private)
                                    {
                                        Send(GetLocaleString("GroupCommandOnly", GetLanguage(id)), id);
                                        return;
                                    }
                                    Bot.CommandsReceived++;
                                    if (update.Message.Chat.Type != ChatType.Private)
                                        AddCount(update.Message.From.Id, update.Message.Text);
                                    command.Method.Invoke(update, args);
                                }

                                #endregion
                            }
                            break;
                        case MessageType.PhotoMessage:
                            break;
                        case MessageType.AudioMessage:
                            break;
                        case MessageType.VideoMessage:
                            break;
                        case MessageType.VoiceMessage:
                            break;
                        case MessageType.DocumentMessage:
                            if (UpdateHelper.Devs.Contains(update.Message.From.Id) && SendGifIds)
                            {
                                var doc = update.Message.Document;
                                Send(doc.FileId, update.Message.Chat.Id);
                            }
                            break;
                        case MessageType.StickerMessage:
                            break;
                        case MessageType.LocationMessage:
                            break;
                        case MessageType.ContactMessage:
                            break;
                        case MessageType.ServiceMessage:
                            using (var DB = new WWContext())
                            {
                                id = update.Message.Chat.Id;
                                var m = update.Message;

                                if (m.LeftChatMember != null)
                                {
                                    if (m.LeftChatMember.Id == Bot.Me.Id)
                                    {
                                        //removed from group
                                        var grps = DB.Groups.Where(x => x.GroupId == id);
                                        if (!grps.Any())
                                        {
                                            return;
                                        }
                                        foreach (var g in grps)
                                        {
                                            g.BotInGroup = false;
                                            g.UserName = update.Message.Chat.Username;
                                            g.Name = update.Message.Chat.Title;
                                        }
                                        DB.SaveChanges();
                                    }
                                    else
                                    {
                                        //player left, attempt smite
                                        Bot.GetGroupNodeAndGame(id)?.SmitePlayer(m.LeftChatMember.Id);
                                    }
                                }
                                if (m.NewChatMember?.Id == Bot.Me.Id)
                                {
                                    //added to a group
                                    grp = DB.Groups.FirstOrDefault(x => x.GroupId == id);
                                    if (grp == null)
                                    {
                                        grp = MakeDefaultGroup(id, update.Message.Chat.Title, "NewChatMember");
                                        DB.Groups.Add(grp);
                                        DB.SaveChanges();
                                        grp = DB.Groups.FirstOrDefault(x => x.GroupId == id);
                                    }
                                    grp.BotInGroup = true;
                                    grp.UserName = update.Message.Chat.Username;
                                    grp.Name = update.Message.Chat.Title;
                                    DB.SaveChanges();

                                    var msg =
                                        $"You've just added Werewolf Moderator!  Use /config (group admins) to configure group settings.   If you need assistance, join the [support channel](https://telegram.me/werewolfsupport)";
                                    msg += Environment.NewLine +
                                           "For updates on what is happening, join the dev channel @werewolfdev" +
                                           Environment.NewLine +
                                           "Full information is available on the [website](http://www.tgwerewolf.com)";
                                    Send(msg, id, parseMode: ParseMode.Markdown);

#if BETA
                                    Send(
                                        "*IMPORTANT NOTE- THIS IS A BETA BOT.  EXPECT BUGS, EXPECT SHUTDOWNS, EXPECT.. THE UNEXPECTED!*",
                                        id, parseMode: ParseMode.Markdown);
#endif
                                }
                                else if (m.NewChatMember != null && m.Chat.Id == Settings.VeteranChatId)
                                {
                                    var uid = m.NewChatMember.Id;
                                    //check that they are allowed to join.
                                    var p = DB.Players.FirstOrDefault(x => x.TelegramId == uid);
                                    if ((p?.GamePlayers.Count ?? 0) >= 500) return;
                                    //user has not reach veteran
                                    Send($"{m.NewChatMember.FirstName} removed, as they have not unlocked veteran", m.Chat.Id);
                                    Commands.KickChatMember(Settings.VeteranChatId, uid);
                                }
                            }
                            break;
                        case MessageType.VenueMessage:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
#if !DEBUG
                catch (Exception ex)
                {
                    Send(ex.Message, id);
                }
#endif
            }
        }




        /// <summary>
        /// Gets the language for the group, defaulting to English
        /// </summary>
        /// <param name="id">The ID of the group</param>
        /// <returns></returns>
        private static string GetLanguage(long id)
        {
            return Commands.GetLanguage(id);
        }

        private static GameInfo GetGroupNodeAndGame(long id)
        {
            var node = Bot.Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.GroupId == id))?.Games.FirstOrDefault(x => x.GroupId == id);
            if (node == null)
                node = Bot.Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.GroupId == id))?.Games.FirstOrDefault(x => x.GroupId == id);
            if (node == null)
                node = Bot.Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.GroupId == id))?.Games.FirstOrDefault(x => x.GroupId == id);
            return node;
        }

        private static string[] nonCommandsList = new[] { "vote", "getlang", "validate", "setlang", "groups", "status", "done" };

        public static void CallbackReceived(object sender, CallbackQueryEventArgs e)
        {
            new Task(() => { HandleCallback(e.CallbackQuery); }).Start();
        }


        internal static void HandleCallback(CallbackQuery query)
        {
            Bot.MessagesProcessed++;
            //Bot.CommandsReceived++;
            using (var DB = new WWContext())
            {
                try
                {
                    if (String.IsNullOrEmpty(query.Data))
                    {
                        //empty request, from Telegram bot monitoring most likely
                        Bot.ReplyToCallback(query, "Invalid Callback");
                        return;
                    }
                    string[] args = query.Data.Split('|');
                    if (args[0] == "update")
                    {
                        bool dontUpdate = args[1] == "no";
                        if (UpdateHelper.Devs.Contains(query.From.Id))
                        {
                            if (dontUpdate)
                            {
                                Bot.ReplyToCallback(query, "Okay, I won't do anything D: *sadface*");
                                return;
                            }
                            //start the update process
                            Updater.DoUpdate(query);
                        }
                        else
                        {
                            Bot.ReplyToCallback(query, "You aren't Para! Go Away!!", false, true);
                        }
                        return;
                    }
                    if (args[0] == "ohai")
                    {
                        bool dontUpdate = args[1] == "no";
                        Bot.ReplyToCallback(query, "Processing...");
                        if (UpdateHelper.Devs.Contains(query.From.Id))
                        {
                            if (dontUpdate)
                            {
                                Bot.Edit(query, "Okay, I won't do anything D: *sadface*");
                                return;
                            }
                            //update ohaider achievement
                            var userid = int.Parse(args[2]);


                            try
                            {
                                var para = DB.Players.FirstOrDefault(x => x.Id == userid);

                                //get all the players Para has played with
                                var players = (from g in DB.Games
                                               join gp in DB.GamePlayers on g.Id equals gp.GameId
                                               join gp2 in DB.GamePlayers on g.Id equals gp2.GameId
                                               join pl in DB.Players on gp2.PlayerId equals pl.Id
                                               where gp.PlayerId == para.Id
                                               select pl).Distinct();

                                //figure out which players don't have the achievement

                                //update the message
                                var ohaimsg = $"Found {players.Count()} players that have earned OHAIDER.";
                                Bot.Edit(query, ohaimsg);
                                var count = 0;
                                foreach (var player in players)
                                {
                                    //add the achievement
                                    if (player.Achievements == null)
                                        player.Achievements = 0;
                                    var ach = (Achievements)player.Achievements;
                                    if (ach.HasFlag(Achievements.OHAIDER)) continue;
                                    count++;
                                    var a = Achievements.OHAIDER;
                                    player.Achievements = (long)(ach | a);
                                    //log these ids, just in case....
                                    using (var sw = new StreamWriter(Path.Combine(Bot.RootDirectory, "..\\Logs\\ohaider.log"), true))
                                    {
                                        sw.WriteLine(player.Id);
                                    }
                                    Send($"Achievement Unlocked!\n{a.GetName().ToBold()}\n{a.GetDescription()}", player.TelegramId);
                                    Thread.Sleep(200);
                                }
                                DB.SaveChanges();
                                ohaimsg += $"\nAchievement added to {count} players\nFinished";
                                Bot.Edit(query, ohaimsg);
                            }
                            catch (AggregateException e)
                            {
                                Send(e.InnerExceptions.First().Message, query.From.Id);
                            }
                            catch (Exception e)
                            {
                                while (e.InnerException != null)
                                    e = e.InnerException;
                                Send(e.Message, query.From.Id);
                            }

                        }
                        else
                        {
                            Bot.ReplyToCallback(query, "You aren't Para! Go Away!!", false, true);
                        }
                        return;
                    }
                    if (args[0] == "restore")
                    {
                        if (UpdateHelper.Devs.Contains(query.From.Id))
                        {
                            Bot.ReplyToCallback(query, "Processing...");
                            if (args[1] == "no")
                            {
                                Bot.Edit(query, "Okay, I won't do anything D: *sadface*");
                                return;
                            }
                            var oldid = int.Parse(args[1]);
                            var newid = int.Parse(args[2]);
                            var result = DB.RestoreAccount(oldid, newid);
                            var oldname = DB.Players.FirstOrDefault(x => x.TelegramId == oldid)?.Name;
                            var newname = DB.Players.FirstOrDefault(x => x.TelegramId == newid)?.Name;
                            Bot.Edit(query, $"Restored stats from {oldname} to {newname}");
                        }
                        else
                        {
                            Bot.ReplyToCallback(query, "You aren't Para! Go Away!!", false, true);
                        }
                    }
                    InlineKeyboardMarkup menu;
                    Group grp;
                    Player p = DB.Players.FirstOrDefault(x => x.TelegramId == query.From.Id);
                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
                    long groupid = 0;
                    if (args[0] == "vote")
                    {
                        var node = Bot.Nodes.FirstOrDefault(x => x.ClientId.ToString() == args[1]);
                        node?.SendReply(query);
                        return;
                    }

                    groupid = long.Parse(args[1]);

                    grp = DB.Groups.FirstOrDefault(x => x.GroupId == groupid);
                    if (grp == null && args[0] != "getlang" && args[0] != "validate" && args[0] != "lang" && args[0] != "setlang" && args[0] != "groups" && args[0] != "upload" && args[0] != "status")
                        return;
                    if (grp == null)
                    {
                        if (p == null && args[0] != "lang" && args[0] != "setlang" && args[0] != "groups") //why am i doing this????  TODO: update later to array contains...
                            return;
                    }

                    var language = GetLanguage(p?.TelegramId ?? grp.GroupId);
                    var command = args[0];
                    var choice = "";
                    if (args.Length > 2)
                        choice = args[2];
                    if (choice == "cancel")
                    {
                        Bot.ReplyToCallback(query, GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        return;
                    }
                    if (!nonCommandsList.Contains(command.ToLower()))
                        if (!UpdateHelper.IsGroupAdmin(query.From.Id, groupid) & !UpdateHelper.Devs.Contains(query.From.Id) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                        {
                            Bot.ReplyToCallback(query, GetLocaleString("GroupAdminOnly", language), false);
                            return;
                        }
                    var Yes = GetLocaleString("Yes", language);
                    var No = GetLocaleString("No", language);
                    var Cancel = GetLocaleString("Cancel", language);
                    switch (command)
                    {
                        case "status":
                            if (args[3] == "null")
                            {
                                //get status
                                menu = new InlineKeyboardMarkup(new[] { "Normal", "Overloaded", "Recovering", "API Bug", "Offline", "Maintenance" }.Select(x => new[] { new InlineKeyboardButton(x, $"status|{groupid}|{choice}|{x}") }).ToArray());
                                Bot.ReplyToCallback(query, "Set status to?", replyMarkup: menu);
                            }
                            else
                            {
                                //update the status
                                var bot = DB.BotStatus.FirstOrDefault(x => x.BotName == choice);
                                if (bot != null)
                                {
                                    bot.BotStatus = args[3];
                                    DB.SaveChanges();
                                }
                                Bot.ReplyToCallback(query, "Status updated");
                            }
                            break;
                        case "groups":
                            var groups = PublicGroups.ForLanguage(choice).ToList().OrderByDescending(x => x.MemberCount).Take(10).ToList(); //top 10 groups, otherwise these lists will get LONG
                            Bot.ReplyToCallback(query, GetLocaleString("HereIsList", language, choice));
                            if (groups.Count() > 5)
                            {
                                //need to split it
                                var reply = groups.Take(5).Aggregate("",
                                    (current, g) =>
                                        current +
                                        $"{(g.MemberCount?.ToString() ?? "Unknown")} {GetLocaleString("Members", language)}\n<a href=\"{g.GroupLink}\">{g.Name}</a>\n\n");
                                Send(reply, query.Message.Chat.Id);
                                Thread.Sleep(500);
                                reply = groups.Skip(5).Aggregate("",
                                    (current, g) =>
                                        current +
                                        $"{(g.MemberCount?.ToString() ?? "Unknown")} {GetLocaleString("Members", language)}\n<a href=\"{g.GroupLink}\">{g.Name}</a>\n\n");
                                Send(reply, query.Message.Chat.Id);
                            }
                            else
                            {
                                var reply = groups.Aggregate("",
                                    (current, g) =>
                                        current +
                                        $"{(g.MemberCount?.ToString() ?? "Unknown")} {GetLocaleString("Members", language)}\n<a href=\"{g.GroupLink}\">{g.Name}</a>\n\n");
                                Send(reply, query.Message.Chat.Id);
                            }
                            break;
                        case "validate":
                            //choice = args[1];
                            if (choice == "All")
                            {
                                LanguageHelper.ValidateFiles(query.Message.Chat.Id, query.Message.MessageId);
                                return;
                            }

                            menu = new InlineKeyboardMarkup();
                            var vlang = SelectLanguage(command, args, ref menu);
                            if (vlang == null)
                            {
                                buttons.Clear();
                                Bot.ReplyToCallback(query, GetLocaleString("WhatVariant", language, choice),
                                       replyMarkup: menu);
                                return;
                            }

                            //var menu = new ReplyKeyboardHide { HideKeyboard = true, Selective = true };
                            //Bot.SendTextMessage(id, "", replyToMessageId: update.Message.MessageId, replyMarkup: menu);
                            LanguageHelper.ValidateLanguageFile(query.Message.Chat.Id, vlang.FilePath, query.Message.MessageId);
                            return;
                        case "getlang":
                            if (choice == "All")
                            {
                                Bot.ReplyToCallback(query, "One moment...");
                                LanguageHelper.SendAllFiles(query.Message.Chat.Id);
                                return;
                            }

                            if (args[4] != "base" && args[3] == "All")
                            {
                                Bot.ReplyToCallback(query, "One moment...");
                                LanguageHelper.SendBase(choice, query.Message.Chat.Id);
                                return;
                            }
                            
                            menu = new InlineKeyboardMarkup();
                            var glang = SelectLanguage(command, args, ref menu);
                            if (glang == null)
                            {
                                buttons.Clear();
                                Bot.ReplyToCallback(query, GetLocaleString("WhatVariant", language, choice),
                                       replyMarkup: menu);
                                return;
                            }
                            Bot.ReplyToCallback(query, "One moment...");
                            LanguageHelper.SendFile(query.Message.Chat.Id, glang.Name);
                            break;
                        case "upload":
                            Console.WriteLine(choice);
                            if (choice == "current")
                            {
                                Bot.ReplyToCallback(query, "No action taken.");
                                return;
                            }
                            Helpers.LanguageHelper.UseNewLanguageFile(choice, query.Message.Chat.Id, query.Message.MessageId);
                            return;

                        case "vote":
                            //send it back to the game;
                            var node = Bot.Nodes.FirstOrDefault(x => x.ClientId.ToString() == args[1]);
                            node?.SendReply(query);
                            break;
                        case "lang":
                            //load up each file and get the names
                            var langs = Directory.GetFiles(Bot.LanguageDirectory).Select(x => new LangFile(x)).ToList();

                            buttons.Clear();
                            buttons.AddRange(langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => new InlineKeyboardButton(x, $"setlang|{groupid}|{x}|null|base")));

                            var baseMenu = new List<InlineKeyboardButton[]>();
                            for (var i = 0; i < buttons.Count; i++)
                            {
                                if (buttons.Count - 1 == i)
                                {
                                    baseMenu.Add(new[] { buttons[i] });
                                }
                                else
                                    baseMenu.Add(new[] { buttons[i], buttons[i + 1] });
                                i++;
                            }

                            menu = new InlineKeyboardMarkup(baseMenu.ToArray());


                            var curLang = langs.First(x => x.FileName == (grp?.Language ?? p.Language));
                            Bot.ReplyToCallback(query, GetLocaleString("WhatLang", language, curLang.Base), replyMarkup: menu);
                            break;
                        case "setlang":
                            menu = new InlineKeyboardMarkup();
                            var slang = SelectLanguage(command, args, ref menu, false);
                            if (slang == null)
                            {
                                buttons.Clear();
                                var curLangfilePath = Directory.GetFiles(Bot.LanguageDirectory).First(x => Path.GetFileNameWithoutExtension(x) == (grp?.Language ?? p.Language));
                                var curVariant = new LangFile(curLangfilePath).Variant;
                                Bot.ReplyToCallback(query, GetLocaleString("WhatVariant", language, curVariant),
                                    replyMarkup: menu);
                                return;
                            }

                            if (
                                Directory.GetFiles(Bot.LanguageDirectory)
                                    .Any(
                                        x =>
                                            String.Equals(Path.GetFileNameWithoutExtension(x), slang.FileName,
                                                StringComparison.InvariantCultureIgnoreCase)))
                            {
                                //now get the group
                                if (grp != null)
                                {
                                    grp.Language = slang.FileName;
                                    //check for any games running
                                    var ig = GetGroupNodeAndGame(groupid);

                                    ig?.LoadLanguage(slang.FileName);
                                    menu = GetConfigMenu(groupid);
                                    Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("LangSet", language, slang.Base + (String.IsNullOrWhiteSpace(slang.Variant) ? "" : ": " + slang.Variant)));
                                    Bot.ReplyToCallback(query, GetLocaleString("WhatToDo", language), replyMarkup: menu);
                                }
                                if (p != null)
                                {
                                    p.Language = slang.FileName;
                                    Bot.ReplyToCallback(query, GetLocaleString("LangSet", language, slang.Base + (String.IsNullOrWhiteSpace(slang.Variant) ? "" : ": " + slang.Variant)));
                                }
                            }
                            DB.SaveChanges();
                            break;
                        //case "online":
                        //    buttons.Add(new InlineKeyboardButton("Yes", $"setonline|{groupid}|show"));
                        //    buttons.Add(new InlineKeyboardButton("No", $"setonline|{groupid}|hide"));
                        //    buttons.Add(new InlineKeyboardButton("Cancel", $"setonline|{groupid}|cancel"));
                        //    menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                        //    Edit(query.Message.Chat.Id, query.Message.MessageId,
                        //        $"Do you want your group to be notified when the bot is online?\nCurrent: {grp.DisableNotification != false}",
                        //        replyMarkup: menu);
                        //    break;
                        //case "setonline":

                        //    grp.DisableNotification = (choice == "hide");
                        //    Bot.Api.AnswerCallbackQuery(query.Id,
                        //        $"Notification will {(grp.DisableNotification == true ? "not " : "")}be shown on startup");
                        //    Edit(query.Message.Chat.Id, query.Message.MessageId,
                        //        GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        //    DB.SaveChanges();
                        //    break;
                        case "flee":
                            buttons.Add(new InlineKeyboardButton(Yes, $"setflee|{groupid}|enable"));
                            buttons.Add(new InlineKeyboardButton(No, $"setflee|{groupid}|disable"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setflee|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("AllowFleeQ", language, grp.DisableFlee == false ? GetLocaleString("Allow", language) : GetLocaleString("Disallow", language)),
                                replyMarkup: menu);
                            break;
                        case "setflee":

                            grp.DisableFlee = (choice == "disable");
                            Bot.Api.AnswerCallbackQuery(query.Id,
                                   GetLocaleString("AllowFleeA", language, grp.DisableFlee == true ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "maxplayer":
                            buttons.Add(new InlineKeyboardButton("10", $"setmaxplayer|{groupid}|10"));
                            buttons.Add(new InlineKeyboardButton("15", $"setmaxplayer|{groupid}|15"));
                            buttons.Add(new InlineKeyboardButton("20", $"setmaxplayer|{groupid}|20"));
                            buttons.Add(new InlineKeyboardButton("25", $"setmaxplayer|{groupid}|25"));
                            buttons.Add(new InlineKeyboardButton("30", $"setmaxplayer|{groupid}|30"));
                            buttons.Add(new InlineKeyboardButton("35", $"setmaxplayer|{groupid}|35"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setmaxplayer|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("MaxPlayersQ", language, grp.MaxPlayers ?? Settings.MaxPlayers),
                                replyMarkup: menu);
                            break;
                        case "setmaxplayer":

                            grp.MaxPlayers = int.Parse(choice);
                            Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("MaxPlayersA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "roles":
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("Show", language), $"setroles|{groupid}|show"));
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("Hide", language), $"setroles|{groupid}|hide"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setroles|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("ShowRolesDeathQ", language, (grp.ShowRoles == false ? "Hidden" : "Shown")),
                                replyMarkup: menu);
                            break;
                        case "setroles":

                            grp.ShowRoles = (choice == "show");
                            Bot.Api.AnswerCallbackQuery(query.Id,
                                GetLocaleString("ShowRolesDeathA", language, grp.ShowRoles == false ? "hidden" : "shown"));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "mode":
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("NormalOnly", language), $"setmode|{groupid}|Normal"));
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("ChaosOnly", language), $"setmode|{groupid}|Chaos"));
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("PlayerChoice", language), $"setmode|{groupid}|Player"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setmode|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("GameModeQ", language, grp.Mode), replyMarkup: menu);
                            break;
                        case "setmode":

                            grp.Mode = choice;
                            Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("GameModeA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "endroles":
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("ShowNone", language), $"setendroles|{groupid}|None"));
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("ShowLiving", language), $"setendroles|{groupid}|Living"));
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("ShowAll", language), $"setendroles|{groupid}|All"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setendroles|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("ShowRolesEndQ", language, grp.ShowRolesEnd),
                                replyMarkup: menu);
                            break;
                        case "setendroles":
                            grp.ShowRolesEnd = choice;
                            Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("ShowRolesEndA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "day":
                            buttons.Add(new InlineKeyboardButton("90", $"setday|{groupid}|30"));
                            buttons.Add(new InlineKeyboardButton("120", $"setday|{groupid}|60"));
                            buttons.Add(new InlineKeyboardButton("150", $"setday|{groupid}|90"));
                            buttons.Add(new InlineKeyboardButton("180", $"setday|{groupid}|120"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setday|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("SetDayTimeQ", language, Settings.TimeDay + 60, grp.DayTime ?? Settings.TimeDay + 60),
                                replyMarkup: menu);
                            break;
                        case "setday":
                            grp.DayTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("SetDayTimeA", language, choice + 60));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "night":
                            buttons.Add(new InlineKeyboardButton("30", $"setnight|{groupid}|30"));
                            buttons.Add(new InlineKeyboardButton("60", $"setnight|{groupid}|60"));
                            buttons.Add(new InlineKeyboardButton("90", $"setnight|{groupid}|90"));
                            buttons.Add(new InlineKeyboardButton("120", $"setnight|{groupid}|120"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setnight|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("SetNightTimeQ", language, Settings.TimeNight, grp.NightTime ?? Settings.TimeNight),
                                replyMarkup: menu);
                            break;
                        case "setnight":

                            grp.NightTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("SetNightTimeA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "lynch":
                            buttons.Add(new InlineKeyboardButton("30", $"setlynch|{groupid}|30"));
                            buttons.Add(new InlineKeyboardButton("60", $"setlynch|{groupid}|60"));
                            buttons.Add(new InlineKeyboardButton("90", $"setlynch|{groupid}|90"));
                            buttons.Add(new InlineKeyboardButton("120", $"setlynch|{groupid}|120"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setlynch|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("SetLynchTimeQ", language, Settings.TimeLynch, grp.LynchTime ?? Settings.TimeLynch),
                                replyMarkup: menu);
                            break;
                        case "setlynch":
                            grp.LynchTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("SetLynchTimeA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "fool":
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("Allow", language), $"setfool|{groupid}|true"));
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("Disallow", language), $"setfool|{groupid}|false"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setfool|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("AllowFoolQ", language, grp.AllowFool == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)), replyMarkup: menu);
                            break;
                        case "setfool":

                            grp.AllowFool = (choice == "true");
                            Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("AllowFoolA", language, grp.AllowFool == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "tanner":
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("Allow", language), $"settanner|{groupid}|true"));
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("Disallow", language), $"settanner|{groupid}|false"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"settanner|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("AllowTannerQ", language, grp.AllowTanner == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)), replyMarkup: menu);
                            break;
                        case "settanner":

                            grp.AllowTanner = (choice == "true");
                            Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("AllowTannerA", language, grp.AllowTanner == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "cult":
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("Allow", language), $"setcult|{groupid}|true"));
                            buttons.Add(new InlineKeyboardButton(GetLocaleString("Disallow", language), $"setcult|{groupid}|false"));
                            buttons.Add(new InlineKeyboardButton(Cancel, $"setcult|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("AllowCultQ", language, grp.AllowCult == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)), replyMarkup: menu);
                            break;
                        case "setcult":
                            grp.AllowCult = (choice == "true");
                            Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("AllowCultA", language, grp.AllowCult == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "done":
                            Bot.ReplyToCallback(query,
                                GetLocaleString("ThankYou", language));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Bot.ReplyToCallback(query, ex.Message, false, true);
                }
            }
        }


        private static string[] GetParameters(string input)
        {
            return input.Contains(" ") ? new[] { input.Substring(1, input.IndexOf(" ")).Trim(), input.Substring(input.IndexOf(" ") + 1) } : new[] { input.Substring(1).Trim(), null };
        }

        internal static Task<Message> Send(string message, long id, bool clearKeyboard = false, InlineKeyboardMarkup customMenu = null, ParseMode parseMode = ParseMode.Html)
        {
            return Bot.Send(message, id, clearKeyboard, customMenu, parseMode);
        }

        
        internal static LangFile SelectLanguage(string command, string[] args, ref InlineKeyboardMarkup menu, bool addAllbutton = true)
        {
            var langs = Directory.GetFiles(Bot.LanguageDirectory).Select(x => new LangFile(x)).ToList();
            var isBase = args[4] == "base";
            if (isBase)
            {
                var variants = langs.Where(x => x.Base == args[2]).ToList();
                if (variants.Count() > 1)
                {
                    var buttons = new List<InlineKeyboardButton>();
                    buttons.AddRange(variants.Select(x => new InlineKeyboardButton(x.Variant, $"{command}|{args[1]}|{x.Base}|{x.Variant}|v")));
                    if (addAllbutton)
                        buttons.Insert(0, new InlineKeyboardButton("All", $"{command}|{args[1]}|{args[2]}|All|v"));

                    var twoMenu = new List<InlineKeyboardButton[]>();
                    for (var i = 0; i < buttons.Count; i++)
                    {
                        if (buttons.Count - 1 == i)
                        {
                            twoMenu.Add(new[] { buttons[i] });
                        }
                        else
                            twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                        i++;
                    }
                    menu = new InlineKeyboardMarkup(twoMenu.ToArray());

                    return null;
                }
                else                
                    return variants.First(); 
            }
            else
            {
                return langs.First(x => x.Base == args[2] && x.Variant == args[3]);
            }
        }


        internal static string GetLocaleString(string key, string language, params object[] args)
        {
            var files = Directory.GetFiles(Bot.LanguageDirectory);
            XDocument doc;
            var file = files.First(x => Path.GetFileNameWithoutExtension(x) == language);
            {
                doc = XDocument.Load(file);
            }
            var strings = doc.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key);
            if (strings == null)
            {
                //fallback to English
                strings = Bot.English.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key);
            }
            var values = strings.Descendants("value");
            var choice = Bot.R.Next(values.Count());
            var selected = values.ElementAt(choice);
            return String.Format(selected.Value.FormatHTML(), args).Replace("\\n", Environment.NewLine);
        }

        internal static Group MakeDefaultGroup(long groupid, string name, string createdBy)
        {
            return new Group
            {
                GroupId = groupid,
                Name = name,
                Language = "English",
                BotInGroup = true,
                ShowRoles = true,
                Mode = "Player",
                DayTime = Settings.TimeDay,
                LynchTime = Settings.TimeLynch,
                NightTime = Settings.TimeNight,
                AllowFool = true,
                AllowTanner = true,
                AllowCult = true,
                DisableFlee = false,
                MaxPlayers = 35,
                CreatedBy = createdBy
            };
        }

        internal static InlineKeyboardMarkup GetConfigMenu(long id)
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            //base menu
            //buttons.Add(new InlineKeyboardButton("Show Online Message", $"online|{id}"));
            buttons.Add(new InlineKeyboardButton("Change Language", $"lang|{id}"));
            buttons.Add(new InlineKeyboardButton("Show Roles On Death", $"roles|{id}"));
            buttons.Add(new InlineKeyboardButton("Show Roles At Game End", $"endroles|{id}"));
            buttons.Add(new InlineKeyboardButton("Allow Fleeing", $"flee|{id}"));
            buttons.Add(new InlineKeyboardButton("Set Max Players", $"maxplayer|{id}"));
            buttons.Add(new InlineKeyboardButton("Change Game Mode", $"mode|{id}"));
            buttons.Add(new InlineKeyboardButton("Set Day Timer", $"day|{id}"));
            buttons.Add(new InlineKeyboardButton("Set Lynch Timer", $"lynch|{id}"));
            buttons.Add(new InlineKeyboardButton("Set Night Timer", $"night|{id}"));
            buttons.Add(new InlineKeyboardButton("Allow Fool", $"fool|{id}"));
            buttons.Add(new InlineKeyboardButton("Allow Tanner", $"tanner|{id}"));
            buttons.Add(new InlineKeyboardButton("Allow Cult", $"cult|{id}"));
            buttons.Add(new InlineKeyboardButton("Done", $"done|{id}"));
            var twoMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    twoMenu.Add(new[] { buttons[i] });
                }
                else
                    twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }

            var menu = new InlineKeyboardMarkup(twoMenu.ToArray());
            return menu;
        }


        
        public static void InlineQueryReceived(object sender, InlineQueryEventArgs e)
        {
            
            var commands = new InlineCommand[]
            {
                new StatsInlineCommand(e.InlineQuery.From),
            };
            var q = e.InlineQuery;
            List<InlineCommand> choices;
            if (String.IsNullOrWhiteSpace(q.Query))
            {
                //show all commands available
                choices = commands.ToList();
            }
            else
            {
                //let's figure out what they wanted
                var com = q.Query;
                choices = commands.Where(command => command.Command.StartsWith(com) || Commands.ComputeLevenshtein(com, command.Command) < 3).ToList();
            }

            Bot.Api.AnswerInlineQuery(q.Id, choices.Select(c => new InlineQueryResultArticle()
            {
                Description = c.Description, Id = c.Command, Title = c.Command, InputMessageContent = new InputTextMessageContent
                {
                    DisableWebPagePreview = true, MessageText = c.Content, ParseMode = ParseMode.Html
                }
            }).Cast<InlineQueryResult>().ToArray(), 0, true);
        }
    }
}

