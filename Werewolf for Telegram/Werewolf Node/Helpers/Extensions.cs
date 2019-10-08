using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Werewolf_Node.Models;
using Shared;

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

        public static string TrimEnd(this string str, bool removeWhitespaces = true, params string[] trim)
        {
            if (removeWhitespaces && str.Length > 0 && char.IsWhiteSpace(str.Last())) return str.Remove(str.Length - 1).TrimEnd(removeWhitespaces, trim);
            foreach (var s in trim)
            {
                if (str.EndsWith(s)) return str.Remove(str.LastIndexOf(s)).TrimEnd(removeWhitespaces, trim);
            }
            return str;
        }

        public static string GetName(this IPlayer player, bool menu = false, bool dead = false)
        {
            var name = player.Name;

            string[] removeStrings = { "🥇", "🥈", "🥉", "💎", "📟", "🏅" };
            var end = "";
            name = name.TrimEnd(true, removeStrings);

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

        /// <summary>
        /// if aliveOnly is false, in case of multiple same roles, this will return the first alive one, or, if no alive one, the one who died the most recently
        /// </summary>
        public static IPlayer GetPlayerForRole(this IEnumerable<IPlayer> players, IRole role, bool aliveOnly = true, IPlayer exceptPlayer = null)
        {
            return players?.OrderByDescending(x => x.TimeDied).FirstOrDefault(x => x.PlayerRole == role && (!aliveOnly || !x.IsDead) && x.Id != exceptPlayer?.Id);
        }

        public static IEnumerable<IPlayer> GetPlayersForRoles(this IEnumerable<IPlayer> players, IRole[] roles,
            bool aliveOnly = true, IPlayer exceptPlayer = null)
        {
            return players?.Where(x => roles.Contains(x.PlayerRole) && (!aliveOnly || !x.IsDead) && x.Id != exceptPlayer?.Id);
        }
    }
}
