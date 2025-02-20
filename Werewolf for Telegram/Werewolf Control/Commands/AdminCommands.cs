using Database;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Handler;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;

#pragma warning disable IDE0060 // Remove unused parameter

namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Attributes.Command(Trigger = "smite", GroupAdminOnly = true, Blockable = true, InGroupOnly = true)]
        public static void Smite(Update u, string[] args)
        {
            //if (u.Message.ReplyToMessage == null)
            //{
            //    Bot.Send(GetLocaleString("MustReplySmite",GetLanguage(u.Message.Chat.Id)).ToBold(), u.Message.Chat.Id);
            //    return;
            //}

            //check for reply
            if (u.Message.ReplyToMessage != null)
                //smite sender
                Bot.GetGroupNodeAndGame(u.Message.Chat.Id)?.SmitePlayer(u.Message.ReplyToMessage.From.Id);

            //get the names to smite
            foreach (var e in u.Message.Entities)
            {
                switch (e.Type)
                {
                    case MessageEntityType.Mention:
                        //get user
                        var username = u.Message.Text.Substring(e.Offset + 1, e.Length - 1);
                        using (var db = new WWContext())
                        {
                            var id = db.Players.FirstOrDefault(x => x.UserName == username)?.TelegramId ?? 0;
                            if (id != 0)
                                Bot.GetGroupNodeAndGame(u.Message.Chat.Id)?.SmitePlayer(id);
                        }
                        break;
                    case MessageEntityType.TextMention:
                        Bot.GetGroupNodeAndGame(u.Message.Chat.Id)?.SmitePlayer(e.User.Id);
                        break;
                }
            }

            if (long.TryParse(args[1], out long did))
                Bot.GetGroupNodeAndGame(u.Message.Chat.Id)?.SmitePlayer(did);

        }

        [Attributes.Command(Trigger = "config", GroupAdminOnly = true, InGroupOnly = true)]
        public static void Config(Update update, string[] args)
        {
            var id = update.Message.Chat.Id;

            //make sure the group is in the database
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                if (grp == null)
                {
                    grp = MakeDefaultGroup(id, update.Message.Chat.Title, "config");
                    db.Groups.Add(grp);
                }

                grp.BotInGroup = true;
                grp.UserName = update.Message.Chat.Username;
                grp.Name = update.Message.Chat.Title;
                grp.UpdateFlags();
                db.SaveChanges();
            }

            var language = GetLanguage(update.Message.From.Id);

            try
            {
                var menu = UpdateHandler.GetConfigMenu(update.Message.Chat.Id, language);
                Bot.Api.SendTextMessageAsync(chatId: update.Message.From.Id, text: GetLocaleString("WhatToDo", language),
                    replyMarkup: menu);
            }
            catch
            {
                RequestPM(update.Message.Chat.Id);
            }
        }

        [Attributes.Command(Trigger = "uploadlang", LangAdminOnly = true)]
        public static void UploadLang(Update update, string[] args)
        {
            try
            {
                var id = update.Message.Chat.Id;
                if (update.Message.ReplyToMessage?.Type != MessageType.Document)
                {
                    Send("Please reply to the file with /uploadlang", id);
                    return;
                }
                var filename = update.Message.ReplyToMessage.Document?.FileName;
                if (string.IsNullOrEmpty(filename) || !filename.ToLower().EndsWith(".xml"))
                {
                    Send("The file must be an XML file! (*.xml)", id);
                    return;
                }
                var fileid = update.Message.ReplyToMessage.Document?.FileId;
                if (fileid != null)
                    LanguageHelper.UploadFile(fileid, id,
                        update.Message.ReplyToMessage.Document.FileName,
                        update.Message.MessageId, update.Message.MessageThreadId);
            }
            catch (Exception e)
            {
                Bot.Api.SendTextMessageAsync(chatId: update.Message.Chat.Id, text: e.Message, parseMode: ParseMode.Html, messageThreadId: update.Message.MessageThreadId);
            }
        }

        [Attributes.Command(Trigger = "getban", GlobalAdminOnly = true)]
        public static void GetUserStatus(Update u, string[] a)
        {
            using (var db = new WWContext())
            {
                var p = u.GetTarget(db);
                var ban = db.GlobalBans.FirstOrDefault(x => x.TelegramId == p.TelegramId);
                var status = "";
                if (ban != null)
                {
                    status = $"<b>Banned for: {ban.Reason}</b>\nBy: {ban.BannedBy} on {ban.BanDate?.ToString("ddMMMyyyy H:mm:ss zzz").ToUpper()}\n";
                    var expire = (ban.Expires - DateTime.UtcNow);
                    if (expire > TimeSpan.FromDays(365))
                    {
                        status += "<b>Perm Ban</b>";
                    }
                    else
                    {
                        status += String.Format("Ban expiration: <b>{0:%d} days, {0:%h} hours, {0:%m} minutes</b>", expire);
                    }
                }
                else
                    status = "Not banned (in Werewolf)";
                var firstSeen = p.GamePlayers?.OrderBy(x => x.GameId).FirstOrDefault()?.Game?.TimeStarted;

                Bot.Api.SendTextMessageAsync(chatId: u.Message.Chat.Id, text: $"Player: {p.Name.FormatHTML()}\nCurrent Status: {status}\nPlayer first seen: {(firstSeen?.ToString("ddMMMyyyy H:mm:ss zzz").ToUpper() ?? "Hasn't played ever!")}", disableWebPagePreview: true, replyToMessageId: u.Message.MessageId, parseMode: ParseMode.Html, messageThreadId: u.Message.MessageThreadId);
            }

        }

        [Attributes.Command(Trigger = "validatelangs", LangAdminOnly = true)]
        public static void ValidateLangs(Update update, string[] args)
        {
            //var langs = Directory.GetFiles(Bot.LanguageDirectory)
            //                                            .Select(x => XDocument.Load(x)
            //                                                        .Descendants("language")
            //                                                        .First()
            //                                                        .Attribute("name")
            //                                                        .Value
            //                                            ).ToList();
            //langs.Insert(0, "All");

            //var buttons =
            //    langs.Select(x => new[] { new InlineKeyboardButton(x, $"validate|{update.Message.Chat.Id}|{x}") }).ToArray();
            //var menu = new InlineKeyboardMarkup(buttons.ToArray());
            //Bot.Api.SendTextMessageAsync(update.Message.Chat.Id, "Validate which language?",
            //    replyToMessageId: update.Message.MessageId, replyMarkup: menu);


            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();


            List<InlineKeyboardButton> buttons = langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => InlineKeyboardButton.WithCallbackData(x, $"validate|{update.Message.From.Id}|{x}|null|base")).ToList();
            //buttons.Insert(0, new InlineKeyboardButton("All", $"validate|{update.Message.From.Id}|All|null|base"));

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

            var menu = new InlineKeyboardMarkup(baseMenu.ToArray());
            try
            {
                Bot.Api.SendTextMessageAsync(chatId: update.Message.Chat.Id, text: "Validate which language?",
                    replyToMessageId: update.Message.MessageId, replyMarkup: menu, messageThreadId: update.Message.MessageThreadId);
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions)
                {
                    var x = ex as ApiRequestException;

                    Send(x.Message, update.Message.Chat.Id);
                }
            }
            catch (ApiRequestException ex)
            {
                Send(ex.Message, update.Message.Chat.Id);
            }
        }


        [Attributes.Command(Trigger = "getidles", GroupAdminOnly = true)]
        public static void GetIdles(Update update, string[] args)
        {
            //check user ids and such
            List<long> ids = new List<long>();
            foreach (var arg in args.Skip(1).FirstOrDefault()?.Split(' ') ?? new[] { "" })
            {
                if (int.TryParse(arg, out int id))
                {
                    ids.Add(id);
                }
            }

            //now check for mentions
            foreach (var ent in update.Message.Entities.Where(x => x.Type == MessageEntityType.TextMention))
            {
                ids.Add(ent.User.Id);
            }

            //check for reply
            if (update.Message.ReplyToMessage != null)
                ids.Add(update.Message.ReplyToMessage.From.Id);

            var reply = "";
            var language = GetLanguage(update.Message.Chat.Id);
            //now get the idle kills
            using (var db = new WWContext())
            {
                foreach (var id in ids)
                {
                    var idles = db.GetIdleKills24Hours(id).FirstOrDefault() ?? 0;
                    var groupidles = db.GetGroupIdleKills24Hours(id, update.Message.Chat.Id).FirstOrDefault() ?? 0;

                    //get the user
                    ChatMember user = null;
                    try
                    {
                        user = Bot.Api.GetChatMemberAsync(chatId: update.Message.Chat.Id, userId: id).Result;
                    }
                    catch
                    {
                        // ignored
                    }

                    var str = $"{id} ({user?.User.FirstName})";
                    reply += GetLocaleString("IdleCount", language, str, idles);
                    reply += " " + GetLocaleString("GroupIdleCount", language, groupidles) + "\n";
                }
            }

            Send(reply, update.Message.Chat.Id);
        }

        [Attributes.Command(Trigger = "remlink", GroupAdminOnly = true, InGroupOnly = true)]
        public static void RemLink(Update u, string[] args)
        {
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == u.Message.Chat.Id) ??
                          MakeDefaultGroup(u.Message.Chat.Id, u.Message.Chat.Title, "setlink");
                grp.GroupLink = null;
                db.SaveChanges();
            }

            Send($"Your group link has been removed.", u.Message.Chat.Id);
        }

        [Attributes.Command(Trigger = "setlink", GroupAdminOnly = true, InGroupOnly = true)]
        public static void SetLink(Update update, string[] args)
        {
            //args[1] should be the link

            //first, check if the group has a username
            if (!String.IsNullOrEmpty(update.Message.Chat.Username))
            {
                Send($"Your group link has already been set to https://telegram.me/{update.Message.Chat.Username}",
                    update.Message.Chat.Id);
                return;
            }

            //now check the args
            if (args.Length < 2 || String.IsNullOrEmpty(args[1]))
            {
                Send($"You must use /setlink with the link to the group (invite link)", update.Message.Chat.Id);
                return;
            }

            var link = args[1].Trim();
            if (!Regex.IsMatch(link, @"^(https?:\/\/)?t(elegram)?\.me\/(\+|joinchat\/)([a-zA-Z0-9_\-]+)$"))
            {
                Send("This is an invalid telegram join link.", update.Message.Chat.Id);
                return;
            }
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == update.Message.Chat.Id) ??
                          MakeDefaultGroup(update.Message.Chat.Id, update.Message.Chat.Title, "setlink");
                grp.GroupLink = link;
                db.SaveChanges();
            }

            Send($"Link set: <a href=\"{link}\">{update.Message.Chat.Title}</a>", update.Message.Chat.Id);
        }

        [Attributes.Command(Trigger = "addach", DevOnly = true)]
        public static void AddAchievement(Update u, string[] args)
        {
#if !RELEASE
            //get the user to add the achievement to
            //first, try by reply
            long id = 0;
            var achIndex = 0;
            var param = args[1].Split(' ');
            if (u.Message.ReplyToMessage != null)
            {
                var m = u.Message.ReplyToMessage;
                while (m.ReplyToMessage != null)
                    m = m.ReplyToMessage;
                //check for forwarded message

                id = m.From.Id;
                if (m.ForwardFrom != null)
                    id = m.ForwardFrom.Id;
            }
            else
            {
                //ok, check for a user mention
                var e = u.Message.Entities?.FirstOrDefault();
                if (e != null)
                {
                    switch (e.Type)
                    {
                        case MessageEntityType.Mention:
                            //get user
                            var username = u.Message.Text.Substring(e.Offset + 1, e.Length - 1);
                            using (var db = new WWContext())
                            {
                                id = db.Players.FirstOrDefault(x => x.UserName == username)?.TelegramId ?? 0;
                            }
                            break;
                        case MessageEntityType.TextMention:
                            id = e.User.Id;
                            break;
                    }
                    achIndex = 1;
                }
            }

            if (id == 0)
            {
                //check for arguments then
                if (long.TryParse(param[0], out id))
                    achIndex = 1;
                else if (long.TryParse(param[1], out id))
                    achIndex = 0;

            }


            if (id != 0)
            {
                //try to get the achievement
                if (Enum.TryParse(param[achIndex], out AchievementsReworked a))
                {
                    //get the player from database
                    using (var db = new WWContext())
                    {
                        var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                        if (p != null)
                        {
                            var ach = new BitArray(200);
                            if (p.NewAchievements != null)
                                ach = new BitArray(p.NewAchievements);
                            if (ach.HasFlag(a)) return; //no point making another db call if they already have it
                            ach = ach.Set(a);
                            p.NewAchievements = ach.ToByteArray();
                            db.SaveChanges();
                            var language = GetLanguage(p.TelegramId);
                            var msg = GetLocaleString("AchUnlocked", language) + Environment.NewLine;
                            msg += GetLocaleString(a.GetName(false), language).ToBold() + Environment.NewLine;
                            msg += GetLocaleString(a.GetDescription(false), language);
                            Send(msg, p.TelegramId);
                            //Send($"Achievement Unlocked!\n{a.GetName().ToBold()}\n{a.GetDescription()}", p.TelegramId);
                            Send($"Achievement {a} unlocked for {p.Name}", u.Message.Chat.Id);
                        }
                    }
                }
            }
#endif
        }

        [Attributes.Command(Trigger = "remach", DevOnly = true)]
        public static void RemAchievement(Update u, string[] args)
        {
#if !RELEASE
            //get the user to add the achievement to
            //first, try by reply
            long id = 0;
            var achIndex = 0;
            var param = args[1].Split(' ');
            if (u.Message.ReplyToMessage != null)
            {
                var m = u.Message.ReplyToMessage;
                while (m.ReplyToMessage != null)
                    m = m.ReplyToMessage;
                //check for forwarded message

                id = m.From.Id;
                if (m.ForwardFrom != null)
                    id = m.ForwardFrom.Id;
            }
            else
            {
                //ok, check for a user mention
                var e = u.Message.Entities?.FirstOrDefault();
                if (e != null)
                {
                    switch (e.Type)
                    {
                        case MessageEntityType.Mention:
                            //get user
                            var username = u.Message.Text.Substring(e.Offset + 1, e.Length - 1);
                            using (var db = new WWContext())
                            {
                                id = db.Players.FirstOrDefault(x => x.UserName == username)?.TelegramId ?? 0;
                            }
                            break;
                        case MessageEntityType.TextMention:
                            id = e.User.Id;
                            break;
                    }
                    achIndex = 1;
                }
            }

            if (id == 0)
            {
                //check for arguments then
                if (long.TryParse(param[0], out id))
                    achIndex = 1;
                else if (long.TryParse(param[1], out id))
                    achIndex = 0;

            }


            if (id != 0)
            {
                //try to get the achievement
                if (Enum.TryParse(param[achIndex], out AchievementsReworked a))
                {
                    //get the player from database
                    using (var db = new WWContext())
                    {
                        var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                        if (p != null)
                        {
                            var ach = new BitArray(200);
                            if (p.NewAchievements != null)
                                ach = new BitArray(p.NewAchievements);
                            if (!ach.HasFlag(a)) return; //no point making another db call if they already have it
                            ach = ach.Unset(a);
                            p.NewAchievements = ach.ToByteArray();
                            db.SaveChanges();

                            Send($"Achievement {a} removed from {p.Name}", u.Message.Chat.Id);
                        }
                    }
                }
            }
#endif
        }

        [Attributes.Command(Trigger = "restore", GlobalAdminOnly = true)]
        public static void RestoreAccount(Update u, string[] args)
        {
#if !BETA
            var score = 100;
            var result = "";
            int oldid, newid;
            var param = args[1].Split(' ');
            if (!int.TryParse(param[0], out oldid) || !int.TryParse(param[1], out newid))
            {
                //fail
                Send("usage: /restore <oldid> <newid>", u.Message.Chat.Id);
                return;
            }

            using (var db = new WWContext())
            {
                var oldP = db.Players.FirstOrDefault(x => x.TelegramId == oldid);
                var newP = db.Players.FirstOrDefault(x => x.TelegramId == newid);

                if (oldP == null || newP == null)
                {
                    Send("Account not found in database", u.Message.Chat.Id);
                    return;
                }
                if (db.GlobalBans.Any(x => x.TelegramId == oldid))
                {
                    Send("Old account was global banned!", u.Message.Chat.Id);
                    return;
                }
                if (oldid > newid || oldP.Id > newP.Id)
                {
                    score -= 30;
                    result += "Old account given is newer than new account\n";
                }

                if (oldP.GamePlayers.Max(x => x.GameId) > newP.GamePlayers.Min(x => x.GameId))
                {
                    score -= 30;
                    result += "Account games overlap - old account has played a game since new account started\n";
                }
                //TODO Check groups played on old account vs new account
                var oldGrp = (from grp in db.Groups
                              join g in db.Games on grp.Id equals g.GrpId
                              join gp in db.GamePlayers on g.Id equals gp.GameId
                              where gp.PlayerId == oldP.Id
                              select grp).Distinct();

                var newGrp = (from grp in db.Groups
                              join g in db.Games on grp.Id equals g.GrpId
                              join gp in db.GamePlayers on g.Id equals gp.GameId
                              where gp.PlayerId == newP.Id
                              select grp).Distinct();

                //compare groups
                var total = newGrp.Count();
                var likeness = newGrp.Count(x => oldGrp.Any(g => g.Id == x.Id));
                var groupLike = ((likeness * 100) / total);
                score -= 20 - ((groupLike * 20) / 100);
                result += $"Percent of new groups that were in old account: {groupLike}%\n";

                //TODO check names (username) likeness
                var undist = ComputeLevenshtein(oldP.UserName, newP.UserName);
                var dndist = ComputeLevenshtein(oldP.Name, newP.Name);


                if (undist == 0) dndist = 0;
                else
                {
                    score -= (undist + dndist) / 2;
                    result += $"\nLevenshtein Distince:\nUsernames: {undist}\nDisplay name: {dndist}\n\n";
                }


                //TODO check languages set
                if (oldP.Language != newP.Language)
                {
                    score -= 10;
                    result += "Account languages are set differently\n";
                }

                //TODO Send a result with the score, and buttons to approve or deny the account restore
                Send($"{result}Accuracy score: {score}%\n\nDo you want to restore the account?", u.Message.Chat.Id, customMenu: new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("Yes", $"restore|{oldP.TelegramId}|{newP.TelegramId}"), InlineKeyboardButton.WithCallbackData("No", "restore|no") }));
            }
#endif
        }

        [Attributes.Command(Trigger = "reviewgifs", GlobalAdminOnly = true, Blockable = true)]
        public static void ReviewGifs(Update u, string[] args)
        {
#if !BETA
            using (var db = new WWContext())
            {
                if (args[1] == null)
                {
                    var packs = db.Players.Where(x => x.CustomGifSet != null).ToList();
                    var count = 0;
                    var list = "<b>Pending Review:</b>\n";
                    foreach (var p in packs)
                    {
                        var pack = JsonConvert.DeserializeObject<CustomGifData>(p.CustomGifSet);
                        if (pack.Approved != null || !pack.Submitted) continue;
                        count++;
                        list += "<code>" + p.TelegramId + "</code>" + Environment.NewLine;
                        if (count == 10)
                            break;
                    }
                    if (count == 0)
                        list += "None!";
                    Send(list, u.Message.Chat.Id);
                }
                else
                {
                    var pid = long.Parse(args[1]);
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == pid);
                    if (p == null)
                    {
                        Send("Id not found.", u.Message.Chat.Id);
                        return;
                    }
                    var json = p.CustomGifSet;
                    if (String.IsNullOrEmpty(json))
                    {
                        Send("User does not have a custom gif pack", u.Message.Chat.Id);
                        return;
                    }
                    if (u.Message.Chat.Type != ChatType.Private)
                        Send("I will send you the gifs in private", u.Message.Chat.Id);

                    var pack = JsonConvert.DeserializeObject<CustomGifData>(json);
                    var id = u.Message.From.Id;
                    Send($"Sending gifs for {pid}", id);
                    Thread.Sleep(1000);
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.CultWins), caption: "Cult Wins");
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.LoversWin), caption: "Lovers Win");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.NoWinner), caption: "No Winner");
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.SerialKillerWins), caption: "SK Wins");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.StartChaosGame), caption: "Chaos Start");
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.StartGame), caption: "Normal Start");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.TannerWin), caption: "Tanner Win");
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.VillagerDieImage), caption: "Villager Eaten");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.VillagersWin), caption: "Village Wins");
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.WolfWin), caption: "Single Wolf Wins");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.WolvesWin), caption: "Wolf Pack Wins");
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.SKKilled), caption: "SK Killed");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.ArsonistWins), caption: "Arsonist Wins");
                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.BurnToDeath), caption: "Arsonist Burnt");
                    Thread.Sleep(500);
                    var msg = $"Approval Status: ";
                    switch (pack.Approved)
                    {
                        case null:
                            msg += "Pending";
                            break;
                        case true:
                            var by = db.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                            msg += "Approved By " + by.Name;
                            break;
                        case false:
                            var dby = db.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                            msg += "Disapproved By " + dby.Name + " for: " + pack.DenyReason;
                            break;
                    }
                    Bot.Send(msg, id);
                }
            }
#endif
        }

        [Attributes.Command(Trigger = "fi", GlobalAdminOnly = true)]
        public static void FullInfo(Update u, string[] a)
        {
            //this is a combo ping and runinfo, with even more information
            var ts = DateTime.UtcNow - u.Message.Date;
            var send = DateTime.UtcNow;
            var msg = "*Run information*\n" + Program.GetFullInfo();
            msg += $"\n*Time to receive*: {ts:mm\\:ss\\.ff}";
            var r = Bot.Send(msg, u.Message.Chat.Id, parseMode: ParseMode.Markdown).Result;
            ts = DateTime.UtcNow - send;
            msg += $"\n*Time to reply*: {ts:mm\\:ss\\.ff}";
            Bot.Api.EditMessageTextAsync(chatId: u.Message.Chat.Id, messageId: r.MessageId, text: msg, parseMode: ParseMode.Markdown);
        }

        private static Task[] DownloadGifFromJson(CustomGifData pack, Update u)
        {
            List<Task> downloadTasks = new List<Task>();
            if (!String.IsNullOrEmpty(pack.ArsonistWins))
                downloadTasks.Add(DownloadGif(pack.ArsonistWins, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.BurnToDeath))
                downloadTasks.Add(DownloadGif(pack.BurnToDeath, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.CultWins))
                downloadTasks.Add(DownloadGif(pack.CultWins, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.LoversWin))
                downloadTasks.Add(DownloadGif(pack.LoversWin, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.NoWinner))
                downloadTasks.Add(DownloadGif(pack.NoWinner, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.SerialKillerWins))
                downloadTasks.Add(DownloadGif(pack.SerialKillerWins, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.SKKilled))
                downloadTasks.Add(DownloadGif(pack.SKKilled, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.StartChaosGame))
                downloadTasks.Add(DownloadGif(pack.StartChaosGame, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.StartGame))
                downloadTasks.Add(DownloadGif(pack.StartGame, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.TannerWin))
                downloadTasks.Add(DownloadGif(pack.TannerWin, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.VillagerDieImage))
                downloadTasks.Add(DownloadGif(pack.VillagerDieImage, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.VillagersWin))
                downloadTasks.Add(DownloadGif(pack.VillagersWin, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.WolfWin))
                downloadTasks.Add(DownloadGif(pack.WolfWin, u.Message.Chat));
            if (!String.IsNullOrEmpty(pack.WolvesWin))
                downloadTasks.Add(DownloadGif(pack.WolvesWin, u.Message.Chat));
            return downloadTasks.ToArray();
        }
        private static async Task<bool> DownloadGif(string fileid, Chat chat, bool logErrors = true)
        {
            try
            {
                var path = Path.Combine(Settings.GifStoragePath, $"{fileid}.mp4");
                if (!System.IO.File.Exists(path))
                    using (var x = System.IO.File.OpenWrite(path))
                        await Bot.Api.DownloadFileAsync(filePath: (await Bot.Api.GetFileAsync(fileId: fileid)).FilePath, x);

                return true;
            }
            catch (Exception e)
            {
                if (logErrors) LogException(e, "Custom Gif", chat);
                return false;
            }
        }

        [Attributes.Command(Trigger = "approvegifs", GlobalAdminOnly = true, Blockable = true)]
        public static void ApproveGifs(Update u, string[] args)
        {
#if !BETA
            using (var db = new WWContext())
            {
                if (args[1] == null)
                {
                    Send("Please use /approvegifs <player id> <1|0 (nsfw)>", u.Message.Chat.Id);

                }
                else
                {

                    var parms = args[1].Split(' ');
                    if (parms.Length == 1)
                    {
                        Send("Please use /approvegifs <player id> <1|0 (nsfw)>", u.Message.Chat.Id);
                        return;
                    }
                    var pid = long.Parse(parms[0]);
                    var nsfw = parms[1] == "1";
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == pid);
                    if (p == null)
                    {
                        Send("Id not found.", u.Message.Chat.Id);
                        return;
                    }
                    var json = p.CustomGifSet;
                    if (String.IsNullOrEmpty(json))
                    {
                        Send("User does not have a custom gif pack", u.Message.Chat.Id);
                        return;
                    }

                    var pack = JsonConvert.DeserializeObject<CustomGifData>(json);
                    // save gifs for external access
                    new Task(() => Task.WhenAll(DownloadGifFromJson(pack, u))).Start();
                    // end
                    var id = u.Message.From.Id;
                    pack.Approved = true;
                    pack.ApprovedBy = id;
                    pack.NSFW = nsfw;
                    pack.Submitted = false;
                    var msg = $"Approval Status: ";
                    var by = db.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                    msg += "Approved By " + by.Name + "\nNSFW: " + pack.NSFW;
                    p.CustomGifSet = JsonConvert.SerializeObject(pack);
                    db.SaveChanges();
                    Bot.Send(msg, pid);
                    Bot.Send(msg, u.Message.Chat.Id);
                }
            }
#endif
        }

        [Attributes.Command(Trigger = "disapprovegifs", GlobalAdminOnly = true, Blockable = true)]
        public static void DisapproveGifs(Update u, string[] args)
        {
#if !BETA
            using (var db = new WWContext())
            {
                if (args[1] == null)
                {
                    Send("Please use /disapprovegifs <player id> <reason>", u.Message.Chat.Id);

                }
                else
                {

                    var parms = args[1].Split(' ');
                    if (parms.Length == 1)
                    {
                        Send("Please use /disapprovegifs <player id> <reason>", u.Message.Chat.Id);
                        return;
                    }
                    var pid = long.Parse(parms[0]);
                    var reason = args[1].Replace(parms[0] + " ", "");
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == pid);
                    if (p == null)
                    {
                        Send("Id not found.", u.Message.Chat.Id);
                        return;
                    }
                    var json = p.CustomGifSet;
                    if (String.IsNullOrEmpty(json))
                    {
                        Send("User does not have a custom gif pack", u.Message.Chat.Id);
                        return;
                    }

                    var pack = JsonConvert.DeserializeObject<CustomGifData>(json);
                    var id = u.Message.From.Id;
                    pack.Approved = false;
                    pack.ApprovedBy = id;
                    pack.Submitted = false;
                    pack.DenyReason = reason;
                    var msg = $"Approval Status: ";
                    var by = db.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                    msg += "Dispproved By " + by.Name + " for: " + pack.DenyReason;
                    p.CustomGifSet = JsonConvert.SerializeObject(pack);
                    db.SaveChanges();
                    Bot.Send(msg, pid);
                    Bot.Send(msg, u.Message.Chat.Id);
                }
            }
#endif
        }

        [Attributes.Command(Trigger = "fixgifs", GlobalAdminOnly = true)]
        public static void FixGifs(Update u, string[] args)
        {
#if !BETA
            string Prefix = "https://tgwerewolf.com/gifs/";
            List<string> FileIds = new List<string>();
            List<long> UserIds = new List<long>();

            if (u.Message.ReplyToMessage != null)
            {
                if (u.Message.ReplyToMessage.Document?.FileId != null)
                    FileIds.Add(u.Message.ReplyToMessage.Document.FileId);

                foreach (var e in u.Message.ReplyToMessage.Entities?.Where(x => x.Type == MessageEntityType.Url || x.Type == MessageEntityType.TextLink))
                {
                    var url = e.Url ?? u.Message.ReplyToMessage.Text?.Substring(e.Offset, e.Length);

                    if (url.StartsWith(Prefix))
                        FileIds.Add(url.Substring(Prefix.Length));
                }

                if (u.Message.ReplyToMessage.ForwardFrom != null)
                {
                    UserIds.Add(u.Message.ReplyToMessage.ForwardFrom.Id);
                }
            }

            foreach (var e in u.Message.Entities?.Where(x => x.Type == MessageEntityType.Url || x.Type == MessageEntityType.TextLink))
            {
                var url = e.Url ?? u.Message.Text?.Substring(e.Offset, e.Length);

                if (url.StartsWith(Prefix))
                    FileIds.Add(url.Substring(Prefix.Length));
            }

            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                if (int.TryParse(args[1], out int id))
                {
                    UserIds.Add(id);
                }
                else FileIds.Add(args[1]);
            }

            if (UserIds.Any())
            {
                using (var db = new WWContext())
                {
                    foreach (long userid in UserIds)
                    {
                        var p = db.Players.FirstOrDefault(x => x.TelegramId == userid);
                        if (p != null && p.CustomGifSet != null)
                        {
                            var json = p.CustomGifSet;
                            var pack = JsonConvert.DeserializeObject<CustomGifData>(json);

                            if (pack.ArsonistWins != null) FileIds.Add(pack.ArsonistWins);
                            if (pack.BurnToDeath != null) FileIds.Add(pack.BurnToDeath);
                            if (pack.CultWins != null) FileIds.Add(pack.CultWins);
                            if (pack.LoversWin != null) FileIds.Add(pack.LoversWin);
                            if (pack.NoWinner != null) FileIds.Add(pack.NoWinner);
                            if (pack.SerialKillerWins != null) FileIds.Add(pack.SerialKillerWins);
                            if (pack.SKKilled != null) FileIds.Add(pack.SKKilled);
                            if (pack.StartChaosGame != null) FileIds.Add(pack.StartChaosGame);
                            if (pack.StartGame != null) FileIds.Add(pack.StartGame);
                            if (pack.TannerWin != null) FileIds.Add(pack.TannerWin);
                            if (pack.VillagerDieImage != null) FileIds.Add(pack.VillagerDieImage);
                            if (pack.VillagersWin != null) FileIds.Add(pack.VillagersWin);
                            if (pack.WolfWin != null) FileIds.Add(pack.WolfWin);
                            if (pack.WolvesWin != null) FileIds.Add(pack.WolvesWin);
                        }
                    }
                }
            }


            FileIds = FileIds.Distinct().ToList();
            List<Task<bool>> DownloadTasks = new List<Task<bool>>();

            foreach (var fileId in FileIds)
            {
                var path = Path.Combine(Settings.GifStoragePath, $"{fileId}.mp4");
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    DownloadTasks.Add(DownloadGif(fileId, u.Message.Chat, true));
                }
            }

            new Thread(async () =>
            {
                string messageText = $"GIF Fix attempt completed!\n\n";

                var results = await Task.WhenAll(DownloadTasks);
                for (int i = 0; i < FileIds.Count; i++)
                {
                    if (results[i]) messageText += $"<a href=\"{Prefix}{FileIds[i]}.mp4\">({i}) Successfully fixed!</a>\n";
                    else messageText += $"<a href=\"{Prefix}{FileIds[i]}.mp4\">({i}) Failed!</a>\n";
                }

                await Bot.Send(messageText, u.Message.Chat.Id);
            }).Start();
#endif
        }
    }
}
