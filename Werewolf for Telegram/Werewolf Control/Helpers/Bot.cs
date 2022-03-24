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
using Telegram.Bot.Extensions.Polling;
using Werewolf_Control.Handler;
using Werewolf_Control.Models;
using Telegram.Bot.Exceptions;
using Newtonsoft.Json;

namespace Werewolf_Control.Helpers
{
    internal static class Bot
    {
        internal static string TelegramAPIKey;
        public static HashSet<Node> Nodes = new HashSet<Node>();
        public static TelegramBotClient Api;

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
            Api = new TelegramBotClient(TelegramAPIKey);
            //#if !BETA
            //            Api.Timeout = TimeSpan.FromSeconds(1.5);
            //#else
            //            Api.Timeout = TimeSpan.FromSeconds(20);
            //#endif
            English = XDocument.Load(Path.Combine(LanguageDirectory, Program.MasterLanguage));

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

            ReceiverOptions receiverOptions = new ReceiverOptions() { AllowedUpdates = new[] { UpdateType.Message, UpdateType.MyChatMember, UpdateType.InlineQuery, UpdateType.ChosenInlineResult, UpdateType.CallbackQuery }, Limit = 100, ThrowPendingUpdates = true };
            var cts = new CancellationTokenSource();



            Me = Api.GetMeAsync().Result;
            //Api.OnMessage += ApiOnOnMessage;
            Console.Title += " " + Me.Username;
            if (!String.IsNullOrEmpty(updateid))
                Api.SendTextMessageAsync(updateid, "Control updated\n" + Program.GetVersion());
            StartTime = DateTime.UtcNow;

            //now we can start receiving
            //Api.StartReceiving(ApiOnOnMessage, ApiOnReceiveError, receiverOptions, cts.Token);
            //Api.OnInlineQuery += UpdateHandler.InlineQueryReceived;
            //Api.OnUpdate += UpdateHandler.UpdateReceived;
            //Api.OnCallbackQuery += UpdateHandler.CallbackReceived;
            //Api.OnReceiveError += ApiOnReceiveError;
            //Api.OnReceiveGeneralError += ApiOnOnReceiveGeneralError;
            //Api.OnStatusChanged += ApiOnStatusChanged;
            //Api.UpdatesReceived += ApiOnUpdatesReceived;
            Api.ReceiveAsync(receiverOptions, cts.Token);
            Api.OnMakingApiRequest += Api_OnMakingApiRequest;
        }

        private static ValueTask Api_OnMakingApiRequest(ITelegramBotClient botClient, ApiRequestEventArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            var method = args.MethodName.ToLower();
            if (method.StartsWith("getUpdate"))
            {
                Program.Log("Getting updates");
            }
            if (method.StartsWith("send") || method.StartsWith("edit"))
            {
                MessagesSent++;
            }
            return new ValueTask();
        }

        private static readonly Update[] EmptyUpdates = { };
        public static int MessageOffset { get; set; }
        public static bool IsReceiving { get; set; }

        private static CancellationTokenSource _receivingCancellationTokenSource;
#pragma warning disable AsyncFixer03 // Avoid fire & forget async void methods
        private static async void ReceiveAsync(this ITelegramBotClient client,
            ReceiverOptions options,
            CancellationToken cancellationToken)
        {
            var sw = new Stopwatch();
            IsReceiving = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                var timeout = 30;
                var updates = EmptyUpdates;
                sw.Reset();
                try
                {
                    //let's see if Telegram is responding slowly....
                    Program.log.Info("Starting a getUpdates request");
                    sw.Start();
                    updates = await client.GetUpdatesAsync(
                        MessageOffset,
                        timeout: timeout,
                        limit: options.Limit,
                        allowedUpdates: options.AllowedUpdates,
                        cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
                    sw.Stop();
                    Program.log.Info($"Time to receive updates: {sw.ElapsedMilliseconds}ms");
                }
                catch (OperationCanceledException opException)
                {
                    Program.log.Error("Error getting updates", opException);
                }
                catch (ApiRequestException apiException)
                {
                    Program.log.Error("Error getting updates", apiException);
                    OnReceiveError?.Invoke("receiver", apiException);
                }
                catch (Exception generalException)
                {
                    Program.log.Error("Error getting updates", generalException);
                    OnReceiveGeneralError?.Invoke("receiver", generalException);
                }

                try
                {
                    Program.log.Info($"Received {updates.Length} updates, processing");
                    MessagesReceived += updates.Length;
                    new Task(() =>
                    {
                        foreach (var update in updates)
                        {
                            OnUpdateReceived(new UpdateEventArgs(update));
                        }
                    }).Start();
                    if (updates.Length > 0)
                        MessageOffset = updates[updates.Length - 1].Id + 1;
                }
                catch (Exception e)
                {
                    Program.log.Error("Error getting updates", e);
                    IsReceiving = false;
                    throw;
                }
            }

            IsReceiving = false;
        }

        #region Events

        /// <summary>
        /// Occurs before sending a request to API
        /// </summary>
        public static event EventHandler<ApiRequestEventArgs> MakingApiRequest;

        /// <summary>
        /// Occurs after receiving the response to an API request
        /// </summary>
        public static event EventHandler<ApiResponseEventArgs> ApiResponseReceived;

        /// <summary>
        /// Raises the <see cref="OnUpdate" />, <see cref="OnMessage"/>, <see cref="OnInlineQuery"/>, <see cref="OnInlineResultChosen"/> and <see cref="OnCallbackQuery"/> events.
        /// </summary>
        /// <param name="e">The <see cref="UpdateEventArgs"/> instance containing the event data.</param>
        static void OnUpdateReceived(UpdateEventArgs e)
        {
            if (e.Update.EditedMessage != null) return;
            if (e.Update.Message?.ViaBot != null && e.Update.Message.Chat.Type != ChatType.Private) return;
            if (e.Update.Message?.Document != null && e.Update.Message.Chat.Type != ChatType.Private) return;
            if (e.Update.Message?.Audio != null) return;
            if (e.Update.Message?.Caption != null && e.Update.Message.Chat.Type != ChatType.Private) return;
            if (e.Update.Message?.Location != null) return;
            if (e.Update.Message?.Game != null) return;
            if (e.Update.Message?.Photo != null) return;
            if (e.Update.Message?.Sticker != null) return;
            if (e.Update.Message?.Video != null) return;
            if (e.Update.Message?.Voice != null) return;
            //Program.log.Info(JsonConvert.SerializeObject(e.Update));
            //OnUpdate?.Invoke("receiver", e);

            switch (e.Update.Type)
            {
                case UpdateType.Message:

                    //OnMessage?.Invoke("receiver", e);
                    UpdateHandler.UpdateReceived(Api, e.Update);
                    break;

                case UpdateType.InlineQuery:
                    //OnInlineQuery?.Invoke("receiver", e);
                    UpdateHandler.InlineQueryReceived(Api, e.Update.InlineQuery);
                    break;

                case UpdateType.ChosenInlineResult:
                    //OnInlineResultChosen?.Invoke("receiver", e);
                    break;

                case UpdateType.CallbackQuery:
                    //OnCallbackQuery?.Invoke("receiver", e);
                    UpdateHandler.CallbackReceived(Api, e.Update.CallbackQuery);
                    break;

                case UpdateType.EditedMessage:
                    //OnMessageEdited?.Invoke("receiver", e);
                    break;
            }
        }

        /// <summary>
        /// Occurs when an <see cref="Update"/> is received.
        /// </summary>
        public static event EventHandler<UpdateEventArgs> OnUpdate;

        /// <summary>
        /// Occurs when a <see cref="Message"/> is received.
        /// </summary>
        public static event EventHandler<MessageEventArgs> OnMessage;

        /// <summary>
        /// Occurs when <see cref="Message"/> was edited.
        /// </summary>
        public static event EventHandler<MessageEventArgs> OnMessageEdited;

        /// <summary>
        /// Occurs when an <see cref="InlineQuery"/> is received.
        /// </summary>
        public static event EventHandler<InlineQueryEventArgs> OnInlineQuery;

        /// <summary>
        /// Occurs when a <see cref="ChosenInlineResult"/> is received.
        /// </summary>
        public static event EventHandler<ChosenInlineResultEventArgs> OnInlineResultChosen;

        /// <summary>
        /// Occurs when an <see cref="CallbackQuery"/> is received
        /// </summary>
        public static event EventHandler<CallbackQueryEventArgs> OnCallbackQuery;

        /// <summary>
        /// Occurs when an error occurs during the background update pooling.
        /// </summary>
        public static event EventHandler<ReceiveErrorEventArgs> OnReceiveError;

        /// <summary>
        /// Occurs when an error occurs during the background update pooling.
        /// </summary>
        public static event EventHandler<ReceiveGeneralErrorEventArgs> OnReceiveGeneralError;

        #endregion

        private static async Task Receive()
        {
            while (true)
            {

            }
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

        //private static async Task ApiOnOnMessage(ITelegramBotClient bot, Update update, CancellationToken token)
        //{
        //    new Task(() =>
        //    {
        //        switch (update.Type)
        //        {
        //            // UpdateType.Unknown:
        //            // UpdateType.ChannelPost:
        //            // UpdateType.EditedChannelPost:
        //            // UpdateType.ShippingQuery:
        //            // UpdateType.PreCheckoutQuery:
        //            // UpdateType.Poll:
        //            case UpdateType.InlineQuery:
        //                UpdateHandler.InlineQueryReceived(bot, update.InlineQuery);
        //                break;
        //            case UpdateType.CallbackQuery:
        //                UpdateHandler.CallbackReceived(bot, update.CallbackQuery);
        //                break;
        //            default:
        //                UpdateHandler.UpdateReceived(bot, update);
        //                break;
        //                //Api.OnInlineQuery += UpdateHandler.InlineQueryReceived;
        //                //Api.OnUpdate += UpdateHandler.UpdateReceived;
        //                //Api.OnCallbackQuery += UpdateHandler.CallbackReceived;
        //                //Api.OnReceiveError += ApiOnReceiveError;
        //                ////Api.OnReceiveGeneralError += ApiOnOnReceiveGeneralError;
        //                ////Api.OnStatusChanged += ApiOnStatusChanged;
        //                ////Api.UpdatesReceived += ApiOnUpdatesReceived;
        //                //UpdateType.Message            => BotOnMessageReceived(botClient, update.Message!),
        //                //UpdateType.EditedMessage      => BotOnMessageReceived(botClient, update.EditedMessage!),
        //                //UpdateType.CallbackQuery      => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
        //                //UpdateType.InlineQuery        => BotOnInlineQueryReceived(botClient, update.InlineQuery!),
        //                //UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(botClient, update.ChosenInlineResult!),
        //                //_                             => UnknownUpdateHandlerAsync(botClient, update)
        //        }
        //    }).Start();
        //    //try
        //    //{
        //    //    await handler;
        //    //}
        //    //catch (Exception exception)
        //    //{
        //    //    await HandleErrorAsync(botClient, exception, cancellationToken);
        //    //}
        //}

        //private static void ApiOnUpdatesReceived(object sender, UpdateEventArgs updateEventArgs)
        //{
        //    //MessagesReceived += updateEventArgs.UpdateCount;
        //}

        internal static void ReplyToCallback(CallbackQuery query, string text = null, bool edit = true, bool showAlert = false, InlineKeyboardMarkup replyMarkup = null, ParseMode parsemode = ParseMode.Html, bool disableWebPagePreview = false)
        {
            //first answer the callback
            Bot.Api.AnswerCallbackQueryAsync(query.Id, edit ? null : text, showAlert);
            //edit the original message
            if (edit)
                Edit(query, text, replyMarkup, parsemode, disableWebPagePreview);
        }

        internal static Task<Message> Edit(CallbackQuery query, string text, InlineKeyboardMarkup replyMarkup = null, ParseMode parsemode = ParseMode.Html, bool disableWebPagePreview = false)
        {
            return Edit(query.Message.Chat.Id, query.Message.MessageId, text, replyMarkup, parsemode, disableWebPagePreview);
        }

        internal static Task<Message> Edit(long id, int msgId, string text, InlineKeyboardMarkup replyMarkup = null, ParseMode parsemode = ParseMode.Html, bool disableWebPagePreview = false)
        {
            return Bot.Api.EditMessageTextAsync(id, msgId, text, parsemode, null, disableWebPagePreview, replyMarkup);
        }

        //        private static void ApiOnStatusChanged(object sender, StatusChangeEventArgs statusChangeEventArgs)
        //        {
        //            try
        //            {
        //                using (var db = new WWContext())
        //                {
        //                    var id =
        //#if RELEASE
        //                        1;
        //#elif RELEASE2
        //                    2;
        //#elif BETA
        //                    3;
        //#else
        //                    4;
        //#endif
        //                    if (id == 4) return;
        //                    var b = db.BotStatus.Find(id);
        //                    b.BotStatus = statusChangeEventArgs.Status.ToString();
        //                    CurrentStatus = b.BotStatus;
        //                    db.SaveChanges();

        //                }
        //            }
        //            finally
        //            {

        //            }

        //        }


        private static Task ApiOnReceiveError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            //if (!Api.IsReceiving)
            //{
            //    Api.StartReceiving();
            //}
            var e = exception;
            using (var sw = new StreamWriter(Path.Combine(RootDirectory, "..\\Logs\\apireceiveerror.log"), true))
            {
                sw.WriteLine($"{DateTime.UtcNow} - {e.Message}\n{e.Source}");
            }

            return Task.CompletedTask;

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
            //MessagesSent++;
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
