using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Newtonsoft.Json;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Handler;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;
using System.Threading;

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

            if (int.TryParse(args[1], out int did))
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

            var menu = UpdateHandler.GetConfigMenu(update.Message.Chat.Id);
            Bot.Api.SendTextMessageAsync(update.Message.From.Id, GetLocaleString("WhatToDo", GetLanguage(update.Message.From.Id)),
                replyMarkup: menu);
        }

        [Attributes.Command(Trigger = "uploadlang", LangAdminOnly = true)]
        public static void UploadLang(Update update, string[] args)
        {
            try
            {
                var id = update.Message.Chat.Id;
                if (update.Message.ReplyToMessage?.Type != MessageType.DocumentMessage)
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
                        update.Message.MessageId);
            }
            catch (Exception e)
            {
                Bot.Api.SendTextMessageAsync(update.Message.Chat.Id, e.Message, parseMode: ParseMode.Default);
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

                Bot.Api.SendTextMessageAsync(u.Message.Chat.Id, $"Player: {p.Name.FormatHTML()}\nCurrent Status: {status}\nPlayer first seen: {(firstSeen?.ToString("ddMMMyyyy H:mm:ss zzz").ToUpper() ?? "Hasn't played ever!")}", disableWebPagePreview: true, replyToMessageId: u.Message.MessageId, parseMode: ParseMode.Html);
            }

        }

        [Attributes.Command(Trigger = "validatelangs", GlobalAdminOnly = true)]
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


            List<InlineKeyboardCallbackButton> buttons = langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => new InlineKeyboardCallbackButton(x, $"validate|{update.Message.From.Id}|{x}|null|base")).ToList();
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
                Bot.Api.SendTextMessageAsync(update.Message.Chat.Id, "Validate which language?",
                    replyToMessageId: update.Message.MessageId, replyMarkup: menu);
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
            List<int> ids = new List<int>();
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
            //now get the idle kills
            using (var db = new WWContext())
            {
                foreach (var id in ids)
                {
                    var idles = db.GetIdleKills24Hours(id).FirstOrDefault() ?? 0;
                    //get the user
                    ChatMember user = null;
                    try
                    {
                        user = Bot.Api.GetChatMemberAsync(update.Message.Chat.Id, id).Result;
                    }
                    catch
                    {
                        // ignored
                    }

                    var str = $"{id} ({user?.User.FirstName})";
                    reply += GetLocaleString("IdleCount", GetLanguage(update.Message.Chat.Id), str, idles);
                    reply += "\n";
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
            if (!Regex.IsMatch(link, @"^(https?:\/\/)?t(elegram)?\.me\/joinchat\/([a-zA-Z0-9_\-]+)$"))
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
#if !BETA
            //get the user to add the achievement to
            //first, try by reply
            var id = 0;
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
                if (int.TryParse(param[0], out id))
                    achIndex = 1;
                else if (int.TryParse(param[1], out id))
                    achIndex = 0;

            }


            if (id != 0)
            {
                //try to get the achievement
                if (Enum.TryParse(param[achIndex], out Achievements a))
                {
                    //get the player from database
                    using (var db = new WWContext())
                    {
                        var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                        if (p != null)
                        {
                            if (p.Achievements == null)
                                p.Achievements = 0;
                            var ach = (Achievements)p.Achievements;
                            if (ach.HasFlag(a)) return; //no point making another db call if they already have it
                            ach = ach | a;
                            p.Achievements = (long)ach;
                            db.SaveChanges();
                            Send($"Achievement Unlocked!\n{a.GetName().ToBold()}\n{a.GetDescription()}", p.TelegramId);
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
#if !BETA
            //get the user to add the achievement to
            //first, try by reply
            var id = 0;
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
                if (int.TryParse(param[0], out id))
                    achIndex = 1;
                else if (int.TryParse(param[1], out id))
                    achIndex = 0;

            }


            if (id != 0)
            {
                //try to get the achievement
                Achievements a;
                if (Enum.TryParse(param[achIndex], out a))
                {
                    //get the player from database
                    using (var db = new WWContext())
                    {
                        var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                        if (p != null)
                        {
                            if (p.Achievements == null)
                                p.Achievements = 0;
                            var ach = (Achievements)p.Achievements;
                            if (!ach.HasFlag(a)) return; //no point making another db call if they already have it
                            ach &= ~a;
                            p.Achievements = (long)ach;
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
                Send($"{result}Accuracy score: {score}%\n\nDo you want to restore the account?", u.Message.Chat.Id, customMenu: new InlineKeyboardMarkup(new[] { new InlineKeyboardCallbackButton("Yes", $"restore|{oldP.TelegramId}|{newP.TelegramId}"), new InlineKeyboardCallbackButton("No", "restore|no") }));
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
                    var list = "Pending Review:\n";
                    foreach (var p in packs)
                    {
                        var pack = JsonConvert.DeserializeObject<CustomGifData>(p.CustomGifSet);
                        if (pack.Approved != null || !pack.Submitted) continue;
                        count++;
                        list += p.TelegramId + Environment.NewLine;
                        if (count == 10)
                            break;
                    }
                    if (count == 0)
                        list += "None!";
                    Send(list, u.Message.Chat.Id);
                }
                else
                {
                    var pid = int.Parse(args[1]);
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
                    Bot.Api.SendDocumentAsync(id, pack.CultWins, "Cult Wins");
                    Bot.Api.SendDocumentAsync(id, pack.LoversWin, "Lovers Win");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(id, pack.NoWinner, "No Winner");
                    Bot.Api.SendDocumentAsync(id, pack.SerialKillerWins, "SK Wins");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(id, pack.StartChaosGame, "Chaos Start");                 
                    Bot.Api.SendDocumentAsync(id, pack.StartGame, "Normal Start");      
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(id, pack.TannerWin, "Tanner Start");
                    Bot.Api.SendDocumentAsync(id, pack.VillagerDieImage, "Villager Eaten");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(id, pack.VillagersWin, "Village Wins");
                    Bot.Api.SendDocumentAsync(id, pack.WolfWin, "Single Wolf Wins");
                    Thread.Sleep(250);
                    Bot.Api.SendDocumentAsync(id, pack.WolvesWin, "Wolf Pack Wins");
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
            Bot.Api.EditMessageTextAsync(u.Message.Chat.Id, r.MessageId, msg, parseMode: ParseMode.Markdown);
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
                    var pid = int.Parse(parms[0]);
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
                    var id = u.Message.From.Id;
                    pack.Approved = true;
                    pack.ApprovedBy = id;
                    pack.NSFW = nsfw;
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
                    var pid = int.Parse(parms[0]);
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
    }
}
