using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Telegram.Bot.Exceptions;
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
            var send = DateTime.UtcNow;
            var message = GetLocaleString("PingInfo", GetLanguage(update.Message.From.Id), $"{ts:mm\\:ss\\.ff}",
                Program.AvgCpuTime.ToString("F0"),
                $"\n{Program.MessagePxPerSecond.ToString("F0")} MAX IN | {Program.MessageTxPerSecond.ToString("F0")} MAX OUT");
            var result = Bot.Send(message, update.Message.Chat.Id).Result;
            ts = DateTime.UtcNow - send;
            message += "\n" + GetLocaleString("Ping2", GetLanguage(update.Message.From.Id), $"{ts:mm\\:ss\\.ff}");
            Bot.Api.EditMessageText(update.Message.Chat.Id, result.MessageId, message);

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
            Bot.Api.SendTextMessage(update.Message.Chat.Id, "[Website](http://www.tgwerewolf.com/?referrer=help)\n/rolelist (don't forget to /setlang first!)\n[Telegram Werewolf Support Group](http://telegram.me/werewolfsupport)\n[Telegram Werewolf Dev Channel](https://telegram.me/werewolfdev)",
                                                        parseMode: ParseMode.Markdown);
        }

        [Command(Trigger = "chatid")]
        public static void ChatId(Update update, string[] args)
        {
            Send(update.Message.Chat.Id.ToString(), update.Message.Chat.Id);

        }

        [Command(Trigger = "donate")]
        public static void Donate(Update u, string[] args)
        {
            Bot.Api.SendTextMessage(u.Message.Chat.Id, "Want to help keep werewolf online? Please donate to info@tgwerewolf.com through PayPal.\n\nDonations help us pay to keep the expensive servers running and the game online. Every donation you make helps to keep us going for another month. For more information please contact @werewolfsupport", parseMode: ParseMode.Html);
        }

        [Command(Trigger = "changelog")]
        public static void ChangeLog(Update update, string[] args)
        {
            Send("Changelog moved to <a href=\"www.tgwerewolf.com/#changes?referrer=changelog\">here</a>\nAlso check out the dev channel @werewolfdev", update.Message.Chat.Id);
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

        [Command(Trigger = "getstatus")]
        public static void GetStatus(Update u, string[] args)
        {
            using (var db = new WWContext())
            {
                var msg =
                    db.BotStatus.ToList().Select(x => $"{x.BotName} (@{x.BotLink}): {x.BotStatus}").ToList()
                        .Aggregate((a, b) => a + "\n" + b);
                Send(msg, u.Message.Chat.Id);
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
            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();


            List<InlineKeyboardButton> buttons = langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => new InlineKeyboardButton(x, $"setlang|{update.Message.From.Id}|{x}|null|base")).ToList();

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
            Bot.Api.SendTextMessage(update.Message.From.Id, GetLocaleString("WhatLang", curLangFileName, curLang.Base),
                replyMarkup: menu);
            if (update.Message.Chat.Type != ChatType.Private)
                Send(GetLocaleString("SentPrivate", GetLanguage(update.Message.From.Id)), update.Message.Chat.Id);
        }

        [Command(Trigger = "start")]
        public static void Start(Update u, string[] args)
        {
            if (u.Message.Chat.Type == ChatType.Private)
            {
                if (u.Message.From != null)
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
                                      $"\nBe sure to stop by <a href=\"https://telegram.me/werewolfsupport\">Werewolf Support</a> for any questions, and subscribe to @werewolfdev for updates from the developer." +
                                      $"\nMore infomation can be found <a href=\"https://www.tgwerewolf.com/?referrer=start\">here</a>!";
                            Bot.Send(msg,u.Message.Chat.Id);
                        }
                        else
                        {
                            var uid = args[1];


                            
                            

                            //check the database for that user
                            {
                                var aspuser = db.AspNetUsers.Find(uid);
                                
                                //we have the asp user, let's find the player
                                var user = db.Players.FirstOrDefault(x => x.TelegramId == u.Message.From.Id);
                                if (user == null)
                                    return;
                                user.WebUserId = uid; //linked!
                                db.SaveChanges();
                                Send($"Your telegram account is now linked to your web account - {aspuser.Email}", u.Message.From.Id);
                            }
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
                if (game != null)
                {

                    if (game?.Users.Contains(update.Message.From.Id) ?? false)
                    {
                        if (game?.GroupId != update.Message.Chat.Id)
                        {
                            //player is already in a game, and alive
                            Send(
                                GetLocaleString("AlreadyInGame", grp.Language ?? "English",
                                    game.ChatGroup.ToBold()), update.Message.Chat.Id);
                            return;
                        }
                    }
                }

                if (db.NotifyGames.Any(x => x.GroupId == id && x.UserId == update.Message.From.Id))
                {
                    Send(GetLocaleString("AlreadyOnWaitList", grp.Language, grp.Name.ToBold()),
                        update.Message.From.Id);
                }
                else
                {
                    db.Database.ExecuteSqlCommand(
                        $"INSERT INTO NotifyGame VALUES ({update.Message.From.Id}, {id})");
                    db.SaveChanges();
                    Send(GetLocaleString("AddedToWaitList", grp.Language, grp.Name.ToBold()),
                        update.Message.From.Id);
                }
            }
        }

        [Command(Trigger = "getlang")]
        public static void GetLang(Update update, string[] args)
        {
            //var glangs = Directory.GetFiles(Bot.LanguageDirectory)
            //                                            .Select(x => XDocument.Load(x)
            //                                                        .Descendants("language")
            //                                                        .First()
            //                                                        .Attribute("name")
            //                                                        .Value
            //                                            ).ToList();
            //glangs.Insert(0, "All");

            //foreach (var lang in glangs)
            //{
            //    var test =
            //        $"getlang|-1001049529775|" + lang;
            //    var count = Encoding.UTF8.GetByteCount(test);
            //    if (count > 64)
            //    {
            //        Send("Problem with " + lang + ": name is too long!", update.Message.Chat.Id);
            //    }
            //}
            //var gbuttons = glangs.Select(x => new InlineKeyboardButton(x, $"getlang|{update.Message.Chat.Id}|{x}")).ToList();
            //var baseMenu = new List<InlineKeyboardButton[]>();
            //for (var i = 0; i < gbuttons.Count; i++)
            //{
            //    if (gbuttons.Count - 1 == i)
            //    {
            //        baseMenu.Add(new[] { gbuttons[i] });
            //    }
            //    else
            //        baseMenu.Add(new[] { gbuttons[i], gbuttons[i + 1] });
            //    i++;
            //}

            //var gmenu = new InlineKeyboardMarkup(baseMenu.ToArray());

            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();


            List<InlineKeyboardButton> buttons = langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => new InlineKeyboardButton(x, $"getlang|{update.Message.From.Id}|{x}|null|base")).ToList();
            buttons.Insert(0, new InlineKeyboardButton("All", $"getlang|{update.Message.From.Id}|All|null|base"));

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
                Bot.Api.SendTextMessage(update.Message.Chat.Id, GetLocaleString("GetLang", GetLanguage(update.Message.Chat.Id)),
                    replyToMessageId: update.Message.MessageId, replyMarkup: menu);
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
                    new InlineKeyboardButton
                    {
                        Text = $"{name} Stats",
                        Url = "http://www.tgwerewolf.com/Stats/Player/" + id + "?referrer=stats"
                    }
                }

                };
                var menu = new InlineKeyboardMarkup(buttons.ToArray());
                Bot.Api.SendTextMessage(u.Message.Chat.Id, "Stats", replyMarkup: menu);
            }
            else
            {


                //change this to buttons
                var buttons = new List<InlineKeyboardButton[]>
                {
                    new[]
                    {
                        new InlineKeyboardButton
                        {
                            Text = "Global Stats",
                            Url = "http://www.tgwerewolf.com/Stats?referrer=stats"
                        }
                    }
                };
                if (u.Message.Chat.Type != ChatType.Private)
                    buttons.Add(new[]
                    {
                        new InlineKeyboardButton
                        {
                            Text = $"{u.Message.Chat.Title} Stats",
                            Url = "http://www.tgwerewolf.com/Stats/Group/" + u.Message.Chat.Id + "?referrer=stats"
                        }
                    });
                buttons.Add(new[]
                {
                    new InlineKeyboardButton
                    {
                        Text = $"{u.Message.From.FirstName} Stats",
                        Url = "http://www.tgwerewolf.com/Stats/Player/" + u.Message.From.Id + "?referrer=stats"
                    }
                });
                var menu = new InlineKeyboardMarkup(buttons.ToArray());
                Bot.Api.SendTextMessage(u.Message.Chat.Id, "Stats", replyMarkup: menu);
            }
        }
    }
}
