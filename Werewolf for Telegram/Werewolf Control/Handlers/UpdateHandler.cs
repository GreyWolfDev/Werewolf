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
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.Payments;
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
            Bot.MessagesReceived++;
            new Task(() => { HandleUpdate(e.Update); }).Start();
        }

        private static bool AddCount(int id, Message m)
        {
            try
            {
                if (!UserMessages.ContainsKey(id))
                    UserMessages.Add(id, new SpamDetector { Messages = new HashSet<UserMessage>() });
                
                var shouldReply = (UserMessages[id].Messages.Where(x => x.Replied).OrderByDescending(x => x.Time).FirstOrDefault()?.Time ?? DateTime.MinValue) <
                       DateTime.UtcNow.AddSeconds(-4);

                UserMessages[id].Messages.Add(new UserMessage(m){Replied = shouldReply});
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
                if (update.PreCheckoutQuery != null)
                {
                    HandlePayment(update.PreCheckoutQuery);
                    return;
                }
                if (update.Message == null) return;
                Program.Analytics.TrackAsync("message", update.Message, update.Message.From.Id.ToString());
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
                        case MessageType.UnknownMessage:
                            break;
                        case MessageType.TextMessage:
                            if (update.Message.Text.StartsWith("!") || update.Message.Text.StartsWith("/"))
                            {

                                if (BanList.Any(x => x.TelegramId == (update.Message?.From?.Id ?? 0)) ||
                                    SpamBanList.Contains(update.Message?.From?.Id ?? 0))
                                {
                                    return;
                                }

                                var args = GetParameters(update.Message.Text);
                                args[0] = args[0].ToLower().Replace("@" + Bot.Me.Username.ToLower(), "");
                                //command is args[0]
                                Program.Analytics.TrackAsync("/" + args[0],
                                    new {groupid = update.Message.Chat.Id, user = update.Message.From},
                                    update.Message.From.Id.ToString());
                                if (args[0].StartsWith("about"))
                                {
                                    if (new[] { "Thief", "WiseElder", "Pacifist" }.Contains(args[0].Replace("about", ""))) return;
                                    var reply = Commands.GetAbout(update, args);
                                    if (!String.IsNullOrEmpty(reply))
                                    {

                                        if (AddCount(update.Message.From.Id, update.Message)) return;
                                        try
                                        {
                                            var result = Send(reply, update.Message.From.Id).Result;
                                            //if (update.Message.Chat.Type != ChatType.Private)
                                            //    Send(
                                            //        GetLocaleString("SentPrivate", GetLanguage(update.Message.From.Id)),
                                            //        update.Message.Chat.Id);

                                        }
                                        catch (Exception e)
                                        {
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
#if RELEASE2
                                    Send($"Bot 2 is retiring.  Please switch to @werewolfbot", update.Message.Chat.Id);
                                    if (update.Message.Chat.Type != ChatType.Private)
                                    {
                                        Thread.Sleep(1000);
                                        Bot.Api.LeaveChat(update.Message.Chat.Id);
                                    }
#endif
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
                                            Send(GetLocaleString("NotGlobalAdmin", GetLanguage(id)), id);
                                            return;
                                        }
                                    }
                                    if (command.GroupAdminOnly & !UpdateHelper.IsGroupAdmin(update) &
                                        !UpdateHelper.Devs.Contains(update.Message.From.Id) &
                                        !UpdateHelper.IsGlobalAdmin(update.Message.From.Id))
                                    {
                                        Send(GetLocaleString("GroupAdminOnly", GetLanguage(update.Message.Chat.Id)),
                                            id);
                                        return;
                                    }
                                    if (command.InGroupOnly & update.Message.Chat.Type == ChatType.Private)
                                    {
                                        Send(GetLocaleString("GroupCommandOnly", GetLanguage(id)), id);
                                        return;
                                    }
                                    Bot.CommandsReceived++;
                                    command.Method.Invoke(update, args);
                                }


                                #endregion
                            }
                            else if (update.Message.Chat.Type == ChatType.Private &&
                                     update.Message?.ReplyToMessage?.Text ==
                                     "Please reply to this message with your Telegram authorization code" &&
                                     update.Message.From.Id == UpdateHelper.Devs[0])
                            {
                                CLI.AuthCode = update.Message.Text;
                            }
                            else if (update.Message.Chat.Type == ChatType.Private &&
                                     (update.Message?.ReplyToMessage?.From?.Id ?? 0) == Bot.Me.Id &&
                                     (update.Message?.ReplyToMessage?.Text?.Contains(
                                          "Please enter a whole number, in US Dollars (USD)") ?? false))
                            {
                                Commands.ValidateDonationAmount(update.Message);
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
                            else if (update.Message.Chat.Type == ChatType.Private &&
                                     (update.Message?.ReplyToMessage?.From?.Id ?? 0) == Bot.Me.Id &&
                                     (update.Message?.ReplyToMessage?.Text?.Contains(
                                          "send me the GIF you want to use for this situation, as a reply") ??
                                      false))
                            {
                                Commands.AddGif(update.Message);
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
                                        Program.Analytics.TrackAsync("botremoved", m, m.From?.Id.ToString() ?? "0");
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
                                    Program.Analytics.TrackAsync("botadded", m, m.From?.Id.ToString() ?? "0");
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
                                else if (m.NewChatMember != null && m.Chat.Id == Settings.VeteranChatId)
                                {
                                    var uid = m.NewChatMember.Id;
                                    //check that they are allowed to join.
                                    var p = DB.Players.FirstOrDefault(x => x.TelegramId == uid);
                                    var gamecount = p?.GamePlayers.Count ?? 0;
                                    if (gamecount >= 500)
                                    {
                                        Send($"{m.NewChatMember.FirstName.FormatHTML()} has played {gamecount} games",
                                            m.Chat.Id);
                                        return;
                                    }
                                    //user has not reach veteran
                                    Send(
                                        $"{m.NewChatMember.FirstName.FormatHTML()} removed, as they have not unlocked veteran ({gamecount} games played, need 500)",
                                        m.Chat.Id);
                                    Commands.KickChatMember(Settings.VeteranChatId, uid);
                                }
                                else if (m.NewChatMember != null && m.Chat.Id == Settings.SupportChatId)
                                {
                                    var uid = m.NewChatMember.Id;
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
                        case MessageType.VenueMessage:
                            break;
                        case MessageType.SuccessfulPayment:
                            HandleSuccessfulPayment(update.Message);
                            break;
                        case MessageType.GameMessage:
                            break;
                        case MessageType.VideoNoteMessage:
                            break;
                        case MessageType.Invoice:
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
                    Send($"Error: {ex.Message}\n{update.Message?.Text}", Settings.ErrorGroup);
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                        ex = ex.InnerException;

                    Send(ex.Message, id);
                    Send($"Error: {ex.Message}\n{update.Message?.Text}", Settings.ErrorGroup);
                }
#endif
            }
        }

        private static void HandleSuccessfulPayment(Message message)
        {
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

        private static void HandlePayment(PreCheckoutQuery q)
        {
            ////get the amount paid
            //var amt = q.TotalAmount / 100;

            //using (var db = new WWContext())
            //{
            //    //get the player
            //    var p = db.Players.FirstOrDefault(x => x.TelegramId == q.From.Id);
            //    if (p == null)
            //    {
            //        //wtf??
            //        Bot.Send($"Successfully received ${amt} from you! YAY!\n\nHowever, we do not see any record of you in our database, so we can't record it.  Please message @ParaCode with this information, and a screenshot", q.From.Id);
            //        return;
            //    }
            //    if (p.DonationLevel == null)
            //        p.DonationLevel = 0;
            //    p.DonationLevel += amt;
            //    var level = p.DonationLevel ?? 0;
            //    var badge = "";
            //    if (level >= 100)
            //        badge += " 🥇";
            //    else if (level >= 50)
            //        badge += " 🥈";
            //    else if (level >= 10)
            //        badge += " 🥉";
            //    if (p.Founder ?? false)
            //        badge += "💎";
            //
            //    Bot.Send($"Successfully received ${amt} from you! YAY!\nTotal Donated: ${level}\nCurrent Badge (ingame): {badge}", q.From.Id);
            //    //check to see how many people have purchased gif packs

            //    if (level > 10)
            //    {
            //        var packs = db.Players.Count(x => x.GifPurchased == true);
            //        if (packs >= 100)
            //        {
            //            //do nothing, they didn't unlock it.
            //        }
            //        else
            //        {
            //            p.GifPurchased = true;
            //            CustomGifData data;
            //            var json = p.CustomGifSet;
            //            if (String.IsNullOrEmpty(json))
            //                data = new CustomGifData();
            //            else
            //                data = JsonConvert.DeserializeObject<CustomGifData>(json);
            //            if (!data.HasPurchased)
            //            {
            //                Bot.Send(
            //                    "Congratulations! You have unlocked Custom Gif Packs :)\nUse /customgif to build your pack, /submitgif to submit for approval",
            //                    q.From.Id);
            //            }
            //            data.HasPurchased = true;

            //            json = JsonConvert.SerializeObject(data);
            //            p.CustomGifSet = json;
            //        }
            //    }
            //    db.SaveChanges();
            //}
            Bot.Api.AnswerPreCheckoutQueryAsync(q.Id, true);
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

        public static void CallbackReceived(object sender, CallbackQueryEventArgs e)
        {
            Bot.MessagesReceived++;
            new Task(() => { HandleCallback(e.CallbackQuery); }).Start();
        }


        internal static void HandleCallback(CallbackQuery query)
        {
            Bot.MessagesProcessed++;
            Program.Analytics.TrackAsync("callback", query, query.From.Id.ToString());
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
                    Program.Analytics.TrackAsync($"cb:{args[0]}", new { args = args }, query.From.Id.ToString());

                    if (args[0] == "donatetg")
                    {
                        Commands.GetDonationInfo(query);
                        return;
                    }

                    if (args[0] == "cancel")
                    {
                        Bot.Api.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
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
                        node?.SendReply(query);
                        return;
                    }
                    if (new[] { "reviewgifs", "approvesfw", "approvensfw" }.Contains(args[0]))
                    {
                        if (UpdateHelper.IsGlobalAdmin(query.From.Id))
                        {
                            CustomGifData pack;
                            Player by;
                            string json;
                            //get player target
                            var pid = int.Parse(args[1]);
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
                                    Bot.Api.SendDocumentAsync(id, pack.CultWins, "Cult Wins");
                                    Bot.Api.SendDocumentAsync(id, pack.LoversWin, "Lovers Win");
                                    Thread.Sleep(250);
                                    Bot.Api.SendDocumentAsync(id, pack.NoWinner, "No Winner");
                                    Bot.Api.SendDocumentAsync(id, pack.SerialKillerWins, "SK Wins");
                                    Thread.Sleep(250);
                                    Bot.Api.SendDocumentAsync(id, pack.StartChaosGame, "Chaos Start");
                                    Bot.Api.SendDocumentAsync(id, pack.StartGame, "Normal Start");
                                    Thread.Sleep(250);
                                    Bot.Api.SendDocumentAsync(id, pack.TannerWin, "Tanner Wins");
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
                                    id = query.From.Id;
                                    pack.Approved = true;
                                    pack.ApprovedBy = id;
                                    pack.NSFW = nsfw;
                                    msg = $"Approval Status: ";
                                    by = DB.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                                    msg += "Approved By " + by.Name + "\nNSFW: " + pack.NSFW;
                                    tplayer.CustomGifSet = JsonConvert.SerializeObject(pack);
                                    DB.SaveChanges();
                                    Bot.Send(msg, query.Message.Chat.Id);
                                    Bot.Send(msg, pid);
                                    Bot.Api.DeleteMessageAsync(query.Message.Chat.Id, query.Message.MessageId);
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
                                    id = query.From.Id;
                                    pack.Approved = true;
                                    pack.ApprovedBy = id;
                                    pack.NSFW = nsfw;
                                    msg = $"Approval Status: ";
                                    by = DB.Players.FirstOrDefault(x => x.TelegramId == pack.ApprovedBy);
                                    msg += "Approved By " + by.Name + "\nNSFW: " + pack.NSFW;
                                    tplayer.CustomGifSet = JsonConvert.SerializeObject(pack);
                                    DB.SaveChanges();
                                    Bot.Send(msg, query.Message.Chat.Id);
                                    Bot.Send(msg, pid);
                                    Bot.Api.DeleteMessageAsync(query.Message.Chat.Id, query.Message.MessageId);
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
                    else if (new[] { "status", "validate", "upload" }.Contains(command))
                    {
                        //global admin only commands
                        if (!UpdateHelper.Devs.Contains(query.From.Id) && !UpdateHelper.IsGlobalAdmin(query.From.Id))
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
                    if (choice == "cancel")
                    {
                        Bot.ReplyToCallback(query, GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                        return;
                    }
                    grp?.UpdateFlags();
                    var Yes = GetLocaleString("Yes", language);
                    var No = GetLocaleString("No", language);
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
                            var userid = int.Parse(args[2]);
                            try
                            {
                                var para = DB.Players.FirstOrDefault(x => x.Id == userid);

                                //get all the players Para has played with
                                var ohaiplayers = (from g in DB.Games
                                                   join gp in DB.GamePlayers on g.Id equals gp.GameId
                                                   join gp2 in DB.GamePlayers on g.Id equals gp2.GameId
                                                   join pl in DB.Players on gp2.PlayerId equals pl.Id
                                                   where gp.PlayerId == para.Id
                                                   select pl).Distinct();

                                //figure out which players don't have the achievement

                                //update the message
                                var ohaimsg = $"Found {ohaiplayers.Count()} players that have earned OHAIDER.";
                                Bot.Edit(query, ohaimsg);
                                var count = 0;
                                foreach (var player in ohaiplayers)
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
                            return;
                        case "restore":
                            var oldid = int.Parse(args[1]);
                            var newid = int.Parse(args[2]);
                            var result = DB.RestoreAccount(oldid, newid);
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
                                menu = new InlineKeyboardMarkup(new[] { "Normal", "Overloaded", "Recovering", "API Bug", "Offline", "Maintenance" }.Select(x => new[] { new InlineKeyboardCallbackButton(x, $"status|{groupid}|{choice}|{x}") }).ToArray());
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
                        case "preferred":
                            var grpid = grp.Id;
                            var rankings = DB.GroupRanking.Where(x => x.GroupId == grpid).ToList();
                            var lang = args[2];
                            if (lang == "null") //preferred
                            {
                                if (args[3] == "toggle")
                                    //toggle preferred
                                    grp.Preferred = !(grp.Preferred ?? true);
                                //say if they're preferred
                                Bot.ReplyToCallback(query, "Global: " + (grp.Preferred == false ? "disabled" : "enabled"), false);
                                if (args[3] == "info")
                                    return;
                            }
                            else
                            {
                                //get the ranking
                                var ranking = rankings.FirstOrDefault(x => x.Language == lang);
                                if (args[3] == "toggle")
                                    //toggle show
                                    ranking.Show = !(ranking.Show ?? true);
                                //say if they're shown
                                Bot.ReplyToCallback(query, lang + ": " + (ranking.Show == false ? "disabled" : "enabled"), false);
                                if (args[3] == "info")
                                    return;
                            }
                            DB.SaveChanges();
                            //make the menu
                            var rows = rankings.Select(x => new[] {
                                new InlineKeyboardCallbackButton(x.Language, $"preferred|{grp.GroupId}|{x.Language}|info"),
                                new InlineKeyboardCallbackButton(x.Show == false ? "☑️" : "✅", $"preferred|{grp.GroupId}|{x.Language}|toggle")
                            }).ToList();
                            //add a button at the beginning and at the end
                            rows.Insert(0, new[] {
                                new InlineKeyboardCallbackButton("Global", $"preferred|{grp.GroupId}|null|info"),
                                new InlineKeyboardCallbackButton(grp.Preferred == false ? "☑️" : "✅", $"preferred|{grp.GroupId}|null|toggle")
                            });
                            rows.Add(new[] { new InlineKeyboardCallbackButton("Done", "done") });
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
                                LanguageHelper.ValidateFiles(query.Message.Chat.Id, query.Message.MessageId);
                                return;
                            }

                            if (args[4] != "base" && args[3] == "All")
                            {
                                LanguageHelper.ValidateFiles(query.Message.Chat.Id, query.Message.MessageId, choice);
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
                            //Bot.SendTextMessageAsync(id, "", replyToMessageId: update.Message.MessageId, replyMarkup: menu);
                            LanguageHelper.ValidateLanguageFile(query.Message.Chat.Id, vlang.FilePath, query.Message.MessageId);
                            return;
                        case "upload":
                            Console.WriteLine(choice);
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
                                    buttons = new List<InlineKeyboardButton>() { new InlineKeyboardCallbackButton(GetLocaleString("All", language), $"groups|{query.From.Id}|{choice}|all") };
                                    buttons.AddRange(variants.OrderBy(x => x).Select(x => new InlineKeyboardCallbackButton(x, $"groups|{query.From.Id}|{choice}|{x}")));
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

                                    Bot.ReplyToCallback(query, message, replyMarkup: new InlineKeyboardMarkup(variantMenu.ToArray()), parsemode:ParseMode.Html);
                                    break;
                                }
                            }
                           

                            var groups = PublicGroups.ForLanguage(choice, variant).ToList().GroupBy(x => x.GroupId).Select(x => x.First()).OrderByDescending(x => x.LastRefresh).ThenByDescending(x => x.Ranking).Take(10).ToList();
                            var variantmsg = args[3] == "all" ? "" : (" " + variant);
                           
                            Bot.ReplyToCallback(query, GetLocaleString("HereIsList", language, choice + variantmsg));
                            if (groups.Count() > 5)
                            {
                                //need to split it
                                var reply = groups.Take(5).Aggregate("",
                                    (current, g) =>
                                        current +
                                        $"<a href=\"{g.GroupLink}\">{g.Name.FormatHTML()}</a>\n\n");
                                Send(reply, query.Message.Chat.Id);
                                Thread.Sleep(500);
                                reply = groups.Skip(5).Aggregate("",
                                    (current, g) =>
                                        current +
                                        $"<a href=\"{g.GroupLink}\">{g.Name.FormatHTML()}</a>\n\n");
                                Send(reply, query.Message.Chat.Id);
                            }
                            else
                            {
                                var reply = groups.Aggregate("",
                                    (current, g) =>
                                        current +
                                        $"<a href=\"{g.GroupLink}\">{g.Name.FormatHTML()}</a>\n\n");
                                Send(reply, query.Message.Chat.Id);
                            }

                            break;
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
                        case "stopwaiting":
                            using (var db = new WWContext())
                                db.Database.ExecuteSqlCommand($"DELETE FROM NotifyGame WHERE GroupId = {groupid} AND UserId = {query.From.Id}");
                            Bot.ReplyToCallback(query, GetLocaleString("DeletedFromWaitList", grp.Language, grp.Name));
                            break;
                        #endregion
                        #region Config Commands
                        case "lang":
                            //load up each file and get the names
                            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();

                            buttons.Clear();
                            buttons.AddRange(langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => new InlineKeyboardCallbackButton(x, $"setlang|{groupid}|{x}|null|base")));

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
                                    Bot.Api.AnswerCallbackQueryAsync(query.Id, GetLocaleString("LangSet", language, slang.Base + (String.IsNullOrWhiteSpace(slang.Variant) ? "" : ": " + slang.Variant)));
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
                        //    buttons.Add(new InlineKeyboardCallbackButton("Yes", $"setonline|{groupid}|show"));
                        //    buttons.Add(new InlineKeyboardCallbackButton("No", $"setonline|{groupid}|hide"));
                        //    buttons.Add(new InlineKeyboardCallbackButton("Cancel", $"setonline|{groupid}|cancel"));
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
                        //    buttons.Add(new InlineKeyboardCallbackButton(Yes, $"setflee|{groupid}|enable"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(No, $"setflee|{groupid}|disable"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setflee|{groupid}|cancel"));
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
                            buttons.Add(new InlineKeyboardCallbackButton("10", $"setmaxplayer|{groupid}|10"));
                            buttons.Add(new InlineKeyboardCallbackButton("15", $"setmaxplayer|{groupid}|15"));
                            buttons.Add(new InlineKeyboardCallbackButton("20", $"setmaxplayer|{groupid}|20"));
                            buttons.Add(new InlineKeyboardCallbackButton("25", $"setmaxplayer|{groupid}|25"));
                            buttons.Add(new InlineKeyboardCallbackButton("30", $"setmaxplayer|{groupid}|30"));
                            buttons.Add(new InlineKeyboardCallbackButton("35", $"setmaxplayer|{groupid}|35"));
                            buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setmaxplayer|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("MaxPlayersQ", language, grp.MaxPlayers ?? Settings.MaxPlayers),
                                replyMarkup: menu);
                            break;
                        case "setmaxplayer":

                            grp.MaxPlayers = int.Parse(choice);
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetLocaleString("MaxPlayersA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        //case "roles":
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Show", language), $"setroles|{groupid}|show"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Hide", language), $"setroles|{groupid}|hide"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setroles|{groupid}|cancel"));
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
                            buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("NormalOnly", language), $"setmode|{groupid}|Normal"));
                            buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("ChaosOnly", language), $"setmode|{groupid}|Chaos"));
                            buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("PlayerChoice", language), $"setmode|{groupid}|Player"));
                            buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setmode|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("GameModeQ", language, grp.Mode), replyMarkup: menu);
                            break;
                        case "setmode":

                            grp.Mode = choice;
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetLocaleString("GameModeA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "endroles":
                            buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("ShowNone", language), $"setendroles|{groupid}|None"));
                            buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("ShowLiving", language), $"setendroles|{groupid}|Living"));
                            buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("ShowAll", language), $"setendroles|{groupid}|All"));
                            buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setendroles|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("ShowRolesEndQ", language, grp.ShowRolesEnd),
                                replyMarkup: menu);
                            break;
                        case "setendroles":
                            grp.ShowRolesEnd = choice;
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetLocaleString("ShowRolesEndA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "day":
                            buttons.Add(new InlineKeyboardCallbackButton("90", $"setday|{groupid}|30"));
                            buttons.Add(new InlineKeyboardCallbackButton("120", $"setday|{groupid}|60"));
                            buttons.Add(new InlineKeyboardCallbackButton("150", $"setday|{groupid}|90"));
                            buttons.Add(new InlineKeyboardCallbackButton("180", $"setday|{groupid}|120"));
                            buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setday|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("SetDayTimeQ", language, Settings.TimeDay + 60, (grp.DayTime ?? Settings.TimeDay) + 60),
                                replyMarkup: menu);
                            break;
                        case "setday":
                            grp.DayTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetLocaleString("SetDayTimeA", language, int.Parse(choice) + 60));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "night":
                            buttons.Add(new InlineKeyboardCallbackButton("30", $"setnight|{groupid}|30"));
                            buttons.Add(new InlineKeyboardCallbackButton("60", $"setnight|{groupid}|60"));
                            buttons.Add(new InlineKeyboardCallbackButton("90", $"setnight|{groupid}|90"));
                            buttons.Add(new InlineKeyboardCallbackButton("120", $"setnight|{groupid}|120"));
                            buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setnight|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("SetNightTimeQ", language, Settings.TimeNight, grp.NightTime ?? Settings.TimeNight),
                                replyMarkup: menu);
                            break;
                        case "setnight":

                            grp.NightTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetLocaleString("SetNightTimeA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "lynch":
                            buttons.Add(new InlineKeyboardCallbackButton("30", $"setlynch|{groupid}|30"));
                            buttons.Add(new InlineKeyboardCallbackButton("60", $"setlynch|{groupid}|60"));
                            buttons.Add(new InlineKeyboardCallbackButton("90", $"setlynch|{groupid}|90"));
                            buttons.Add(new InlineKeyboardCallbackButton("120", $"setlynch|{groupid}|120"));
                            buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setlynch|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("SetLynchTimeQ", language, Settings.TimeLynch, grp.LynchTime ?? Settings.TimeLynch),
                                replyMarkup: menu);
                            break;
                        case "setlynch":
                            grp.LynchTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetLocaleString("SetLynchTimeA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        //case "fool":
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Allow", language), $"setfool|{groupid}|true"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Disallow", language), $"setfool|{groupid}|false"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setfool|{groupid}|cancel"));
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
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Allow", language), $"settanner|{groupid}|true"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Disallow", language), $"settanner|{groupid}|false"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"settanner|{groupid}|cancel"));
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
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Allow", language), $"setsecretlynch|{groupid}|true"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Disallow", language), $"setsecretlynch|{groupid}|false"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setsecretlynch|{groupid}|cancel"));
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
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Allow", language), $"setcult|{groupid}|true"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Disallow", language), $"setcult|{groupid}|false"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setcult|{groupid}|cancel"));
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
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Allow", language), $"setextend|{groupid}|true"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString("Disallow", language), $"setextend|{groupid}|false"));
                        //    buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setextend|{groupid}|cancel"));
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
                            buttons.Add(new InlineKeyboardCallbackButton("60", $"setmaxextend|{groupid}|60"));
                            buttons.Add(new InlineKeyboardCallbackButton("120", $"setmaxextend|{groupid}|120"));
                            buttons.Add(new InlineKeyboardCallbackButton("180", $"setmaxextend|{groupid}|180"));
                            buttons.Add(new InlineKeyboardCallbackButton("240", $"setmaxextend|{groupid}|240"));
                            buttons.Add(new InlineKeyboardCallbackButton("300", $"setmaxextend|{groupid}|300"));
                            buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"setmaxextend|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.ReplyToCallback(query,
                                GetLocaleString("MaxExtendQ", language, Settings.MaxExtend, grp.MaxExtend ?? Settings.MaxExtend), replyMarkup: menu);
                            break;
                        case "setmaxextend":
                            grp.MaxExtend = int.Parse(choice);
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetLocaleString("MaxExtendA", language, choice));
                            Bot.ReplyToCallback(query,
                                GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "done":
                            Bot.ReplyToCallback(query,
                                GetLocaleString("ThankYou", language));
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
                                Bot.Api.AnswerCallbackQueryAsync(query.Id, GetLocaleString($"{chosen.ToString()}A", language, current ? GetLocaleString(pos, language) : GetLocaleString(neg, language)));
                                Bot.ReplyToCallback(query, GetLocaleString("WhatToDo", language), replyMarkup: GetConfigMenu(groupid));
                                DB.SaveChanges();
                            }
                            else
                            {

                                buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString(pos, language), $"set{command}|{groupid}|true"));
                                buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString(neg, language), $"set{command}|{groupid}|false"));
                                buttons.Add(new InlineKeyboardCallbackButton(Cancel, $"set{command}|{groupid}|cancel"));

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
            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();
            var isBase = args[4] == "base";
            if (isBase)
            {
                var variants = langs.Where(x => x.Base == args[2]).ToList();
                if (variants.Count() > 1)
                {
                    var buttons = new List<InlineKeyboardButton>();
                    buttons.AddRange(variants.Select(x => new InlineKeyboardCallbackButton(x.Variant, $"{command}|{args[1]}|{x.Base}|{x.Variant}|v")));
                    if (addAllbutton)
                        buttons.Insert(0, new InlineKeyboardCallbackButton("All", $"{command}|{args[1]}|{args[2]}|All|v"));

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
            try
            {
                var values = strings.Descendants("value");
                var choice = Bot.R.Next(values.Count());
                var selected = values.ElementAt(choice);
                return String.Format(selected.Value.FormatHTML(), args).Replace("\\n", Environment.NewLine);
            }
            catch (Exception e)
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

        internal static InlineKeyboardMarkup GetConfigMenu(long id)
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            //base menu
            //buttons.Add(new InlineKeyboardCallbackButton("Show Online Message", $"online|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton("Change Language", $"lang|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton("Change Game Mode", $"mode|{id}"));
            //buttons.Add(new InlineKeyboardCallbackButton("Show Roles On Death", $"roles|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton("Show Roles At Game End", $"endroles|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton("Allow Fleeing", $"flee|{id}")); //TODO add
            //buttons.Add(new InlineKeyboardCallbackButton("Allow Extending Timer", $"extend|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton("Set Max Players", $"maxplayer|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton("Set Max Extend Time", $"maxextend|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton("Set Day Timer", $"day|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton("Set Lynch Timer", $"lynch|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton("Set Night Timer", $"night|{id}"));
            //buttons.Add(new InlineKeyboardCallbackButton("Allow Fool", $"fool|{id}"));
            //buttons.Add(new InlineKeyboardCallbackButton("Allow Tanner", $"tanner|{id}"));  //DONE
            //buttons.Add(new InlineKeyboardCallbackButton("Allow Cult", $"cult|{id}"));
            //buttons.Add(new InlineKeyboardCallbackButton("Enable Secret Lynch", $"secretlynch|{id}"));  //DONE
            foreach (var flag in Enum.GetValues(typeof(GroupConfig)).Cast<GroupConfig>())
            {
                if (!flag.IsEditable()) continue;
                //get the flag, determine the shortname and make a button out of it.
                buttons.Add(new InlineKeyboardCallbackButton(GetLocaleString(flag.ToString(), GetLanguage(id)), $"{flag.GetInfo().ShortName}|{id}"));
            }

            buttons.Add(new InlineKeyboardCallbackButton("Done", $"done"));
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
            Bot.MessagesReceived++;
            new Task(() => { HandleInlineQuery(e.InlineQuery); }).Start();
        }

        internal static void HandleInlineQuery(InlineQuery q)
        {

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
            Program.Analytics.TrackAsync("inline", q, q.From.Id.ToString());
            Bot.Api.AnswerInlineQueryAsync(q.Id, choices.Select(c => new InlineQueryResultArticle()
            {
                Description = c.Description,
                Id = c.Command,
                Title = c.Command,
                InputMessageContent = new InputTextMessageContent
                {
                    DisableWebPagePreview = true,
                    MessageText = c.Content,
                    ParseMode = ParseMode.Html
                }
            }).Cast<InlineQueryResult>().ToArray(), 0, true);
        }
    }
}

