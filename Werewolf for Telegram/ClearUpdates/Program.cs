using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ClearUpdates
{
    class Program
    {
        static int total = 0;
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
            var Api = new TelegramBotClient(TelegramAPIKey);
            Api.OnUpdate += Api_OnUpdate;
            Api.StartReceiving();
            while (true)
            {
                Console.WriteLine(total);
                CheckMessages();
                Thread.Sleep(1000);
            }
        }

        private static void Api_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            total++;

            if ((e.Update.Message?.Text ?? "").StartsWith("/"))
                mQueue.Enqueue(e.Update.Message);
        }

        private static Dictionary<int, List<Message>> Commands = new Dictionary<int, List<Message>>();
        private static Queue<Message> mQueue = new Queue<Message>();


        private static void CheckMessages()
        {
            while (mQueue.Any())
            {
                var m = mQueue.Dequeue();

                if (Commands.ContainsKey(m.From.Id))
                    Commands[m.From.Id].Add(m);
                else
                    Commands.Add(m.From.Id, new List<Message> { m });

            }

            var top = Commands.Where(x => x.Value.Count > 20).OrderByDescending(x => x.Value.Count).Select(x => x.Value).ToList();
            foreach (var t in top)
            {
                var user = t[0].From;
                var startTime = t[0].Date;
                var endTime = t[t.Count - 1].Date;
                var ticks = (endTime - startTime).Ticks;
                ticks /= t.Count;
                var avg = new TimeSpan(ticks);
                Log($"User @{user.Username} ({user.Id}): {t.Count} - Average time between commands: {avg}");
            }
        }

        private static void Log(string s)
        {
            using (var sw = new StreamWriter("log.log", true))
            {
                sw.WriteLine(s);
            }

            Console.WriteLine(s);
        }
    }
}
