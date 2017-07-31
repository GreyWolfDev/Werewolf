using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;

namespace Werewolf_Control
{
    public static partial class Commands
    {
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
                menu.Buttons.Add(new InlineKeyboardCallbackButton("Telegram", "donatetg"));
            else
            {
                menu.Buttons.Add(new InlineKeyboardUrlButton("Telegram", $"https://t.me/{Bot.Me.Username}?start=donatetg"));
            }
            menu.Buttons.Add(new InlineKeyboardUrlButton("PayPal", "https://PayPal.me/greywolfdevelopment"));
            var markup = menu.CreateMarkupFromMenu();
            Bot.Api.SendTextMessageAsync(u.Message.Chat.Id,
                "Want to help keep Werewolf online?\n" +
                "We now offer some rewards for donating!\n" +
                "Donate $10USD or more to unlock a custom gif pack that you can choose.  There are also donation badges you can get in game.  These badges are added to the end of your name in game, so everyone can see you donated!\n\n" +
                "•$10 USD: 🥉\n" +
                "•$50 USD: 🥈\n" +
                "•$100 USD: 🥇\n\n" +
                "You might also see this special badge: 💎\nThis is reserved for people who donated prior to there being any rewards for donating\n" +
                "We also accept Bitcoin donations at: 13QvBKfAattcSxSsW274fbgnKU5ASpnK3A\n" +
                "If you donate via PayPal or Bitcoin, you will need to contact @werewolfsupport to claim your prize.  If you donate via Telegram, it's automated, no need to contact an admin :)\n" +
                "More information about the Custom Gif Packs: http://telegra.ph/Dont-Shoot-the-Messenger\n" +
                "How would you like to donate?",
                replyMarkup: markup);
        }

        public static void GetDonationInfo(CallbackQuery q = null, Message m = null)
        {
            var menu = new Menu();
            Bot.Api.SendTextMessageAsync(q?.From.Id ?? m.From.Id,
                "How much would you like to donate?  Please enter a whole number, in US Dollars (USD), in reply to this message",
                replyMarkup: new ForceReply {Force = true});
        }

        public static void ValidateDonationAmount(Message m)
        {
            var input = m.Text.Replace("$","");
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
