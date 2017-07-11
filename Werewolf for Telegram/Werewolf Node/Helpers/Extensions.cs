using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Database;
using Werewolf_Node.Models;

namespace Werewolf_Node.Helpers
{
    public static class Extensions
    {
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

        public static string ToBold(this object str)
        {
            if (str == null)
                return null;
            return $"<b>{str.ToString().FormatHTML()}</b>";
        }

        public static string ToItalic(this object str)
        {
            if (str == null)
                return null;
            return $"<i>{str.ToString().FormatHTML()}</i>";
        }

        public static string FormatHTML(this string str)
        {
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        public static string GetName(this IPlayer player, bool menu = false)
        {
            if (menu)
                return player.Name;
            if (!String.IsNullOrEmpty(player.TeleUser.Username))
                return $"<a href=\"telegram.me/{player.TeleUser.Username}\">{player.Name.FormatHTML()}</a>";

            return player.Name.ToBold();
        }

        public static string GetFinalEmojis(this IPlayer p)
        {
            string emoji = "";
            if (p.OriginalRole != p.PlayerRole)
                emoji += p.OriginalRole.GetEmoji();
            if (p.PlayerRole == IRole.PsychicMage && p.Team != ITeam.Neutral)
                emoji += p.Team == ITeam.Village ? IRole.Villager.GetEmoji() : IRole.Wolf.GetEmoji();
            emoji += p.InLove ? "❤️" : "";
            return emoji;
        }

        public static IEnumerable<IPlayer> GetLivingPlayers(this IEnumerable<IPlayer> players)
        {
            return players?.Where(x => !x.IsDead);
        }

        public static IEnumerable<IPlayer> GetPlayersForTeam(this IEnumerable<IPlayer> players, ITeam team, bool aliveOnly = true, IPlayer exceptPlayer = null)
        {
            return players?.Where(x => x.Team == team && (!aliveOnly || !x.IsDead) && x.Id != exceptPlayer?.Id);
        }

        public static IPlayer GetPlayerForRole(this IEnumerable<IPlayer> players, IRole role, bool aliveOnly = true, IPlayer exceptPlayer = null)
        {
            return players?.FirstOrDefault(x => x.PlayerRole == role && (!aliveOnly || !x.IsDead) && x.Id != exceptPlayer?.Id);
        }

        public static IEnumerable<IPlayer> GetPlayersForRoles(this IEnumerable<IPlayer> players, IRole[] roles,
            bool aliveOnly = true, IPlayer exceptPlayer = null)
        {
            return players?.Where(x => roles.Contains(x.PlayerRole) && (!aliveOnly || !x.IsDead) && x.Id != exceptPlayer?.Id);
        }


        public static int GetStrength(this IRole role, List<IRole> allRoles)
        {
            IRole[] WolfRoles = { IRole.WolfCub, IRole.WolfCub, IRole.AlphaWolf };
            IRole[] nonConvertibleRoles = { IRole.Seer, IRole.GuardianAngel, IRole.Detective, IRole.Cursed, IRole.Harlot, IRole.Hunter, IRole.Doppelgänger, IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.SerialKiller, IRole.PsychicMage };
            switch (role)
            {
                case IRole.Villager:
                    return 1;
                case IRole.Drunk:
                    return 3;
                case IRole.Harlot:
                    return 6;
                case IRole.Seer:
                    return 7;
                case IRole.Traitor:
                    return 0;
                case IRole.GuardianAngel:
                    return 7;
                case IRole.Detective:
                    return 6;
                case IRole.Wolf:
                    return 10;
                case IRole.Cursed:
                    return 1 - allRoles.Count(x => WolfRoles.Contains(x)) / 2; //vg, or worse
                case IRole.Gunner:
                    return 6;
                case IRole.Tanner:
                    return allRoles.Count / 2;
                case IRole.Fool:
                    return 3;
                case IRole.WildChild:
                    return 1;
                case IRole.Beholder:
                    return 2 + (allRoles.Any(x => x == IRole.Seer) ? 4 : 0); //only good if seer is present!
                case IRole.ApprenticeSeer:
                    return 6;
                case IRole.Cultist:
                    return 10 + allRoles.Count(x => !nonConvertibleRoles.Contains(x));
                case IRole.CultistHunter:
                    return allRoles.Count(x => x == IRole.Cultist) == 0 ? 1 : 7;
                case IRole.Mason:
                    return allRoles.Count(x => x == IRole.Mason) <= 1 ? 1 : allRoles.Count(x => x == IRole.Mason) + 2 ; //strength in numbers
                case IRole.Doppelgänger:
                    return 2;
                case IRole.Cupid:
                    return 2;
                case IRole.Hunter:
                    return 6;
                case IRole.SerialKiller:
                    return 15;
                case IRole.Sorcerer:
                    return 2;
                case IRole.AlphaWolf:
                    return 12;
                case IRole.WolfCub:
                    return 12;
                case IRole.Blacksmith:
                    return 5;
                case IRole.ClumsyGuy:
                    return -1;
                case IRole.Mayor:
                    return 4;
                case IRole.Prince:
                    return 3;
                case IRole.PsychicMage:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
        public static string GetEmoji(this IRole role)
        {
            switch (role)
            {
                case IRole.Villager:
                    return "👨";
                case IRole.Drunk:
                    return "🍻";
                case IRole.Harlot:
                    return "💋";
                case IRole.Seer:
                    return "👳";
                case IRole.Traitor:
                    return "🖕";
                case IRole.GuardianAngel:
                    return "👼";
                case IRole.Detective:
                    return "🕵️";
                case IRole.Wolf:
                    return "🐺";
                case IRole.Cursed:
                    return "😾";
                case IRole.Gunner:
                    return "🔫";
                case IRole.Tanner:
                    return "👺";
                case IRole.Fool:
                    return "🃏";
                case IRole.WildChild:
                    return "👶";
                case IRole.Beholder:
                    return "👁";
                case IRole.ApprenticeSeer:
                    return "🙇";
                case IRole.Cultist:
                    return "👤";
                case IRole.CultistHunter:
                    return "💂";
                case IRole.Mason:
                    return "👷";
                case IRole.Doppelgänger:
                    return "🎭";
                case IRole.Cupid:
                    return "🏹";
                case IRole.Hunter:
                    return "🎯";
                case IRole.SerialKiller:
                    return "🔪";
                case IRole.Sorcerer:
                    return "🔮";
                case IRole.AlphaWolf:
                    return "⚡️";
                case IRole.WolfCub:
                    return "🐶";
                case IRole.Blacksmith:
                    return "⚒";
                case IRole.ClumsyGuy:
                    return "🤕";
                case IRole.Mayor:
                    return "🎖";
                case IRole.Prince:
                    return "👑";
                case IRole.PsychicMage:
                    return "🌀";
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
    }
}
