using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Database;
using Telegram.Bot.Types;
using Werewolf_Control.Attributes;
using Werewolf_Control.Handler;
using Werewolf_Control.Helpers;

namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Command(Trigger = "winchart", DevOnly = true)]
        public static void WinChart(Update update, string[] args)
        {
            Charting.TeamWinChart(args[1], update);
        }

        [Command(Trigger = "learngif", DevOnly = true)]
        public static void LearnGif(Update update, string[] args)
        {
            UpdateHandler.SendGifIds = !UpdateHandler.SendGifIds;
            Bot.Send($"GIF learning = {UpdateHandler.SendGifIds}", update.Message.Chat.Id);
        }

        [Command(Trigger = "update", DevOnly = true)]
        public static void Update(Update update, string[] args)
        {
            if (update.Message.Date > DateTime.UtcNow.AddSeconds(-3))
            {
                Process.Start(Path.Combine(Bot.RootDirectory, "Resources\\update.exe"));
                Bot.Running = false;
                Program.Running = false;
                Thread.Sleep(500);
                Environment.Exit(1);
            }
        }

        [Command(Trigger = "sendonline", DevOnly = true)]
        public static void SendOnline(Update update, string[] args)
        {
            new Task(Bot.SendOnline).Start();
        }

        [Command(Trigger = "replacenodes", DevOnly = true)]
        public static void ReplaceNodes(Update update, string[] args)
        {
            foreach(var n in Bot.Nodes)
                n.ShutDown();
            //get version
            var baseDirectory = Path.Combine(Bot.RootDirectory, ".."); //go up one directory
            var currentChoice = new NodeChoice();
            foreach (var dir in Directory.GetDirectories(baseDirectory, "*Node*"))
            {
                //get the node exe in this directory
                var file = Directory.GetFiles(dir, "Werewolf Node.exe").First();
                Version fvi = System.Version.Parse(FileVersionInfo.GetVersionInfo(file).FileVersion);
                if (fvi > currentChoice.Version)
                {
                    currentChoice.Path = file;
                    currentChoice.Version = fvi;
                }
            }

            Bot.Send($"Replacing nodes with latest version: {currentChoice.Version}", update.Message.Chat.Id);
        }

        [Command(Trigger = "playtime", DevOnly = true)]
        public static void PlayTime(Update update, string[] args)
        {
            if (args.Length > 1)
            {
                var playerCount = int.Parse(args[1]);
                using (var db = new WWContext())
                {
                    var counts = db.getPlayTime(playerCount).First();
                    var msg =
                        $"(In minutes)\nMin: {counts.Minimum}\nMax: {counts.Maximum}\nAverage: {counts.Average}";
                    Bot.Send(msg, update.Message.Chat.Id);
                }
            }
        }

        [Command(Trigger = "getroles", DevOnly = true)]
        public static void GetRoles(Update update, string[] args)
        {
            if (args.Length > 1)
            {
                var groupName = args.Skip(1).Aggregate((a, b) => a + " " + b).Trim();
                using (var db = new WWContext())
                {
                    var roles = db.getRoles(groupName);
                    var msg = roles.Aggregate("", (current, r) => current + $"{r.name}: {r.role}\n");
                    Bot.Send(msg, update.Message.Chat.Id);
                }
            }
        }

        [Command(Trigger = "skipvote", DevOnly = true)]
        public static void SkipVote(Update update, string[] args)
        {
            var node = GetPlayerNode(update.Message.From.Id);
            var game = GetGroupNodeAndGame(update.Message.Chat.Id);
            if (game != null || node != null)
            {
                //try grabbing the game again...
                if (node != null && game == null)
                    game =
                        node.Games.FirstOrDefault(
                            x => x.Users.Contains(update.Message.From.Id));
                //try again.....
                if (game == null)
                    game = GetGroupNodeAndGame(update.Message.Chat.Id);
                //player is not in game, they need to join, if they can
                game?.SkipVote();
                
                return;
            }
        }

        [Command(Trigger = "cpu", DevOnly = true)]
        public static void Cpu(Update update, string[] args)
        {
            Send(Program.AvgCpuTime.ToString(), update.Message.Chat.Id);
        }
    }
}
