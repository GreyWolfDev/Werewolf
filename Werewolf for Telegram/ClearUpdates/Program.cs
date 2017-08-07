using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

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
                Thread.Sleep(1000);
            }
        }

        private static void Api_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            total++;
            //do nothing at all.  We are simply clearing the update queue so the bot can catch up

        }
    }
}
