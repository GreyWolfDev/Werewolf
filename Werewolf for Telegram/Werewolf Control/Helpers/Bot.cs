using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Microsoft.Win32;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Handler;
using Werewolf_Control.Models;

namespace Werewolf_Control.Helpers
{
    internal static class Bot
    {
        internal static string TelegramAPIKey;
        public static HashSet<Node> Nodes = new HashSet<Node>();
        public static Client Api;

        public static User Me;
        public static DateTime StartTime = DateTime.UtcNow;
        public static bool Running = true;
        public static long CommandsReceived = 0;
        public static long MessagesProcessed = 0;
        public static long MessagesReceived = 0;
        public static long TotalPlayers = 0;
        public static long TotalGames = 0;
        public static Random R = new Random();
        public static XDocument English;
        public static int MessagesSent = 0;
        public static string CurrentStatus = "";
        internal static string RootDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        internal static string LogDirectory = Path.Combine(RootDirectory, "..\\Logs\\");
        internal delegate void ChatCommandMethod(Update u, string[] args);
        internal static List<Command> Commands = new List<Command>();
#if DEBUG
        internal static string LanguageDirectory => Path.GetFullPath(Path.Combine(RootDirectory, @"..\..\..\Languages"));
#else
        internal static string LanguageDirectory => Path.GetFullPath(Path.Combine(RootDirectory, @"..\..\Languages"));
#endif
        internal static string TempLanguageDirectory => Path.GetFullPath(Path.Combine(RootDirectory, @"..\..\TempLanguageFiles"));
        public static void Initialize(string updateid = null)
        {

            //get api token from registry
            var key =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey("SOFTWARE\\Werewolf");
#if DEBUG
            TelegramAPIKey = key.GetValue("DebugAPI").ToString();
#elif RELEASE
            TelegramAPIKey = key.GetValue("ProductionAPI").ToString();
#elif RELEASE2
            TelegramAPIKey = key.GetValue("ProductionAPI2").ToString();
#elif BETA
            TelegramAPIKey = key.GetValue("BetaAPI").ToString();
#endif
            Api = new Client(TelegramAPIKey, LogDirectory);
//#if !BETA
//            Api.Timeout = TimeSpan.FromSeconds(1.5);
//#else
//            Api.Timeout = TimeSpan.FromSeconds(20);
//#endif
            English = XDocument.Load(Path.Combine(LanguageDirectory, "English.xml"));

            //load the commands list
            foreach (var m in typeof(Commands).GetMethods())
            {
                var c = new Command();
                foreach (var a in m.GetCustomAttributes(true))
                {
                    if (a is Attributes.Command)
                    {
                        var ca = a as Attributes.Command;
                        c.Blockable = ca.Blockable;
                        c.DevOnly = ca.DevOnly;
                        c.GlobalAdminOnly = ca.GlobalAdminOnly;
                        c.GroupAdminOnly = ca.GroupAdminOnly;
                        c.Trigger = ca.Trigger;
                        c.Method = (ChatCommandMethod)Delegate.CreateDelegate(typeof(ChatCommandMethod), m);
                        c.InGroupOnly = ca.InGroupOnly;
                        c.LangAdminOnly = ca.LangAdminOnly;
                        Commands.Add(c);
                    }
                }
            }

            Api.InlineQueryReceived += UpdateHandler.InlineQueryReceived;
            Api.UpdateReceived += UpdateHandler.UpdateReceived;
            Api.CallbackQueryReceived += UpdateHandler.CallbackReceived;
            Api.ReceiveError += ApiOnReceiveError;
            //Api.OnReceiveGeneralError += ApiOnOnReceiveGeneralError;
            Api.StatusChanged += ApiOnStatusChanged;
            //Api.UpdatesReceived += ApiOnUpdatesReceived;
            Me = Api.GetMeAsync().Result;
            //Api.OnMessage += ApiOnOnMessage;
            Console.Title += " " + Me.Username;
            if (!String.IsNullOrEmpty(updateid))
                Api.SendTextMessageAsync(updateid, "Control updated\n" + Program.GetVersion());
            StartTime = DateTime.UtcNow;
            
            //now we can start receiving
            Api.StartReceiving();
        }

        //private static void ApiOnOnReceiveGeneralError(object sender, ReceiveGeneralErrorEventArgs receiveGeneralErrorEventArgs)
        //{
        //    if (!Api.IsReceiving)
        //    {
        //        Api.StartReceiving();// cancellationToken: new CancellationTokenSource(1000).Token);
        //    }
        //    var e = receiveGeneralErrorEventArgs.Exception;
        //    using (var sw = new StreamWriter(Path.Combine(RootDirectory, "..\\Logs\\apireceiveerror.log"), true))
        //    {
        //        sw.WriteLine($"{DateTime.UtcNow} {e.Message} - {e.StackTrace}\n{e.Source}");
        //    }
        //}

        private static void ApiOnOnMessage(object sender, MessageEventArgs messageEventArgs)
        {
            
        }

        private static void ApiOnUpdatesReceived(object sender, UpdateEventArgs updateEventArgs)
        {
            //MessagesReceived += updateEventArgs.UpdateCount;
        }

        internal static void ReplyToCallback(CallbackQuery query, string text = null, bool edit = true, bool showAlert = false, InlineKeyboardMarkup replyMarkup = null, ParseMode parsemode = ParseMode.Default)
        {
            //first answer the callback
            Bot.Api.AnswerCallbackQueryAsync(query.Id, edit ? null : text, showAlert);
            //edit the original message
            if (edit)
                Edit(query, text, replyMarkup, parsemode);
        }

        internal static Task<Message> Edit(CallbackQuery query, string text, InlineKeyboardMarkup replyMarkup = null, ParseMode parsemode = ParseMode.Default)
        {
            return Edit(query.Message.Chat.Id, query.Message.MessageId, text, replyMarkup, parsemode);
        }

        internal static Task<Message> Edit(long id, int msgId, string text, InlineKeyboardMarkup replyMarkup = null, ParseMode parsemode = ParseMode.Default)
        {
            Bot.MessagesSent++;
            return Bot.Api.EditMessageTextAsync(id, msgId, text, parsemode, replyMarkup: replyMarkup);
        }

        private static void ApiOnStatusChanged(object sender, StatusChangeEventArgs statusChangeEventArgs)
        {
            try
            {
                using (var db = new WWContext())
                {
                    var id =
#if RELEASE
                        1;
#elif RELEASE2
                    2;
#elif BETA
                    3;
#else
                    4;
#endif
                    if (id == 4) return;
                    var b = db.BotStatus.Find(id);
                    b.BotStatus = statusChangeEventArgs.Status.ToString();
                    CurrentStatus = b.BotStatus;
                    db.SaveChanges();

                }
            }
            finally
            {

            }

        }


        private static void ApiOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            if (!Api.IsReceiving)
            {
                Api.StartReceiving();
            }
            var e = receiveErrorEventArgs.ApiRequestException;
            using (var sw = new StreamWriter(Path.Combine(RootDirectory, "..\\Logs\\apireceiveerror.log"), true))
            {
                sw.WriteLine($"{DateTime.UtcNow} {e.ErrorCode} - {e.Message}\n{e.Source}");
            }
                
        }

        private static void Reboot()
        {
            Running = false;
            Program.Running = false;
            Process.Start(Assembly.GetExecutingAssembly().Location);
            Environment.Exit(4);

        }

        //TODO this needs to be an event
        public static void NodeConnected(Node n)
        {
#if DEBUG
            //Api.SendTextMessageAsync(Settings.MainChatId, $"Node connected with guid {n.ClientId}");
#endif
        }

        //TODO this needs to be an event as well
        public static void Disconnect(this Node n, bool notify = true)
        {
#if DEBUG
            //Api.SendTextMessageAsync(Settings.MainChatId, $"Node disconnected with guid {n.ClientId}");
#endif
            if (notify && n.Games.Count > 2)
                foreach (var g in n.Games)
                {
                    Send(UpdateHandler.GetLocaleString("NodeShutsDown", g.Language), g.GroupId);
                }
            Nodes.Remove(n);
            n = null;
        }

        /// <summary>
        /// Gets the node with the least number of current games
        /// </summary>
        /// <returns>Best node, or null if no nodes</returns>
        public static Node GetBestAvailableNode()
        {
            //make sure we remove bad nodes first
            foreach (var n in Nodes.Where(x => x.TcpClient.Connected == false).ToList())
                Nodes.Remove(n);
            return Nodes.Where(x => x.ShuttingDown == false && x.CurrentGames < Settings.MaxGamesPerNode).OrderBy(x => x.CurrentGames).FirstOrDefault(); //if this is null, there are no nodes
        }


        internal static Task<Message> Send(string message, long id, bool clearKeyboard = false, InlineKeyboardMarkup customMenu = null, ParseMode parseMode = ParseMode.Html)
        {
            MessagesSent++;
            //message = message.Replace("`",@"\`");
            if (clearKeyboard)
            {
                //var menu = new ReplyKeyboardRemove() { RemoveKeyboard = true };
                return Api.SendTextMessageAsync(id, message, replyMarkup: customMenu, disableWebPagePreview: true, parseMode: parseMode);
            }
            else if (customMenu != null)
            {
                return Api.SendTextMessageAsync(id, message, replyMarkup: customMenu, disableWebPagePreview: true, parseMode: parseMode);
            }
            else
            {
                return Api.SendTextMessageAsync(id, message, disableWebPagePreview: true, parseMode: parseMode);
            }

        }

        internal static GameInfo GetGroupNodeAndGame(long id)
        {
            var node = Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.GroupId == id))?.Games.FirstOrDefault(x => x.GroupId == id);
            if (node == null)
                node = Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.GroupId == id))?.Games.FirstOrDefault(x => x.GroupId == id);
            if (node == null)
                node = Nodes.ToList().FirstOrDefault(n => n.Games.Any(g => g.GroupId == id))?.Games.FirstOrDefault(x => x.GroupId == id);
            return node;
        }
    }
}
