using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Database;
using Werewolf_Control.Handler;
using Werewolf_Control.Helpers;

namespace Werewolf_Control
{
    class Program
    {
        internal static bool Running = true;
        private static bool _writingInfo = false;
        internal static PerformanceCounter CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        internal static float AvgCpuTime;
        ///private static List<float> CpuTimes = new List<float>();
        internal static List<long> MessagesReceived = new List<long>();
        internal static List<long> MessagesProcessed = new List<long>();
        internal static List<long> MessagesSent = new List<long>();
        private static long _previousMessages, _previousMessagesTx, _previousMessagesRx;
        internal static float MessagePxPerSecond, MessageRxPerSecond, MessageTxPerSecond;
        internal static int NodeMessagesSent = 0;
        private static System.Timers.Timer _timer;
        public static int MaxGames;
        public static DateTime MaxTime = DateTime.MinValue;
        public static bool MaintMode = false;
#if BETA
        public static bool BetaUnlocked = false;
#endif
        internal static string XsollaLink = null;
        internal static string XsollaApiId = null;
        internal static string XsollaApiKey = null;
        internal static int? xsollaProjId = 0;
        internal static readonly HttpClient xsollaClient = new HttpClient();
        internal const string MasterLanguage = "English.xml";
        internal static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                //drop the error to log file and exit
                using (var sw = new StreamWriter(Path.Combine(Bot.RootDirectory, "..\\Logs\\error.log"), true))
                {
                    var e = (eventArgs.ExceptionObject as Exception);
                    sw.WriteLine(DateTime.UtcNow);
                    sw.WriteLine(e.Message);
                    sw.WriteLine(e.StackTrace + "\n");
                    if (eventArgs.IsTerminating)
                        Environment.Exit(5);
                }
            };
#endif
            //get the version of the bot and set the window title
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            Console.Title = $"Werewolf Moderator {version}";


            //Make sure another instance isn't already running
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                Environment.Exit(2);
            }


            var updateid = "";
            //attempt to get id from update
            if (args.Length > 0)
            {
                updateid = args[0];
            }

            XsollaLink = Helpers.RegHelper.GetRegValue("XsollaLink");
            XsollaApiId = Helpers.RegHelper.GetRegValue("XsollaApiId");
            XsollaApiKey = Helpers.RegHelper.GetRegValue("XsollaApiKey");
            try { xsollaProjId = int.Parse(Helpers.RegHelper.GetRegValue("XsollaProjId")); } catch { xsollaProjId = 0; }
            xsollaClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{XsollaApiId}:{XsollaApiKey}")));

            //Initialize the TCP connections
            TCP.Initialize();
            //Let the nodes reconnect
            Thread.Sleep(1000);

            //initialize EF before we start receiving
            using (var db = new WWContext())
            {
                var count = db.GlobalBans.Count();
            }

#if BETA
            BetaUnlocked = File.Exists(Path.Combine(Bot.RootDirectory, ".betaunlocked"));
#endif

            //start up the bot
            new Thread(() => Bot.Initialize(updateid)).Start();
            new Thread(NodeMonitor).Start();

            //new Thread(CpuMonitor).Start();
            new Thread(UpdateHandler.SpamDetection).Start();
            new Thread(UpdateHandler.BanMonitor).Start();
            //new Thread(MessageMonitor).Start();
            _timer = new System.Timers.Timer();
            _timer.Elapsed += new ElapsedEventHandler(TimerOnTick);
            _timer.Interval = 1000;
            _timer.Enabled = true;

            //now pause the main thread to let everything else run
            Thread.Sleep(-1);
        }



        private static void TimerOnTick(object sender, EventArgs eventArgs)
        {
            try
            {
                var newMessages = Bot.MessagesProcessed - _previousMessages;
                _previousMessages = Bot.MessagesProcessed;
                MessagesProcessed.Insert(0, newMessages);
                if (MessagesProcessed.Count > 60)
                    MessagesProcessed.RemoveAt(60);
                MessagePxPerSecond = MessagesProcessed.Max();

                newMessages = (Bot.MessagesSent + NodeMessagesSent) - _previousMessagesTx;
                _previousMessagesTx = (Bot.MessagesSent + NodeMessagesSent);
                MessagesSent.Insert(0, newMessages);
                if (MessagesSent.Count > 60)
                    MessagesSent.RemoveAt(60);
                MessageTxPerSecond = MessagesSent.Max();

                newMessages = Bot.MessagesReceived - _previousMessagesRx;
                _previousMessagesRx = Bot.MessagesReceived;
                MessagesReceived.Insert(0, newMessages);
                if (MessagesReceived.Count > 60)
                    MessagesReceived.RemoveAt(60);
                MessageRxPerSecond = MessagesReceived.Max();
            }
            catch
            {
                // ignored
            }
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
            if (error)
                log.Error(s);
            else
                log.Info(s);
            //while (_writingInfo)
            //    Thread.Sleep(50);
            //Console.CursorTop = Math.Max(Console.CursorTop, 6 + Bot.Nodes.Count + 1);
            //if (Console.CursorTop >= 30)
            //    Console.CursorTop = 19;
            //Console.ForegroundColor = error ? ConsoleColor.Red : ConsoleColor.Gray;
            //Console.WriteLine(s);
            //try
            //{
            //    using (var sw = new StreamWriter(Path.Combine(Bot.RootDirectory, "..\\Logs\\ControlLog.log"), true))
            //    {
            //        sw.WriteLine($"{DateTime.UtcNow} - {s}");
            //    }
            //}
            //catch
            //{
            //    // ignored
            //}
        }

        private static void MessageMonitor()
        {
            while (Running)
            {
                try
                {
                    var newMessages = Bot.MessagesProcessed - _previousMessages;
                    _previousMessages = Bot.MessagesProcessed;
                    MessagesProcessed.Insert(0, newMessages);
                    if (MessagesProcessed.Count > 10)
                        MessagesProcessed.RemoveAt(10);
                    MessagePxPerSecond = (float)MessagesProcessed.Average() / 10;

                    newMessages = (Bot.MessagesSent + NodeMessagesSent) - _previousMessagesTx;
                    _previousMessagesTx = (Bot.MessagesSent + NodeMessagesSent);
                    MessagesSent.Insert(0, newMessages);
                    if (MessagesSent.Count > 10)
                        MessagesSent.RemoveAt(10);
                    MessageTxPerSecond = (float)MessagesSent.Average() / 10;
                }
                catch
                {
                    // ignored
                }

                Thread.Sleep(1000);
            }
        }

        //private static void CpuMonitor()
        //{
        //    while (Running)
        //    {
        //        try
        //        {
        //            CpuTimes.Insert(0, CpuCounter.NextValue());
        //            if (CpuTimes.Count > 10)
        //                CpuTimes.RemoveAt(10);
        //            AvgCpuTime = CpuTimes.Average();

        //        }
        //        catch
        //        {
        //            // ignored
        //        }

        //        Thread.Sleep(1000);
        //    }
        //}

        internal static string GetFullInfo()
        {
            var nodes = Bot.Nodes.OrderBy(x => x.Version).ToList();
            NodeMessagesSent = nodes.Sum(x => x.MessagesSent);
            var currentPlayers = nodes.Sum(x => x.CurrentPlayers);
            var currentGames = nodes.Sum(x => x.CurrentGames);
            //var NumThreads = Process.GetCurrentProcess().Threads.Count;
            var uptime = DateTime.UtcNow - Bot.StartTime;
            var messagesRx = Bot.MessagesProcessed;
            var commandsRx = Bot.CommandsReceived;
            var messagesTx = nodes.Sum(x => x.MessagesSent) + Bot.MessagesSent;

            if (currentGames > MaxGames)
            {
                MaxGames = currentGames;
                MaxTime = DateTime.UtcNow;
            }
            //Threads: {NumThreads}\t
            var msg = 
                $"`Uptime   : {uptime}`\n" +
                $"`Nodes    : {nodes.Count}`\n" +
                $"`Players  : {currentPlayers}`\n" +
                $"`Games    : {currentGames}`\n" +
                $"`Msgs Rx  : {messagesRx}`\n" +
                $"`Cmds Rx  : {commandsRx}`\n" +
                $"`Msgs Tx  : {messagesTx}`\n" +
                $"`MPS (IN) : {MessagePxPerSecond}`\n" +
                $"`MPS (OUT): {MessageTxPerSecond}`\n" +
                $"`Max Games: {MaxGames} at {MaxTime.ToString("T")}`\n\n";


            try
            {
                msg = nodes.Aggregate(msg,
                    (current, n) =>
                        current +
                        $"`{(n.ShuttingDown ? "X " : "  ")}{n.ClientId}`\n`  {n.Version}` - *Games: {n.Games.Count}*\n");
            }
            catch
            {
                // ignored
            }

            return msg;
        }

        private static void NodeMonitor()
        {
            //wait a bit to allow nodes to register
            Thread.Sleep(2000);
#if !DEBUG
            new Task(Updater.MonitorUpdates).Start();
#endif
            while (Running)
            {
                try
                {
                    var Nodes = Bot.Nodes.OrderBy(x => x.Version).ToList();
                    NodeMessagesSent = Nodes.Sum(x => x.MessagesSent);
                    var CurrentPlayers = Nodes.Sum(x => x.CurrentPlayers);
                    var CurrentGames = Nodes.Sum(x => x.CurrentGames);
                    var TotalPlayers = Nodes.Sum(x => x.TotalPlayers);
                    var TotalGames = Nodes.Sum(x => x.TotalGames);
                    //var NumThreads = Process.GetCurrentProcess().Threads.Count;
                    var Uptime = DateTime.UtcNow - Bot.StartTime;
                    var MessagesRx = Bot.MessagesReceived;
                    var CommandsRx = Bot.MessagesProcessed;
                    var MessagesTx = Nodes.Sum(x => x.MessagesSent) + Bot.MessagesSent;

                    if (CurrentGames > MaxGames)
                    {
                        MaxGames = CurrentGames;
                        MaxTime = DateTime.UtcNow;
                    }
                    //Threads: {NumThreads}\t
                    var msg =
                        $"Connected Nodes: {Nodes.Count}  \n" +
                        $"Current Players: {CurrentPlayers}  \tCurrent Games: {CurrentGames}  \n" +
                        //$"Total Players: {TotalPlayers}  \tTotal Games: {TotalGames}  \n" +
                        $"Uptime: {Uptime}\n" +
                        $"Messages Rx: {MessagesRx}\tCommands Rx: {CommandsRx}\tMessages Tx: {MessagesTx}\n" +
                        $"Messages Per Second (IN): {MessagePxPerSecond}\tMessage Per Second (OUT): {MessageTxPerSecond}\t\n" +
                        $"Max Games: {MaxGames} at {MaxTime.ToString("T")}\t\n\n";


                    try
                    {
                        msg = Nodes.Aggregate(msg,
                            (current, n) =>
                                current +
                                $"{(n.ShuttingDown ? "X " : "  ")}{n.ClientId} - {n.Version} - Games: {n.Games.Count}\t\n");
                    }
                    catch
                    {
                        // ignored
                    }
                    msg += new string(' ', Console.WindowWidth);
                    msg += Environment.NewLine + new string(' ', Console.WindowWidth);

                    //for (var i = 0; i < 12; i++)
                    //    msg += new string(' ', Console.WindowWidth);

                    //we don't need this anymore, but keeping code just in case
                    //try
                    //{
                    //    var top = UpdateHandler.UserMessages.OrderByDescending(x => x.Value.Messages.Count()).Take(10);
                    //    msg += "\nSPAM DETECTION\n" +
                    //           top.Aggregate("", (a, b) => a + b.Key + ":\t" + b.Value.Messages.Count() + "\t\n");
                    //    msg += new string(' ', Console.WindowWidth);
                    //}
                    //catch
                    //{
                    //    // ignored
                    //}


                    //now dump all this to the console
                    //first get our current caret position
                    _writingInfo = true;
                    //var ypos = Math.Max(Console.CursorTop, 30);
                    //if (ypos >= 60)
                    //    ypos = 30;
                    Console.CursorTop = 0;
                    //var xpos = Console.CursorLeft;
                    Console.CursorLeft = 0;
                    //Console.Clear();
                    //write the info
                    Console.WriteLine(msg);
                    //put the cursor back;
                    //Console.CursorTop = ypos;
                    //Console.CursorLeft = xpos;
                    _writingInfo = false;


#if !DEBUG
                    //now, let's manage our nodes.
                    if (Nodes.All(x => x.Games.Count <= Settings.ShutDownNodesAt & !x.ShuttingDown) && Nodes.Count > 1)
                    {
                        //we have too many nodes running, kill one.
                        Nodes.First().ShutDown();
                    }

                    if (!MaintMode)
                    {
                        if (Nodes.Where(x => !x.ShuttingDown).All(x => x.Games.Count >= Settings.NewNodeThreshhold))
                        {
                            NewNode();
                            Thread.Sleep(5000); //give the node time to register
                        }

                        if (Nodes.All(x => x.ShuttingDown)) //replace nodes
                        {
                            NewNode();
                            Thread.Sleep(5000); //give the node time to register
                        }
                    }
#endif
                }
                finally
                {
                    _writingInfo = false;
                }
                Thread.Sleep(1000);
            }
        }

        internal static void NewNode()
        {
            //all nodes have quite a few games, let's spin up another
            //this is a bit more tricky, we need to figure out which node folder has the latest version...
            var baseDirectory = Path.Combine(Bot.RootDirectory, ".."); //go up one directory
            var currentChoice = new NodeChoice();
            foreach (var dir in Directory.GetDirectories(baseDirectory, "*Node*"))
            {
                //get the node exe in this directory
                var file = Directory.GetFiles(dir, "Werewolf Node.exe").First();
                Version fvi = Version.Parse(FileVersionInfo.GetVersionInfo(file).FileVersion);
                if (fvi > currentChoice.Version)
                {
                    currentChoice.Path = file;
                    currentChoice.Version = fvi;
                }
            }

            //now we have the most recent version, launch one
            Process.Start(currentChoice.Path);
        }
    }

    class NodeChoice
    {
        public string Path { get; set; }
        public Version Version { get; set; } = Version.Parse("0.0.0.0");
    }
    
}
