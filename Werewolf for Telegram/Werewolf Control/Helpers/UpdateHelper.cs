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
        internal static bool IsGroupAdmin(Update update)
        {
            return IsGroupAdmin(update.Message.From.Id, update.Message.Chat.Id);
        }

        internal static bool IsGroupAdmin(int user, long group)
        {
            //fire off admin request
            try
            {
                var admin = Bot.Api.GetChatMember(group, user).Result;
                return admin.Status == ChatMemberStatus.Administrator || admin.Status == ChatMemberStatus.Creator;
            }
            catch
            {
                return false;
            }
        }
    }
}
