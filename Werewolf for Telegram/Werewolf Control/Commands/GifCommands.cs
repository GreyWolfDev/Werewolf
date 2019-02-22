using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;
using RegHelper = Werewolf_Control.Helpers.RegHelper;

namespace Werewolf_Control
{
    public static partial class Commands
    {
#if DEBUG
        public const long GifChannelId = 0;
#else
        public const long GifChannelId = -1001373234685;
#endif
        [Attributes.Command(Trigger = "donate")]
        public static void Donate(Update u, string[] args)
        {
            //Bot.Api.SendTextMessageAsync(u.Message.Chat.Id, 
            //    "Want to help keep Werewolf online? Please donate to:\n" +
            //    "•PayPal: PayPal.me/greywolfdevelopment\n" +
            //    "•Bitcoin: 13QvBKfAattcSxSsW274fbgnKU5ASpnK3A" +
            //    "\n\nDonations help us pay to keep the expensive servers running and the game online. Every donation you make helps to keep us going for another month. For more information please contact @werewolfsupport", ParseMode.Html, true);
            var menu = new Menu();
            if (u.Message.Chat.Type == ChatType.Private)
            {
#if RELEASE
                menu.Buttons.Add(new InlineKeyboardCallbackButton("Telegram", "donatetg"));
                menu.Buttons.Add(new InlineKeyboardCallbackButton("Xsolla", "xsolla"));
#else
                menu.Buttons.Add(new InlineKeyboardUrlButton("Telegram", $"https://t.me/werewolfbot?start=donatetg"));
                menu.Buttons.Add(new InlineKeyboardUrlButton("Xsolla", $"https://t.me/werewolfbot?start=xsolla"));
#endif
            }
            else
            {
                menu.Buttons.Add(new InlineKeyboardUrlButton("Telegram", $"https://t.me/werewolfbot?start=donatetg"));
                menu.Buttons.Add(new InlineKeyboardUrlButton("Xsolla", $"https://t.me/werewolfbot?start=xsolla"));
            }
            var markup = menu.CreateMarkupFromMenu();
            var txt = $"Want to help keep Werewolf Moderator online? Donate now and gets: {"Custom gifs".ToBold()} and {"Badges".ToBold()}!\n\nClick the button below to donate!!\n\nMore Info: https://telegra.ph/Custom-Gif-Packs-and-Donation-Levels-06-27";
            Bot.Api.SendTextMessageAsync(u.Message.Chat.Id, txt, replyMarkup: markup, parseMode: ParseMode.Html, disableWebPagePreview: true);
        }

        [Attributes.Command(Trigger = "customgif")]
        public static void SetCustomGifs(Update u, string[] args)
        {
#if BETA
            Bot.Send("Please use this command with @werewolfbot", u.Message.From.Id);
            return;
#endif
            //check player has access!
            using (var db = new WWContext())
            {
                var p = db.Players.FirstOrDefault(x => x.TelegramId == u.Message.From.Id);
                var json = p?.CustomGifSet;
                if ((p?.DonationLevel ?? 0) < 10)
                {
                    Bot.Send("You have not unlocked a custom GIF pack.  Please use /donate", u.Message.From.Id);
                    return;
                }

                CustomGifData data;
                if (String.IsNullOrEmpty(json))
                {
                    data = new CustomGifData();
                    data.HasPurchased = true;
                    p.GifPurchased = true;
                    p.CustomGifSet = JsonConvert.SerializeObject(data);
                    db.SaveChanges();
                }

                else { data = JsonConvert.DeserializeObject<CustomGifData>(json ?? ""); }
                //if (!data.HasPurchased)
                //{
                //    Bot.Send("You have not unlocked a custom GIF pack.  Please use /donate", u.Message.From.Id);
                //    return;
                //}

                Bot.Api.SendTextMessageAsync(u.Message.From.Id,
                    "Ready to build your custom gif pack? Great! Before we begin, a few notes you should be aware of:\n" +
                    "• Your pack will be submitted for approval.  An admin will check it, and once approved, you can start using it in games\n" +
                    "• NSFW packs will be marked NSFW, and only groups that allow them will use it.  Check with your group admin first.\n" +
                    "• Gifs that will NOT be allowed:\n" +
                    " - Gifs containing brutal images\n" +
                    " - Gifs containing illegal content\n" +
                    " - Others, at our discretion\n" +
                    "• I will send you a description of the image, to which you will reply (to the message) with the gif you want to use\n" +
                    "• Gifs now work in @werewolfbetabot, too!\n" +
                    "\n\n" +
                    "PLEASE NOTE: Changing any gifs will automatically remove the approval for your pack, and an admin will need to approve it again\n" +
                    "Let's begin! Select the situation you want to set a gif for",
                    replyMarkup: GetGifMenu(data));

                var msg = "Current Approval Status:\n";
                switch (data.Approved)
                {
                    case null:
                        msg += "Pending";
                        break;
                    case true:
                        var by = db.Players.FirstOrDefault(x => x.TelegramId == data.ApprovedBy);
                        msg += "Approved By " + by.Name;
                        break;
                    case false:
                        var dby = db.Players.FirstOrDefault(x => x.TelegramId == data.ApprovedBy);
                        msg += "Disapproved By " + dby.Name + " for: " + data.DenyReason;
                        break;
                }
                Bot.Send(msg, u.Message.From.Id);
            }

        }

        public static InlineKeyboardMarkup GetGifMenu(CustomGifData d)
        {
            var m = new Menu(2);
            var images = new[] { "Villager Eaten", "Lone Wolf Wins", "Wolf Pack Win", "Village Wins", "Tanner Wins", "Cult Wins", "Serial Killer Wins", "Lovers Win", "No Winner", "Normal Game Start", "Chaos Game Start", "SK Killed", "Arsonist Wins", "Burn to death" };
            foreach (var img in images)
            {
                var i = img;
                if (d != null)
                {
                    var gifchoice = i.Split(' ')[0];
                    var added = false;
                    switch (gifchoice)
                    {
                        case "Villager":
                            added = d.VillagerDieImage != null;
                            break;
                        case "Lone":
                            added = d.WolfWin != null;
                            break;
                        case "Wolf":
                            added = d.WolvesWin != null;
                            break;
                        case "Village":
                            added = d.VillagersWin != null;
                            break;
                        case "Tanner":
                            added = d.TannerWin != null;
                            break;
                        case "Cult":
                            added = d.CultWins != null;
                            break;
                        case "Serial":
                            added = d.SerialKillerWins != null;
                            break;
                        case "Lovers":
                            added = d.LoversWin != null;
                            break;
                        case "No":
                            added = d.NoWinner != null;
                            break;
                        case "Normal":
                            added = d.StartGame != null;
                            break;
                        case "Chaos":
                            added = d.StartChaosGame != null;
                            break;
                        case "SK":
                            added = d.SKKilled != null;
                            break;
                        case "Arsonist":
                            added = d.ArsonistWins != null;
                            break;
                        case "Burn":
                            added = d.BurnToDeath != null;
                            break;
                    }
                    i += (added ? " ✅" : " 🚫");
                }
                else
                {
                    i += " 🚫";
                }
                m.Buttons.Add(new InlineKeyboardCallbackButton(i, "customgif|" + i));
            }
            m.Buttons.Add(new InlineKeyboardCallbackButton("Show Badge: " + (d.ShowBadge ? "✅" : "🚫"), "customgif|togglebadge"));
            m.Buttons.Add(new InlineKeyboardCallbackButton("❗️ RESET GIFS ❗️", "customgif|resetgifs"));
            m.Buttons.Add(new InlineKeyboardCallbackButton("Done for now", "cancel|cancel|cancel"));
            m.Buttons.Add(new InlineKeyboardCallbackButton("Submit for approval", "customgif|submit"));

            return m.CreateMarkupFromMenu();
        }

        public static void RequestGif(CallbackQuery q)
        {
            Bot.Api.DeleteMessageAsync(q.From.Id, q.Message.MessageId);
            var choice = q.Data.Split('|')[1].Split(' ')[0];
            if (choice == "submit")
            {
                using (var db = new WWContext())
                {
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == q.From.Id);
                    if (p != null)
                    {
                        var json = p?.CustomGifSet;
                        var data = JsonConvert.DeserializeObject<CustomGifData>(json);
                        if (data.Approved != null)
                        {
                            Bot.Send($"Your current GIF pack has already been {(data.Approved == true ? "" : "dis")}approved! You can't submit it again without any changes!", q.From.Id, customMenu: GetGifMenu(data));
                            return;
                        }
                        if (new[] { data.CultWins, data.LoversWin, data.NoWinner, data.SerialKillerWins, data.SKKilled, data.StartChaosGame, data.StartGame, data.TannerWin, data.VillagerDieImage, data.VillagersWin, data.WolfWin, data.WolvesWin, data.ArsonistWins, data.BurnToDeath }.All(x => string.IsNullOrEmpty(x)))
                        {
                            Bot.Send($"Please set at least one GIF before you submit your pack!", q.From.Id, customMenu: GetGifMenu(data));
                            return;
                        }
                        data.Submitted = true;
                        p.CustomGifSet = JsonConvert.SerializeObject(data);
                        db.SaveChanges();
                    }
                }
                var menu = new Menu(2);
                menu.Buttons.Add(new InlineKeyboardCallbackButton("Review", "reviewgifs|" + q.From.Id));
                menu.Buttons.Add(new InlineKeyboardCallbackButton("Dismiss", $"dismiss|" + q.From.Id));
                menu.Buttons.Add(new InlineKeyboardCallbackButton("Approved: SFW", "approvesfw|" + q.From.Id));
                menu.Buttons.Add(new InlineKeyboardCallbackButton("Approved: NSFW", "approvensfw|" + q.From.Id));
                Bot.Send($"User {q.From.Id} - @{q.From.Username} - has submitted a gif pack for approval", Settings.AdminChatId, customMenu: menu.CreateMarkupFromMenu());
                Bot.Send("Your pack has been submitted for approval to the admins.  Please wait while we review.",
                    q.From.Id);
                return;
            }
            if (choice == "togglebadge")
            {
                using (var db = new WWContext())
                {
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == q.From.Id);
                    var json = p?.CustomGifSet;
                    var data = JsonConvert.DeserializeObject<CustomGifData>(json);
                    data.ShowBadge = !data.ShowBadge;
                    p.CustomGifSet = JsonConvert.SerializeObject(data);
                    db.SaveChanges();
                    Bot.Send($"Your badge will {(data.ShowBadge ? "" : "not ")}be shown.", q.From.Id, customMenu: GetGifMenu(data));
                    return;
                }
            }
            if (choice == "resetgifs")
            {
                var menu = new Menu();
                menu.Buttons.Add(new InlineKeyboardCallbackButton("❗️ CONFIRM RESET ❗️", "customgif|confirmreset"));
                menu.Buttons.Add(new InlineKeyboardCallbackButton("Cancel", "customgif|cancelreset"));
                Bot.Send("You are about to reset your custom GIF set! All your saved GIFs will be deleted! You can set them again, but you will not be able to restore your current GIFs. Are you sure you want to continue?", q.From.Id, customMenu: menu.CreateMarkupFromMenu());
                return;
            }
            if (choice == "confirmreset")
            {
                using (var db = new WWContext())
                {
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == q.From.Id);
                    var data = new CustomGifData
                    {
                        HasPurchased = true
                    };
                    p.CustomGifSet = JsonConvert.SerializeObject(data);
                    db.SaveChanges();
                    Bot.Send("Your custom GIFs have successfully been reset.", q.From.Id, customMenu: GetGifMenu(data));
                    return;
                }
            }
            if (choice == "cancelreset")
            {
                using (var db = new WWContext())
                {
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == q.From.Id);
                    var json = p?.CustomGifSet;
                    var data = JsonConvert.DeserializeObject<CustomGifData>(json);
                    Bot.Send($"You cancelled resetting your GIFs.", q.From.Id, customMenu: GetGifMenu(data));
                    return;
                }
            }
            Bot.Api.SendTextMessageAsync(q.From.Id,
                q.Data.Split('|')[1] + "\nOk, send me the GIF you want to use for this situation, as a reply\n" +
                "#" + choice,
                replyMarkup: new ForceReply() { Force = true });
        }

        public static void AddGif(Message m)
        {
            using (var db = new WWContext())
            {
#if !BETA
                var p = db.Players.FirstOrDefault(x => x.TelegramId == m.From.Id);
#else
                var caption = m.Caption;
                var split = caption.Split('|');
                var p = db.Players.FirstOrDefault(x => x.TelegramId.ToString() == split[1]);
#endif
                var json = p?.CustomGifSet;

                if (String.IsNullOrEmpty(json))
                {
                    Bot.Send("You have not unlocked a custom GIF pack.  Please use /donate", m.From.Id);
                    return;
                }
                var data = JsonConvert.DeserializeObject<CustomGifData>(json);
                if (!data.HasPurchased)
                {
                    Bot.Send("You have not unlocked a custom GIF pack.  Please use /donate", m.From.Id);
                    return;
                }

                //figure out which gif
#if !BETA
                var gifchoice = m.ReplyToMessage.Text;
                gifchoice = gifchoice.Substring(gifchoice.IndexOf("#") + 1);
#else
                var gifchoice = split[0];
#endif

                if (m.Document.FileSize >= 1048576) // Maximum size is 1 MB
                {
                    Bot.Api.SendTextMessageAsync(m.From.Id, "This GIF is too large, the maximum allowed size is 1MB.\n\n" +
                    "Please send me the GIF you want to use for this situation, as a reply\n#" + gifchoice,
                    replyMarkup: new ForceReply() { Force = true });
                    return;
                }

                var id = m.Document.FileId;
#if !BETA
                switch (gifchoice)
                {
                    case "Villager":
                        data.VillagerDieImage = id;
                        break;
                    case "Lone":
                        data.WolfWin = id;
                        break;
                    case "Wolf":
                        data.WolvesWin = id;
                        break;
                    case "Village":
                        data.VillagersWin = id;
                        break;
                    case "Tanner":
                        data.TannerWin = id;
                        break;
                    case "Cult":
                        data.CultWins = id;
                        break;
                    case "Serial":
                        data.SerialKillerWins = id;
                        break;
                    case "Lovers":
                        data.LoversWin = id;
                        break;
                    case "No":
                        data.NoWinner = id;
                        break;
                    case "Normal":
                        data.StartGame = id;
                        break;
                    case "Chaos":
                        data.StartChaosGame = id;
                        break;
                    case "SK":
                        data.SKKilled = id;
                        break;
                    case "Arsonist":
                        data.ArsonistWins = id;
                        break;
                    case "Burn":
                        data.BurnToDeath = id;
                        break;
                }
#else
                switch (gifchoice)
                {
                    case "Villager":
                        data.Beta.VillagerDieImage = id;
                        break;
                    case "Lone":
                        data.Beta.WolfWin = id;
                        break;
                    case "Wolf":
                        data.Beta.WolvesWin = id;
                        break;
                    case "Village":
                        data.Beta.VillagersWin = id;
                        break;
                    case "Tanner":
                        data.Beta.TannerWin = id;
                        break;
                    case "Cult":
                        data.Beta.CultWins = id;
                        break;
                    case "Serial":
                        data.Beta.SerialKillerWins = id;
                        break;
                    case "Lovers":
                        data.Beta.LoversWin = id;
                        break;
                    case "No":
                        data.Beta.NoWinner = id;
                        break;
                    case "Normal":
                        data.Beta.StartGame = id;
                        break;
                    case "Chaos":
                        data.Beta.StartChaosGame = id;
                        break;
                    case "SK":
                        data.Beta.SKKilled = id;
                        break;
                    case "Arsonist":
                        data.Beta.ArsonistWins = id;
                        break;
                    case "Burn":
                        data.Beta.BurnToDeath = id;
                        break;
                }
#endif
                data.Approved = null;
                data.ApprovedBy = 0;
                p.CustomGifSet = JsonConvert.SerializeObject(data);
                db.SaveChanges();
#if RELEASE
                SendGifToChannel(p.TelegramId, gifchoice, id);
#endif
                Bot.Send("Got it! Any more?", m.From.Id, customMenu: GetGifMenu(data));
            }
        }

        private static void SendGifToChannel(int pId, string gifchoice, string id)
        {
            Bot.Api.SendDocumentAsync(GifChannelId, id, caption: $"{gifchoice}|{pId}");
            Thread.Sleep(200);
        }

        [Attributes.Command(Trigger = "submitgif")]
        public static void SubmitGifs(Update u, string[] args)
        {

        }

        [Attributes.Command(Trigger = "movegifs", DevOnly = true)]
        public static void MoveGifs(Update u, string[] args)
        {
#if RELEASE
            using (var db = new WWContext())
            {
                foreach (var p in db.Players.Where(x => x.CustomGifSet != null))
                {
                    var gifset = JsonConvert.DeserializeObject<CustomGifData>(p.CustomGifSet);
                    if (gifset.ArsonistWins != null && gifset.Beta.ArsonistWins == null)
                        SendGifToChannel(p.TelegramId, "Arsonist", gifset.ArsonistWins);
                    if (gifset.BurnToDeath != null && gifset.Beta.BurnToDeath == null)
                        SendGifToChannel(p.TelegramId, "Burn", gifset.BurnToDeath);
                    if (gifset.CultWins != null && gifset.Beta.CultWins == null)
                        SendGifToChannel(p.TelegramId, "Cult", gifset.CultWins);
                    if (gifset.LoversWin != null && gifset.Beta.LoversWin == null)
                        SendGifToChannel(p.TelegramId, "Lovers", gifset.LoversWin);
                    if (gifset.NoWinner != null && gifset.Beta.NoWinner == null)
                        SendGifToChannel(p.TelegramId, "No", gifset.NoWinner);
                    if (gifset.SerialKillerWins != null && gifset.Beta.SerialKillerWins == null)
                        SendGifToChannel(p.TelegramId, "Serial", gifset.SerialKillerWins);
                    if (gifset.SKKilled != null && gifset.Beta.SKKilled == null)
                        SendGifToChannel(p.TelegramId, "SK", gifset.SKKilled);
                    if (gifset.StartChaosGame != null && gifset.Beta.StartChaosGame == null)
                        SendGifToChannel(p.TelegramId, "Chaos", gifset.StartChaosGame);
                    if (gifset.StartGame != null && gifset.Beta.StartGame == null)
                        SendGifToChannel(p.TelegramId, "Normal", gifset.StartGame);
                    if (gifset.TannerWin != null && gifset.Beta.TannerWin == null)
                        SendGifToChannel(p.TelegramId, "Tanner", gifset.TannerWin);
                    if (gifset.VillagerDieImage != null && gifset.Beta.VillagerDieImage == null)
                        SendGifToChannel(p.TelegramId, "Villager", gifset.VillagerDieImage);
                    if (gifset.VillagersWin != null && gifset.Beta.VillagersWin == null)
                        SendGifToChannel(p.TelegramId, "Village", gifset.VillagersWin);
                    if (gifset.WolfWin != null && gifset.Beta.WolfWin == null)
                        SendGifToChannel(p.TelegramId, "Lone", gifset.WolfWin);
                    if (gifset.WolvesWin != null && gifset.Beta.WolvesWin == null)
                        SendGifToChannel(p.TelegramId, "Wolf", gifset.WolvesWin);
                    Thread.Sleep(1000);
                }
            }
#endif
        }

        public static string CreateXsollaJson(User from)
        {
            var data = new Models.Xsolla.XsollaData();
            data.User.Id.Value = from.Id.ToString();
            data.User.Name.Value = from.FirstName;
            data.Settings.Currency = "USD";
            data.Settings.ProjectId = Program.xsollaProjId.Value;
            data.Settings.Ui.Theme = "dark";
            var res = JsonConvert.SerializeObject(data, 
                new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    },
                    NullValueHandling = NullValueHandling.Ignore
                });
            return res;
        }

        public static void LogException(Exception e, string reason, Chat chat)
        {
            Send($"=={reason}==\n\nChatId: {chat.Id}\n\n{e.Message}\n{e.StackTrace}", Helpers.Settings.ErrorGroup);
        }

        public static void GetXsollaLink(CallbackQuery q = null, Message m = null)
        {
            var from = q?.From ?? m?.From;
            var txt = "";
            InlineKeyboardMarkup markup = null;
            try
            {
                var res = Program.xsollaClient.PostAsync(Program.XsollaLink, new StringContent(CreateXsollaJson(from), Encoding.UTF8, "application/json")).Result;
                var token = JsonConvert.DeserializeObject<Dictionary<string, string>>(res.Content.ReadAsStringAsync().Result)["token"];
                txt = $"Please click the button below to donate via Xsolla.\nPlease note that this link is ONLY for you and valid for 24 hours.";
                markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][] { new InlineKeyboardButton[] { new InlineKeyboardUrlButton("Donate Now!", $"https://tgwerewolf.com/donate/xsolla?uid={from.Id}&token={token}") } });
            }
            catch (Exception e)
            {
                txt = "Error Occurred. This incident has been reported to the devs. Please try again later or seek help at @werewolfsupport.";
                LogException(e, "Xsolla", m.Chat);
            }
            

            if (q != null)
            {
                Bot.Api.EditMessageTextAsync(q.Message.Chat.Id, q.Message.MessageId, txt, disableWebPagePreview: true, replyMarkup: markup);
            }
            else if (m != null)
            {
                Bot.Api.SendTextMessageAsync(from.Id, txt, disableWebPagePreview: true, replyMarkup: markup, replyToMessageId: m.MessageId);
            }
        }

        public static void GetDonationInfo(CallbackQuery q = null, Message m = null)
        {
            var menu = new Menu();
            Bot.Api.SendTextMessageAsync(q?.From.Id ?? m.From.Id,
                "How much would you like to donate?  Please enter a whole number, in US Dollars (USD), in reply to this message",
                replyMarkup: new ForceReply { Force = true });
        }

        public static void ValidateDonationAmount(Message m)
        {
            var input = m.Text.Replace("$", "");
            var amt = 0;
            if (int.TryParse(input, out amt))
            {
#if DEBUG
                var api = RegHelper.GetRegValue("DebugStripeTestAPI");
#elif BETA
                var api = RegHelper.GetRegValue("BetaStripeProdAPI");
#elif RELEASE
                var api = RegHelper.GetRegValue("MainStripeProdAPI");
#endif
                Bot.Api.SendInvoiceAsync(m.From.Id, "Werewolf Donation", "Make a donation to Werewolf to help keep us online", "somepayloadtest", api,
                    "startparam", "USD", new[] { new LabeledPrice() { Amount = amt * 100, Label = "Donation" } });
            }
            else
            {
                Bot.Api.SendTextMessageAsync(m.From.Id,
                    "Invalid input.\n" +
                    "How much would you like to donate?  Please enter a whole number, in US Dollars (USD), in reply to this message",
                    replyMarkup: new ForceReply { Force = true });
            }

        }
    }
}
