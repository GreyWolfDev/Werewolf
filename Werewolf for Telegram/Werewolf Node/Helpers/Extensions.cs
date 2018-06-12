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

        public static string GetName(this IPlayer player, bool menu = false, bool dead = false)
        {
            var name = player.Name;

            var end = name.Substring(name.Length - Math.Min(name.Length, 5));
            name = name.Substring(0, Math.Max(name.Length - 5, 0));
            end = end.Replace("🥇", "").Replace("🥈", "").Replace("🥉", "").Replace("💎", "").Replace("📟", "");

            if (player.GifPack?.ShowBadge ?? false || (player.GifPack == null && player.DonationLevel >= 10))
            {
                if (player.DonationLevel >= 100)
                    end += " 🥇";
                else if (player.DonationLevel >= 50)
                    end += " 🥈";
                else if (player.DonationLevel >= 10)
                    end += " 🥉";
                if (player.Founder && player.Id != 142032675 && player.Id != 129046388)
                    end += "💎";
            }
            if (player.Id == 142032675 || player.Id == 129046388)
                end += "📟";
            name += end;

            if (menu)
                return name;
            //if (!String.IsNullOrEmpty(player.TeleUser.Username))
            if (!dead)
                return $"<a href=\"tg://user?id={player.TeleUser.Id}\">{name.FormatHTML()}</a>";

            return name.ToBold();
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
            IRole[] WolfRoles = { IRole.Wolf, IRole.WolfCub, IRole.AlphaWolf, IRole.Lycan };
            IRole[] nonConvertibleRoles = { IRole.Seer, IRole.GuardianAngel, IRole.Detective, IRole.Cursed, IRole.Harlot, IRole.Hunter, IRole.Doppelgänger, IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.SerialKiller, IRole.Lycan, IRole.Thief };
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
                    return allRoles.Count(x => x == IRole.Mason) <= 1 ? 1 : allRoles.Count(x => x == IRole.Mason) + 3; //strength in numbers
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
                case IRole.WolfMan:
                    return -1;
                case IRole.Pacifist:
                    return 3;
                case IRole.WiseElder:
                    return 3;
                case IRole.Oracle:
                    return 4;
                case IRole.Sandman:
                    return 3;
                case IRole.Lycan:
                    return 11;
                case IRole.Thief:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }

        }
    }
}
