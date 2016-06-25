﻿using System;
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
using Werewolf_Control.Helpers;

namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Command(Trigger = "ping")]
        public static void Ping(Update update, string[] args)
        {
            var ts = DateTime.UtcNow - update.Message.Date;
            Bot.Send($"Reply process time: {ts:ss\\.ff}", update.Message.Chat.Id);
        }

        [Command(Trigger = "help")]
        public static void Help(Update update, string[] args)
        {
            Bot.Api.SendTextMessage(update.Message.Chat.Id, "[Website](http://werewolf.parawuff.com)\n[Telegram Werewolf Support Group](http://telegram.me/werewolfsupport)\n[Telegram Werewolf Dev Channel](https://telegram.me/werewolfdev)",
                                                        parseMode: ParseMode.Markdown);
        }

        [Command(Trigger = "chatid")]
        public static void ChatId(Update update, string[] args)
        {
            Send(update.Message.Chat.Id.ToString(), update.Message.Chat.Id);
        }

        [Command(Trigger = "changelog")]
        public static void ChangeLog(Update update, string[] args)
        {
            Send("Changelog moved to: werewolf.parawuff.com/#changes\nAlso check out the dev channel @werewolfdev", update.Message.Chat.Id);
        }

        [Command(Trigger = "runinfo")]
        public static void RunInfo(Update update, string[] args)
        {
            var result = "*Run information*\n";
            result +=
                $"Uptime: {DateTime.UtcNow - Bot.StartTime}\nConnected Nodes: {Bot.Nodes.Count}\n" +
                $"Current Games: {Bot.Nodes.Sum(x => x.CurrentGames)}\n" +
                $"Current Players: {Bot.Nodes.Sum(x => x.CurrentPlayers)}";
            Bot.Api.SendTextMessage(update.Message.Chat.Id, result, parseMode: ParseMode.Markdown);
        }

        [Command(Trigger = "version")]
        public static void Version(Update update, string[] args)
        {
            var version = Program.GetVersion();
            try
            {
                var nodeVersion =
                    Bot.Nodes.ToList().FirstOrDefault(x => x.Games.Any(g => g.GroupId == update.Message.Chat.Id))?
                        .Version;
                version += !String.IsNullOrWhiteSpace(nodeVersion)
                    ? $"\nNode Version: {nodeVersion}"
                    : "\nNode Version: You are not on a node right now (no game running in this group)";
            }
            catch
            {
                // ignored
            }
            Send(version, update.Message.Chat.Id);
        }

        [Command(Trigger = "start")]
        public static void Start(Update update, string[] args)
        {
            if (update.Message.Chat.Type == ChatType.Private)
            {
                if (update.Message.From != null)
                {
                    using (var db = new WWContext())
                    {
                        var p = GetDBPlayer(update.Message.From.Id, db);
                        p.HasPM = true;
                        db.SaveChanges();
                        Bot.Send("PM Status set to true", update.Message.Chat.Id);
                    }
                }
            }
        }

        [Command(Trigger = "nextgame", Blockable = true, InGroupOnly = true)]
        public static void NextGame(Update update, string[] args)
        {
            var id = update.Message.Chat.Id;
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                if (grp == null)
                {
                    grp = MakeDefaultGroup(id, update.Message.Chat.Title, "nextgame");
                    db.Groups.Add(grp);
                    db.SaveChanges();
                }

                //check nodes to see if player is in a game
                //node = GetPlayerNode(update.Message.From.Id);
                var game = GetGroupNodeAndGame(update.Message.Chat.Id);
                if (game != null)
                {

                    if (game?.Users.Contains(update.Message.From.Id) ?? false)
                    {
                        if (game?.GroupId != update.Message.Chat.Id)
                        {
                            //player is already in a game, and alive
                            Send(
                                GetLocaleString("AlreadyInGame", grp.Language ?? "English",
                                    game.ChatGroup), update.Message.Chat.Id);
                            return;
                        }
                    }
                }

                if (db.NotifyGames.Any(x => x.GroupId == id && x.UserId == update.Message.From.Id))
                {
                    Send(GetLocaleString("AlreadyOnWaitList", grp.Language, grp.Name),
                        update.Message.From.Id);
                }
                else
                {
                    db.Database.ExecuteSqlCommand(
                        $"INSERT INTO NotifyGame VALUES ({update.Message.From.Id}, {id})");
                    db.SaveChanges();
                    Send(GetLocaleString("AddedToWaitList", grp.Language, grp.Name),
                        update.Message.From.Id);
                }
            }
        }

        [Command(Trigger = "getlang")]
        public static void GetLang(Update update, string[] args)
        {
            var glangs = Directory.GetFiles(Bot.LanguageDirectory)
                                                        .Select(x => XDocument.Load(x)
                                                                    .Descendants("language")
                                                                    .First()
                                                                    .Attribute("name")
                                                                    .Value
                                                        ).ToList();
            glangs.Insert(0, "All");

            var gbuttons = glangs.Select(x => new InlineKeyboardButton(x, $"getlang|{update.Message.Chat.Id}|{x}")).ToList();
            var baseMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < gbuttons.Count; i++)
            {
                if (gbuttons.Count - 1 == i)
                {
                    baseMenu.Add(new[] { gbuttons[i] });
                }
                else
                    baseMenu.Add(new[] { gbuttons[i], gbuttons[i + 1] });
                i++;
            }

            var gmenu = new InlineKeyboardMarkup(baseMenu.ToArray());
            Bot.Api.SendTextMessage(update.Message.Chat.Id, "Get which language file?", replyToMessageId: update.Message.MessageId, replyMarkup: gmenu);
        }

        [Command(Trigger = "stats")]
        public static void GetStats(Update update, string[] args)
        {
            var reply = $"[Global Stats](werewolf.parawuff.com/Stats)\n";
            if (update.Message.Chat.Type != ChatType.Private)
                reply += $"[Group Stats](werewolf.parawuff.com/Stats/Group/{update.Message.Chat.Id}) ({update.Message.Chat.Title})\n";
            reply += $"[Player Stats](werewolf.parawuff.com/Stats/Player/{update.Message.From.Id}) ({update.Message.From.FirstName})";
            Bot.Api.SendTextMessage(update.Message.Chat.Id, reply, parseMode: ParseMode.Markdown,
                disableWebPagePreview: true);
        }
    }
}
