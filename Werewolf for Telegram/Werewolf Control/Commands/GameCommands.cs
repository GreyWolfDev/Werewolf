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
            if (!Program.MaintMode)
                StartGame(false, update);
            else
            {
                Send("Sorry, we are about to start maintenance.  Please check @werewolfdev for more information.",
                    update.Message.Chat.Id);
            }
        }

        [Command(Trigger = "startchaos", Blockable = true, InGroupOnly = true)]
        public static void StartChaos(Update update, string[] args)
        {
            if (!Program.MaintMode)
                StartGame(true, update);
            else
            {
                Send("Sorry, we are about to start maintenance.  Please check @werewolfdev for more information.",
                    update.Message.Chat.Id);
            }
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
                var node = GetPlayerNode(update.Message.From.Id);
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
                            var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                            Send(GetLocaleString("AlreadyInGame", grp?.Language ?? "English", game.ChatGroup.ToBold()), update.Message.Chat.Id);
                            return;
                        }
                    }

                    //player is not in game, they need to join, if they can
                    game?.AddPlayer(update);
                    if (game == null)
                        Program.Log($"{update.Message.From.FirstName} tried to join a game on node {node?.ClientId}, but game object was null", true);
                    return;
                }
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
                if(game.State.Equals(Models.GameState.Joining))
                {
                    using (var db = new WWContext())
                    {
                        string msg;
                        if (game.Users.Count == 1) msg = "Apenas 1 jogador deu /join até agora:\n";
                        else msg = $"{game.Users.Count} jogadores deram /join:\n";

                        var user = db.Players.Where(p => game.Users.Contains(p.TelegramId)).AsEnumerable();
                        msg += user.Aggregate("", (current, p) => current + ($"{p.Name.ToBold()} (@{p.UserName})\n"));
                        Send(msg, id);
                    }
                }
                else
                {
                    game.ShowPlayers();
                }
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

        [Command(Trigger = "extend", Blockable = true, InGroupOnly = true)]
        public static void Extend(Update update, string[] args)
        {
            // this method is a MESS...
            
            var id = update.Message.Chat.Id;
            var isadmin = UpdateHelper.IsGroupAdmin(update) || UpdateHelper.IsGlobalAdmin(update.Message.From.Id);
            //check nodes to see if player is in a game
            var node = GetPlayerNode(update.Message.From.Id);
            var game = GetGroupNodeAndGame(update.Message.Chat.Id);
            if (game != null || node != null)
            {
                //try grabbing the game again...
                if (node != null || isadmin)
                {
                    game =
                        node?.Games.FirstOrDefault(
                            x => x.GroupId == update.Message.Chat.Id);
                    if (isadmin || (game?.Users.Contains(update.Message.From.Id) ?? false))
                    {
                        int seconds;
                        seconds = int.TryParse(args[1], out seconds) ? seconds : 30;
                        if (seconds < 0 && !isadmin)
                            Send(GetLocaleString("GroupAdminOnly", GetLanguage(id)), id); //otherwise we're allowing people to /forcestart
                        else
                            using (var db = new WWContext())
                            {
                                if (isadmin || (db.Groups.FirstOrDefault(x => x.GroupId == update.Message.Chat.Id).AllowExtend ?? false)) //default value is false. if you change that, check MakeDefaultGroup and config menu!
                                    game?.ExtendTime(update.Message.From.Id, isadmin, seconds);
                                else
                                    Send(GetLocaleString("GroupAdminOnly", GetLanguage(id)), id);
                            }
                        return;
                    }
                }
                else
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
