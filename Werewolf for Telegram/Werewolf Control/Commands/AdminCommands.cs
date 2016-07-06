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
                Bot.Send("You must reply to the user you want to smite".ToBold(), u.Message.Chat.Id);
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


        [Command(Trigger = "getidles", GroupAdminOnly = true)]
        public static void GetIdles(Update update, string[] args)
        {
            //check user ids and such
            List<int> ids = new List<int>();
            foreach (var arg in args.Skip(1).FirstOrDefault()?.Split(' ')??new [] {""})
            {
                var id = 0;
                if (int.TryParse(arg, out id))
                {
                    ids.Add(id);
                }
            }

            //now check for mentions
            foreach (var ent in update.Message.Entities.Where(x => x.Type == MessageEntityType.TextMention))
            {
                ids.Add(ent.User.Id);
            }

            //check for reply
            if (update.Message.ReplyToMessage != null)
                ids.Add(update.Message.ReplyToMessage.From.Id);

            var reply = "";
            //now get the idle kills
            using (var db = new WWContext())
            {
                foreach (var id in ids)
                {
                    var idles = db.GetIdleKills24Hours(id).FirstOrDefault() ?? 0;
                    //get the user
                    ChatMember user = null;
                    try
                    {
                        user = Bot.Api.GetChatMember(update.Message.Chat.Id, id).Result;
                    }
                    catch
                    {
                        // ignored
                    }

                    reply += $"{id} ({user?.User.FirstName}) has been idle killed {idles} time(s) in the past 24 hours\n";
                }
            }

            Send(reply, update.Message.Chat.Id);
        }
    }
}
