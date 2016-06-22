using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Attributes;
using Werewolf_Control.Handler;
using Werewolf_Control.Helpers;

namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Command(Trigger = "smite", GroupAdminOnly = true, Blockable = true, InGroupOnly = true)]
        public static void Smite(Update u, string[] args)
        {
            if (u.Message.ReplyToMessage == null)
            {
                Bot.Send("You must reply to the user you want to smite", u.Message.Chat.Id);
                return;
            }

            if (UpdateHelper.IsGroupAdmin(u))
            {
                int smiteid = u.Message.ReplyToMessage?.From?.Id ??
                            u.Message.ForwardFrom?.Id ?? 0;
                if (smiteid != 0)
                {
                    Bot.GetGroupNodeAndGame(u.Message.Chat.Id)?.SmitePlayer(smiteid);
                }
            }
        }

        [Command(Trigger = "config", GroupAdminOnly = true, InGroupOnly = true)]
        public static void Config(Update update, string[] args)
        {
            var id = update.Message.Chat.Id;

            //make sure the group is in the database
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                if (grp == null)
                {
                    grp = MakeDefaultGroup(id, update.Message.Chat.Title, "config");
                    db.Groups.Add(grp);
                }

                grp.BotInGroup = true;
                grp.UserName = update.Message.Chat.Username;
                grp.Name = update.Message.Chat.Title;
                db.SaveChanges();
            }

            var menu = UpdateHandler.GetConfigMenu(update.Message.Chat.Id);
            Bot.Api.SendTextMessage(update.Message.From.Id, "What would you like to do?",
                replyMarkup: menu);
        }

        [Command(Trigger = "uploadlang", GlobalAdminOnly = true)]
        public static void UploadLang(Update update, string[] args)
        {
            var id = update.Message.Chat.Id;
            if (update.Message.ReplyToMessage?.Type != MessageType.DocumentMessage)
            {
                Send("Please reply to the file with /uploadlang", id);
                return;
            }
            var fileid = update.Message.ReplyToMessage.Document?.FileId;
            if (fileid != null)
                LanguageHelper.UploadFile(fileid, id,
                    update.Message.ReplyToMessage.Document.FileName,
                    update.Message.MessageId);
        }

        [Command(Trigger = "validatelangs", GlobalAdminOnly = true)]
        public static void ValidateLangs(Update update, string[] args)
        {
            var langs = Directory.GetFiles(Bot.LanguageDirectory)
                                                        .Select(x => XDocument.Load(x)
                                                                    .Descendants("language")
                                                                    .First()
                                                                    .Attribute("name")
                                                                    .Value
                                                        ).ToList();
            langs.Insert(0, "All");

            var buttons =
                langs.Select(x => new[] { new InlineKeyboardButton(x, $"validate|{update.Message.Chat.Id}|{x}") }).ToArray();
            var menu = new InlineKeyboardMarkup(buttons.ToArray());
            Bot.Api.SendTextMessage(update.Message.Chat.Id, "Validate which language?",
                replyToMessageId: update.Message.MessageId, replyMarkup: menu);
        }
    }
}
