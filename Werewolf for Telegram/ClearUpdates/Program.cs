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
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using Database;

namespace ClearUpdates
{
    class Program
    {
        static int total = 0;
        static TelegramBotClient WWAPI;
        static TelegramBotClient Api;
        internal static int[] Devs = new[] { 129046388, 133748469, 125311351 };
        static long DevGroup = -1001076212715;
        static void Main(string[] args)
        {

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
            if (!Devs.Contains(e.CallbackQuery.From.Id))
                return;
            var q = e.CallbackQuery;
            if (q.Data == "close")
            {
                Api.DeleteMessageAsync(DevGroup, q.Message.MessageId);
                return;
            }
            var id = int.Parse(q.Data);
            var t = Commands[id];
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

        private static void Api_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var m = e.Message;
            
            if (Devs.Contains(m.From.Id))
            {
                Console.WriteLine($"{m.MessageId} - {m.From.FirstName}: {m.Text}");
                switch (m.Text.Replace("@wwcleanbot",""))
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
                    menu.Buttons.Add(new InlineKeyboardCallbackButton($"{user.Id}: {t.Count}", user.Id.ToString()));
                }
            }
            if (menu.Buttons.Count > 0)
            {
                menu.Buttons.Add(new InlineKeyboardCallbackButton("Close", "close"));
                Api.SendTextMessageAsync(DevGroup, "Here is the report:", replyMarkup: menu.CreateMarkupFromMenu());
            }
            using (var fs = new FileStream("log.log", FileMode.Open))
            {
                Api.SendDocumentAsync(DevGroup, new FileToSend("Spam Log.txt", fs));
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
