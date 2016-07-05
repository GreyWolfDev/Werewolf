using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Helpers;
using Werewolf_Control.Models;

namespace Werewolf_Control
{
    public static partial class Commands
    {
        private static Player GetDBPlayer(int id, WWContext db)
        {
            return db.Players.FirstOrDefault(x => x.TelegramId == id);
        }

        private static void StartGame(bool chaos, Update update)
        {
            if (update.Message.Chat.Title == null)
            {
                //PM....  can't do that here
                Bot.Send("You must start a game from within a group chat!", update.Message.Chat.Id);
                return;
            }
            Group grp;
            using (var db = new WWContext())
            {
                grp = db.Groups.FirstOrDefault(x => x.GroupId == update.Message.Chat.Id);
                if (grp == null)
                {
                    grp = MakeDefaultGroup(update.Message.Chat.Id, update.Message.Chat.Title, "StartGame");
                    db.Groups.Add(grp);
                }
                grp.Name = update.Message.Chat.Title;
                grp.UserName = update.Message.Chat.Username;
                grp.BotInGroup = true;
                db.SaveChanges();
            }
            //check nodes to see if player is in a game
            var node = GetPlayerNode(update.Message.From.Id);
            var game = GetGroupNodeAndGame(update.Message.Chat.Id);
            if (game != null || node != null)
            {
                //try grabbing the game again...
                if (game == null)
                    game = node.Games.FirstOrDefault(x => x.Users.Contains(update.Message.From.Id));
                if (game?.Users.Contains(update.Message.From.Id) ?? false)
                {
                    if (game.GroupId != update.Message.Chat.Id)
                    {
                        //player is already in a game, and alive
                        Send(
                            GetLocaleString("AlreadyInGame", grp.Language ?? "English",
                                game.ChatGroup.ToBold() ), update.Message.Chat.Id);
                        return;
                    }
                }

                //player is not in game, they need to join, if they can
                game?.AddPlayer(update);
                if (game == null)
                    Program.Log($"{update.Message.From.FirstName} tried to join a game on node {node?.ClientId}, but game object was null", true);
                return;
            }
            //no game found, start one
            node = Bot.GetBestAvailableNode();
            if (node != null)
            {
                node.StartGame(update, chaos);
                //notify waiting players
                using (var db = new WWContext())
                {
                    var notify = db.NotifyGames.Where(x => x.GroupId == update.Message.Chat.Id).ToList();
                    foreach (var n in notify)
                    {
                        var groupName = update.Message.Chat.Title;
                        if (update.Message.Chat.Username != null)
                            groupName += $" @{update.Message.Chat.Username}";
                        Send(GetLocaleString("NotifyNewGame", grp.Language, groupName.ToBold()), n.UserId);
                        Thread.Sleep(100);
                    }

                    //just to be sure...
                    db.Database.ExecuteSqlCommand($"DELETE FROM NotifyGame WHERE GroupId = {update.Message.Chat.Id}");
                    db.SaveChanges();
                }
            }
            else
            {
                Send("There are no nodes online right now, please try again in a few seconds", update.Message.Chat.Id);
            }
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
            return String.Format(selected.Value.FormatHTML(), args).Replace("\\n", Environment.NewLine);
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

        private static Node GetPlayerNode(int id)
        {
            var node = Bot.Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.Users.Contains(id)));
            if (node == null)
                node = Bot.Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.Users.Contains(id)));
            if (node == null)
                node = Bot.Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.Users.Contains(id)));
            return node;
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
    }
}
