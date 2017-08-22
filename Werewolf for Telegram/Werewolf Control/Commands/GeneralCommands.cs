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
using Telegram.Bot.Types.InlineKeyboardButtons;
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
            var ts = DateTime.Now - update.Message.Date;
            var send = DateTime.Now;
            var message = GetLocaleString("PingInfo", GetLanguage(update.Message.From.Id), $"{ts:mm\\:ss\\.ff}",
                $"\n{Program.MessagePxPerSecond.ToString("F0")} MAX IN | {Program.MessageTxPerSecond.ToString("F0")} MAX OUT");
            message += $"\nIN last min: {Program.MessagesReceived.Sum()}\nOUT last min: {Program.MessagesSent.Sum()}";
            var result = Bot.Send(message, update.Message.Chat.Id).Result;
            ts = DateTime.Now - send;
            message += "\n" + GetLocaleString("Ping2", GetLanguage(update.Message.From.Id), $"{ts:mm\\:ss\\.ff}");
            Bot.Api.EditMessageTextAsync(update.Message.Chat.Id, result.MessageId, message);

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
            Bot.Api.SendTextMessageAsync(update.Message.Chat.Id, "\n/rolelist (não esqueça de setar /setlang primeiro!)\n[Werewolf Zion Suporte](http://telegram.me/WerewolfZionSuporte)\n[Versão Modificada desse Bot no GitHub](https://github.com/FernandoTBarros/Werewolf)",
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
            Bot.Api.SendTextMessageAsync(u.Message.Chat.Id, "Quer ajudar a manter o Werewolf Zion online? Por favor, doe para fernando.tbarros@gmail.com através do PayPal.\n\nDoações servem para cobrir os custos de manutenção dos servidores. \nComo nosso bot é uma modificação do bot original feito pela GreyWolf, 20% das doações serão direcionadas para a doação oficial deles como forma de reconhecimento e incentivo pelo desenvolvimento do bot original. \nPara mais informações, visitar [Werewolf Zion Suporte](http://telegram.me/WerewolfZionSuporte).", parseMode: ParseMode.Markdown);
        }

        [Command(Trigger = "changelog")]
        public static void ChangeLog(Update update, string[] args)
        {
            Send("Changelog https://github.com/FernandoTBarros/Werewolf/commits/master", update.Message.Chat.Id);
        }

        [Command(Trigger = "runinfo")]
        public static void RunInfo(Update update, string[] args)
        {
            var result = "*Run information*\n";
            result +=
                $"Uptime: {DateTime.Now - Bot.StartTime}\nConnected Nodes: {Bot.Nodes.Count}\n" +
                $"Current Games: {Bot.Nodes.Sum(x => x.CurrentGames)}\n" +
                $"Current Players: {Bot.Nodes.Sum(x => x.CurrentPlayers)}";
            Bot.Api.SendTextMessageAsync(update.Message.Chat.Id, result, parseMode: ParseMode.Markdown);
        }

        [Command(Trigger = "getstatus")]
        public static void GetStatus(Update u, string[] args)
        {
            using (var db = new WWContext())
            {
                var msg =
                    db.BotStatus.ToList().Where(x => x.BotName != "Bot 2").Select(x => $"[{x.BotName.Replace("Bot 1", "Moderator")}](https://t.me/{x.BotLink}): *{x.BotStatus}* ").ToList()
                        .Aggregate((a, b) => a + "\n" + b);
                Bot.Api.SendTextMessageAsync(u.Message.Chat.Id, msg, ParseMode.Markdown, disableWebPagePreview: true);
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


            List<InlineKeyboardCallbackButton> buttons = langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => new InlineKeyboardCallbackButton(x, $"setlang|{update.Message.From.Id}|{x}|null|base")).ToList();

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
            var curLang = langs.FirstOrDefault(x => x.FileName == curLangFileName);
            Bot.Api.SendTextMessageAsync(update.Message.From.Id, GetLocaleString("WhatLang", curLangFileName, curLang?.Base),
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
#elif BETA || DEBUG
                        p.HasDebugPM = true;
#endif
                    db.SaveChanges();

                    if (String.IsNullOrEmpty(args[1]))
                    {
                        var msg = $"Olá! Sou @{Bot.Me.Username}, e gerencio jogos de Werewolf." +
                                      $"\nJunte-se ao grupo principal @BRMarvelWW, ou para encontrar um grupo para jogar, você pode usar /grouplist." +
                                      $"\nPara informações de papéis, use /rolelist." +
                                      $"\nSe quer definir sua língua principal, use /setlang." +
                                      $"\nSe tiver alguma dúvida ou pergunta venha até Werewolf Zion Suporte (@WerewolfZionSuporte)!";
                        Bot.Send(msg, u.Message.Chat.Id);
                        return;
                    }

                    if (args[1] == "donatetg")
                    {
                        GetDonationInfo(m: u.Message);
                        return;
                    }

                    //okay, they are joining a game.

                    string[] argsSplit = args[1].Split('_');
                    
                    var nodeid = argsSplit[0];
                    var gameid = argsSplit[1];

                    //try to get the guid of the game they want to join
                    int n,g;
                    long gid = 0;
                    if (!(int.TryParse(nodeid, out n) && int.TryParse(gameid, out g)))
                        return;

                    //first get the node where to search for the game
                    Models.Node node = null;
                    for (var i = 0; i < 3; i++)
                    {
                        node = Bot.Nodes.FirstOrDefault(x => x.ClientId == n);
                        if (node != null) break;
                    }
                    if (node == null)
                    {
                        //log it
                        //Bot.Send($"{u.Message.From.Id} (@{u.Message.From.Username ?? ""}) didn't find node with guid {n.ToString()} while attempting to play in {g.ToString()}", -1001098399855);
                        return;
                    }

                    //we have the node, get the game
                    Models.GameInfo game = null;
                    for (var i = 0; i < 5; i++)
                    {
                        game = node.Games.FirstOrDefault(x => x.Guid == g);
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
                    var status = Bot.Api.GetChatMemberAsync(game.GroupId, u.Message.From.Id).Result.Status;
                    if (status == ChatMemberStatus.Left || status == ChatMemberStatus.Kicked)
                    {
                        Bot.Send(GetLocaleString("NotMember", GetLanguage(u.Message.From.Id), game.ChatGroup.ToBold()), u.Message.Chat.Id);
                        return;
                    }

                    //if (game?.Users.Contains(u.Message.From.Id) ?? false)
                    //{
                    //    if (game.GroupId != gid)
                    //    {
                    //        //player is already in a game (in another group), and alive
                    //        var grp = db.Groups.FirstOrDefault(x => x.GroupId == gid);
                    //        Send(GetLocaleString("AlreadyInGame", grp?.Language ?? "English", game.ChatGroup.ToBold()), gid);
                    //        return;
                    //    }
                    //    else
                    //    {
                    //        //do nothing, player is in the game, in that group, they are just being spammy
                    //        return;
                    //    }
                    //}

                    game.AddPlayer(u);
                    return;


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
                        new InlineKeyboardCallbackButton(GetLocaleString("Cancel", grp.Language), $"stopwaiting|{id}")
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
            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).ToList();

            List<InlineKeyboardCallbackButton> buttons = langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => new InlineKeyboardCallbackButton(x, $"getlang|{update.Message.From.Id}|{x}|null|base")).ToList();
            buttons.Insert(0, new InlineKeyboardCallbackButton("All", $"getlang|{update.Message.From.Id}|All|null|base"));

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
                Bot.Api.SendTextMessageAsync(update.Message.Chat.Id, GetLocaleString("GetLang", GetLanguage(update.Message.Chat.Id)),
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

            Bot.Api.SendPhotoAsync(u.Message.Chat.Id, new FileToSend("AgADAQADkdUxG9tS9wFjV189xLDOYW705y8ABKUOCQdNzMe8cDECAAEC"), replyToMessageId: u.Message.MessageId);
            //Bot.Api.SendTextMessage(u.Message.Chat.Id, "#stats");
            //if (u.Message.ReplyToMessage != null)
            //{
            //    var m = u.Message.ReplyToMessage;
            //    while (m.ReplyToMessage != null)
            //        m = m.ReplyToMessage;
            //    //check for forwarded message
            //    var name = m.From.FirstName;
            //    var id = m.From.Id;
            //    if (m.ForwardFrom != null)
            //    {
            //        id = m.ForwardFrom.Id;
            //        name = m.ForwardFrom.FirstName;
            //    }
            //    var buttons = new List<InlineKeyboardButton[]>
            //    {
            //        new[]
            //        {
            //        new InlineKeyboardButton
            //        {
            //            Text = $"{name} Stats",
            //            Url = "http://www.tgwerewolf.com/Stats/Player/" + id + "?referrer=stats"
            //        }
            //    }

            //    };
            //    var menu = new InlineKeyboardMarkup(buttons.ToArray());
            //    Bot.Api.SendTextMessage(u.Message.Chat.Id, "Stats", replyMarkup: menu);
            //}
            //else
            //{


            //    //change this to buttons
            //    var buttons = new List<InlineKeyboardButton[]>
            //    {
            //        new[]
            //        {
            //            new InlineKeyboardButton
            //            {
            //                Text = "Global Stats",
            //                Url = "http://www.tgwerewolf.com/Stats?referrer=stats"
            //            }
            //        }
            //    };
            //    if (u.Message.Chat.Type != ChatType.Private)
            //        buttons.Add(new[]
            //        {
            //            new InlineKeyboardButton
            //            {
            //                Text = $"{u.Message.Chat.Title} Stats",
            //                Url = "http://www.tgwerewolf.com/Stats/Group/" + u.Message.Chat.Id + "?referrer=stats"
            //            }
            //        });
            //    buttons.Add(new[]
            //    {
            //        new InlineKeyboardButton
            //        {
            //            Text = $"{u.Message.From.FirstName} Stats",
            //            Url = "http://www.tgwerewolf.com/Stats/Player/" + u.Message.From.Id + "?referrer=stats"
            //        }
            //    });
            //    var menu = new InlineKeyboardMarkup(buttons.ToArray());
            //    Bot.Api.SendTextMessage(u.Message.Chat.Id, "Stats", replyMarkup: menu);
            //}
        }

        [Command(Trigger = "conquistas")]
        public static void Conquistas(Update update, string[] args)
        {
            Player p = null;
            var Content = "";
            using (var db = new WWContext())
            {
                p = db.Players.FirstOrDefault(x => x.TelegramId == update.Message.From.Id);

                if (p == null)
                {
                    return;
                }
                //find the player
                var ach = (Achievements)(p.Achievements ?? 0);
                var count = ach.GetUniqueFlags().Count();
                Content += $"\n{count} {Commands.GetLocaleString("AchievementsUnlocked", p.Language)}\n";
                foreach (var a in ach.GetUniqueFlags())
                    Content += $"\t✅ {a.GetName().ToBold()}\n<i>{a.GetDescription()}.</i>\n";
                Content += "\n/conquistasBloqueadas";
                Bot.Api.SendTextMessageAsync(update.Message.From.Id, Content, disableWebPagePreview: true, parseMode: ParseMode.Html);
            }
            if (update.Message.Chat.Type != ChatType.Private)
                Send(GetLocaleString("SentPrivate", GetLanguage(update.Message.From.Id)), update.Message.Chat.Id);
        }

        [Command(Trigger = "conquistasBloqueadas")]
        public static void ConquistasBloqueadas(Update update, string[] args)
        {
            Player p = null;
            var Content = "";
            using (var db = new WWContext())
            {
                p = db.Players.FirstOrDefault(x => x.TelegramId == update.Message.From.Id);

                if (p == null)
                {
                    return;
                }
                //find the player
                var notach = (Achievements)~(Convert.ToUInt64((p.Achievements ?? 0)));
                var count = notach.GetUniqueFlags().Count();
                Content = $"\n{count} {Commands.GetLocaleString("AchievementsLocked", p.Language)}\n";
                foreach (var a in notach.GetUniqueFlags())
                    Content += $"\t❌ {a.GetName().ToBold()}\n<i>{a.GetDescription()}.</i>\n";
                Bot.Api.SendTextMessageAsync(update.Message.From.Id, Content, disableWebPagePreview: true, parseMode: ParseMode.Html);
            }
            if (update.Message.Chat.Type != ChatType.Private)
                Send(GetLocaleString("SentPrivate", GetLanguage(update.Message.From.Id)), update.Message.Chat.Id);
        }
    }
}
