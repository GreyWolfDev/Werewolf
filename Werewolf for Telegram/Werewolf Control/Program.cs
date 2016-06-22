using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Database;
using Werewolf_Control.Helpers;

namespace Werewolf_Control
{
    class Program
    {
        internal static bool Running = true;
        private static bool _writingInfo = false;
        static void Main(string[] args)
        {
            //get the version of the bot and set the window title
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            Console.Title = $"Werewolf Moderator {version}";

            //Initialize the TCP connections
            TCP.Initialize();
            //Let the nodes reconnect
            Thread.Sleep(1000);

            //start up the bot
            new Thread(Bot.Initialize).Start();
            new Thread(NodeMonitor).Start();
            //now pause the main thread to let everything else run
            Thread.Sleep(-1);
        }

        internal static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            DateTime dt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local).AddDays(fvi.ProductBuildPart).AddSeconds(fvi.ProductPrivatePart * 2).ToLocalTime();
            return "Current Version: " + version + Environment.NewLine + "Build time: " + dt + " (Central)";
        }

        public static void Log(string s, bool error = false)
        {
            while (_writingInfo)
                Thread.Sleep(50);
            Console.ForegroundColor = error ? ConsoleColor.Red : ConsoleColor.Gray;
            Console.WriteLine(s);
        }

        private static void NodeMonitor()
        {
            while (Running)
            {
                try
                {
                    var info = GetRunInfo();

                    //now dump all this to the console
                    //first get our current caret position
                    _writingInfo = true;
                    var ypos = Console.CursorTop;
                    Console.CursorTop = 0;
                    var xpos = Console.CursorLeft;
                    Console.CursorLeft = 0;

                    //write the info
                    Console.WriteLine(info);
                    //put the cursor back;
                    Console.CursorTop = ypos;
                    Console.CursorLeft = xpos;
                    _writingInfo = false;
                }
                finally
                {
                    _writingInfo = false;
                }
                Thread.Sleep(1000);
            }
        }

        public static string GetRunInfo()
        {
            var Nodes = Bot.Nodes.OrderBy(x => x.Version).ToList();
            var CurrentPlayers = Nodes.Sum(x => x.CurrentPlayers);
            var CurrentGames = Nodes.Sum(x => x.CurrentGames);
            var TotalPlayers = Nodes.Sum(x => x.TotalPlayers);
            var TotalGames = Nodes.Sum(x => x.TotalGames);
            var NumThreads = Process.GetCurrentProcess().Threads.Count;
            var Uptime = DateTime.UtcNow - Bot.StartTime;
            var MessagesRx = Bot.MessagesReceived;
            var CommandsRx = Bot.CommandsReceived;

            return
                $"Connected Nodes: {Nodes.Count}\nCurrent Players: {CurrentPlayers}\tCurrent Games: {CurrentGames}\nTotal Players: {TotalPlayers}\tTotal Games: {TotalGames}\n" +
                $"Threads: {NumThreads}\tUptime: {Uptime}\nMessages: {MessagesRx}\tCommands: {CommandsRx}";

        }
    }
}
