using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using Database;

namespace Werewolf_Website.Helpers
{
    public static class StatusMonitor
    {
        private static string _bot1, _bot2, _betaBot;
        public static void Start()
        {
            
                while (true)
                {
                    try
                    {
                        using (var db = new WWContext())
                        {
                            var status = db.BotStatus.ToList();
                            _bot1 = status.First(x => x.BotName == "Bot 1").BotStatus;
                            _bot2 = status.First(x => x.BotName == "Bot 2").BotStatus;
                            _betaBot = status.First(x => x.BotName == "Beta Bot").BotStatus;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    Thread.Sleep(500);
                
            }
        }

        public static List<string> GetStatus => new List<string> {_bot1, _bot2, _betaBot};
    }
}