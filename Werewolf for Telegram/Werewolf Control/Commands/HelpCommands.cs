using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Werewolf_Control.Attributes;
using Werewolf_Control.Helpers;

namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Command(Trigger = "grouplist")]
        public static void GroupList(Update update, string[] args)
        {
            var reply = "";
            using (var db = new WWContext())
            {
                reply = Enumerable.Aggregate(db.v_PreferredGroups, "", (current, g) => current + $"{GetLanguageName(g.Language)}{(String.IsNullOrEmpty(g.Description) ? "" : $" - {g.Description}")}\n<a href=\"{g.GroupLink}\">{g.Name}</a>\n\n");
            }
            try
            {
                var result = Bot.Api.SendTextMessage(update.Message.From.Id, reply).Result;
                if (update.Message.Chat.Type != ChatType.Private)
                    Send("I have sent you a PM", update.Message.Chat.Id);
            }
            catch (Exception e)
            {
                Send("Please send me a pm so I can PM you: @werewolfbot", update.Message.Chat.Id);
            }

        }

        [Command(Trigger = "rolelist")]
        public static void RoleList(Update update, string[] args)
        {
            var lang = GetLanguage(update.Message.Chat.Id);
            var reply =
                "/AboutVG - Villager\n/AboutSeer - Seer\n/AboutWw - Werewolf\n/AboutHarlot - Harlot\n/AboutDrunk - Drunk\n/AboutCursed - Cursed\n/AboutTraitor - Traitor\n/AboutGA - Guardian Angel\n/AboutDetective - Detective\n/AboutGunner - Gunner\n/AboutTanner - Tanner\n/AboutFool - Fool\n/AboutCult - Cultist\n/AboutCH - Cultist Hunter\n/AboutWC - Wild Child\n/AboutAppS - Apprentice seer\n/AboutBH - Beholder\n/AboutMason - Mason\n/AboutDG - Doppelgänger\n/AboutCupid - Cupid\n/AboutHunter - Hunter\n/AboutSK - Serial Killer";
            try
            {
                var result = Bot.Api.SendTextMessage(update.Message.From.Id, reply).Result;
                if (update.Message.Chat.Type != ChatType.Private)
                    Send("I have sent you a PM", update.Message.Chat.Id);
            }
            catch (Exception e)
            {
                Send("Please send me a pm so I can PM you: @werewolfbot", update.Message.Chat.Id);
            }

        }
    }
}
