using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        [Command(Trigger = "startgame", Blockable = true, InGroupOnly = true)]
        public static void StartGame(Update update, string[] args)
        {
            StartGame(false, update);
        }

        [Command(Trigger = "startchaos", Blockable = true, InGroupOnly = true)]
        public static void StartChaos(Update update, string[] args)
        {
            StartGame(true, update);
        }

        [Command(Trigger = "join", Blockable = true, InGroupOnly = true)]
        public static void Join(Update update, string[] args)
        {
            var id = update.Message.Chat.Id;
            using (var db = new WWContext())
            {
                if (update.Message.Chat.Type == ChatType.Private)
                {
                    //PM....  can't do that here
                    Send(GetLocaleString("JoinFromGroup", GetLanguage(update.Message.From.Id)), id);
                    return;
                }
                //check nodes to see if player is in a game

                var game = GetGroupNodeAndGame(update.Message.Chat.Id);
                if (game == null)
                {
                    Thread.Sleep(50);
                    game = GetGroupNodeAndGame(update.Message.Chat.Id);
                }
                if (game == null)
                {
                    Thread.Sleep(50);
                    game = GetGroupNodeAndGame(update.Message.Chat.Id);
                }
                game?.AddPlayer(update);
                if (game == null)
                {
                    var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                    if (grp == null)
                    {
                        grp = MakeDefaultGroup(id, update.Message.Chat.Title, "join");
                        db.Groups.Add(grp);
                        db.SaveChanges();
                    }
                    Send(GetLocaleString("NoGame", grp?.Language ?? "English"), id);
                }
            }
        }

        [Command(Trigger = "forcestart", Blockable = true, GroupAdminOnly = true, InGroupOnly = true)]
        public static void ForceStart(Update update, string[] args)
        {
            var id = update.Message.Chat.Id;
            using (var db = new WWContext())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                if (grp == null)
                {
                    grp = MakeDefaultGroup(id, update.Message.Chat.Title, "forcestart");
                    db.Groups.Add(grp);
                    db.SaveChanges();
                }
                if (UpdateHelper.IsGroupAdmin(update))
                {
                    var game = GetGroupNodeAndGame(update.Message.Chat.Id);
                    if (game != null)
                    {
                        //send forcestart                                            
                        game.ForceStart();
                    }
                    else
                    {
                        Send(GetLocaleString("NoGame", grp.Language), id);
                    }
                }
                else
                    Send(GetLocaleString("GroupAdminOnly", grp?.Language ?? "English"), id);
            }

        }

        [Command(Trigger = "players", Blockable = true, InGroupOnly = true)]
        public static void Players(Update update, string[] args)
        {
            var id = update.Message.Chat.Id;

            var game = GetGroupNodeAndGame(id);
            if (game == null)
            {
                Send(GetLocaleString("NoGame", GetLanguage(id)), id);
            }
            else
            {
                game.ShowPlayers();
            }

        }

        [Command(Trigger = "flee", Blockable = true, InGroupOnly = true)]
        public static void Flee(Update update, string[] args)
        {
            var id = update.Message.Chat.Id;
            //check nodes to see if player is in a game
            var node = GetPlayerNode(update.Message.From.Id);
            var game = GetGroupNodeAndGame(update.Message.Chat.Id);
            if (game != null || node != null)
            {
                //try grabbing the game again...
                if (node != null)
                    game =
                        node.Games.FirstOrDefault(
                            x => x.Users.Contains(update.Message.From.Id));
                if (game?.Users.Contains(update.Message.From.Id) ?? false)
                {
                    game?.RemovePlayer(update);

                    return;
                }
                if (node != null)
                {
                    //there is a game, but this player is not in it
                    Send(GetLocaleString("NotPlaying", GetLanguage(id)), id);
                }
            }
            else
            {
                Send(GetLocaleString("NoGame", GetLanguage(id)), id);
            }


        }
    }
}
