using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace Werewolf_Node.Helpers
{
    internal static class Helpers
    {
        internal static void KickChatMember(long chatid, int userid)
        {
            var status = Program.Bot.GetChatMemberAsync(chatid, userid).Result.Status;

            if (status == ChatMemberStatus.Administrator) //ignore admins
                return;
            //kick
            Program.Bot.KickChatMemberAsync(chatid, userid);
            //get their status
            status =Program.Bot.GetChatMemberAsync(chatid, userid).Result.Status;
            while (status == ChatMemberStatus.Member) //loop
            {
                //wait for database to report status is kicked.
                status =Program.Bot.GetChatMemberAsync(chatid, userid).Result.Status;
                Thread.Sleep(100);
            }
            //status is now kicked (as it should be)

            while (status != ChatMemberStatus.Left) //unban until status is left
            {
               Program.Bot.UnbanChatMemberAsync(chatid, userid);
                Thread.Sleep(100);
                status =Program.Bot.GetChatMemberAsync(chatid, userid).Result.Status;
            }
            //yay unbanned

        }
    }
}
