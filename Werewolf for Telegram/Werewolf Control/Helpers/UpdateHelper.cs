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
            //fire off admin request
            try
            {
                var admin = Bot.Api.GetChatMember(update.Message.Chat.Id, update.Message.From.Id).Result;
                return admin.Status == ChatMemberStatus.Administrator || admin.Status == ChatMemberStatus.Creator;
            }
            catch
            {
                return false;
            }
        }
    }
}
