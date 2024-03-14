using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using Werewolf_Control.Attributes;
using Werewolf_Control.Helpers;

#pragma warning disable IDE0060 // Remove unused parameter

namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Command(Trigger = "ping")]
        public static void Ping(Update update, string[] args)
        {
            var ts = DateTime.UtcNow - update.Message.Date;
            var send = DateTime.UtcNow;
            var message = GetLocaleString("PingInfo", GetLanguage(update.Message.From.Id), $"{ts:mm\\:ss\\.ff}",
                $"\n{Program.MessageRxPerSecond.ToString("F0")} MAX IN | {Program.MessageTxPerSecond.ToString("F0")} MAX OUT");
            message += $"\nActually processed per second: {Program.MessagePxPerSecond}";
            message += $"\nIN last min: {Program.MessagesReceived.Sum()}\nOUT last min: {Program.MessagesSent.Sum()}";
            var result = Bot.Send(message, update.Message.Chat.Id).Result;
            ts = DateTime.UtcNow - send;
            message += "\n" + GetLocaleString("Ping2", GetLanguage(update.Message.From.Id), $"{ts:mm\\:ss\\.ff}");
            Bot.Api.EditMessageTextAsync(chatId: update.Message.Chat.Id, messageId: result.MessageId, text: message);

        }
#if (BETA || DEBUG)
        [Command(Trigger = "achv")]
        public static void GetAchievements(Update u, string[] args)
        {
            Send("Please use /stats", u.Message.Chat.Id);
        }
#endif
        [Command(Trigger = "help")]
        public static void Help(Update update, string[] args)
        {
            if (args[1] == null) //only send the message if there is no extra args (otherwise it's more likely for other bots)
            {
                Bot.Api.SendTextMessageAsync(chatId: update.Message.Chat.Id, text: "[Website](https://www.tgwerewolf.com/?referrer=help)\n/rolelist (don't forget to /setlang first!)\n[Telegram Werewolf Support Group](http://telegram.me/greywolfsupport)\n[Telegram Werewolf Dev Channel](https://telegram.me/greywolfdev)",
                                                            parseMode: ParseMode.Markdown, disableWebPagePreview: true, messageThreadId: update.Message.MessageThreadId);
            }
        }

        [Command(Trigger = "chatid")]
        public static void ChatId(Update update, string[] args)
        {
            Send(update.Message.Chat.Id.ToString(), update.Message.Chat.Id);

        }

        [Command(Trigger = "changelog")]
        public static void ChangeLog(Update update, string[] args)
        {
            Send("Changelog moved to <a href=\"https://www.tgwerewolf.com/#changes?referrer=changelog\">here</a>\nAlso check out the dev channel @greywolfdev", update.Message.Chat.Id);
        }

        [Command(Trigger = "runinfo")]
        public static void RunInfo(Update update, string[] args)
        {
            var result = "*Run information*\n";
            result +=
                $"Uptime: {DateTime.UtcNow - Bot.StartTime}\nConnected Nodes: {Bot.Nodes.Count}\n" +
                $"Current Games: {Bot.Nodes.Sum(x => x.CurrentGames)}\n" +
                $"Current Players: {Bot.Nodes.Sum(x => x.CurrentPlayers)}";
            Bot.Api.SendTextMessageAsync(chatId: update.Message.Chat.Id, text: result, parseMode: ParseMode.Markdown, messageThreadId: update.Message.MessageThreadId);
        }

        [Command(Trigger = "getstatus")]
        public static void GetStatus(Update u, string[] args)
        {
            using (var db = new WWContext())
            {
                //var msg =
                //    db.BotStatus.ToList().Where(x => x.BotName != "Bot 2").Select(x => $"[{x.BotName.Replace("Bot 1", "Moderator")}](https://t.me/{x.BotLink}): *{x.BotStatus}* ").ToList()
                //        .Aggregate((a, b) => a + "\n" + b);
                var msg = "Command currently disabled, sorry";
                Bot.Api.SendTextMessageAsync(chatId: u.Message.Chat.Id, text: msg, parseMode: ParseMode.Markdown, disableWebPagePreview: true, messageThreadId: u.Message.MessageThreadId);
            }
        }

        [Command(Trigger = "version")]
        public static void Version(Update update, string[] args)
        {
            var version = Program.GetVersion();
            try
            {
                var node =
                    Bot.Nodes.ToList().FirstOrDefault(x => x.Games.Any(g => g.GroupId == update.Message.Chat.Id));

                version += !String.IsNullOrWhiteSpace(node?.Version)
                    ? $"\nNode Version: {node?.Version}\nNode Id: {node?.ClientId}"
                    : "\nNode Version: You are not on a node right now (no game running in this group)";
            }
            catch
            {
                // ignored
            }
            Send(version, update.Message.Chat.Id);
        }

        [Command(Trigger = "setlang")]
        public static void SetLang(Update update, string[] args)
        {
            Player p = null;
            using (var db = new WWContext())
            {
                p = db.Players.FirstOrDefault(x => x.TelegramId == update.Message.From.Id);

                if (p == null)
                {


                    p = new Player
                    {
                        TelegramId = update.Message.From.Id,
                        Language = "English",
#if RELEASE
                        HasPM = update.Message.Chat.Type == ChatType.Private
#elif RELEASE2
                        HasPM2 = update.Message.Chat.Type == ChatType.Private
#elif DEBUG
                        HasDebugPM = update.Message.Chat.Type == ChatType.Private
#endif
                    };
                    db.Players.Add(p);


                    p.UserName = update.Message.From.Username;
                    p.Name = $"{update.Message.From.FirstName} {update.Message.From.LastName}".Trim();

                    db.SaveChanges();
                    //user obvious has no PM status, notify them
#if RELEASE
                    if (p.HasPM != true)
#elif RELEASE2
                    if (p.HasPM2 != true)
#elif DEBUG
                    if (p.HasDebugPM != true)
#endif
                    {
                        RequestPM(update.Message.Chat.Id);
                        return;
                    }
                }


            }
            //user wants to pick personal language
            var langs = LanguageHelper.GetAllLanguages(); // Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();


            List<InlineKeyboardButton> buttons = langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => InlineKeyboardButton.WithCallbackData(x, $"setlang|{update.Message.From.Id}|{x}|null|base")).ToList();

            var baseMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    baseMenu.Add(new[] { buttons[i] });
                }
                else
                    baseMenu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }

            var menu = new InlineKeyboardMarkup(baseMenu.ToArray());

            var curLangFileName = GetLanguage(update.Message.From.Id);
            var curLang = langs.First(x => x.FileName == curLangFileName);
            Bot.Api.SendTextMessageAsync(chatId: update.Message.From.Id, text: GetLocaleString("WhatLang", curLangFileName, curLang.Base),
                replyMarkup: menu);
            if (update.Message.Chat.Type != ChatType.Private)
                Send(GetLocaleString("SentPrivate", GetLanguage(update.Message.From.Id)), update.Message.Chat.Id);
        }

        [Command(Trigger = "start")]
        public static void Start(Update u, string[] args)
        {
            if (u.Message.Chat.Type == ChatType.Private && u.Message.From != null)
            {
                using (var db = new WWContext())
                {
                    var p = GetDBPlayer(u.Message.From.Id, db);
                    if (p == null)
                    {
                        var usr = u.Message.From;
                        p = new Player
                        {
                            UserName = usr.Username,
                            Name = (usr.FirstName + " " + usr.LastName).Trim(),
                            TelegramId = usr.Id,
                            Language = "English"
                        };
                        db.Players.Add(p);
                        db.SaveChanges();
                        p = GetDBPlayer(u.Message.From.Id, db);
                    }
#if RELEASE
                        p.HasPM = true;
#elif RELEASE2
                        p.HasPM2 = true;
#elif BETA
                        p.HasDebugPM = true;
#endif
                    db.SaveChanges();

                    if (String.IsNullOrEmpty(args[1]))
                    {
                        var msg = $"Hi there! I'm @{Bot.Me.Username}, and I moderate games of Werewolf." +
                                  $"\nJoin the main group @werewolfgame, or to find a group to play in, you can use /grouplist." +
                                  $"\nFor role information, use /rolelist." +
                                  $"\nIf you want to set your default language, use /setlang." +
                                  $"\nBe sure to stop by <a href=\"https://telegram.me/greywolfsupport\">Werewolf Support</a> for any questions, and subscribe to @greywolfdev for updates from the developer." +
                                  $"\nMore information can be found <a href=\"https://www.tgwerewolf.com/?referrer=start\">here</a>!";
                        Bot.Send(msg, u.Message.Chat.Id);
                        return;
                    }

                    if (args[1] == "donatetg")
                    {
                        GetDonationInfo(m: u.Message);
                        return;
                    }
                    
                    if (args[1] == "xsolla")
                    {
                        GetXsollaLink(m: u.Message);
                        return;
                    }

                    if (args[1].StartsWith("join") && args[1].Length == 48) // 4 "join" + 22 node id + 22 game id
                    {
                        //okay, they are joining a game.
                        string nodeid = "";
                        string gameid = "";
                        Models.Node node = null;
                        Models.GameInfo game = null;
                        ChatMember chatmember = null;
                        try
                        {
                            nodeid = args[1].Substring(4, 22);
                            gameid = args[1].Substring(26, 22);

                            // check that they aren't ingame in another group
                            node = GetPlayerNode(u.Message.From.Id);
                            if (node != null)
                            {
                                game = node.Games.ToList().FirstOrDefault(x => x.Users.Contains(u.Message.From.Id));
                                if (game == null)
                                    game = node.Games.ToList().FirstOrDefault(x => x.Users.Contains(u.Message.From.Id));
                                if (game == null)
                                    game = node.Games.ToList().FirstOrDefault(x => x.Users.Contains(u.Message.From.Id));

                                if (game != null)
                                {
                                    if (node.ClientId != nodeid || game.Guid != gameid)
                                    {
                                        // they are in game in another group, can't join here
                                        var pl = db.Players.FirstOrDefault(x => x.TelegramId == u.Message.From.Id);
                                        Send(GetLocaleString("AlreadyInGame", pl?.Language ?? "English", game.ChatGroup.ToBold()), u.Message.Chat.Id);
                                        return;
                                    }
                                    else
                                    {
                                        // do nothing, they are in the game, they are just being spammy
                                        return;
                                    }
                                }
                            }

                            node = null;
                            game = null;

                            //first get the node where to search for the game

                            for (var i = 0; i < 3; i++)
                            {
                                node = Bot.Nodes.ToList().FirstOrDefault(x => x.ClientId == nodeid);
                                if (node != null) break;
                            }
                            if (node == null)
                            {
                                //log it
                                //Bot.Send($"{u.Message.From.Id} (@{u.Message.From.Username ?? ""}) didn't find node with guid {n.ToString()} while attempting to play in {g.ToString()}", -1001098399855);
                                return;
                            }

                            //we have the node, get the game

                            for (var i = 0; i < 5; i++)
                            {
                                game = node.Games.ToList().FirstOrDefault(x => x.Guid == gameid);
                                if (game != null) break;
                            }
                            if (game == null)
                            {
                                //log it
                                //Bot.Send($"{u.Message.From.Id} (@{u.Message.From.Username ?? ""}) found node with guid {n.ToString()} but not the game {g.ToString()}", -1001098399855);
                                return;
                            }

                            //ok we got the game, now join 
                            //make sure they are member
                            try
                            {
                                chatmember = Bot.Api.GetChatMemberAsync(game.GroupId, u.Message.From.Id).Result;
                            }
                            catch
                            {
                                return;
                            }
                            if (chatmember == null) // if we fail to determine their chatmember status, just let them try again
                                return;

                            var canSend = (chatmember as ChatMemberRestricted)?.CanSendMessages;

                            if (chatmember.Status == ChatMemberStatus.Left || chatmember.Status == ChatMemberStatus.Kicked || (chatmember.Status == ChatMemberStatus.Restricted && !(canSend ?? true)))
                            {
                                Bot.Send(
                                    GetLocaleString("NotMember", GetLanguage(u.Message.From.Id), game.ChatGroup.ToBold()),
                                    u.Message.Chat.Id);
                                return;
                            }

                            game.AddPlayer(u, gameid);
                            return;
                        }
                        catch (AggregateException e)
                        {
                            var ex = e.InnerExceptions[0];
                            while (ex.InnerException != null)
                                ex = ex.InnerException;

                            Send(ex.Message, u.Message.Chat.Id);
                            Send($"Error in START:\n" +
                                 $"{u.Message.Text}\n" +
                                 $"Node: {nodeid}\n" +
                                 $"Game: {gameid}\n" +
                                 $"Found Node: {node?.ClientId}\n" +
                                 $"Found Game: {game?.Guid}\n" +
                                 $"Chat Member Status: {chatmember?.Status.ToString() ?? "NULL"}\n" +
                                 $"{ex.Message}\n{ex.StackTrace}",
                                Settings.ErrorGroup);

                        }
                        catch (Exception ex)
                        {
                            while (ex.InnerException != null)
                                ex = ex.InnerException;

                            Send(ex.Message, u.Message.Chat.Id);
                            Send($"Error in START:\n" +
                                 $"{u.Message.Text}\n" +
                                 $"Node: {nodeid}\n" +
                                 $"Game: {gameid}\n" +
                                 $"Found Node: {node?.ClientId}\n" +
                                 $"Found Game: {game?.Guid}\n" +
                                 $"Chat Member Status: {chatmember?.Status.ToString() ?? "NULL"}\n" +
                                 $"{ex.Message}\n{ex.StackTrace}",
                                Settings.ErrorGroup);
                        }
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
                if (game != null && (game?.Users.Contains(update.Message.From.Id) ?? false) && game?.GroupId != update.Message.Chat.Id)
                {
                    //player is already in a game, and alive
                    Send(
                        GetLocaleString("AlreadyInGame", grp.Language ?? "English",
                            game.ChatGroup.ToBold()), update.Message.Chat.Id);
                    return;
                }

                var button = new InlineKeyboardMarkup(new[] {
                        InlineKeyboardButton.WithCallbackData(GetLocaleString("Cancel", grp.Language), $"stopwaiting|{id}")
                    });
                if (db.NotifyGames.Any(x => x.GroupId == id && x.UserId == update.Message.From.Id))
                {
                    Send(GetLocaleString("AlreadyOnWaitList", grp.Language, grp.Name.ToBold()),
                        update.Message.From.Id, customMenu: button);
                }
                else
                {
                    db.Database.ExecuteSqlCommand(
                        $"INSERT INTO NotifyGame VALUES ({update.Message.From.Id}, {id})");
                    db.SaveChanges();
                    Send(GetLocaleString("AddedToWaitList", grp.Language, grp.Name.ToBold()),
                        update.Message.From.Id, customMenu: button);
                }
            }
        }

        [Command(Trigger = "getlang")]
        public static void GetLang(Update update, string[] args)
        {
            if (!string.IsNullOrEmpty(args[1]))
            {
                string pattern = args[1];

                if (pattern.ToLower().EndsWith(".xml")) pattern = pattern.Remove(pattern.Length - 4);
                if (pattern.Contains("*") || pattern.Contains("\\") || pattern.Contains("."))
                {
                    Bot.Send("Invalid language file name. Make sure you enter the <b>filename</b> of the file you wish to download.", update.Message.Chat.Id);
                    return;
                }

                var lang = Directory.GetFiles(Bot.LanguageDirectory, pattern + ".xml");

                switch (lang.Length)
                {
                    case 0:
                        Bot.Send("No matching language file found. Make sure you enter the <b>filename</b> of the file you wish to download.", update.Message.Chat.Id);
                        return;

                    case 1:
                        LanguageHelper.SendFileByFilepath(update.Message.Chat.Id, update.Message.MessageThreadId, lang[0]);
                        return;

                    default: //shouldn't happen, but you never know...
                        Bot.Send("Multiple matching language files found. Make sure you enter the <b>filename</b> of the file you wish to download.", update.Message.Chat.Id);
                        return;
                }
            }
            
            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();

            List<InlineKeyboardButton> buttons = langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => InlineKeyboardButton.WithCallbackData(x, $"getlang|{update.Message.From.Id}|{x}|null|base")).ToList();
            buttons.Insert(0, InlineKeyboardButton.WithCallbackData("All", $"getlang|{update.Message.From.Id}|All|null|base"));

            var baseMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    baseMenu.Add(new[] { buttons[i] });
                }
                else
                    baseMenu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }

            var menu = new InlineKeyboardMarkup(baseMenu.ToArray());
            try
            {
                Bot.Api.SendTextMessageAsync(chatId: update.Message.Chat.Id, text: GetLocaleString("GetLang", GetLanguage(update.Message.Chat.Id)),
                    replyToMessageId: update.Message.MessageId, replyMarkup: menu, messageThreadId: update.Message.MessageThreadId);
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions)
                {
                    var x = ex as ApiRequestException;

                    Send(x.Message, update.Message.Chat.Id);
                }
            }
            catch (ApiRequestException ex)
            {
                Send(ex.Message, update.Message.Chat.Id);
            }
        }

        [Command(Trigger = "stats")]
        public static void GetStats(Update u, string[] args)
        {
            //var reply = $"[Global Stats](www.tgwerewolf.com/Stats)\n";
            //if (update.Message.Chat.Type != ChatType.Private)
            //    reply += $"[Group Stats](www.tgwerewolf.com/Stats/Group/{update.Message.Chat.Id}) ({update.Message.Chat.Title})\n";
            //reply += $"[Player Stats](www.tgwerewolf.com/Stats/Player/{update.Message.From.Id}) ({update.Message.From.FirstName})";

            if (u.Message.ReplyToMessage != null)
            {
                var m = u.Message.ReplyToMessage;
                while (m.ReplyToMessage != null)
                    m = m.ReplyToMessage;
                //check for forwarded message
                var name = m.From.FirstName;
                var id = m.From.Id;
                if (m.ForwardFrom != null)
                {
                    id = m.ForwardFrom.Id;
                    name = m.ForwardFrom.FirstName;
                }
                var buttons = new List<InlineKeyboardButton[]>
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl($"{name} Stats", "https://www.tgwerewolf.com/Stats/Player/" + id + "?referrer=stats")
                }

                };
                var menu = new InlineKeyboardMarkup(buttons.ToArray());
                Bot.Api.SendTextMessageAsync(chatId: u.Message.Chat.Id, text: "Stats", replyMarkup: menu, messageThreadId: u.Message.MessageThreadId);
            }
            else
            {


                //change this to buttons
                var buttons = new List<InlineKeyboardButton[]>
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Global Stats",
                           "https://www.tgwerewolf.com/Stats?referrer=stats")

                    }
                };
                if (u.Message.Chat.Type != ChatType.Private)
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithUrl( $"{u.Message.Chat.Title} Stats",
                        "https://www.tgwerewolf.com/Stats/Group/" + u.Message.Chat.Id + "?referrer=stats")
                    });
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithUrl($"{u.Message.From.FirstName} Stats","https://www.tgwerewolf.com/Stats/Player/" + u.Message.From.Id + "?referrer=stats")

                });
                var menu = new InlineKeyboardMarkup(buttons.ToArray());
                Bot.Api.SendTextMessageAsync(chatId: u.Message.Chat.Id, text: "Stats", replyMarkup: menu, messageThreadId: u.Message.MessageThreadId);
            }
        }
        
        [Command(Trigger = "myidles")]
        public static void MyIdles(Update update, string[] args)
        {
            bool isgroup = new[] { ChatType.Group, ChatType.Supergroup }.Contains(update.Message.Chat.Type);
            
            var idles = 0;
            var groupidles = 0;
            
            using (var db = new WWContext())
            {
                idles = db.GetIdleKills24Hours(update.Message.From.Id).FirstOrDefault() ?? 0;
                if (isgroup)
                    groupidles = db.GetGroupIdleKills24Hours(update.Message.From.Id, update.Message.Chat.Id).FirstOrDefault() ?? 0;
            }
            
            var str = $"{update.Message.From.Id} ({update.Message.From.FirstName})";
            var language = GetLanguage(update.Message.Chat.Id);
            var reply = GetLocaleString("IdleCount", language, str, idles);
            if (isgroup)
                reply += " " + GetLocaleString("GroupIdleCount", language, groupidles);
            
            try
            {
                var result = Bot.Api.SendTextMessageAsync(chatId: update.Message.From.Id, text: reply).Result;
                if (update.Message.Chat.Type != ChatType.Private)
                    Send(GetLocaleString("SentPrivate", GetLanguage(update.Message.From.Id)), update.Message.Chat.Id);
            }
            catch
            {
                RequestPM(update.Message.Chat.Id);
            }
        }
    }
}
