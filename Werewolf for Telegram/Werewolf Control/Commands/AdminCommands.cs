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
            //if (u.Message.ReplyToMessage == null)
            //{
            //    Bot.Send(GetLocaleString("MustReplySmite",GetLanguage(u.Message.Chat.Id)).ToBold(), u.Message.Chat.Id);
            //    return;
            //}

            //get the names to smite




            if (UpdateHelper.IsGroupAdmin(u))
            {
                foreach (var e in u.Message.Entities)
                {
                    switch (e.Type)
                    {
                        case MessageEntityType.Mention:
                            //get user
                            var username = u.Message.Text.Substring(e.Offset + 1, e.Length - 1);
                            using (var db = new WWContext())
                            {
                                var id = db.Players.FirstOrDefault(x => x.UserName == username)?.TelegramId ?? 0;
                                if (id != 0)
                                    Bot.GetGroupNodeAndGame(u.Message.Chat.Id)?.SmitePlayer(id);
                            }
                            break;
                        case MessageEntityType.TextMention:
                            Bot.GetGroupNodeAndGame(u.Message.Chat.Id)?.SmitePlayer(e.User.Id);
                            break;
                    }


                }

                var did = 0;
                if (int.TryParse(args[1], out did))
                {
                    Bot.GetGroupNodeAndGame(u.Message.Chat.Id)?.SmitePlayer(did);
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
            Bot.Api.SendTextMessage(update.Message.From.Id, GetLocaleString("WhatToDo", GetLanguage(update.Message.From.Id)),
                replyMarkup: menu);
        }

        [Command(Trigger = "uploadlang", GlobalAdminOnly = true)]
        public static void UploadLang(Update update, string[] args)
        {
            try
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
            catch (Exception e)
            {
                Bot.Api.SendTextMessage(update.Message.Chat.Id, e.Message, parseMode: ParseMode.Default);
            }
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
            foreach (var arg in args.Skip(1).FirstOrDefault()?.Split(' ') ?? new[] { "" })
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
                    
                    var str = $"{id} ({user?.User.FirstName})";
                    reply += GetLocaleString("IdleCount", GetLanguage(update.Message.Chat.Id, str, idles));
                    reply += "\n";
                }
            }

            Send(reply, update.Message.Chat.Id);
        }

        [Command(Trigger = "remlink", GroupAdminOnly = true, InGroupOnly = true)]
        public static void RemLink(Update u, string[] args)
        {
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == u.Message.Chat.Id) ??
                          MakeDefaultGroup(u.Message.Chat.Id, u.Message.Chat.Title, "setlink");
                grp.GroupLink = null;
                db.SaveChanges();
            }

            Send($"Your group link has been removed.  You will no longer appear on the /grouplist", u.Message.Chat.Id);
        }

        [Command(Trigger = "setlink", GroupAdminOnly = true, InGroupOnly = true)]
        public static void SetLink(Update update, string[] args)
        {
            //args[1] should be the link

            //first, check if the group has a username
            if (!String.IsNullOrEmpty(update.Message.Chat.Username))
            {
                Send($"You're group link has already been set to https://telegram.me/{update.Message.Chat.Username}",
                    update.Message.Chat.Id);
                return;
            }

            //now check the args
            if (args.Length < 2 || String.IsNullOrEmpty(args[1]))
            {
                Send($"You must use /setlink with the link to the group (invite link)", update.Message.Chat.Id);
                return;
            }

            var link = args[1].Trim();
            if (!link.Contains("telegram.me/joinchat"))
            {
                Send("This is an invalid telegram join link.", update.Message.Chat.Id);
                return;
            }
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == update.Message.Chat.Id) ??
                          MakeDefaultGroup(update.Message.Chat.Id, update.Message.Chat.Title, "setlink");
                grp.GroupLink = link;
                db.SaveChanges();
            }

            Send($"Your group will be listed as: <a href=\"{link}\">{update.Message.Chat.Title}</a>", update.Message.Chat.Id);
        }

        [Command(Trigger = "addach", DevOnly = true)]
        public static void AddAchievement(Update u, string[] args)
        {
            //get the user to add the achievement to
            //first, try by reply
            var id = 0;
            var achIndex = 0;
            var param = args[1].Split(' ');
            if (u.Message.ReplyToMessage != null)
            {
                var m = u.Message.ReplyToMessage;
                while (m.ReplyToMessage != null)
                    m = m.ReplyToMessage;
                //check for forwarded message
               
                id = m.From.Id;
                if (m.ForwardFrom != null)
                    id = m.ForwardFrom.Id;
            }
            else
            {
                //ok, check for a user mention
                var e = u.Message.Entities?.FirstOrDefault();
                if (e != null)
                {
                    switch (e.Type)
                    {
                        case MessageEntityType.Mention:
                            //get user
                            var username = u.Message.Text.Substring(e.Offset + 1, e.Length - 1);
                            using (var db = new WWContext())
                            {
                                id = db.Players.FirstOrDefault(x => x.UserName == username)?.TelegramId ?? 0;
                            }
                            break;
                        case MessageEntityType.TextMention:
                            id = e.User.Id;
                            break;
                    }
                    achIndex = 1;
                }
            }

            if (id == 0)
            {
                //check for arguments then
                if (int.TryParse(param[0], out id))
                    achIndex = 1;
            }


            if (id != 0)
            {
                //try to get the achievement
                Achievements a;
                if (Enum.TryParse(param[achIndex], out a))
                {
                    //get the player from database
                    using (var db = new WWContext())
                    {
                        var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                        if (p != null)
                        {
                            if (p.Achievements == null)
                                p.Achievements = 0;
                            var ach = (Achievements)p.Achievements;
                            if (ach.HasFlag(a)) return; //no point making another db call if they already have it
                            ach = ach | a;
                            p.Achievements = (long)ach;
                            db.SaveChanges();
                            Send($"Achievement Unlocked!\n{a.GetName().ToBold()}\n{a.GetDescription()}", p.Id);
                            Send($"Achievement {a} unlocked for {p.Name}", u.Message.Chat.Id);
                        }
                    }
                }
            }

        }
    }
}
