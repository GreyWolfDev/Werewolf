using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;
using RegHelper = Werewolf_Control.Helpers.RegHelper;

#pragma warning disable IDE0060 // Remove unused parameter

namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Attributes.Command(Trigger = "donate")]
        public static void Donate(Update u, string[] args)
        {
            // Donations disabled as of 2024-06-08
            var link = $"<a href=\"https://t.me/greywolfdev/1318\">currently disabled</a>";
            Bot.Api.SendTextMessageAsync(chatId: u.Message.Chat.Id, text: $"Donations are {link}, sorry!", parseMode: ParseMode.Html, disableWebPagePreview: true).Wait();
            return;

            //Bot.Api.SendTextMessageAsync(u.Message.Chat.Id, 
            //    "Want to help keep Werewolf online? Please donate to:\n" +
            //    "•PayPal: PayPal.me/greywolfdevelopment\n" +
            //    "•Bitcoin: 13QvBKfAattcSxSsW274fbgnKU5ASpnK3A" +
            //    "\n\nDonations help us pay to keep the expensive servers running and the game online. Every donation you make helps to keep us going for another month. For more information please contact @werewolfsupport", ParseMode.Html, true);
            var menu = new Menu();
            if (u.Message.Chat.Type == ChatType.Private)
            {
#if !BETA
                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("Telegram", "donatetg"));
                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("Xsolla", "xsolla"));
#else
                menu.Buttons.Add(InlineKeyboardButton.WithUrl("Telegram", $"https://t.me/werewolfbot?start=donatetg"));
                menu.Buttons.Add(InlineKeyboardButton.WithUrl("Xsolla", $"https://t.me/werewolfbot?start=xsolla"));
#endif
            }
            else
            {
                menu.Buttons.Add(InlineKeyboardButton.WithUrl("Telegram", $"https://t.me/werewolfbot?start=donatetg"));
                menu.Buttons.Add(InlineKeyboardButton.WithUrl("Xsolla", $"https://t.me/werewolfbot?start=xsolla"));
            }
            var markup = menu.CreateMarkupFromMenu();
            var txt = $"Want to help keep Werewolf Moderator online? Donate now and gets: {"Custom gifs".ToBold()} and {"Badges".ToBold()}!\n\nClick the button below to donate!!\n\nMore Info: https://telegra.ph/Custom-Gif-Packs-and-Donation-Levels-06-27";
            Bot.Api.SendTextMessageAsync(chatId: u.Message.Chat.Id, text: txt, replyMarkup: markup, parseMode: ParseMode.Html, disableWebPagePreview: true, messageThreadId: u.Message.MessageThreadId);
        }

        [Attributes.Command(Trigger = "donatenew", DevOnly = true)]
        public static void DonateNew(Update u, string[] args)
        {
            // Donations re-enabl preparation 2025-08-08
            var menu = new Menu();
            if (u.Message.Chat.Type == ChatType.Private)
            {
#if !BETA
                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("Telegram", "donatetg"));
#else
                menu.Buttons.Add(InlineKeyboardButton.WithUrl("Telegram", $"https://t.me/werewolfbot?start=donatetgnew"));
#endif
            }
            else
            {
                menu.Buttons.Add(InlineKeyboardButton.WithUrl("Telegram", $"https://t.me/werewolfbot?start=donatetgnew"));
            }
            var markup = menu.CreateMarkupFromMenu();
            var txt = $"Want to help keep Werewolf Moderator online? Donate now and gets: {"Custom gifs".ToBold()} and {"Badges".ToBold()}!\n\nClick the button below to donate!!\n\nMore Info: https://telegra.ph/Custom-Gif-Packs-and-Donation-Levels-06-27";
            Bot.Api.SendTextMessageAsync(chatId: u.Message.Chat.Id, text: txt, replyMarkup: markup, parseMode: ParseMode.Html, disableWebPagePreview: true, messageThreadId: u.Message.MessageThreadId);
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
                    data = new CustomGifData
                    {
                        HasPurchased = true
                    };
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

                Bot.Api.SendTextMessageAsync(chatId: u.Message.From.Id,
                    text: "Ready to build your custom gif pack? Great! Before we begin, a few notes you should be aware of:\n" +
                    "• Your pack will be submitted for approval.  An admin will check it, and once approved, you can start using it in games\n" +
                    "• NSFW packs will be marked NSFW, and only groups that allow them will use it.  Check with your group admin first.\n" +
                    "• Gifs that will NOT be allowed:\n" +
                    " - Gifs containing brutal images\n" +
                    " - Gifs containing illegal content\n" +
                    " - Others, at our discretion\n" +
                    "• I will send you a description of the image, to which you will reply (to the message) with the gif you want to use\n" +
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
            var images = new[] { "Villager Eaten", "Lone Wolf Wins", "Wolf Pack Win", "Village Wins", "Tanner Wins", "Cult Wins", "Serial Killer Wins", "Lovers Win", "No Winner", "Normal Start", "Chaos Start", "SK Killed", "Arsonist Wins", "Burn to death" };
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
                m.Buttons.Add(InlineKeyboardButton.WithCallbackData(i, "customgif|" + i));
            }
            m.Buttons.Add(InlineKeyboardButton.WithCallbackData("Show Badge: " + (d.ShowBadge ? "✅" : "🚫"), "customgif|togglebadge"));
            m.Buttons.Add(InlineKeyboardButton.WithCallbackData("❗️ RESET GIFS ❗️", "customgif|resetgifs"));
            m.Buttons.Add(InlineKeyboardButton.WithCallbackData("Done for now", "cancel|cancel|cancel"));
            m.Buttons.Add(InlineKeyboardButton.WithCallbackData("Submit for approval", "customgif|submit"));

            return m.CreateMarkupFromMenu();
        }

        public static void RequestGif(CallbackQuery q)
        {
            Bot.Api.DeleteMessageAsync(chatId: q.From.Id, messageId: q.Message.MessageId);
            var choice = q.Data.Split('|')[1].Split(' ')[0];
            if (choice == "submit")
            {
                using (var db = new WWContext())
                {
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == q.From.Id);
                    if (p != null)
                    {
                        var json = p?.CustomGifSet;
                        if ((p.DonationLevel ?? 0) < 10)
                        {
                            Bot.Send("You have not unlocked a custom GIF pack.  Please use /donate", q.From.Id);
                            return;
                        }
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
                        if (data.LastSubmit.AddMinutes(60) >= DateTime.UtcNow)
                        {
                            Bot.Send($"You have already submitted your GIF pack in the last hour! If you submit your GIFs and then change something before it gets approved, you don't have to submit it again! Please wait patiently while your GIFs are being reviewed.", q.From.Id, customMenu: GetGifMenu(data));
                            return;
                        }
                        data.Submitted = true;
                        data.LastSubmit = DateTime.UtcNow;
                        p.CustomGifSet = JsonConvert.SerializeObject(data);
                        db.SaveChanges();
                    }
                }
                var menu = new Menu(2);
                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("Review", "reviewgifs|" + q.From.Id));
                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("Dismiss", $"dismiss|" + q.From.Id));
                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("Approved: SFW", "approvesfw|" + q.From.Id));
                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("Approved: NSFW", "approvensfw|" + q.From.Id));
                Bot.Send($"User {q.From.Id} - {(q.From.Username == null ? q.From.FirstName : $"@{q.From.Username}")} - has submitted a gif pack for approval", Settings.AdminChatId, customMenu: menu.CreateMarkupFromMenu());
                Bot.Send("Your GIF pack has been submitted to the admins for approval!\n\nPlease keep in mind that admins have a huge number of GIF packs to review, hence this process may take up to 2-5 working days.\n\nOur admins will keep track of the submitted GIF packs, you don’t have to PM them directly. Thank you for your patience!", q.From.Id);
                return;
            }
            if (choice == "togglebadge")
            {
                using (var db = new WWContext())
                {
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == q.From.Id);
                    if ((p.DonationLevel ?? 0) < 10)
                    {
                        Bot.Send("You have not unlocked a custom GIF pack.  Please use /donate", q.From.Id);
                        return;
                    }
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
                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("❗️ CONFIRM RESET ❗️", "customgif|confirmreset"));
                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("Cancel", "customgif|cancelreset"));
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
            Bot.Api.SendTextMessageAsync(chatId: q.From.Id,
                text: q.Data.Split('|')[1] + "\nOk, send me the GIF you want to use for this situation, as a reply\n" +
                "#" + choice,
                replyMarkup: new ForceReplyMarkup());
        }

        public static void AddGif(Message m)
        {
            using (var db = new WWContext())
            {
                var p = db.Players.FirstOrDefault(x => x.TelegramId == m.From.Id);
                var json = p?.CustomGifSet;

                if (String.IsNullOrEmpty(json) || (p.DonationLevel ?? 0) < 10)
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

                var gifchoice = m.ReplyToMessage.Text;
                gifchoice = gifchoice.Substring(gifchoice.IndexOf("#") + 1);
				
                if (m.Animation == null && m.Document != null)
                {
                    Bot.Api.SendTextMessageAsync(chatId: m.From.Id, text: "This GIF is in old .gif format, but since iOS " +
                        "users are unable to view them, we require you to use telegram's " +
                        "[new GIFs in .mp4 format](https://telegram.org/blog/gif-revolution). " +
                        "To fix this, try reuploading the GIF, your telegram app should then render it as .mp4. " +
                        "Please send me the GIF you want to use for this situation, as a reply\n#" + gifchoice, replyMarkup: new ForceReplyMarkup(), parseMode: ParseMode.Markdown);
                    return;
                }
				if (m.Animation.FileSize >= 1048576) // Maximum size is 1 MB
				{
					Bot.Api.SendTextMessageAsync(chatId: m.From.Id, text: "This GIF is too large, the maximum allowed size is 1MB.\n\n" + 
					"Please send me the GIF you want to use for this situation, as a reply\n#" + gifchoice, 
					replyMarkup: new ForceReplyMarkup());
					return;
				}
				
                var id = m.Animation.FileId;
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
                data.Approved = null;
                data.ApprovedBy = 0;
                p.CustomGifSet = JsonConvert.SerializeObject(data);
                db.SaveChanges();
                Bot.Send("Got it! Any more?", m.From.Id, customMenu: GetGifMenu(data));
            }
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
            // Donations disabled as of 2024-06-08
            var link = $"<a href=\"https://t.me/greywolfdev/1318\">currently disabled</a>";
            Bot.Api.SendTextMessageAsync(chatId: (q?.Message ?? m).Chat.Id, text: $"Donations are {link}, sorry!", parseMode: ParseMode.Html, disableWebPagePreview: true).Wait();
            return;

            var from = q?.From ?? m?.From;
            var txt = "";
            InlineKeyboardMarkup markup = null;
            try
            {
                var res = Program.xsollaClient.PostAsync(Program.XsollaLink, new StringContent(CreateXsollaJson(from), Encoding.UTF8, "application/json")).Result;
                var token = JsonConvert.DeserializeObject<Dictionary<string, string>>(res.Content.ReadAsStringAsync().Result)["token"];
                txt = $"Please click the button below to donate via Xsolla.\nPlease note that this link is ONLY for you and valid for 24 hours.";
                markup = new InlineKeyboardMarkup(new InlineKeyboardButton[][] { new InlineKeyboardButton[] { InlineKeyboardButton.WithUrl("Donate Now!", $"https://tgwerewolf.com/donate/xsolla?uid={from.Id}&token={token}") } });
            }
            catch (Exception e)
            {
                txt = "Error Occurred. This incident has been reported to the devs. Please try again later or seek help at @greywolfsupport.";
                LogException(e, "Xsolla", m.Chat);
            }
            

            if (q != null)
            {
                Bot.Api.EditMessageTextAsync(chatId: q.Message.Chat.Id, messageId: q.Message.MessageId, text: txt, disableWebPagePreview: true, replyMarkup: markup);
            }
            else if (m != null)
            {
                Bot.Api.SendTextMessageAsync(chatId: from.Id, text: txt, disableWebPagePreview: true, replyMarkup: markup, replyToMessageId: m.MessageId);
            }
        }

        public static void GetDonationInfo(CallbackQuery q = null, Message m = null)
        {
            // Donations disabled as of 2024-06-08
            var link = $"<a href=\"https://t.me/greywolfdev/1318\">currently disabled</a>";
            Bot.Api.SendTextMessageAsync(chatId: q?.From.Id ?? m.From.Id, text: $"Donations are {link}, sorry!", parseMode: ParseMode.Html, disableWebPagePreview: true).Wait();
            return;

            var menu = new Menu();
            Bot.Api.SendTextMessageAsync(chatId: q?.From.Id ?? m.From.Id,
                text: "How much would you like to donate?  Please enter a whole number, in US Dollars (USD), in reply to this message",
                replyMarkup: new ForceReplyMarkup());
        }

        public static void GetDonationInfoNew(CallbackQuery q = null, Message m = null)
        {
            // Donations re-enable preparation 2025-08-08
            var menu = new Menu();
            Bot.Api.SendTextMessageAsync(chatId: q?.From.Id ?? m.From.Id,
                text: "How much would you like to donate?  Please enter a whole number, in TON (XTR), in reply to this message",
                replyMarkup: new ForceReplyMarkup());
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
                Bot.Api.SendInvoiceAsync(chatId: m.From.Id, title: "Werewolf Donation", description: "Make a donation to Werewolf to help keep us online", payload: "somepayloadtest", providerToken: api,
                    currency: "USD", prices: new[] { new LabeledPrice("Donation", amt * 100) }, startParameter: "donatetg").Wait();
            }
            else
            {
                Bot.Api.SendTextMessageAsync(chatId: m.From.Id,
                    text: "Invalid input.\n" +
                    "How much would you like to donate?  Please enter a whole number, in US Dollars (USD), in reply to this message",
                    replyMarkup: new ForceReplyMarkup());
            }

        }

        public static void ValidateDonationAmountNew(Message m)
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
                Bot.Api.SendInvoiceAsync(chatId: m.From.Id, title: "Werewolf Donation", description: "Make a donation to Werewolf to help keep us online", payload: "somepayloadtest", providerToken: "",
                    currency: "XTR", prices: new[] { new LabeledPrice("Donation", amt) }, startParameter: "donatetgnew").Wait();
            }
            else
            {
                Bot.Api.SendTextMessageAsync(chatId: m.From.Id,
                    text: "Invalid input.\n" +
                    "How much would you like to donate?  Please enter a whole number, in TON (XTR), in reply to this message",
                    replyMarkup: new ForceReplyMarkup());
            }

        }
    }
}
