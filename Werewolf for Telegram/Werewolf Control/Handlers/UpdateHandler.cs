using Database;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;
using System.Collections;
using Shared;
using Telegram.Bot;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
namespace Werewolf_Control.Handler
{
    internal static class UpdateHandler
    {
        internal static Dictionary<long, SpamDetector> UserMessages = new Dictionary<long, SpamDetector>();

        internal static HashSet<long> SpamBanList = new HashSet<long>
        {

        };
        internal static List<GlobalBan> BanList = new List<GlobalBan>();

        internal static bool SendGifIds = false;
        public static void UpdateReceived(ITelegramBotClient bot, Update update)
        {
            new Task(() => { HandleUpdate(update); }).Start();
        }

        private static bool AddCount(long id, Message m)
        {
            try
            {
                if (!UserMessages.ContainsKey(id))
                    UserMessages.Add(id, new SpamDetector { Messages = new HashSet<UserMessage>() });

                var shouldReply = (UserMessages[id].Messages.Where(x => x.Replied).OrderByDescending(x => x.Time).FirstOrDefault()?.Time ?? DateTime.MinValue) <
                       DateTime.UtcNow.AddSeconds(-4);

                UserMessages[id].Messages.Add(new UserMessage(m) { Replied = shouldReply });
                return !shouldReply;

            }
            catch
            {
                // ignored
                return false;
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

                            var expireTime = DateTime.UtcNow;
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
                                BanDate = DateTime.UtcNow,
                                Name = name
                            });
                        }
                        SpamBanList.Clear();
                        db.SaveChanges();

                        //now refresh the list
                        var list = db.GlobalBans.ToList();
#if RELEASE
                        for (var i = list.Count - 1; i >= 0; i--)
                        {
                            if (list[i].Expires > DateTime.UtcNow) continue;
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
                            temp[key].Messages.RemoveWhere(x => x.Time < DateTime.UtcNow.AddMinutes(-1));

                            //comment this out - if we remove it, it doesn't keep the warns
                            //if (temp[key].Messages.Count == 0)
                            //{
                            //    temp.Remove(key);
                            //    continue;
                            //}
                            //now count, notify if limit hit
                            if (temp[key].Messages.Count() >= 10) // 20 in a minute
                            {
                                temp[key].Warns++;
                                if (temp[key].Warns < 2 && temp[key].Messages.Count < 20)
                                {
                                    Send($"Please do not spam me. Next time is automated ban.", key);
                                    //Send($"User {key} has been warned for spamming: {temp[key].Warns}\n{temp[key].Messages.GroupBy(x => x.Command).Aggregate("", (a, b) => a + "\n" + b.Count() + " " + b.Key)}",
                                    //    Para);
                                    continue;
                                }
                                if ((temp[key].Warns >= 3 || temp[key].Messages.Count >= 20) & !temp[key].NotifiedAdmin)
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
                        catch
                        {
                        }
                    }
                    UserMessages = temp;
                }
                catch
                {
                }
                Thread.Sleep(2000);
            }
        }

        internal static void HandleUpdate(Update update)
        {
            {
                if (update.Message == null || update.Message.From.Id == 777000 /*Channel Post*/) return;

                // Dirty hack for a dirtier "feature" by Telegram:
                // EVERY message in a sub-forum counts as reply to the original forum creation message
                // This doesn't make any sense at all so we just disregard these replies.
                if (update.Message.ReplyToMessage != null && (update.Message.ReplyToMessage.Type == MessageType.ForumTopicCreated || update.Message.ReplyToMessage.Type == MessageType.ForumTopicReopened))
                {
                    update.Message.ReplyToMessage = null;
                }

                // Another dirty hack because of a weird design choice
                // Not only messages in topics have a message thread ID, but also reply threads in ordinary supergroups
                // Which in itself would be fine, but passing such a message thread ID as message_thread_id when making
                // requests is an error. That's only allowed for forum groups. So let's disregard that thread ID otherwise
                if (update.Message.MessageThreadId.HasValue && !(update.Message.Chat.IsForum ?? false))
                {
                    update.Message.MessageThreadId = null;
                }


#if !DEBUG
                //ignore previous messages
                if ((update.Message?.Date ?? DateTime.MinValue) < DateTime.Now.AddSeconds(-10))
                    return; //toss it
#endif

                var id = update.Message.Chat.Id;

#if DEBUG
                if (!new[] { -1001341772435 /*alphatest*/, -1001094155678 /*staff*/, -1001098399855 /*errorlog*/,
                    -1001077134233 /*developer team chat*/, -1001135292560 /*para test chat*/}.Contains(id))
                {
                    try
                    {
                        Bot.Api.LeaveChatAsync(chatId: update.Message.Chat.Id).Wait();
                    }
                    catch
                    {
                        //ignored
                    }
                }
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
                //
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
                bool block = new[] { Settings.SupportChatId, Settings.PersianSupportChatId, Settings.TranslationChatId }.Contains(id);

#if !DEBUG
                try
#endif
                {
                    Group grp;
                    switch (update.Message.Type)
                    {
                        case MessageType.Unknown:
                            break;
                        case MessageType.Text:
                            if (update.Message.Text.StartsWith("!") || update.Message.Text.StartsWith("/"))
                            {
                                var isAnonymousSender = update.Message.SenderChat != null;
                                var isAnonymousAdmin = isAnonymousSender && (update.Message.SenderChat.Id == update.Message.Chat.Id);

                                if (BanList.Any(x => x.TelegramId == (update.Message?.From?.Id ?? 0)) ||
                                    SpamBanList.Contains(update.Message?.From?.Id ?? 0))
                                {
                                    return;
                                }

                                var args = GetParameters(update.Message.Text);
                                args[0] = args[0].ToLower().Replace("@" + Bot.Me.Username.ToLower(), "");
                                //command is args[0]
                                if (args[0].StartsWith("about") && !isAnonymousSender) // Anonymous admin, through @GroupAnonymousBot)
                                {
                                    var reply = Commands.GetAbout(update, args);
                                    if (!String.IsNullOrEmpty(reply))
                                    {

                                        if (AddCount(update.Message.From.Id, update.Message)) return;
                                        try
                                        {
                                            Bot.MessagesProcessed++;
                                            var result = Send(reply, update.Message.From.Id).Result;
                                            //if (update.Message.Chat.Type != ChatType.Private)
                                            //    Send(
                                            //        GetLocaleString("SentPrivate", GetLanguage(update.Message.From.Id)),
                                            //        update.Message.Chat.Id);

                                        }
                                        catch
                                        {
                                            Bot.MessagesProcessed++;
                                            Commands.RequestPM(update.Message.Chat.Id);
                                        }
                                    }
                                    return;
                                }

                                //check for the command

#region More optimized code

                                var command = Bot.Commands.FirstOrDefault(
                                    x =>
                                        String.Equals(x.Trigger, args[0],
                                            StringComparison.InvariantCultureIgnoreCase));
                                if (command != null)
                                {
                                    Bot.MessagesProcessed++;
#if RELEASE2
                                    Send($"Bot 2 is retiring.  Please switch to @werewolfbot", update.Message.Chat.Id);
                                    if (update.Message.Chat.Type != ChatType.Private)
                                    {
                                        Thread.Sleep(1000);
                                        Bot.Api.LeaveChat(update.Message.Chat.Id);
                                    }
#endif
                                    if (command.AllowAnonymousAdmins ?
                                        isAnonymousSender && !isAnonymousAdmin :
                                        isAnonymousSender) // Anonymous admin, or channel
                                    {
                                        Send(GetLocaleString("ExitAnonymousMode", GetLanguage(update.Message.Chat.Id)), update.Message.Chat.Id);
                                        return;
                                    }
                                    if (AddCount(update.Message.From.Id, update.Message)) return;
                                    //check that we should run the command
                                    if (block && command.Blockable)
                                    {
                                        if (id == Settings.SupportChatId)
                                            Send(
                                                "No games in support chat!\nIf you want to play, find a group in the /grouplist.",
                                                id);
                                        else if (id == Settings.PersianSupportChatId)
                                            Send("اینجا گروه پشتیبانیه نه بازی، لطفا دکمه استارت رو نزنید.", id);
                                        else if (id == Settings.TranslationChatId)
                                            Send("No games in translation group!", id);
                                        else if (id == Settings.BetaReportingChatId)
                                            Send("No games in Beta Reporting Group!", id);
                                        return;
                                    }
                                    if (command.DevOnly & !UpdateHelper.Devs.Contains(update.Message.From.Id))
                                    {
                                        Send(GetLocaleString("NotPara", GetLanguage(id)), id);
                                        return;
                                    }
                                    if (command.LangAdminOnly)
                                    {
                                        if (!UpdateHelper.IsLangAdmin(update.Message.From.Id) && !UpdateHelper.IsGlobalAdmin(update.Message.From.Id))
                                        {
                                            Send(GetLocaleString("NotGlobalAdmin", GetLanguage(id)), id);
                                            return;
                                        }
                                    }
                                    if (command.GlobalAdminOnly)
                                    {
                                        if (!UpdateHelper.IsGlobalAdmin(update.Message.From.Id))
                                        {
                                            if (command.Trigger != "user") Send(GetLocaleString("NotGlobalAdmin", GetLanguage(id)), id);
                                            return;
                                        }
                                    }
                                    if (command.InGroupOnly & update.Message.Chat.Type == ChatType.Private)
                                    {
                                        Send(GetLocaleString("GroupCommandOnly", GetLanguage(id)), id);
                                        return;
                                    }
                                    if (command.GroupAdminOnly & !isAnonymousAdmin & !UpdateHelper.IsGroupAdmin(update) &
                                        !UpdateHelper.Devs.Contains(update.Message.From.Id) &
                                        !UpdateHelper.IsGlobalAdmin(update.Message.From.Id))
                                    {
                                        Send(GetLocaleString("GroupAdminOnly", GetLanguage(update.Message.Chat.Id)),
                                            id);
                                        return;
                                    }
                                    Bot.CommandsReceived++;
                                    command.Method.Invoke(update, args);
                                }


#endregion
                            }
                            //else if (update.Message.Chat.Type == ChatType.Private &&
                            //         update.Message?.ReplyToMessage?.Text ==
                            //         "Please reply to this message with your Telegram authorization code" &&
                            //         update.Message.From.Id == UpdateHelper.Devs[0])
                            //{
                            //    CLI.AuthCode = update.Message.Text;
                            //}
                            else if (update.Message.Chat.Type == ChatType.Private &&
                                     (update.Message?.ReplyToMessage?.From?.Id ?? 0) == Bot.Me.Id &&
                                     (update.Message?.ReplyToMessage?.Text?.Contains(
                                          "Please enter a whole number, in US Dollars (USD)") ?? false))
                            {
                                Bot.MessagesProcessed++;
                                Commands.ValidateDonationAmount(update.Message);
                            }

                            else if (update.Message.Chat.Type == ChatType.Private &&
                                     (update.Message?.ReplyToMessage?.From?.Id ?? 0) == Bot.Me.Id &&
                                     (update.Message?.ReplyToMessage?.Text?.Contains(
                                          "Please enter a whole number, in TON (XTR)") ?? false))
                            {
                                Bot.MessagesProcessed++;
                                Commands.ValidateDonationAmountNew(update.Message);
                            }
                            break;
                        //case MessageType.Animation:
                            
                        //    break;
                        case MessageType.Document:
                        case MessageType.Animation:
                            if (update.Message.Animation != null)
                            {
                                if (UpdateHelper.Devs.Contains(update.Message.From.Id) && SendGifIds)
                                {
                                    Bot.MessagesProcessed++;
                                    var doc = update.Message.Animation;
                                    Send(doc.FileId, update.Message.Chat.Id);
                                }
                                else if (update.Message.Chat.Type == ChatType.Private &&
                                         (update.Message?.ReplyToMessage?.From?.Id ?? 0) == Bot.Me.Id &&
                                         (update.Message?.ReplyToMessage?.Text?.Contains(
                                              "send me the GIF you want to use for this situation, as a reply") ??
                                          false))
                                {
                                    Bot.MessagesProcessed++;
                                    Commands.AddGif(update.Message);
                                }
                            }


                            else if (update.Message.Document?.MimeType?.ToLower().Equals("image/gif") ?? false)
                            {
                                if (UpdateHelper.Devs.Contains(update.Message.From.Id) && SendGifIds)
                                {
                                    Bot.MessagesProcessed++;
                                    Send("This is an old GIF, that is still in .gif format and not in .mp4 format. Please try reuploading it.", update.Message.Chat.Id);
                                }
                                else if (update.Message.Chat.Type == ChatType.Private &&
                                         (update.Message?.ReplyToMessage?.From?.Id ?? 0) == Bot.Me.Id &&
                                         (update.Message?.ReplyToMessage?.Text?.Contains(
                                              "send me the GIF you want to use for this situation, as a reply") ??
                                          false))
                                {
                                    Bot.MessagesProcessed++;
                                    Commands.AddGif(update.Message);
                                }
                            }
                            break;
                        case MessageType.ChatMemberLeft:
                        case MessageType.ChatMembersAdded:
                        case MessageType.MigratedToSupergroup:
                            using (var DB = new WWContext())
                            {
                                Bot.MessagesProcessed++;
                                id = update.Message.Chat.Id;
                                var m = update.Message;
                                if (m.MigrateFromChatId != 0)
                                {
                                    grp = DB.Groups.FirstOrDefault(x => x.GroupId == m.MigrateFromChatId);
                                    if (grp == null) return;
                                    grp.GroupId = m.Chat.Id;
                                    DB.SaveChanges();
                                    return;
                                }
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
                                if (m.NewChatMembers?[0]?.Id == Bot.Me.Id)
                                {
                                    //added to a group
                                    grp = DB.Groups.FirstOrDefault(x => x.GroupId == id);
                                    if (grp == null)
                                    {
                                        grp = Commands.MakeDefaultGroup(id, update.Message.Chat.Title, "NewChatMember");
                                        DB.Groups.Add(grp);
                                        DB.SaveChanges();
                                        grp = DB.Groups.FirstOrDefault(x => x.GroupId == id);
                                    }
                                    grp.BotInGroup = true;
                                    grp.UserName = update.Message.Chat.Username;
                                    grp.Name = update.Message.Chat.Title;
                                    DB.SaveChanges();

                                    var msg =
                                        $"You've just added Werewolf Moderator!  Use /config (group admins) to configure group settings.   If you need assistance, join the [support channel](https://telegram.me/greywolfsupport)";
                                    msg += Environment.NewLine +
                                           "For updates on what is happening, join the dev channel @greywolfdev" +
                                           Environment.NewLine +
                                           "Full information is available on the [website](http://www.tgwerewolf.com)";
                                    Send(msg, id, parseMode: ParseMode.Markdown);

#if BETA
                                    Send(
                                        "*IMPORTANT NOTE- THIS IS A BETA BOT.  EXPECT BUGS, EXPECT SHUTDOWNS, EXPECT.. THE UNEXPECTED!*",
                                        id, parseMode: ParseMode.Markdown);
#endif
                                }
                                /*
                                 * Executrix will do this job for now, that will hopefully work better than this did before
                                 * 
                                else if (m.NewChatMember != null && m.Chat.Id == Settings.VeteranChatId)
                                {
                                    var uid = m.NewChatMember.Id;
                                    //check that they are allowed to join.
                                    var p = DB.Players.FirstOrDefault(x => x.TelegramId == uid);
                                    var gamecount = p?.GamePlayers.Count ?? 0;
                                    if (gamecount >= 500)
                                    {
                                        Send($"{m.NewChatMember.FirstName} has played {gamecount} games", m.Chat.Id);
                                        return;
                                    }
                                    //user has not reach veteran
                                    Send(
                                        $"{m.NewChatMember.FirstName} removed, as they have not unlocked veteran ({gamecount} games played, need 500)",
                                        m.Chat.Id);
                                    Commands.KickChatMember(Settings.VeteranChatId, uid);
                                }
                                */

                                else if (m.NewChatMembers?[0] != null && (m.Chat.Id == Settings.SupportChatId || m.Chat.Id == Settings.PersianSupportChatId))
                                {
                                    var uid = m.NewChatMembers[0].Id;
                                    var p = DB.GlobalBans.FirstOrDefault(x => x.TelegramId == uid);
                                    if (p != null)
                                    {
                                        var result =
                                            $"<b>PLAYER IS CURRENTLY BANNED</b>\nReason: {p.Reason}\nBanned on: {p.BanDate}\nBanned by: {p.BannedBy}\n";
                                        if (p.Expires < DateTime.UtcNow.AddYears(5))
                                        {
                                            var expiry = (p.Expires - DateTime.UtcNow);
                                            result +=
                                                $"Ban will be lifted in {expiry.Days} days, {expiry.Hours} hours, and {expiry.Minutes} minutes\n";
                                        }
                                        else
                                            result += $"This ban is permanent.\n";
                                        Send(result, m.Chat.Id);
                                    }
                                }
                            }
                            break;
                        case MessageType.SuccessfulPayment:
                            Bot.MessagesProcessed++;
                            HandleSuccessfulPayment(update.Message);
                            break;
                        default:
                            break;
                            //throw new ArgumentOutOfRangeException();
                    }
                }
#if !DEBUG
                catch (AggregateException e)
                {
                    var ex = e.InnerExceptions[0];
                    while (ex.InnerException != null)
                        ex = ex.InnerException;

                    Send(ex.Message, id);
                    Send($"Error at chatId <code>{id.ToString()}</code>: {ex.Message}\n{update.Message?.Text}", Settings.ErrorGroup);
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                        ex = ex.InnerException;

                    Send(ex.Message, id);
                    Send($"Error at chatId <code>{id.ToString()}</code>: {ex.Message}\n{update.Message?.Text}", Settings.ErrorGroup);
                }
#endif
            }
        }

        private static void HandleSuccessfulPayment(Message message)
        {
            // Donations Re-enable preparation 2025-08-08
            if (message.Chat.Id == 106665913)
            {
                var q2 = message.SuccessfulPayment;
                var amt2 = q2.TotalAmount;
                Bot.Send($"Successfully received {amt2} TON from you! YAY!", message.From.Id);
                return;
            }
            var q = message.SuccessfulPayment;
            //get the amount paid
            var amt = q.TotalAmount / 100;

            using (var db = new WWContext())
            {
                //get the player
                var p = db.Players.FirstOrDefault(x => x.TelegramId == message.From.Id);
                if (p == null)
                {
                    //wtf??
                    Bot.Send($"Successfully received ${amt} from you! YAY!\n\nHowever, we do not see any record of you in our database, so we can't record it.  Please message @ParaCode with this information, and a screenshot", message.From.Id);
                    return;
                }
                if (p.DonationLevel == null)
                    p.DonationLevel = 0;
                p.DonationLevel += amt;
                var level = p.DonationLevel ?? 0;
                var badge = "";
                if (level >= 100)
                    badge += " 🥇";
                else if (level >= 50)
                    badge += " 🥈";
                else if (level >= 10)
                    badge += " 🥉";
                if (p.Founder ?? false)
                    badge += "💎";

                Bot.Send($"Successfully received ${amt} from you! YAY!\nTotal Donated: ${level}\nCurrent Badge (ingame): {badge}", message.From.Id);
                //check to see how many people have purchased gif packs

                if (level > 10)
                {
                    var packs = db.Players.Count(x => x.GifPurchased == true);
                    if (packs >= 100)
                    {
                        //do nothing, they didn't unlock it.
                    }
                    else
                    {
                        p.GifPurchased = true;
                        CustomGifData data;
                        var json = p.CustomGifSet;
                        if (String.IsNullOrEmpty(json))
                            data = new CustomGifData();
                        else
                            data = JsonConvert.DeserializeObject<CustomGifData>(json);
                        if (!data.HasPurchased)
                        {
                            Bot.Send(
                                "Congratulations! You have unlocked Custom Gif Packs :)\nUse /customgif to build your pack, /submitgif to submit for approval",
                                message.From.Id);
                        }
                        data.HasPurchased = true;

                        json = JsonConvert.SerializeObject(data);
                        p.CustomGifSet = json;
                    }
                }
                db.SaveChanges();
            }
        }

        public static void PreCheckoutReceived(ITelegramBotClient botClient, PreCheckoutQuery preCheckoutQuery)
        {
            new Task(() => { HandlePayment(preCheckoutQuery); }).Start();
        }

        private static void HandlePayment(PreCheckoutQuery q)
        {
            Bot.Api.AnswerPreCheckoutQueryAsync(q.Id).Wait();
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

        private static string[] nonCommandsList = new[] { "vote", "getlang", "validate", "setlang", "groups", "status", "done", "stopwaiting" };

        public static void CallbackReceived(ITelegramBotClient bot, CallbackQuery query)
        {
            new Task(() => { HandleCallback(query); }).Start();
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

                    if (args[0] == "donatetg")
                    {
                        Commands.GetDonationInfo(query);
                        return;
                    }

                    if (args[0] == "donatetgnew")
                    {
                        Commands.GetDonationInfoNew(query);
                        return;
                    }

                    if (args[0] == "xsolla")
                    {
                        Commands.GetXsollaLink(query);
                        return;
                    }

                    if (args[0] == "cancel")
                    {
                        Bot.Api.DeleteMessageAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId);
                        return;
                    }

                    if (args[0] == "customgif")
                    {
                        Commands.RequestGif(query);
                        return;
                    }

                    //first off, if it's a game, send it to the node.
                    if (args[0] == "vote")
                    {
                        var node = Bot.Nodes.FirstOrDefault(x => x.ClientId.ToString() == args[1]);
                        node?.SendReply(query, args[2]);
                        return;
                    }
                    if (new[] { "reviewgifs", "approvesfw", "approvensfw", "dismiss" }.Contains(args[0]))
                    {
                        if (UpdateHelper.IsGlobalAdmin(query.From.Id))
                        {
                            CustomGifData pack;
                            Player by;
                            string json;
                            //get player target
                            var pid = long.Parse(args[1]);
                            var tplayer = DB.Players.FirstOrDefault(x => x.TelegramId == pid);
                            switch (args[0])
                            {
                                case "reviewgifs":
                                    json = tplayer.CustomGifSet;
                                    if (String.IsNullOrEmpty(json))
                                    {
                                        Send("User does not have a custom gif pack", query.Message.Chat.Id);
                                        return;
                                    }
                                    if (query.Message.Chat.Type != ChatType.Private)
                                        Send("I will send you the gifs in private", query.Message.Chat.Id);

                                    pack = JsonConvert.DeserializeObject<CustomGifData>(json);
                                    var id = query.From.Id;
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
                                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.TannerWin), caption: "Tanner Wins");
                                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.VillagerDieImage), caption: "Villager Eaten");
                                    Thread.Sleep(250);
                                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.VillagersWin), caption: "Village Wins");
                                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.WolfWin), caption: "Single Wolf Wins");
                                    Thread.Sleep(250);
                                    Bot.Api.SendDocumentAsync(chatId: id, document: new InputFileId(pack.WolvesWin), caption: "Wolf new InputFileId(pack.Wins)");
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
                                            by = DB.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                                            msg += "Approved By " + by.Name;
                                            break;
                                        case false:
                                            var dby = DB.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                                            msg += "Disapproved By " + dby.Name + " for: " + pack.DenyReason;
                                            break;
                                    }
                                    Bot.Send(msg, id);
                                    break;
                                case "approvesfw":
                                    var nsfw = false;

                                    if (tplayer == null)
                                    {
                                        Send("Id not found.", query.Message.Chat.Id);
                                        return;
                                    }
                                    json = tplayer.CustomGifSet;
                                    if (String.IsNullOrEmpty(json))
                                    {
                                        Send("User does not have a custom gif pack", query.Message.Chat.Id);
                                        return;
                                    }

                                    pack = JsonConvert.DeserializeObject<CustomGifData>(json);
                                    new Thread(() => Task.WhenAll(DownloadGifFromJson(pack, query.Message))).Start();
                                    id = query.From.Id;
                                    pack.Approved = true;
                                    pack.ApprovedBy = id;
                                    pack.NSFW = nsfw;
                                    pack.Submitted = false;
                                    msg = $"Approval Status: ";
                                    by = DB.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                                    msg += "Approved By " + by.Name + "\nNSFW: " + pack.NSFW;
                                    tplayer.CustomGifSet = JsonConvert.SerializeObject(pack);
                                    DB.SaveChanges();
                                    Bot.Send(msg, query.Message.Chat.Id);
                                    Bot.Send(msg, pid);
                                    Bot.Api.DeleteMessageAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId);
                                    break;
                                case "approvensfw":
                                    nsfw = true;

                                    if (tplayer == null)
                                    {
                                        Send("Id not found.", query.Message.Chat.Id);
                                        return;
                                    }
                                    json = tplayer.CustomGifSet;
                                    if (String.IsNullOrEmpty(json))
                                    {
                                        Send("User does not have a custom gif pack", query.Message.Chat.Id);
                                        return;
                                    }

                                    pack = JsonConvert.DeserializeObject<CustomGifData>(json);
                                    new Thread(() => Task.WhenAll(DownloadGifFromJson(pack, query.Message))).Start();
                                    id = query.From.Id;
                                    pack.Approved = true;
                                    pack.ApprovedBy = id;
                                    pack.NSFW = nsfw;
                                    pack.Submitted = false;
                                    msg = $"Approval Status: ";
                                    by = DB.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                                    msg += "Approved By " + by.Name + "\nNSFW: " + pack.NSFW;
                                    tplayer.CustomGifSet = JsonConvert.SerializeObject(pack);
                                    DB.SaveChanges();
                                    Bot.Send(msg, query.Message.Chat.Id);
                                    Bot.Send(msg, pid);
                                    Bot.Api.DeleteMessageAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId);
                                    break;

                                case "dismiss":
                                    json = tplayer?.CustomGifSet;
                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        pack = JsonConvert.DeserializeObject<CustomGifData>(json);
                                        pack.Submitted = false;
                                        DB.SaveChanges();
                                    }
                                    Bot.Api.DeleteMessageAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId);
                                    break;
                            }
                        }
                        return;
                    }

                    //declare objects
                    InlineKeyboardMarkup menu;
                    Group grp = null;
                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                    //get group and player
                    long groupid = 0;
                    if (args.Length > 1 && long.TryParse(args[1], out groupid))
                        grp = DB.Groups.FirstOrDefault(x => x.GroupId == groupid);
                    Player p = DB.Players.FirstOrDefault(x => x.TelegramId == query.From.Id);

                    //some variable helpers
                    var language = GetLanguage(p?.TelegramId ?? groupid);
                    var command = args[0];
                    var choice = "";
                    if (args.Length > 2)
                        choice = args[2];


                    //check for permission
                    if (new[] { "update", "build", "ohai", "restore", "movelang" }.Contains(command))
                    {
                        //dev only commands
                        if (!UpdateHelper.Devs.Contains(query.From.Id))
                        {
                            Bot.ReplyToCallback(query, "You aren't Para! Go Away!!", false, true);
                            return;
                        }
                        Bot.ReplyToCallback(query, "Processing...", false);
                        //args[1] is yes or no
                        if (args[1] == "no")
                        {
                            Bot.Edit(query, query.Message.Text + "\n\nOkay, I won't do anything D: *sadface*");
                            return;
                        }
                    }
                    else if (command == "status")
                    {
                        //global admin only commands
                        if (!UpdateHelper.Devs.Contains(query.From.Id) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                        {
                            Bot.ReplyToCallback(query, GetLocaleString("GlobalAdminOnly", language), false, true);
                            return;
                        }
                    }
                    else if (new[] { "validate", "upload" }.Contains(command))
                    {
                        //lang admin only commands
                        if (!UpdateHelper.Devs.Contains(query.From.Id) && !UpdateHelper.IsGlobalAdmin(query.From.Id) && !UpdateHelper.IsLangAdmin(query.From.Id))
                        {
                            Bot.ReplyToCallback(query, GetLocaleString("GlobalAdminOnly", language), false, true);
                            return;
                        }
                    }
                    else if (command == "setlang")
                    {
                        //requires a player
                        if (p == null)
                            return;
                    }
                    else if (!new[] { "groups", "getlang", "done" }.Contains(command))
                    {
                        //the commands in the array don't require a group. every other command requires a group
                        if (grp == null)
                            return;
                        //among the remaining commands, only stopwaiting can be used by anyone. the other commands are config commands.
                        if (command != "stopwaiting" && !UpdateHelper.Devs.Contains(query.From.Id) && !UpdateHelper.IsGlobalAdmin(query.From.Id) && !UpdateHelper.IsGroupAdmin(query.From.Id, groupid))
                        {
                            Bot.ReplyToCallback(query, GetLocaleString("GroupAdminOnly", language), false, true);
                            return;
                        }
                    }

                    //config helpers
                    if (choice == "back")
                    {
                        Bot.ReplyToCallback(query, GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid, language));
                        return;
                    }
                    if (choice == "cancel")
                    {
                        Bot.ReplyToCallback(query, GetLocaleString("WhatToDo", language), replyMarkup: GetConfigSubmenu(groupid, language, ConfigGroupAttribute.GetConfigGroup(command.Substring(3))));
                        return;
                    }
                    if (Enum.TryParse(command, out ConfigGroup confGroup))
                    {
                        var text = confGroup == ConfigGroup.RoleConfig ? "RoleConfigInfo" : "WhatToDo";
                        Bot.ReplyToCallback(query, GetLocaleString(text, language), replyMarkup: GetConfigSubmenu(groupid, language, confGroup));
                        return;
                    }
                    grp?.UpdateFlags();
                    var Cancel = GetLocaleString("Cancel", language);

                    switch (command)
                    {
#region Dev Commands
                        case "update":
                            Updater.DoUpdate(query);
                            return;
                        case "build":
                            //start the build process
                            Updater.DoBuild(query);
                            return;
                        case "ohai":
                            //update ohaider achievement
                            var userid = long.Parse(args[2]);
                            try
                            {
                                using (var db = new WWContext())
                                {
                                    var para = db.Players.FirstOrDefault(x => x.Id == userid).Id;

                                    //get all the players Para has played with
                                    var ohaiplayers = (from g in db.Games
                                                       join gp in db.GamePlayers on g.Id equals gp.GameId
                                                       join gp2 in db.GamePlayers on g.Id equals gp2.GameId
                                                       join pl in db.Players on gp2.PlayerId equals pl.Id
                                                       where gp.PlayerId == para
                                                       select pl).Distinct().Select(x => new { x.Id, x.NewAchievements }).ToList();

                                    //now filter
                                    for(var i = ohaiplayers.Count - 1; i >= 0; i--)
                                    {
                                        var pl = ohaiplayers[i];
                                        var ach = pl.NewAchievements == null ? new BitArray(200) : new BitArray(pl.NewAchievements);
                                        if (ach.HasFlag(AchievementsReworked.OHAIDER))
                                            ohaiplayers.RemoveAt(i);
                                    }
                                    
                                    //figure out which players don't have the achievement

                                    //update the message
                                    var ohaimsg = $"Found {ohaiplayers.Count()} new players that have earned OHAIDER.";
                                    Bot.Edit(query, ohaimsg);
                                    var count = 0;
                                    foreach (var player in ohaiplayers)
                                    {
                                        var pl = db.Players.FirstOrDefault(x => x.Id == player.Id);
                                        if (AddAchievement(pl, AchievementsReworked.OHAIDER, db))
                                            count++;
                                        Thread.Sleep(200);
                                    }
                                    //Console.WriteLine("Saving");
                                    db.SaveChanges();
                                    ohaimsg += $"\nAchievement added to {count} players\nFinished";
                                    Bot.Edit(query, ohaimsg);
                                }
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
                            return;
                        case "restore":
                            var oldid = long.Parse(args[1]);
                            var newid = long.Parse(args[2]);
                            var result = DB.RestoreAccount(oldid, newid);
                            using (var db = new WWContext())
                            {
                                var oldPlayer = db.Players.FirstOrDefault(x => x.TelegramId == oldid);
                                var newPlayer = db.Players.FirstOrDefault(x => x.TelegramId == newid);

                                if (oldPlayer.NewAchievements != null)
                                {
                                    var oldach = new BitArray(oldPlayer.NewAchievements);
                                    var newach = newPlayer.NewAchievements == null
                                        ? new BitArray(200)
                                        : new BitArray(newPlayer.NewAchievements);

                                    for (int i = 0; i < 200; i++)
                                    {
                                        newach[i] = newach[i] | oldach[i];
                                    }
                                    newPlayer.NewAchievements = newach.ToByteArray();
                                    db.SaveChanges();
                                }
                            }
                            var oldname = DB.Players.FirstOrDefault(x => x.TelegramId == oldid)?.Name;
                            var newname = DB.Players.FirstOrDefault(x => x.TelegramId == newid)?.Name;
                            Bot.Edit(query, $"Restored stats from {oldname} to {newname}");
                            return;
                        case "movelang":
                            var oldfilename = args[2];
                            var newfilename = args[3];
                            int grpcount = 0, plcount = 0, grprcount = 0;

                            var groupsmoved = (from g in DB.Groups where g.Language == oldfilename select g).ToList();
                            var players = (from pl in DB.Players where pl.Language == oldfilename select pl).ToList();
                            var grouprankings = (from gr in DB.GroupRanking where gr.Language == oldfilename select gr).ToList();
                            var dblang = DB.Language.FirstOrDefault(x => x.FileName == oldfilename);

                            if (dblang != null)
                            {
                                DB.Language.Remove(dblang);
                            }
                            foreach (var g in groupsmoved)
                            {
                                g.Language = newfilename;
                                grpcount++;
                            }
                            foreach (var pl in players)
                            {
                                pl.Language = newfilename;
                                plcount++;
                            }
                            foreach (var gr in grouprankings)
                            {
                                gr.Language = newfilename;
                                grprcount++;
                            }
                            DB.SaveChanges();
                            var msg = $"Groups changed: {grpcount}\nPlayers changed: {plcount}\nTotal rows changed: {grpcount + plcount}";
                            try
                            {
                                System.IO.File.Delete(Path.Combine(Bot.LanguageDirectory, oldfilename + ".xml"));
                                msg += $"\n\nSuccessfully deleted {oldfilename}.xml";
                            }
                            catch (Exception e)
                            {
                                msg += $"\n\n*Error: *";
                                msg += e.Message;
                            }
                            Bot.Edit(query, query.Message.Text + "\n\n" + msg);
                            return;
#endregion
#region Global Admin Commands
                        case "status":
                            if (args[3] == "null")
                            {
                                //get status
                                menu = new InlineKeyboardMarkup(new[] { "Normal", "Overloaded", "Recovering", "API Bug", "Offline", "Maintenance" }.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x, $"status|{groupid}|{choice}|{x}") }).ToArray());
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
                        case "pf": // "preferred"
                            var grpid = grp.Id;
                            var rankings = DB.GroupRanking.Where(x => x.GroupId == grpid && !x.Language.EndsWith("BaseAllVariants")).ToList();
                            var lang = args[2];
                            if (lang == "null") //preferred
                            {
                                if (args[3] == "t") // "toggle"
                                    //toggle preferred
                                    grp.Preferred = !(grp.Preferred ?? true);
                                //say if they're preferred
                                Bot.ReplyToCallback(query, "Global: " + (grp.Preferred == false ? "disabled" : "enabled"), false);
                                if (args[3] == "i") // "info"
                                    return;
                            }
                            else
                            {
                                //get the ranking
                                var ranking = rankings.FirstOrDefault(x => x.Language == lang);
                                if (args[3] == "t") // "toggle"
                                    //toggle show
                                    ranking.Show = !(ranking.Show ?? true);
                                //say if they're shown
                                Bot.ReplyToCallback(query, lang + ": " + (ranking.Show == false ? "disabled" : "enabled"), false);
                                if (args[3] == "i") // "info"
                                    return;
                            }
                            DB.SaveChanges();
                            //make the menu
                            var rows = rankings.Select(x => new[] {
                                InlineKeyboardButton.WithCallbackData(x.Language, $"pf|{grp.GroupId}|{x.Language}|i"),
                                InlineKeyboardButton.WithCallbackData(x.Show == false ? "☑️" : "✅", $"pf|{grp.GroupId}|{x.Language}|t")
                            }).ToList();
                            //add a button at the beginning and at the end
                            rows.Insert(0, new[] {
                                InlineKeyboardButton.WithCallbackData("Global", $"pf|{grp.GroupId}|null|i"),
                                InlineKeyboardButton.WithCallbackData(grp.Preferred == false ? "☑️" : "✅", $"pf|{grp.GroupId}|null|t")
                            });
                            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Done", "done") });
                            //send everything
                            Bot.ReplyToCallback(query,
                                $"{grp.GroupId} | " + (grp.GroupLink == null ? grp.Name : $" <a href=\"{grp.GroupLink}\">{grp.Name}</a>") +
                                "\n\nSelect the languages under which the group is allowed to appear in grouplist.\nNote that the first option, if disabled, overrides all the others.",
                                replyMarkup: new InlineKeyboardMarkup(rows.ToArray()),
                                parsemode: ParseMode.Html
                            );
                            return;
                        case "validate":
                            //choice = args[1];
                            if (choice == "All")
                            {
                                LanguageHelper.ValidateFiles(query.Message.Chat.Id, query.Message.MessageId, query.Message.MessageThreadId);
                                return;
                            }

                            if (args[4] != "base" && args[3] == "All")
                            {
                                LanguageHelper.ValidateFiles(query.Message.Chat.Id, query.Message.MessageId, query.Message.MessageThreadId, choice);
                                return;
                            }

                            var baseMenu = new List<InlineKeyboardButton[]>();
                            var vlang = SelectLanguage(command, args, ref baseMenu);
                            menu = new InlineKeyboardMarkup(baseMenu.ToArray());
                            if (vlang == null)
                            {
                                buttons.Clear();
                                Bot.ReplyToCallback(query, GetLocaleString("WhatVariant", language, choice),
                                       replyMarkup: menu);
                                return;
                            }

                            //var menu = new ReplyKeyboardHide { HideKeyboard = true, Selective = true };
                            //Bot.SendTextMessageAsync(id, "", replyToMessageId: update.Message.MessageId, replyMarkup: menu);
                            LanguageHelper.ValidateLanguageFile(query.Message.Chat.Id, vlang.FilePath, query.Message.MessageId);
                            return;
                        case "upload":
                            //Console.WriteLine(choice);
                            if (choice == "current")
                            {
                                Bot.ReplyToCallback(query, "No action taken.");
                                return;
                            }
                            Helpers.LanguageHelper.UseNewLanguageFile(choice, query.Message.Chat.Id, query.Message.MessageId);
                            return;
#endregion
#region Other Commands
                        case "groups":
                            if (choice == "null")
                            {
                                Commands.GroupList(query.Message.Chat.Id, query.From.Id, query.Message.MessageId);
                                break;
                            }

                            var variant = args[3];
                            if (variant == "null")
                            {
                                var variants = PublicGroups.GetVariants(choice);
                                if (variants.Count() == 1)
                                {
                                    variant = variants.First();
                                }
                                else
                                {
                                    //create a menu out of this
                                    buttons = new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData(GetLocaleString("All", language), $"groups|{query.From.Id}|{choice}|all") };
                                    buttons.AddRange(variants.OrderBy(x => x).Select(x => InlineKeyboardButton.WithCallbackData(x, $"groups|{query.From.Id}|{choice}|{x}")));
                                    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Back", language), $"groups|{query.From.Id}|null"));
                                    var playersGames = DB.Players.FirstOrDefault(x => x.TelegramId == query.From.Id);
                                    var gamecount = playersGames?.GamePlayers.Count ?? 0;
                                    var message = "";
                                    if (gamecount >= 500 && choice.Equals("English"))
                                    {
                                        message = GetLocaleString("WhatVariantVets", language, choice);
                                        message =
                                            $"{message} <a href=\"{Settings.VeteranChatUrl}\">Werewolf Veterans</a>";
                                    }
                                    else
                                        message = GetLocaleString("WhatVariant", language, choice);
                                    var variantMenu = new List<InlineKeyboardButton[]>();
                                    for (var i = 0; i < buttons.Count; i++)
                                    {
                                        if (buttons.Count - 1 == i)
                                        {
                                            variantMenu.Add(new[] { buttons[i] });
                                        }
                                        else
                                            variantMenu.Add(new[] { buttons[i], buttons[i + 1] });
                                        i++;
                                    }

                                    Bot.ReplyToCallback(query, message, replyMarkup: new InlineKeyboardMarkup(variantMenu.ToArray()), parsemode: ParseMode.Html);
                                    break;
                                }
                            }

                            var callbackdata = PublicGroups.GetVariants(choice).Count == 1
                                ? $"groups|{query.From.Id}|null|"
                                : $"groups|{query.From.Id}|{choice}|null";

                            var markup = new InlineKeyboardMarkup(
                                new InlineKeyboardButton[] {
                                    InlineKeyboardButton.WithCallbackData(GetLocaleString("Back", language), callbackdata)
                                }
                            );

                            var groups = PublicGroups.ForLanguage(choice, variant).ToList().GroupBy(x => x.GroupId).Select(x => x.First()).Where(x => x.LastRefresh >= DateTime.Now.Date.AddDays(-21)).OrderByDescending(x => x.LastRefresh).ThenByDescending(x => x.Ranking).Take(10).ToList();
                            var variantmsg = args[3] == "all" ? "" : (" " + variant);

                            var reply = GetLocaleString("HereIsList", language, choice + variantmsg) + "\n\n" +
                                groups.Aggregate("",
                                (current, g) =>
                                    current +
                                    $"<a href=\"{g.GroupLink}\">{g.Name.FormatHTML()}</a>\n\n");

                            Bot.ReplyToCallback(query, reply, replyMarkup: markup, parsemode: ParseMode.Html, disableWebPagePreview: true);
                            break;
                        case "getlang":
                            if (choice == "All")
                            {
                                Bot.ReplyToCallback(query, "One moment...");
                                LanguageHelper.SendAllFiles(query.Message.Chat.Id, query.Message.MessageThreadId);
                                return;
                            }

                            if (args[4] != "base" && args[3] == "All")
                            {
                                Bot.ReplyToCallback(query, "One moment...");
                                LanguageHelper.SendBase(choice, query.Message.Chat.Id, query.Message.MessageThreadId);
                                return;
                            }

                            baseMenu = new List<InlineKeyboardButton[]>();
                            var glang = SelectLanguage(command, args, ref baseMenu);
                            menu = new InlineKeyboardMarkup(baseMenu.ToArray());
                            if (glang == null)
                            {
                                buttons.Clear();
                                Bot.ReplyToCallback(query, GetLocaleString("WhatVariant", language, choice),
                                       replyMarkup: menu);
                                return;
                            }
                            Bot.ReplyToCallback(query, "One moment...");
                            LanguageHelper.SendFile(query.Message.Chat.Id, query.Message.MessageThreadId, glang.Name);
                            break;
                        case "stopwaiting":
                            using (var db = new WWContext())
                                db.Database.ExecuteSqlCommand($"DELETE FROM NotifyGame WHERE GroupId = {groupid} AND UserId = {query.From.Id}");
                            Bot.ReplyToCallback(query, GetLocaleString("DeletedFromWaitList", grp.Language, grp.Name));
                            break;
#endregion
#region Config Commands
                        case "lang":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            //load up each file and get the names
                            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();

                            buttons.Clear();
                            buttons.AddRange(langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => InlineKeyboardButton.WithCallbackData(x, $"setlang|{groupid}|{x}|null|base")));

                            baseMenu = new List<InlineKeyboardButton[]>();
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
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            if (args[3] == "Random" && args[4] == "v")
                            {
                                if (grp == null) return;
                                menu = GetConfigSubmenu(grp.GroupId, language, ConfigGroup.GroupSettings);

                                var langbase = args[2];
                                var basefiles = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).Where(x => x.Base == langbase);
                                var defaultfile = basefiles.FirstOrDefault(x => x.IsDefault || new[] { "normal", "standard", "default" }.Contains(x.Variant.ToLower())) ?? basefiles.First();
                                grp.AddFlag(GroupConfig.RandomLangVariant);
                                grp.Language = defaultfile.FileName;
                                DB.SaveChanges();
                                Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString("LangSet", language, langbase + ": Random"));
                                Bot.ReplyToCallback(query, GetLocaleString("WhatToDo", language), replyMarkup: menu);
                                return;
                            }

                            baseMenu = new List<InlineKeyboardButton[]>();
                            var slang = SelectLanguage(command, args, ref baseMenu, false, grp != null);
                            menu = new InlineKeyboardMarkup(baseMenu.ToArray());
                            if (slang == null)
                            {
                                buttons.Clear();
                                var curLangfilePath = Directory.GetFiles(Bot.LanguageDirectory).First(x => Path.GetFileNameWithoutExtension(x) == (grp?.Language ?? p.Language));
                                var curVariant = grp != null && grp.HasFlag(GroupConfig.RandomLangVariant) ? "Random" : new LangFile(curLangfilePath).Variant;
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
                                    grp.RemoveFlag(GroupConfig.RandomLangVariant);
                                    //check for any games running
                                    var ig = GetGroupNodeAndGame(groupid);

                                    ig?.LoadLanguage(slang.FileName);
                                    menu = GetConfigMenu(groupid, language);
                                    Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString("LangSet", language, slang.Base + (String.IsNullOrWhiteSpace(slang.Variant) ? "" : ": " + slang.Variant)));
                                    Bot.ReplyToCallback(query, GetLocaleString("WhatToDo", language), replyMarkup: menu);
                                }
                                else if (p != null)
                                {
                                    p.Language = slang.FileName;
                                    Bot.ReplyToCallback(query, GetLocaleString("LangSet", language, slang.Base + (String.IsNullOrWhiteSpace(slang.Variant) ? "" : ": " + slang.Variant)));
                                }
                            }
                            DB.SaveChanges();
                            break;
                        //case "online":
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData("Yes", $"setonline|{groupid}|show"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData("No", $"setonline|{groupid}|hide"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData("Cancel", $"setonline|{groupid}|cancel"));
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
                        //case "flee":
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(Yes, $"setflee|{groupid}|enable"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(No, $"setflee|{groupid}|disable"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setflee|{groupid}|cancel"));
                        //    menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("AllowFleeQ", language, grp.DisableFlee == false ? GetLocaleString("Allow", language) : GetLocaleString("Disallow", language)),
                        //        replyMarkup: menu);
                        //    break;
                        //case "setflee":

                        //    grp.DisableFlee = (choice == "disable"); //also an issue.  this is reversed, it should have been EnableFlee.
                        //    //Para - Stop coding drunk.  It's bad.
                        //    Bot.Api.AnswerCallbackQuery(query.Id,
                        //           GetLocaleString("AllowFleeA", language, grp.DisableFlee == true ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)));
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        //    DB.SaveChanges();
                        //    break;
                        case "maxplayer":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            buttons.Add(InlineKeyboardButton.WithCallbackData("5", $"setmaxplayer|{groupid}|5"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("10", $"setmaxplayer|{groupid}|10"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("15", $"setmaxplayer|{groupid}|15"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("20", $"setmaxplayer|{groupid}|20"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("25", $"setmaxplayer|{groupid}|25"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("30", $"setmaxplayer|{groupid}|30"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("35", $"setmaxplayer|{groupid}|35"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setmaxplayer|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("MaxPlayersQ", language, grp.MaxPlayers ?? Settings.MaxPlayers),
                                replyMarkup: menu);
                            break;
                        case "setmaxplayer":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            int oldMaxPlayers = grp.MaxPlayers ?? 35;
                            grp.MaxPlayers = int.Parse(choice);
                            if (grp.MaxPlayers > oldMaxPlayers && grp.RoleFlags.HasValue)
                                grp.RoleFlags = (long)((IRole)grp.RoleFlags & ~IRole.VALID);
                            Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString("MaxPlayersA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigSubmenu(groupid, language, ConfigGroup.GroupSettings));
                            DB.SaveChanges();
                            break;
                        //case "roles":
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Show", language), $"setroles|{groupid}|show"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Hide", language), $"setroles|{groupid}|hide"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setroles|{groupid}|cancel"));
                        //    menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("ShowRolesDeathQ", language, (grp.ShowRoles == false ? "Hidden" : "Shown")),
                        //        replyMarkup: menu);
                        //    break;
                        //case "setroles":

                        //    grp.ShowRoles = (choice == "show");
                        //    Bot.Api.AnswerCallbackQuery(query.Id,
                        //        GetLocaleString("ShowRolesDeathA", language, grp.ShowRoles == false ? "hidden" : "shown"));
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        //    DB.SaveChanges();
                        //    break;
                        case "mode":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("NormalOnly", language), $"setmode|{groupid}|Normal"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("ChaosOnly", language), $"setmode|{groupid}|Chaos"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("PlayerChoice", language), $"setmode|{groupid}|Player"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setmode|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("GameModeQ", language, grp.Mode), replyMarkup: menu);
                            break;
                        case "setmode":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            grp.Mode = choice;
                            Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString("GameModeA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigSubmenu(groupid, language, ConfigGroup.GroupSettings));
                            DB.SaveChanges();
                            break;
                        case "endroles":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(query.Message.Chat.Id, query.Message.MessageId).Wait();
                                return;
                            }

                            buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("ShowNone", language), $"setendroles|{groupid}|None"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("ShowLiving", language), $"setendroles|{groupid}|Living"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("ShowAll", language), $"setendroles|{groupid}|All"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setendroles|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("ShowRolesEndQ", language, grp.ShowRolesEnd),
                                replyMarkup: menu);
                            break;
                        case "setendroles":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            grp.ShowRolesEnd = choice;
                            Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString("ShowRolesEndA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigSubmenu(groupid, language, ConfigGroup.Mechanics));
                            DB.SaveChanges();
                            break;
                        case "daytimer":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            buttons.Add(InlineKeyboardButton.WithCallbackData("90", $"setday|{groupid}|30"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("120", $"setday|{groupid}|60"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("150", $"setday|{groupid}|90"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("180", $"setday|{groupid}|120"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setday|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("SetDayTimeQ", language, Settings.TimeDay + 60, (grp.DayTime ?? Settings.TimeDay) + 60),
                                replyMarkup: menu);
                            break;
                        case "setday":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            grp.DayTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString("SetDayTimeA", language, int.Parse(choice) + 60));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigSubmenu(groupid, language, ConfigGroup.Timers));
                            DB.SaveChanges();
                            break;
                        case "nighttimer":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            buttons.Add(InlineKeyboardButton.WithCallbackData("30", $"setnight|{groupid}|30"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("60", $"setnight|{groupid}|60"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("90", $"setnight|{groupid}|90"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("120", $"setnight|{groupid}|120"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setnight|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("SetNightTimeQ", language, Settings.TimeNight, grp.NightTime ?? Settings.TimeNight),
                                replyMarkup: menu);
                            break;
                        case "setnight":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            grp.NightTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString("SetNightTimeA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigSubmenu(groupid, language, ConfigGroup.Timers));
                            DB.SaveChanges();
                            break;
                        case "lynchtimer":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            buttons.Add(InlineKeyboardButton.WithCallbackData("30", $"setlynch|{groupid}|30"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("60", $"setlynch|{groupid}|60"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("90", $"setlynch|{groupid}|90"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("120", $"setlynch|{groupid}|120"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setlynch|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("SetLynchTimeQ", language, Settings.TimeLynch, grp.LynchTime ?? Settings.TimeLynch),
                                replyMarkup: menu);
                            break;
                        case "setlynch":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            grp.LynchTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString("SetLynchTimeA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigSubmenu(groupid, language, ConfigGroup.Timers));
                            DB.SaveChanges();
                            break;
                        //case "fool":
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Allow", language), $"setfool|{groupid}|true"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Disallow", language), $"setfool|{groupid}|false"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setfool|{groupid}|cancel"));
                        //    menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("AllowFoolQ", language, grp.AllowFool == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)), replyMarkup: menu);
                        //    break;
                        //case "setfool":
                        //    grp.AllowFool = (choice == "true");
                        //    Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("AllowFoolA", language, grp.AllowFool == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)));
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        //    DB.SaveChanges();
                        //    break;
                        //case "tanner":
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Allow", language), $"settanner|{groupid}|true"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Disallow", language), $"settanner|{groupid}|false"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"settanner|{groupid}|cancel"));
                        //    menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("AllowTannerQ", language, grp.AllowTanner == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)), replyMarkup: menu);
                        //    break;
                        //case "settanner":
                        //    grp.AllowTanner = (choice == "true");
                        //    Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("AllowTannerA", language, grp.AllowTanner == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)));
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        //    DB.SaveChanges();
                        //    break;

                        //case "secretlynch":
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Allow", language), $"setsecretlynch|{groupid}|true"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Disallow", language), $"setsecretlynch|{groupid}|false"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setsecretlynch|{groupid}|cancel"));
                        //    menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("EnableSecretLynchQ", language, grp.EnableSecretLynch != true ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)), replyMarkup: menu);
                        //    break;
                        //case "setsecretlynch":
                        //    grp.EnableSecretLynch = (choice == "true");
                        //    Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("EnableSecretLynchA", language, grp.EnableSecretLynch != true ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)));
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        //    DB.SaveChanges();
                        //    break;
                        //case "cult":
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Allow", language), $"setcult|{groupid}|true"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Disallow", language), $"setcult|{groupid}|false"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setcult|{groupid}|cancel"));
                        //    menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("AllowCultQ", language, grp.AllowCult == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)), replyMarkup: menu);
                        //    break;
                        //case "setcult":
                        //    grp.AllowCult = (choice == "true");
                        //    Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("AllowCultA", language, grp.AllowCult == false ? GetLocaleString("Disallow", language) : GetLocaleString("Allow", language)));
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        //    DB.SaveChanges();
                        //    break;
                        //case "extend":
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Allow", language), $"setextend|{groupid}|true"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Disallow", language), $"setextend|{groupid}|false"));
                        //    buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setextend|{groupid}|cancel"));
                        //    menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("AllowExtendQ", language, grp.AllowExtend == true ? GetLocaleString("Allow", language) : GetLocaleString("Disallow", language)), replyMarkup: menu);
                        //    break;
                        //case "setextend":
                        //    grp.AllowExtend = (choice == "true");
                        //    Bot.Api.AnswerCallbackQuery(query.Id, GetLocaleString("AllowExtendA", language, grp.AllowExtend == true ? GetLocaleString("Allow", language) : GetLocaleString("Disallow", language)));
                        //    Bot.ReplyToCallback(query,
                        //        GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        //    DB.SaveChanges();
                        //    break;
                        case "maxextend":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            buttons.Add(InlineKeyboardButton.WithCallbackData("60", $"setmaxextend|{groupid}|60"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("120", $"setmaxextend|{groupid}|120"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("180", $"setmaxextend|{groupid}|180"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("240", $"setmaxextend|{groupid}|240"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData("300", $"setmaxextend|{groupid}|300"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"setmaxextend|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("MaxExtendQ", language, Settings.MaxExtend, grp.MaxExtend ?? Settings.MaxExtend), replyMarkup: menu);
                            break;
                        case "setmaxextend":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            grp.MaxExtend = int.Parse(choice);
                            Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString("MaxExtendA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigSubmenu(groupid, language, ConfigGroup.Timers));
                            DB.SaveChanges();
                            break;
                        case "done":
                            Bot.ReplyToCallback(query,
                                GetLocaleString("ThankYou", language));
                            break;
                        case "togglerole":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            var disabledRoles = (IRole)(grp.RoleFlags ?? 0);
                            switch (choice)
                            {
                                case "enableall":
                                    disabledRoles = IRole.VALID; // Enable all roles and turn the "VALID" flag on
                                    break;

                                case "disableall":
                                    disabledRoles = IRole.None;
                                    foreach (var disRole in RoleConfigHelper.GetRoles().Where(x => x.GetRoleAttribute().CanBeDisabled))
                                    {
                                        disabledRoles |= disRole; // Disable all roles that can be disabled
                                    }
                                    break;

                                default:
                                    var role = (IRole)long.Parse(choice);
                                    disabledRoles ^= role; // Toggle the role
                                    disabledRoles &= ~IRole.VALID; // Remove the "VALID" flag
                                    break;
                            }
                            disabledRoles &= ~IRole.Wolf;
                            disabledRoles &= ~IRole.Villager;
                            disabledRoles &= ~IRole.Spumpkin; // Make SURE ww, vg and special are not disabled
                            grp.RoleFlags = (long)disabledRoles;
                            DB.SaveChanges();
                            Bot.Edit(query, GetLocaleString("RoleConfigInfo", language), GetRoleConfigMenu(groupid, language));
                            break;
                        case "validateroles":
                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            disabledRoles = (IRole)(grp.RoleFlags ?? 0);
                            disabledRoles &= ~IRole.Wolf;
                            disabledRoles &= ~IRole.Villager;
                            disabledRoles &= ~IRole.Spumpkin; // Make SURE ww, vg and special are not disabled
                            bool valid = GameBalancing.TryBalance(disabledRoles, grp.MaxPlayers ?? 35);
                            if (valid)
                            {
                                disabledRoles |= IRole.VALID; // Add the "VALID" flag
                                Bot.ReplyToCallback(query, GetLocaleString("RoleConfigValid", language), false, true);
                            }
                            else
                            {
                                disabledRoles &= ~IRole.VALID; // Remove the "VALID" flag
                                Bot.ReplyToCallback(query, GetLocaleString("RoleConfigInvalid", language), false, true);
                            }
                            grp.RoleFlags = (long)disabledRoles;
                            DB.SaveChanges();
                            Bot.Edit(query, GetLocaleString("RoleConfigInfo", language), GetRoleConfigMenu(groupid, language));
                            break;
                        default:
                            //check the statement against various flags to see if it a boolean group setting.  If so, build out the response.
                            var settings = Enum.GetValues(typeof(GroupConfig)).Cast<GroupConfig>()
                                .Where(x => x.IsEditable());
                            var chosen =
                                settings.FirstOrDefault(
                                    x => x.GetInfo().ShortName == command || "set" + x.GetInfo().ShortName == command);
                            if (chosen == GroupConfig.None) break; //always false? on a FirstOrDefault?....  I'll check later. //reny's answer: yep, enum's default is zero, ie GroupConfig.None
                            //TODO we need to call the database method to update the group flags based on current settings (also under TODO)

                            if (grp != null && !UpdateHelper.IsGroupAdmin(query.From.Id, grp.GroupId) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
                            {
                                Bot.Api.EditMessageReplyMarkupAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId).Wait();
                                return;
                            }

                            string pos = "", neg = "";
                            switch (chosen.GetInfo().Question)
                            {
                                case SettingQuestion.AllowDisallow:
                                    pos = "Allow";
                                    neg = "Disallow";
                                    break;
                                case SettingQuestion.YesNo:
                                    pos = "Yes";
                                    neg = "No";
                                    break;
                                case SettingQuestion.ShowHide:
                                    pos = "Show";
                                    neg = "Hide";
                                    break;
                            }

                            if (command.StartsWith("set"))
                            {
                                var current = choice == "true";
                                //get the flags.
                                var flagLong = grp.Flags ?? 0;
                                var flags = (GroupConfig)flagLong; // i think that's right....

                                if (current)
                                    flags = flags | chosen;
                                else
                                    flags = flags & ~chosen;

                                grp.Flags = (long)flags;
                                command = command.Substring(3);
                                Bot.Api.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: GetLocaleString($"{chosen.ToString()}A", language, current ? GetLocaleString(pos, language) : GetLocaleString(neg, language)));
                                Bot.ReplyToCallback(query, GetLocaleString("WhatToDo", language), replyMarkup: GetConfigSubmenu(groupid, language, ConfigGroupAttribute.GetConfigGroup(command)));
                                DB.SaveChanges();
                            }
                            else
                            {

                                buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString(pos, language), $"set{command}|{groupid}|true"));
                                buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString(neg, language), $"set{command}|{groupid}|false"));
                                buttons.Add(InlineKeyboardButton.WithCallbackData(Cancel, $"set{command}|{groupid}|cancel"));

                                menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                                var current = chosen.GetDefaultValue();
                                if (grp.Flags != null)
                                {
                                    current = ((GroupConfig)grp.Flags).HasFlag(chosen);
                                }
                                Bot.ReplyToCallback(query,
                                    GetLocaleString(chosen + "Q", language, current
                                            ? GetLocaleString(pos, language)
                                            : GetLocaleString(neg, language)), replyMarkup: menu);
                            }

                            break;
#endregion
                    }
                }
                catch (Exception ex)
                {
                    Bot.ReplyToCallback(query, ex.Message, false, true);
                }
            }
        }

        private static bool AddAchievement(Player p, AchievementsReworked a, WWContext db)
        {

            if (p != null)
            {
                //Console.WriteLine(p.Name);
                var ach = p.NewAchievements == null ? new BitArray(200) : new BitArray(p.NewAchievements);
                if (ach.HasFlag(a)) return false; //no point making another db call if they already have it
                ach = ach.Set(a);
                p.NewAchievements = ach.ToByteArray();
                db.SaveChanges();

                Send($"Achievement Unlocked!\n{a.GetName().ToBold()}\n{a.GetDescription()}", p.TelegramId);
                return true;
            }
            return false;
        }

        private static Task[] DownloadGifFromJson(CustomGifData pack, Message m)
        {
            List<Task> downloadTasks = new List<Task>();
            if (!String.IsNullOrEmpty(pack.ArsonistWins))
                downloadTasks.Add(DownloadGif(pack.ArsonistWins, m.Chat));
            if (!String.IsNullOrEmpty(pack.BurnToDeath))
                downloadTasks.Add(DownloadGif(pack.BurnToDeath, m.Chat));
            if (!String.IsNullOrEmpty(pack.CultWins))
                downloadTasks.Add(DownloadGif(pack.CultWins, m.Chat));
            if (!String.IsNullOrEmpty(pack.LoversWin))
                downloadTasks.Add(DownloadGif(pack.LoversWin, m.Chat));
            if (!String.IsNullOrEmpty(pack.NoWinner))
                downloadTasks.Add(DownloadGif(pack.NoWinner, m.Chat));
            if (!String.IsNullOrEmpty(pack.SerialKillerWins))
                downloadTasks.Add(DownloadGif(pack.SerialKillerWins, m.Chat));
            if (!String.IsNullOrEmpty(pack.SKKilled))
                downloadTasks.Add(DownloadGif(pack.SKKilled, m.Chat));
            if (!String.IsNullOrEmpty(pack.StartChaosGame))
                downloadTasks.Add(DownloadGif(pack.StartChaosGame, m.Chat));
            if (!String.IsNullOrEmpty(pack.StartGame))
                downloadTasks.Add(DownloadGif(pack.StartGame, m.Chat));
            if (!String.IsNullOrEmpty(pack.TannerWin))
                downloadTasks.Add(DownloadGif(pack.TannerWin, m.Chat));
            if (!String.IsNullOrEmpty(pack.VillagerDieImage))
                downloadTasks.Add(DownloadGif(pack.VillagerDieImage, m.Chat));
            if (!String.IsNullOrEmpty(pack.VillagersWin))
                downloadTasks.Add(DownloadGif(pack.VillagersWin, m.Chat));
            if (!String.IsNullOrEmpty(pack.WolfWin))
                downloadTasks.Add(DownloadGif(pack.WolfWin, m.Chat));
            if (!String.IsNullOrEmpty(pack.WolvesWin))
                downloadTasks.Add(DownloadGif(pack.WolvesWin, m.Chat));
            return downloadTasks.ToArray();
        }

        private static async Task DownloadGif(string fileid, Chat chat)
        {
            try
            {
                var path = Path.Combine(Settings.GifStoragePath, $"{fileid}.mp4");
                if (!System.IO.File.Exists(path))
                    using (var x = System.IO.File.OpenWrite(path))
                        await Bot.Api.DownloadFileAsync(filePath: (await Bot.Api.GetFileAsync(fileId: fileid)).FilePath, x);
            }
            catch (Exception e)
            {
                Send(e.ToString(), Settings.ErrorGroup);
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


        internal static LangFile SelectLanguage(string command, string[] args, ref List<InlineKeyboardButton[]> menu, bool addAllbutton = true, bool addRandomButton = false)
        {
            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();
            var isBase = args[4] == "base";
            if (isBase)
            {
                var variants = langs.Where(x => x.Base == args[2]).OrderBy(x => !x.IsDefault).ThenBy(x => x.Variant).ToList();
                if (variants.Count() > 1)
                {
                    var buttons = new List<InlineKeyboardButton>();
                    buttons.AddRange(variants.Select(x => InlineKeyboardButton.WithCallbackData(x.Variant, $"{command}|{args[1]}|{x.Base}|{x.Variant}|v")));
                    //if (addRandomButton) // TO DO: Publish Random Language Variant Mode by uncommenting these 2 lines
                    //    buttons.Insert(0, InlineKeyboardButton.WithCallbackData("Random", $"{command}|{args[1]}|{args[2]}|Random|v"));
                    if (addAllbutton)
                        buttons.Insert(0, InlineKeyboardButton.WithCallbackData("All", $"{command}|{args[1]}|{args[2]}|All|v"));

                    for (var i = 0; i < buttons.Count; i++)
                    {
                        if (buttons.Count - 1 == i)
                        {
                            menu.Add(new[] { buttons[i] });
                        }
                        else
                            menu.Add(new[] { buttons[i], buttons[i + 1] });
                        i++;
                    }

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
            try
            {
                var values = strings.Descendants("value");
                var choice = Bot.R.Next(values.Count());
                var selected = values.ElementAt(choice);
                if (String.IsNullOrWhiteSpace(selected.Value))
                {
                    strings = Bot.English.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key);
                    values = strings.Descendants("value");
                    choice = Bot.R.Next(values.Count());
                    selected = values.ElementAt(choice);
                }
                return String.Format(selected.Value.FormatHTML(), args).Replace("\\n", Environment.NewLine);
            }
            catch
            {
                return "";
            }

        }

        //internal static Group MakeDefaultGroup(long groupid, string name, string createdBy)
        //{
        //    return new Group
        //    {
        //        GroupId = groupid,
        //        Name = name,
        //        Language = "English",
        //        BotInGroup = true,
        //        ShowRoles = true,
        //        Mode = "Player",
        //        DayTime = Settings.TimeDay,
        //        LynchTime = Settings.TimeLynch,
        //        NightTime = Settings.TimeNight,
        //        AllowFool = true,
        //        AllowTanner = true,
        //        AllowCult = true,
        //        DisableFlee = false,
        //        MaxPlayers = 35,
        //        EnableSecretLynch = false,
        //        CreatedBy = createdBy,
        //        Flags = (long)(GroupConfig.Update | GroupConfig.ThiefFull | GroupConfig.AllowThief)
        //    };
        //}

        internal static InlineKeyboardMarkup GetConfigMenu(long id, string lang)
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            //base menu
            var configGroups = ConfigGroupAttribute.GetConfigGroups();

            foreach (var cg in configGroups)
            {
                buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString(cg.ToString(), lang), $"{cg.ToString()}|{id}"));
            }

            buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Done", lang), $"done"));
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

        internal static InlineKeyboardMarkup GetConfigSubmenu(long id, string language, ConfigGroup configGroup)
        {
            if (configGroup == ConfigGroup.RoleConfig) return GetRoleConfigMenu(id, language);

            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

            if (ConfigGroupAttribute.hardcodedConfigOptions.ContainsKey(configGroup))
            {
                foreach (var opt in ConfigGroupAttribute.hardcodedConfigOptions[configGroup])
                {
                    buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString(opt, language), $"{opt.ToLower()}|{id}"));
                }
            }

            /*//buttons.Add(InlineKeyboardButton.WithCallbackData("Show Online Message", $"online|{id}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Change Language", $"lang|{id}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Change Game Mode", $"mode|{id}"));
            //buttons.Add(InlineKeyboardButton.WithCallbackData("Show Roles On Death", $"roles|{id}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Show Roles At Game End", $"endroles|{id}"));
            //buttons.Add(InlineKeyboardButton.WithCallbackData("Allow Fleeing", $"flee|{id}")); //TODO add
            //buttons.Add(InlineKeyboardButton.WithCallbackData("Allow Extending Timer", $"extend|{id}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Set Max Players", $"maxplayer|{id}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Set Max Extend Time", $"maxextend|{id}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Set Day Timer", $"daytimer|{id}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Set Lynch Timer", $"lynch|{id}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Set Night Timer", $"night|{id}"));
            //buttons.Add(InlineKeyboardButton.WithCallbackData("Allow Fool", $"fool|{id}"));
            //buttons.Add(InlineKeyboardButton.WithCallbackData("Allow Tanner", $"tanner|{id}"));  //DONE
            //buttons.Add(InlineKeyboardButton.WithCallbackData("Allow Cult", $"cult|{id}"));
            //buttons.Add(InlineKeyboardButton.WithCallbackData("Enable Secret Lynch", $"secretlynch|{id}"));  //DONE*/
            foreach (var flag in Enum.GetValues(typeof(GroupConfig)).Cast<GroupConfig>())
            {
                if (!flag.IsEditable() || ConfigGroupAttribute.GetConfigGroup(flag.GetInfo().ShortName) != configGroup) continue;
                //get the flag, determine the shortname and make a button out of it.
                buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString(flag.ToString(), language), $"{flag.GetInfo().ShortName}|{id}"));
            }

            buttons.Add(InlineKeyboardButton.WithCallbackData(GetLocaleString("Back", language), $"{configGroup}|{id}|back"));

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

        public static InlineKeyboardMarkup GetRoleConfigMenu(long id, string language, bool addEnableAll = true, bool addDisableAll = true)
        {
            var buttons = new List<InlineKeyboardButton>();

            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                var disabledRoles = (IRole)(grp.RoleFlags ?? 0);

                foreach (IRole role in RoleConfigHelper.GetRoles())
                {
                    var roleAttr = role.GetRoleAttribute();
                    if (!roleAttr.CanBeDisabled) continue;
                    buttons.Add(InlineKeyboardButton.WithCallbackData(
                        $"{roleAttr.Emoji} {(disabledRoles.HasFlag(role) ? "❌" : "✅")}",
                        $"togglerole|{id}|{(long)role}"));
                }

                var threeMenu = new List<InlineKeyboardButton[]>();
                for (var i = 0; i < buttons.Count; i += 3)
                {
                    if (buttons.Count - 1 == i)
                    {
                        threeMenu.Add(new[] { buttons[i] });
                    }
                    else if (buttons.Count - 2 == i)
                    {
                        threeMenu.Add(new[] { buttons[i], buttons[i + 1] });
                    }
                    else
                        threeMenu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2] });
                }

                threeMenu.Add(new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData(GetLocaleString("EnableAllRoles", language), $"togglerole|{id}|enableall"),
                    InlineKeyboardButton.WithCallbackData(GetLocaleString("DisableAllRoles", language), $"togglerole|{id}|disableall")
                });

                List<InlineKeyboardButton> lastRow = new List<InlineKeyboardButton>
                {
                    disabledRoles.HasFlag(IRole.VALID)
                        ? InlineKeyboardButton.WithCallbackData(GetLocaleString("Valid", language), $"dummmy")
                        : InlineKeyboardButton.WithCallbackData(GetLocaleString("Validate", language), $"validateroles|{id}"),

                    InlineKeyboardButton.WithCallbackData(GetLocaleString("Back", language), $"{ConfigGroup.RoleConfig.ToString()}|{id}|back")
                };

                threeMenu.Add(lastRow.ToArray());

                var menu = new InlineKeyboardMarkup(threeMenu.ToArray());
                return menu;
            }
        }


        public static void InlineQueryReceived(ITelegramBotClient botClient, InlineQuery inlineQuery)
        {
            
            new Task(() => { HandleInlineQuery(inlineQuery); }).Start();
        }

        internal static void HandleInlineQuery(InlineQuery q)
        {
            Bot.MessagesProcessed++;
            var commands = new InlineCommand[]
            {
                new StatsInlineCommand(q.From),
                new KillsInlineCommand(q),
                new KilledByInlineCommand(q),
                new TypesOfDeathInlineCommand(q),
            };

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
            Bot.Api.AnswerInlineQueryAsync(inlineQueryId: q.Id, results: choices.Select(c => 
                new InlineQueryResultArticle(c.Command, c.Command,
                    new InputTextMessageContent(c.Content)
                    {
                        DisableWebPagePreview = true,
                        ParseMode = ParseMode.Html
                    }
                )
                {
                    Description = c.Description,
                }).Cast<InlineQueryResultArticle>().ToArray(), cacheTime: 0, isPersonal: true);
        }
    }
}

