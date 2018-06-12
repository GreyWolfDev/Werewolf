using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Werewolf_Control.Helpers
{
    internal static class UpdateHelper
    {
        internal static int[] Devs =
        {
            129046388,  //Para
            133748469,  //reny
            142032675,  //Para 2
            295152997,  //Ludwig
            106665913,  //Jeff
        };

        internal static int[] LangAdmins =
        {
            267376056,  //Florian
        };

        internal static bool IsGroupAdmin(Update update)
        {
            return IsGroupAdmin(update.Message.From.Id, update.Message.Chat.Id);
        }

        internal static bool IsGlobalAdmin(int id)
        {
            using (var db = new Database.WWContext())
            {
                return db.Admins.Any(x => x.UserId == id);
            }
        }

        internal static bool IsLangAdmin(int id)
        {
            return LangAdmins.Contains(id);
        }

        internal static bool IsGroupAdmin(int user, long group)
        {
            //fire off admin request
            try
            {
                //check all admins
                if (Bot.Api.GetChatAsync(group).Result.AllMembersAreAdministrators)
                    return true;
                var admin = Bot.Api.GetChatMemberAsync(group, user).Result;
                return admin.Status == ChatMemberStatus.Administrator || admin.Status == ChatMemberStatus.Creator;
            }
            catch
            {
                return false;
            }
        }
    }
}
