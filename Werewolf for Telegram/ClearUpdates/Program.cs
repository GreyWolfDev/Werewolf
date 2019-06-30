using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Database;
using Telegram.Bot.Types.InputFiles;
using System.Data.SqlTypes;

namespace ClearUpdates
{
    class Program
    {
        static int total = 0;
        static TelegramBotClient WWAPI;
        static TelegramBotClient Api;
        internal static int[] Devs = new[] { 129046388, 133748469, 295152997, 106665913 };
        static long DevGroup = -1001077134233;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exc = (Exception)e.ExceptionObject;
                Console.WriteLine("==" + exc.Message + "==\n" + exc.StackTrace);
            };

            var key =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey("SOFTWARE\\Werewolf");
            string TelegramAPIKey;
#if DEBUG
            TelegramAPIKey = key.GetValue("DebugAPI").ToString();
#elif RELEASE
            TelegramAPIKey = key.GetValue("ProductionAPI").ToString();
#elif RELEASE2
            TelegramAPIKey = key.GetValue("ProductionAPI2").ToString();
#elif BETA
            TelegramAPIKey = key.GetValue("BetaAPI").ToString();
#endif
            WWAPI = new TelegramBotClient(TelegramAPIKey);
            WWAPI.OnUpdate += WWAPI_OnUpdate;
            var apikey = key.GetValue("QueueAPI").ToString();
            Api = new TelegramBotClient(apikey);
            Api.OnMessage += Api_OnMessage;
            Api.OnUpdate += ApiOnOnUpdate;
            Api.OnCallbackQuery += Api_OnCallbackQuery;
            Api.StartReceiving();
            new Task(() => MonitorStatus()).Start();
            Thread.Sleep(-1);
        }

        private static void ApiOnOnUpdate(object sender, UpdateEventArgs updateEventArgs)
        {
            Console.WriteLine(updateEventArgs.Update.Id);
            Api.MessageOffset = updateEventArgs.Update.Id + 1;
        }

        private static void Api_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            try
            {
                if (!Devs.Contains(e.CallbackQuery.From.Id))
                    return;
                var q = e.CallbackQuery;
                if (q.Data == "close")
                {
                    Api.DeleteMessageAsync(DevGroup, q.Message.MessageId);
                    return;
                }
                var id = int.Parse(q.Data);
                if (!Commands.TryGetValue(id, out var t))
                {
                    Api.AnswerCallbackQueryAsync(q.Id, "Sorry, this flood isn't in my memory anymore! :(", true).Wait();
                    return;
                }
                var user = t[0].From;
                var startTime = t[0].Date;
                var endTime = t[t.Count - 1].Date;
                var ticks = (endTime - startTime).Ticks;
                ticks /= t.Count;
                var avg = new TimeSpan(ticks);
                var msg = ($"User @{user.Username} ({user.Id}): {t.Count} - Average time between commands: {avg}\n");
                msg = t.Aggregate(msg, (a, b) => a + $"{b.Text}: {b.Date}\n");
                Api.SendTextMessageAsync(DevGroup, msg);
                Api.AnswerCallbackQueryAsync(q.Id);
            }
            catch (Exception exc)
            {
                var trace = exc.StackTrace;
                var error = "";
                do
                {
                    error += exc.Message + "\n\n";
                    exc = exc.InnerException;
                }
                while (exc != null);
                Api.SendTextMessageAsync(DevGroup, error + trace).Wait();
            }
        }

        private static void Api_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e == null || e.Message == null || string.IsNullOrEmpty(e.Message.Text)) return;

            var m = e.Message;
            if (Devs.Contains(m.From.Id))
            {
                Console.WriteLine($"{m.MessageId} - {m.From.FirstName}: {m.Text}");
                switch (m.Text.Replace("@wwcleanbot", ""))
                {
                    case "/clearqueue":
                        if (m.Date < DateTime.Now.AddSeconds(-1))
                            return;
                        ClearQueue();
                        break;
                    default:
                        break;
                }
            }
        }

        private static void MonitorStatus()
        {
            var dead = false;
            var lastChange = DateTime.Now;
            while (true)
            {
                using (var DB = new WWContext())
                {
                    var status = DB.BotStatus.First(x => x.Id == 1);
                    if (status.BotStatus != "Normal")
                    {
                        if (!dead)
                        {
                            dead = true;
                            lastChange = DateTime.Now;
                            Console.WriteLine(lastChange + " - Detected issue.");
                        }
                        if ((DateTime.Now - lastChange) > TimeSpan.FromSeconds(3))
                        {
                            Console.WriteLine(DateTime.Now + " - Issue persisted, clearing.");
                            ClearQueue();
                            dead = false;
                            lastChange = DateTime.Now;
                        }
                    }
                    else
                    {
                        dead = false;
                        lastChange = DateTime.Now;
                    }
                }
                Console.WriteLine($"Dead: {dead} - {lastChange}");
                Thread.Sleep(1000);
            }
        }

        private static void ClearQueue()
        {
            Console.WriteLine("Clearing Queue!");

            Commands.Clear();
            mQueue.Clear();
            total = 0;
            WWAPI.StartReceiving();
            var current = 0;
            while (true)
            {
                //foreach (var p in Process.GetProcessesByName("Werewolf Control"))
                //    p.Kill();
                if ((total - current) < 50 && total != 0)
                {
                    WWAPI.StopReceiving();
                    //Api.SendTextMessageAsync(DevGroup, $"Cleared {total} messages from queue. Inspecting for spammers.");
                    CheckMessages();
                    break;
                }
                current = total;
                Thread.Sleep(1000);
            }
            Thread.Sleep(1000);
        }

        private static void WWAPI_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            total++;
            if ((e.Update.Message?.Text ?? "").StartsWith("/"))
                mQueue.Enqueue(e.Update.Message);
        }

        private static Dictionary<int, List<Message>> Commands = new Dictionary<int, List<Message>>();
        private static Queue<Message> mQueue = new Queue<Message>();


        private static void CheckMessages()
        {
            for (int i = 0; i < Math.Min(100, mQueue.Count); i++)
            {
                var m = mQueue.Dequeue();

                if (Commands.ContainsKey(m.From.Id))
                    Commands[m.From.Id].Add(m);
                else
                    Commands.Add(m.From.Id, new List<Message> { m });

            }

            var top = Commands.Where(x => x.Value.Count > 15).OrderByDescending(x => x.Value.Count).Select(x => x.Value).ToList();
            var menu = new Menu(2);
            Dictionary<int, bool> ToBan = new Dictionary<int, bool>();
            using (var sw = new StreamWriter("log.log"))
            {
                foreach (var t in top)
                {
                    var user = t[0].From;
                    var startTime = t[0].Date;
                    var endTime = t[t.Count - 1].Date;
                    var ticks = (endTime - startTime).Ticks;
                    ticks /= t.Count;
                    var avg = new TimeSpan(ticks);
                    var msg = ($"User @{user.Username} ({user.Id}): {t.Count} - Average time between commands: {avg}\r\n");
                    msg = t.Aggregate(msg, (a, b) => a + $"{b.Text}: {b.Date}\r\n");
                    msg += "\r\n";
                    sw.WriteLine(msg);
                    Console.WriteLine(msg);
                    ToBan.Add(user.Id, t.Count >= 40); // if it's 40 or more messages, ban permanently immediately
                    menu.Buttons.Add(InlineKeyboardButton.WithCallbackData($"{user.Id}: {t.Count}", user.Id.ToString()));
                }
            }
            if (menu.Buttons.Count > 0)
            {
                using (var db = new WWContext())
                {
                    foreach (var spam in ToBan)
                    {
                        var id = spam.Key;
                        var permanent = spam.Value;
                        var player = db.Players.FirstOrDefault(x => x.TelegramId == id);

                        if (player != null)
                        {
                            if (player.TempBanCount == null) player.TempBanCount = 0;
                            permanent = ++player.TempBanCount > 3 || permanent;
                        }

                        var globalban = db.GlobalBans.FirstOrDefault(x => x.TelegramId == id);
                        if (globalban != null)
                        {
                            if (globalban.Expires > new DateTime(3000, 1, 1)) continue; // ignore if alr perm banned
                            db.GlobalBans.Remove(globalban); // else, remove the old ban so the new one counts.
                        }

                        //add the ban
                        var ban = new GlobalBan
                        {
                            Expires = permanent
                                ? (DateTime)SqlDateTime.MaxValue
                                : DateTime.UtcNow.AddDays(7),
                            Reason = $"{(spam.Value ? "INSANE" : "HEAVY")} Spam / Flood",
                            TelegramId = id,
                            BanDate = DateTime.UtcNow,
                            BannedBy = "AntiFlood System",
                            Name = player?.Name ?? "Unknown User"
                        };
                        db.GlobalBans.Add(ban);
                    }
                    db.SaveChanges();
                }

                menu.Buttons.Add(InlineKeyboardButton.WithCallbackData("Close", "close"));
                Api.SendTextMessageAsync(DevGroup, "Here is the report:", replyMarkup: menu.CreateMarkupFromMenu());

                using (var fs = new FileStream("log.log", FileMode.Open))
                {
                    try
                    {
                        Api.SendDocumentAsync(DevGroup, new InputOnlineFile(fs, "Spam Log.txt")).Wait();
                    }
                    catch (Exception exc)
                    {
                        var trace = exc.StackTrace;
                        var error = "";
                        do
                        {
                            error += exc.Message + "\n\n";
                            exc = exc.InnerException;
                        }
                        while (exc != null);
                        Api.SendTextMessageAsync(DevGroup, error + trace).Wait();
                    }
                }
            }
        }
    }

    public class Menu
    {
        /// <summary>
        /// The buttons you want in your menu
        /// </summary>
        public List<InlineKeyboardButton> Buttons { get; set; }
        /// <summary>
        /// How many columns.  Defaults to 1.
        /// </summary>
        public int Columns { get; set; }

        public Menu(int col = 1, List<InlineKeyboardButton> buttons = null)
        {
            Buttons = buttons ?? new List<InlineKeyboardButton>();
            Columns = Math.Max(col, 1);
        }

        public InlineKeyboardMarkup CreateMarkupFromMenu()
        {
            var col = Columns - 1;
            var final = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < Buttons.Count; i++)
            {
                var row = new List<InlineKeyboardButton>();
                do
                {
                    row.Add(Buttons[i]);
                    i++;
                    if (i == Buttons.Count) break;
                } while (i % (col + 1) != 0);
                i--;
                final.Add(row.ToArray());
                if (i == Buttons.Count) break;
            }
            return new InlineKeyboardMarkup(final.ToArray());
        }
    }
}
