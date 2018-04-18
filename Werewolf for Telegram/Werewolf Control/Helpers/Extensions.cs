using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Models;

namespace Werewolf_Control.Helpers
{
    public static class Extensions
    {
        public static string ToBold(this string str)
        {
            return $"<b>{str.FormatHTML()}</b>";
        }

        public static string ToItalic(this string str)
        {
            return $"<i>{str.FormatHTML()}</i>";
        }

        public static string FormatHTML(this string str)
        {
            return str?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
        public static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        //public static int GetStrength(this IRole role, List<IRole> allRoles)
        //{
        //    switch (role)
        //    {
        //        case IRole.Villager:
        //            return 1;
        //        case IRole.Drunk:
        //            return 3;
        //        case IRole.Harlot:
        //            return 6;
        //        case IRole.Seer:
        //            return 7;
        //        case IRole.Traitor:
        //            return 0;
        //        case IRole.GuardianAngel:
        //            return 7;
        //        case IRole.Detective:
        //            return 6;
        //        case IRole.Wolf:
        //            return 10;
        //        case IRole.Cursed:
        //            return 6 - allRoles.Count(x => x == IRole.Wolf);
        //        case IRole.Gunner:
        //            return 6;
        //        case IRole.Tanner:
        //            return allRoles.Count / 2;
        //        case IRole.Fool:
        //            return 3;
        //        case IRole.WildChild:
        //            return 2;
        //        case IRole.Beholder:
        //            return 2 + (allRoles.Any(x => x == IRole.Seer) ? 4 : 0); //only good if seer is present!
        //        case IRole.ApprenticeSeer:
        //            return 6;
        //        case IRole.Cultist:
        //            return 12 + allRoles.Count(x => x == IRole.Villager);
        //        case IRole.CultistHunter:
        //            return 7;
        //        case IRole.Mason:
        //            return 3 + (allRoles.Count(x => x == IRole.Mason)); //strength in numbers
        //        case IRole.Doppelgänger:
        //            return 4;
        //        case IRole.Cupid:
        //            return 2;
        //        case IRole.Hunter:
        //            return 6;
        //        case IRole.SerialKiller:
        //            return 15;
        //        case IRole.Sorcerer:
        //            return 2;
        //        case IRole.AlphaWolf:
        //            return 12;
        //        case IRole.WolfCub:
        //            return 11;
        //        case IRole.Blacksmith:
        //            return 5;
        //        case IRole.Preacher:
        //            return 4;
        //        case IRole.Faithful:
        //            return 3;
        //        case IRole.ClumsyGuy:
        //            return -1;
        //        case IRole.Mayor:
        //            return 4;
        //        case IRole.Prince:
        //            return 3;
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(role), role, null);
        //    }

        //}

        public static string Pad(this int val)
        {
            return "<code>" + val.ToString().PadRight(5) + "</code>";
        }

        public static string[] GetParameters(this Message m)
        {
            var input = m.Text;
            if (String.IsNullOrEmpty(input)) return new[] { "", "" };
            // ReSharper disable StringIndexOfIsCultureSpecific.1  -- It's a space, I don't care about culture.
            var result = input.Contains(" ") ? new[] { input.Substring(1, input.IndexOf(" ")).Trim(), input.Substring(input.IndexOf(" ") + 1) } : new[] { input.Substring(1).Trim(), null };
            result[0] = result[0].Replace("@" + Bot.Me.Username, "");
            return result;
        }

        public static Player GetTarget(this Update u, WWContext db)
        {
            var message = u.Message;
            var args = message.GetParameters()[1].Split(' ')[0];

            var sourceUser = message.GetBasePlayer(db);
            if (message == null) return sourceUser;
            if (message?.ReplyToMessage != null)
            {
                var m = message.ReplyToMessage;
                var userid = m.ForwardFrom?.Id ?? m.From.Id;
                return db.Players.FirstOrDefault(x => x.TelegramId == userid) ?? sourceUser;
            }
            if (String.IsNullOrWhiteSpace(args))
            {
                return sourceUser;
            }
            //check for a user mention
            var mention = message?.Entities.FirstOrDefault(x => x.Type == MessageEntityType.Mention);
            var textmention = message?.Entities.FirstOrDefault(x => x.Type == MessageEntityType.TextMention);
            var id = 0;
            var username = "";
            if (!String.IsNullOrEmpty(args))
            {
                if (!int.TryParse(args, out id))
                    username = args;
            }
            
            if (mention != null)
                username = message.Text.Substring(mention.Offset + 1, mention.Length - 1);
            else if (textmention != null)
            {
                id = textmention.User.Id;
            }
            Player result = null;
            if (!String.IsNullOrEmpty(username) && id == 0)
                result = db.Players.FirstOrDefault(
                    x => username.Equals(x.UserName, StringComparison.InvariantCultureIgnoreCase));
            else if (id != 0)
                result = db.Players.FirstOrDefault(x => x.TelegramId == id);
            else
                result = db.Players.FirstOrDefault(
                        x =>
                            String.Equals(x.TelegramId.ToString(), args, StringComparison.InvariantCultureIgnoreCase) ||
                            String.Equals(x.UserName?? "", args.Replace("@", ""), StringComparison.InvariantCultureIgnoreCase));
            return result ?? sourceUser;
        }

        public static Player GetBasePlayer(this Message m, WWContext db)
        {
            return db.Players.FirstOrDefault(x => x.TelegramId == m.From.Id);
        }

        public static InlineKeyboardMarkup CreateMarkupFromMenu(this Menu menu)
        {
            if (menu == null) return null;
            var col = menu.Columns - 1;
            //this is gonna be fun...
            var final = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < menu.Buttons.Count; i++)
            {
                var row = new List<InlineKeyboardButton>();
                do
                {
                    row.Add(menu.Buttons[i]);
                    i++;
                    if (i == menu.Buttons.Count) break;
                } while (i % (col + 1) != 0);
                i--;
                final.Add(row.ToArray());
                if (i == menu.Buttons.Count) break;
            }
            return new InlineKeyboardMarkup(final.ToArray());
        }
    }
}
