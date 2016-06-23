using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
<<<<<<< HEAD
=======
using System.Windows.Forms.Design;
>>>>>>> a7441d7026626d52c1faa6849e40980ab92907c7
using System.Xml.Linq;
using Database;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;

namespace Werewolf_Control.Handler
{
    internal static class UpdateHandler
    {
        internal static int Para = 129046388;

        internal static int[] PermaBanList = { 226424085 };//Duce
        internal static bool SendGifIds = false;
        public static void UpdateReceived(object sender, UpdateEventArgs e)
        {
            {
                Bot.MessagesReceived++;

                //ignore previous messages
                if ((e.Update.Message?.Date ?? DateTime.MinValue) < Bot.StartTime.AddSeconds(-10))
                    return; //toss it
                var update = e.Update;
                var id = update.Message.Chat.Id;
#if DEBUG
                if (update.Message.Chat.Title != "Werewolf Beta Testing" && !String.IsNullOrEmpty(update.Message.Chat.Title) && update.Message.Chat.Title != "Werewolf Mod / Dev chat (SFW CUZ YOUNGENS)")
                {
                    try
                    {
                        Bot.Api.LeaveChat(update.Message.Chat.Id);
                    }
                    catch
                    {
                        // ignored
                    }
                }
#endif
                //Settings.Main.LogText += update?.Message?.Text + Environment.NewLine;
                bool block = (id == Settings.SupportChatId);

#if !DEBUG
                try
#endif
                {
                    Group grp;
                    switch (update.Message.Type)
                    {
                        case MessageType.UnknownMessage:
                            break;
                        case MessageType.TextMessage:
                            if (update.Message.Text.StartsWith("!") || update.Message.Text.StartsWith("/"))
                            {
<<<<<<< HEAD
                                
=======

>>>>>>> a7441d7026626d52c1faa6849e40980ab92907c7
                                if (PermaBanList.Contains(update.Message?.From?.Id ?? 0))
                                {
                                    Bot.Api.SendTextMessage(id, "*You have been permanently banned from Werewolf.*",
                                        replyToMessageId: update.Message.MessageId, parseMode: ParseMode.Markdown);
                                    if (update.Message.From != null)
                                        Program.Log($"@{update.Message.From.Username} has been notified of ban");
                                    return;
                                }
                                var args = GetParameters(update.Message.Text);
                                args[0] = args[0].ToLower().Replace("@" + Bot.Me.Username.ToLower(), "");

                                //check for the command
<<<<<<< HEAD
=======
                                #region More optimized code, but slow as hell
                                /*
>>>>>>> a7441d7026626d52c1faa6849e40980ab92907c7
                                var command = Bot.Commands.FirstOrDefault(
                                        x =>
                                            String.Equals(x.Trigger, args[0],
                                                StringComparison.InvariantCultureIgnoreCase));
                                if (command != null)
                                {
                                    //check that we should run the command
                                    if (block && command.Blockable)
                                        return;
                                    if (command.DevOnly && update.Message.From.Id != Para)
                                    {
                                        Send("You aren't the developer...", id);
                                        return;
                                    }
                                    if (command.GlobalAdminOnly)
                                    {
                                        using (var DB = new WWContext())
                                        {
                                            if (!DB.Admins.Any(x => x.UserId == update.Message.From.Id))
                                            {
                                                Send($"You aren't a global admin...", id);
                                                return;
                                            }
                                        }
                                    }
                                    if (command.GroupAdminOnly & !UpdateHelper.IsGroupAdmin(update))
                                    {
                                        Send(GetLocaleString("GroupAdminOnly", GetLanguage(update.Message.Chat.Id)), id);
                                        return;
                                    }
                                    if (command.InGroupOnly & update.Message.Chat.Type == ChatType.Private)
                                    {
                                        Send($"You must run this command in a group", id);
                                        return;
                                    }
                                    Bot.CommandsReceived++;
                                    command.Method.Invoke(update, args);
                                }
<<<<<<< HEAD
=======
                                */
                                Bot.CommandsReceived++;
                                switch (args[0].ToLower())
                                {
                                    
                                    #region Admin Commands
                                    case "smite":
                                        if (UpdateHelper.IsGroupAdmin(update))
                                            Commands.Smite(update, args);
                                        break;
                                    case "config":
                                        if (UpdateHelper.IsGroupAdmin(update))
                                            Commands.Config(update, args);
                                        break;
                                    case "uploadlang":
                                        using (var DB = new WWContext())
                                        {
                                            if (!DB.Admins.Any(x => x.UserId == update.Message.From.Id))
                                            {
                                                Send($"You aren't a global admin...", id);
                                                return;
                                            }
                                        }
                                        Commands.UploadLang(update, args);
                                        break;
                                    case "validatelangs":
                                        using (var DB = new WWContext())
                                        {
                                            if (!DB.Admins.Any(x => x.UserId == update.Message.From.Id))
                                            {
                                                Send($"You aren't a global admin...", id);
                                                return;
                                            }
                                        }
                                        Commands.ValidateLangs(update, args);
                                        break;

                                    #endregion
                                    #region Dev Commands
                                    case "winchart":
                                        if (update.Message.From.Id == Para)
                                        {
                                            Commands.WinChart(update, args);
                                        }
                                        else
                                        {
                                            Send("You aren't the developer...", id);
                                        }
                                        break;
                                    case "learngif":
                                        if (update.Message.From.Id == Para)
                                        {
                                            Commands.LearnGif(update, args);
                                        }
                                        else
                                        {
                                            Send("You aren't the developer...", id);
                                        }
                                        break;
                                    case "update":
                                        if (update.Message.From.Id == Para)
                                        {
                                            Commands.Update(update, args);
                                        }
                                        else
                                        {
                                            Send("You aren't the developer...", id);
                                        }
                                        break;
                                    case "sendonline":
                                        if (update.Message.From.Id == Para)
                                        {
                                            Commands.SendOnline(update, args);
                                        }
                                        else
                                        {
                                            Send("You aren't the developer...", id);
                                        }
                                        break;
                                    case "replacenodes":
                                        if (update.Message.From.Id == Para)
                                        {
                                            Commands.ReplaceNodes(update, args);
                                        }
                                        else
                                        {
                                            Send("You aren't the developer...", id);
                                        }
                                        break;
                                    case "playtime":
                                        if (update.Message.From.Id == Para)
                                        {
                                            Commands.PlayTime(update, args);
                                        }
                                        else
                                        {
                                            Send("You aren't the developer...", id);
                                        }
                                        break;
                                    case "getroles":
                                        if (update.Message.From.Id == Para)
                                        {
                                            Commands.GetRoles(update, args);
                                        }
                                        else
                                        {
                                            Send("You aren't the developer...", id);
                                        }
                                        break;
                                    #endregion
                                    #region Game Commands
                                    case "startgame":
                                        if (block) return;
                                        if (update.Message.Chat.Type == ChatType.Private)
                                        {
                                            Send($"You must run this command in a group", id);
                                            return;
                                        }
                                        Commands.StartGame(update, args);
                                        break;
                                    case "startchaos":
                                        if (block) return;
                                        if (update.Message.Chat.Type == ChatType.Private)
                                        {
                                            Send($"You must run this command in a group", id);
                                            return;
                                        }
                                        Commands.StartChaos(update, args);
                                        break;
                                    case "join":
                                        if (block) return;
                                        if (update.Message.Chat.Type == ChatType.Private)
                                        {
                                            Send($"You must run this command in a group", id);
                                            return;
                                        }
                                        Commands.Join(update, args);
                                        break;
                                    case "forcestart":
                                        if (block) return;
                                        if (update.Message.Chat.Type == ChatType.Private)
                                        {
                                            Send($"You must run this command in a group", id);
                                            return;
                                        }
                                        if (UpdateHelper.IsGroupAdmin(update))
                                            Commands.ForceStart(update, args);
                                        break;
                                    case "players":
                                        if (block) return;
                                        if (update.Message.Chat.Type == ChatType.Private)
                                        {
                                            Send($"You must run this command in a group", id);
                                            return;
                                        }
                                        Commands.Players(update, args);
                                        break;
                                    case "flee":
                                        if (block) return;
                                        if (update.Message.Chat.Type == ChatType.Private)
                                        {
                                            Send($"You must run this command in a group", id);
                                            return;
                                        }
                                        Commands.Flee(update, args);
                                        break;
                                    #endregion
                                    #region General Commands
                                    case "ping":
                                        Commands.Ping(update, args);
                                        break;
                                    case "help":
                                        Commands.Help(update, args);
                                        break;
                                    case "chatid":
                                        Commands.ChatId(update, args);
                                        break;
                                    case "changelog":
                                        Commands.ChangeLog(update, args);
                                        break;
                                    case "runinfo":
                                        Commands.RunInfo(update, args);
                                        break;
                                    case "version":
                                        Commands.Version(update, args);
                                        break;
                                    case "start":
                                        Commands.Start(update, args);
                                        break;
                                    case "nextgame":
                                        if (block) return;
                                        if (update.Message.Chat.Type == ChatType.Private)
                                        {
                                            Send($"You must run this command in a group", id);
                                            return;
                                        }
                                        Commands.NextGame(update, args);
                                        break;
                                    case "getlang":
                                        Commands.GetLang(update, args);
                                        break;

                                    #endregion
                                }


                                #endregion
>>>>>>> a7441d7026626d52c1faa6849e40980ab92907c7
                            }
                            break;
                        case MessageType.PhotoMessage:
                            break;
                        case MessageType.AudioMessage:
                            break;
                        case MessageType.VideoMessage:
                            break;
                        case MessageType.VoiceMessage:
                            break;
                        case MessageType.DocumentMessage:
                            if (update.Message.From.Id == Para && SendGifIds)
                            {
                                var doc = update.Message.Document;
                                Send(doc.FileId, update.Message.Chat.Id);
                            }
                            break;
                        case MessageType.StickerMessage:
                            break;
                        case MessageType.LocationMessage:
                            break;
                        case MessageType.ContactMessage:
                            break;
                        case MessageType.ServiceMessage:
                            using (var DB = new WWContext())
                            {
<<<<<<< HEAD
                                
=======

>>>>>>> a7441d7026626d52c1faa6849e40980ab92907c7
                                id = update.Message.Chat.Id;
                                var m = update.Message;
                                if (m.LeftChatMember?.Id == Bot.Me.Id)
                                {
                                    //removed from group
                                    var grps = DB.Groups.Where(x => x.GroupId == id);
                                    if (!grps.Any())
                                    {
                                        return;
                                    }
                                    foreach (var g in grps)
                                    {
                                        g.BotInGroup = false;
                                        g.UserName = update.Message.Chat.Username;
                                        g.Name = update.Message.Chat.Title;
                                    }
                                    DB.SaveChanges();
                                }
                                if (m.NewChatMember?.Id == Bot.Me.Id)
                                {
                                    //added to a group
                                    grp = DB.Groups.FirstOrDefault(x => x.GroupId == id);
                                    if (grp == null)
                                    {
                                        grp = MakeDefaultGroup(id, update.Message.Chat.Title, "NewChatMember");
                                        DB.Groups.Add(grp);
                                        DB.SaveChanges();
                                        grp = DB.Groups.FirstOrDefault(x => x.GroupId == id);
                                    }
                                    grp.BotInGroup = true;
                                    grp.UserName = update.Message.Chat.Username;
                                    grp.Name = update.Message.Chat.Title;
                                    DB.SaveChanges();

                                    var msg =
                                        $"You've just added Werewolf Moderator!  Use /config (group admins) to configure group settings.   If you need assistance, join the support channel @werewolfsupport";
                                    msg += Environment.NewLine +
                                           "For updates on what is happening, join the dev channel @werewolfdev" +
                                           Environment.NewLine +
                                           "Full information is available on the [website](http://werewolf.parawuff.com)";
                                    Bot.Api.SendTextMessage(id, msg, parseMode: ParseMode.Markdown);
                                }
                            }
                            break;
                        case MessageType.VenueMessage:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
#if !DEBUG
                catch (Exception ex)
                {
                    Send(ex.Message, id);
                }
#endif
            }
        }
<<<<<<< HEAD
        
=======

>>>>>>> a7441d7026626d52c1faa6849e40980ab92907c7
        /// <summary>
        /// Gets the language for the group, defaulting to English
        /// </summary>
        /// <param name="id">The ID of the group</param>
        /// <returns></returns>
        private static string GetLanguage(long id)
        {
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                return grp?.Language ?? "English";
            }
        }

        private static GameInfo GetGroupNodeAndGame(long id)
        {
            var node = Bot.Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.GroupId == id))?.Games.FirstOrDefault(x => x.GroupId == id);
            if (node == null)
                node = Bot.Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.GroupId == id))?.Games.FirstOrDefault(x => x.GroupId == id);
            if (node == null)
                node = Bot.Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.GroupId == id))?.Games.FirstOrDefault(x => x.GroupId == id);
            return node;
        }

        public static void CallbackReceived(object sender, CallbackQueryEventArgs e)
        {
            using (var DB = new WWContext())
            {
                try
                {
                    var query = e.CallbackQuery;
                    string[] args = query.Data.Split('|');
                    InlineKeyboardMarkup menu;
                    Group grp;
                    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
                    long groupid = 0;
                    if (args[0] == "vote")
                    {
                        var node = Bot.Nodes.FirstOrDefault(x => x.ClientId.ToString() == args[1]);
                        node?.SendReply(query);
                        return;
                    }

                    groupid = long.Parse(args[1]);
                    grp = DB.Groups.FirstOrDefault(x => x.GroupId == groupid);
                    if (grp == null && args[0] != "getlang")
                        return;
                    var command = args[0];
                    var choice = "";
                    if (args.Length > 2)
                        choice = args[2];
                    if (choice == "cancel")
                    {
                        Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                            $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                        return;
                    }
                    switch (command)
                    {
                        case "validate":
                            //choice = args[1];
                            if (choice == "All")
                            {
                                Helpers.LanguageHelper.ValidateFiles(query.Message.Chat.Id, query.Message.MessageId);
                                return;
                            }
                            //var menu = new ReplyKeyboardHide { HideKeyboard = true, Selective = true };
                            //Bot.SendTextMessage(id, "", replyToMessageId: update.Message.MessageId, replyMarkup: menu);
                            var langOptions =
                                Directory.GetFiles(Bot.LanguageDirectory)
                                    .Select(
                                        x =>
                                            new
                                            {
                                                Name =
                                                    XDocument.Load(x)
                                                        .Descendants("language")
                                                        .First()
                                                        .Attribute("name")
                                                        .Value,
                                                FilePath = x
                                            });
                            var option = langOptions.First(x => x.Name == choice);
                            LanguageHelper.ValidateLanguageFile(query.Message.Chat.Id, option.FilePath, query.Message.MessageId);
                            return;
                        case "getlang":
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "One moment...");
                            if (choice == "All")
                                LanguageHelper.SendAllFiles(query.Message.Chat.Id);
                            else
                                LanguageHelper.SendFile(query.Message.Chat.Id, choice);

                            break;
                        case "upload":
                            Console.WriteLine(choice);
                            if (choice == "current")
                            {
                                Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, "No action taken.");
                                return;
                            }


                            Helpers.LanguageHelper.UseNewLanguageFile(choice, query.Message.Chat.Id, query.Message.MessageId);
                            return;

                        case "vote":
                            //send it back to the game;
                            var node = Bot.Nodes.FirstOrDefault(x => x.ClientId.ToString() == args[1]);
                            node?.SendReply(query);
                            break;
                        case "lang":
                            //load up each file and get the names
                            var langs =
                                Directory.GetFiles(Bot.LanguageDirectory)
                                    .Select(
                                        x =>
                                            new
                                            {
                                                Name =
                                                        XDocument.Load(x)
                                                            .Descendants("language")
                                                            .First()
                                                            .Attribute("name")
                                                            .Value,
                                                Base = XDocument.Load(x)
                                                            .Descendants("language")
                                                            .First()
                                                            .Attribute("base")
                                                            .Value,
                                                Variant = XDocument.Load(x)
                                                            .Descendants("language")
                                                            .First()
                                                            .Attribute("variant")
                                                            .Value,
                                                FileName = Path.GetFileNameWithoutExtension(x)
                                            });

                            buttons.Clear();
                            buttons.AddRange(langs.Select(x => x.Base).Distinct().OrderBy(x => x).Select(x => new InlineKeyboardButton(x, $"setlang|{groupid}|{x}|null|base")));

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

                            menu = new InlineKeyboardMarkup(baseMenu.ToArray());


                            var curLang = langs.First(x => x.FileName == grp.Language);
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What Language?\nCurrent: {curLang.Base}",
                                replyMarkup: menu);
                            break;
                        case "setlang":

                            //first, is this the base or variant?
                            var isBase = args[4] == "base";
                            //ok, they picked a language, let's set it.
                            var validlangs =
                                Directory.GetFiles(Bot.LanguageDirectory)
                                        .Select(
                                            x =>
                                                new
                                                {
                                                    Name =
                                                        XDocument.Load(x)
                                                            .Descendants("language")
                                                            .First()
                                                            .Attribute("name")
                                                            .Value,
                                                    Base = XDocument.Load(x)
                                                            .Descendants("language")
                                                            .First()
                                                            .Attribute("base")
                                                            .Value,
                                                    Variant = XDocument.Load(x)
                                                            .Descendants("language")
                                                            .First()
                                                            .Attribute("variant")
                                                            .Value,
                                                    FileName = Path.GetFileNameWithoutExtension(x)
                                                });
                            //ok, if base we need to check for variants....
                            var lang = validlangs.First(x => x.Base == choice);
                            if (isBase)
                            {
                                var variants = validlangs.Where(x => x.Base == choice);
                                if (variants.Count() > 1)
                                {
                                    buttons.Clear();
                                    buttons.AddRange(variants.Select(x => new InlineKeyboardButton(x.Variant, $"setlang|{groupid}|{x.Base}|{x.Variant}|v")));

                                    var twoMenu = new List<InlineKeyboardButton[]>();
                                    for (var i = 0; i < buttons.Count; i++)
                                    {
                                        if (buttons.Count - 1 == i)
                                        {
                                            twoMenu.Add(new[] { buttons[i] });
                                        }
                                        else
                                            twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                                        i++;
                                    }

                                    menu = new InlineKeyboardMarkup(twoMenu.ToArray());

                                    var curVariant = validlangs.First(x => x.FileName == grp.Language);
                                    Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                        $"What Variant?\nCurrent: {curVariant.Variant}",
                                        replyMarkup: menu);
                                    return;
                                }
                                //only one variant, move along
                            }
                            else
                            {
                                lang = validlangs.First(x => x.Base == choice && x.Variant == args[3]);
                            }


                            if (
                                Directory.GetFiles(Bot.LanguageDirectory)
                                    .Any(
                                        x =>
                                            String.Equals(Path.GetFileNameWithoutExtension(x), lang.FileName,
                                                StringComparison.InvariantCultureIgnoreCase)))
                            {
                                //now get the group

                                grp.Language = lang.FileName;
                                //check for any games running
                                var ig = GetGroupNodeAndGame(groupid);

                                ig?.LoadLanguage(lang.FileName);
                                menu = GetConfigMenu(groupid);
                                Bot.Api.AnswerCallbackQuery(query.Id, $"Language set to {lang.Base}{(String.IsNullOrWhiteSpace(lang.Variant) ? "" : ": " + lang.Variant)}");
                                Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId, $"What would you like to do?", replyMarkup: menu);
                            }
                            DB.SaveChanges();
                            break;
                        case "online":
                            buttons.Add(new InlineKeyboardButton("Yes", $"setonline|{groupid}|show"));
                            buttons.Add(new InlineKeyboardButton("No", $"setonline|{groupid}|hide"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setonline|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"Do you want your group to be notified when the bot is online?\nCurrent: {grp.DisableNotification != false}",
                                replyMarkup: menu);
                            break;
                        case "setonline":

                            grp.DisableNotification = (choice == "hide");
                            Bot.Api.AnswerCallbackQuery(query.Id,
                                $"Notification will {(grp.DisableNotification == true ? "not " : "")}be shown on startup");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "flee":
                            buttons.Add(new InlineKeyboardButton("Yes", $"setflee|{groupid}|enable"));
                            buttons.Add(new InlineKeyboardButton("No", $"setflee|{groupid}|disable"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setflee|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"Do you want to allow fleeing once the game has started?\nNote: players can still flee during join phase\nCurrent: Players can {(grp.DisableFlee == false ? "" : "not ")}flee",
                                replyMarkup: menu);
                            break;
                        case "setflee":

                            grp.DisableFlee = (choice == "disable");
                            Bot.Api.AnswerCallbackQuery(query.Id,
                                $"Players will {(grp.DisableFlee == true ? "not " : "")}be allowed to flee after game start");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "maxplayer":
                            buttons.Add(new InlineKeyboardButton("10", $"setmaxplayer|{groupid}|10"));
                            buttons.Add(new InlineKeyboardButton("15", $"setmaxplayer|{groupid}|15"));
                            buttons.Add(new InlineKeyboardButton("20", $"setmaxplayer|{groupid}|20"));
                            buttons.Add(new InlineKeyboardButton("25", $"setmaxplayer|{groupid}|25"));
                            buttons.Add(new InlineKeyboardButton("30", $"setmaxplayer|{groupid}|30"));
                            buttons.Add(new InlineKeyboardButton("35", $"setmaxplayer|{groupid}|35"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setmaxplayer|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"How many players would like to set as the maximum?\nCurrent: {grp.MaxPlayers ?? Settings.MaxPlayers}",
                                replyMarkup: menu);
                            break;
                        case "setmaxplayer":

                            grp.MaxPlayers = int.Parse(choice);
                            Bot.Api.AnswerCallbackQuery(query.Id, $"Max players set to {choice}");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "roles":
                            buttons.Add(new InlineKeyboardButton("Show", $"setroles|{groupid}|show"));
                            buttons.Add(new InlineKeyboardButton("Hide", $"setroles|{groupid}|hide"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setroles|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"Show or Hide roles on death?\nCurrent: {(grp.ShowRoles == false ? "Hidden" : "Shown")}",
                                replyMarkup: menu);
                            break;
                        case "setroles":

                            grp.ShowRoles = (choice == "show");
                            Bot.Api.AnswerCallbackQuery(query.Id,
                                $"Roles will be {(grp.ShowRoles == false ? "hidden" : "shown")} on death.");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "mode":
                            buttons.Add(new InlineKeyboardButton("Normal Only", $"setmode|{groupid}|Normal"));
                            buttons.Add(new InlineKeyboardButton("Chaos Only", $"setmode|{groupid}|Chaos"));
                            buttons.Add(new InlineKeyboardButton("Player Choice", $"setmode|{groupid}|Player"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setmode|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What game mode will the group be?\nCurrent: {grp.Mode}", replyMarkup: menu);
                            break;
                        case "setmode":

                            grp.Mode = choice;
                            Bot.Api.AnswerCallbackQuery(query.Id, $"Game mode set to {choice}");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "endroles":
                            buttons.Add(new InlineKeyboardButton("Don't show any", $"setendroles|{groupid}|None"));
                            buttons.Add(new InlineKeyboardButton("Show only living players",
                                $"setendroles|{groupid}|Living"));
                            buttons.Add(new InlineKeyboardButton("Show all players", $"setendroles|{groupid}|All"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setendroles|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"How do you want roles to be shown at the end?\nCurrent: {grp.ShowRolesEnd}",
                                replyMarkup: menu);
                            break;
                        case "setendroles":

                            grp.ShowRolesEnd = choice;
                            Bot.Api.AnswerCallbackQuery(query.Id, $"Roles shown at end set to: {choice}");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "day":
                            buttons.Add(new InlineKeyboardButton("30", $"setday|{groupid}|30"));
                            buttons.Add(new InlineKeyboardButton("60", $"setday|{groupid}|60"));
                            buttons.Add(new InlineKeyboardButton("90", $"setday|{groupid}|90"));
                            buttons.Add(new InlineKeyboardButton("120", $"setday|{groupid}|120"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setday|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"Choose the base time (in seconds) for day time.   This will still be modified based on number of players.\nMinimum time added based on players is 60 seconds.  Default setting: {Settings.TimeDay}\nCurrent: {grp.DayTime ?? Settings.TimeDay}",
                                replyMarkup: menu);
                            break;
                        case "setday":

                            grp.DayTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQuery(query.Id, $"Base day time set to {choice} seconds");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "night":
                            buttons.Add(new InlineKeyboardButton("30", $"setnight|{groupid}|30"));
                            buttons.Add(new InlineKeyboardButton("60", $"setnight|{groupid}|60"));
                            buttons.Add(new InlineKeyboardButton("90", $"setnight|{groupid}|90"));
                            buttons.Add(new InlineKeyboardButton("120", $"setnight|{groupid}|120"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setnight|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"Choose the time (in seconds) for night time. Default setting: {Settings.TimeNight}\nCurrent:{grp.NightTime ?? Settings.TimeNight}",
                                replyMarkup: menu);
                            break;
                        case "setnight":

                            grp.NightTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQuery(query.Id, $"Night time set to {choice} seconds");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "lynch":
                            buttons.Add(new InlineKeyboardButton("30", $"setlynch|{groupid}|30"));
                            buttons.Add(new InlineKeyboardButton("60", $"setlynch|{groupid}|60"));
                            buttons.Add(new InlineKeyboardButton("90", $"setlynch|{groupid}|90"));
                            buttons.Add(new InlineKeyboardButton("120", $"setlynch|{groupid}|120"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setlynch|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"Choose the time (in seconds) for lynch voting. Default setting: {Settings.TimeLynch}\nCurrent:{grp.LynchTime ?? Settings.TimeLynch}",
                                replyMarkup: menu);
                            break;
                        case "setlynch":

                            grp.LynchTime = int.Parse(choice);
                            Bot.Api.AnswerCallbackQuery(query.Id, $"Lynch voting time set to {choice} seconds");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "fool":
                            buttons.Add(new InlineKeyboardButton("Allow", $"setfool|{groupid}|true"));
                            buttons.Add(new InlineKeyboardButton("Disallow", $"setfool|{groupid}|false"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setfool|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"Allow fool as a role option?\nCurrent: {grp.AllowFool}", replyMarkup: menu);
                            break;
                        case "setfool":

                            grp.AllowFool = (choice == "true");
                            Bot.Api.AnswerCallbackQuery(query.Id, $"Fool as a role set to: {choice}");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "tanner":
                            buttons.Add(new InlineKeyboardButton("Allow", $"settanner|{groupid}|true"));
                            buttons.Add(new InlineKeyboardButton("Disallow", $"settanner|{groupid}|false"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"settanner|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"Allow tanner as a role option?\nCurrent: {grp.AllowTanner}", replyMarkup: menu);
                            break;
                        case "settanner":

                            grp.AllowTanner = (choice == "true");
                            Bot.Api.AnswerCallbackQuery(query.Id, $"Tanner as a role set to: {choice}");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "cult":
                            buttons.Add(new InlineKeyboardButton("Allow", $"setcult|{groupid}|true"));
                            buttons.Add(new InlineKeyboardButton("Disallow", $"setcult|{groupid}|false"));
                            buttons.Add(new InlineKeyboardButton("Cancel", $"setcult|{groupid}|cancel"));
                            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"Allow cult as a role option?\nCurrent: {grp.AllowCult}", replyMarkup: menu);
                            break;
                        case "setcult":

                            grp.AllowCult = (choice == "true");
                            Bot.Api.AnswerCallbackQuery(query.Id, $"Cult as a role set to: {choice}");
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                $"What would you like to do?", replyMarkup: GetConfigMenu(groupid));
                            DB.SaveChanges();
                            break;
                        case "done":
                            Bot.Api.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                                "Thank you, have a good day :)");
                            break;
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }


        private static string[] GetParameters(string input)
        {
            return input.Contains(" ") ? new[] { input.Substring(1, input.IndexOf(" ")).Trim(), input.Substring(input.IndexOf(" ") + 1) } : new[] { input.Substring(1).Trim(), null };
        }

        internal static void Send(string message, long id, bool clearKeyboard = false, ReplyKeyboardMarkup customMenu = null)
        {
            Bot.Send(message, id, clearKeyboard, customMenu);
        }



        private static string GetLocaleString(string key, string language, params object[] args)
        {
            var files = Directory.GetFiles(Bot.LanguageDirectory);
            XDocument doc;
            var file = files.First(x => Path.GetFileNameWithoutExtension(x) == language);
            {
                doc = XDocument.Load(file);
            }
            var strings = doc.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key);
            if (strings == null)
            {
                //fallback to English
                var efile = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));
                strings = efile.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key);
            }
            var values = strings.Descendants("value");
            var choice = Bot.R.Next(values.Count());
            var selected = values.ElementAt(choice);
            return String.Format(selected.Value, args).Replace("\\n", Environment.NewLine);
        }

        internal static Group MakeDefaultGroup(long groupid, string name, string createdBy)
        {
            return new Group
            {
                GroupId = groupid,
                Name = name,
                Language = "English",
                BotInGroup = true,
                ShowRoles = true,
                Mode = "Player",
                DayTime = Settings.TimeDay,
                LynchTime = Settings.TimeLynch,
                NightTime = Settings.TimeNight,
                AllowFool = true,
                AllowTanner = true,
                AllowCult = true,
                DisableFlee = false,
                MaxPlayers = 35,
                CreatedBy = createdBy
            };
        }

        internal static InlineKeyboardMarkup GetConfigMenu(long id)
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            //base menu
            buttons.Add(new InlineKeyboardButton("Show Online Message", $"online|{id}"));
            buttons.Add(new InlineKeyboardButton("Change Language", $"lang|{id}"));
            buttons.Add(new InlineKeyboardButton("Show Roles On Death", $"roles|{id}"));
            buttons.Add(new InlineKeyboardButton("Show Roles At Game End", $"endroles|{id}"));
            buttons.Add(new InlineKeyboardButton("Allow Fleeing", $"flee|{id}"));
            buttons.Add(new InlineKeyboardButton("Set Max Players", $"maxplayer|{id}"));
            buttons.Add(new InlineKeyboardButton("Change Game Mode", $"mode|{id}"));
            buttons.Add(new InlineKeyboardButton("Set Day Timer", $"day|{id}"));
            buttons.Add(new InlineKeyboardButton("Set Lynch Timer", $"lynch|{id}"));
            buttons.Add(new InlineKeyboardButton("Set Night Timer", $"night|{id}"));
            buttons.Add(new InlineKeyboardButton("Allow Fool", $"fool|{id}"));
            buttons.Add(new InlineKeyboardButton("Allow Tanner", $"tanner|{id}"));
            buttons.Add(new InlineKeyboardButton("Allow Cult", $"cult|{id}"));
            buttons.Add(new InlineKeyboardButton("Done", $"done|{id}"));
            var twoMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    twoMenu.Add(new[] { buttons[i] });
                }
                else
                    twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }

            var menu = new InlineKeyboardMarkup(twoMenu.ToArray());
            return menu;
        }
    }
}

