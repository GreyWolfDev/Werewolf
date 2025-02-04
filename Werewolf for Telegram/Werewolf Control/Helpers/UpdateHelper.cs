using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Runtime.Caching;
using Telegram.Bot;

namespace Werewolf_Control.Helpers
{
    internal static class UpdateHelper
    {
        public static readonly MemoryCache AdminCache = new MemoryCache("GroupAdmins");

        internal static long[] Devs =
        {
            129046388,  //Para
            133748469,  //reny
            142032675,  //Para 2
            295152997,  //Ludwig
            106665913,  //Jeff
            1430807001, //reny2
        };

        internal static long[] LangAdmins =
        {
            267376056,  //Florian
            654995039,  //Cordarion
        };

        internal static bool IsGroupAdmin(Update update)
        {
            return IsGroupAdmin(update.Message.From.Id, update.Message.Chat.Id);
        }

        internal static bool IsGlobalAdmin(long id)
        {
            using (var db = new Database.WWContext())
            {
                return db.Admins.Any(x => x.UserId == id);
            }
        }

        internal static bool IsLangAdmin(long id)
        {
            return LangAdmins.Contains(id);
        }

        internal static bool IsGroupAdmin(long user, long group)
        {
            string itemIndex = $"{group}";
            if (!(AdminCache[itemIndex] is List<long> admins))
            {
                CacheItemPolicy policy = new CacheItemPolicy() { AbsoluteExpiration = DateTime.Now.AddHours(1) };

                //fire off admin request
                try
                {
                    //check all admins
                    var t = Bot.Api.GetChatAdministratorsAsync(chatId: group).Result;
                    admins = t.Where(x => !string.IsNullOrEmpty(x.User.FirstName)).Select(x => x.User.Id).ToList(); // if their first name is empty, the account is deleted
                    AdminCache.Set(itemIndex, admins, policy); // Write admin list into cache
                }
                catch
                {
                    return false;
                }

            }
            return admins.Any(x => x == user);

        }
    }
}
