using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Werewolf_Website.Models;
// ReSharper disable FunctionNeverReturns

namespace Werewolf_Website.Helpers
{
    public static class StatusMonitor
    {
        private static StatusResponseInfo _bot1Stat, _bot2Stat, _betaBotStat, _debugBotStat;
        private static List<NodeResponseInfo> _bot1Nodes, _bot2Nodes, _betaBotNodes, _debugBotNodes;
        public static void Start()
        {
            new Thread(MonitorBot1Status).Start();
            //new Thread(MonitorBot2Status).Start();
            new Thread(MonitorBetaBotStatus).Start();
#if DEBUG
            //new Thread(MonitorDebugBotStatus).Start();
#endif

        }

        public static void MonitorBot1Status()
        {
            var conn = new TcpAdminConnection(BotConnectionInfo.Bot1IP, BotConnectionInfo.Bot1Port);
            while (true)
            {
                try
                {
                    _bot1Stat = conn.GetStatus();
                    
                }
                catch (Exception e)
                {
                    _bot1Stat = new StatusResponseInfo { BotName = e.Message };
                }
                finally
                {
                    Thread.Sleep(800);
                }
            }
        }

        public static void MonitorBot2Status()
        {
            var conn = new TcpAdminConnection(BotConnectionInfo.Bot2IP, BotConnectionInfo.Bot2Port);
            while (true)
            {
                try
                {
                    _bot2Stat = conn.GetStatus();
                    
                }
                catch (Exception e)
                {
                    _bot2Stat = new StatusResponseInfo { BotName = e.Message };
                }
                finally
                {
                    Thread.Sleep(800);
                }
            }
        }

        public static void MonitorBetaBotStatus()
        {
            var conn = new TcpAdminConnection(BotConnectionInfo.BetaIP, BotConnectionInfo.BetaPort);
            while (true)
            {
                try
                {
                    _betaBotStat = conn.GetStatus();
                    
                }
                catch(Exception e)
                {
                    _betaBotStat = new StatusResponseInfo {BotName = e.Message};
                }
                finally
                {
                    Thread.Sleep(800);
                }
                
            }
        }

        public static void MonitorDebugBotStatus()
        {
            var conn = new TcpAdminConnection(BotConnectionInfo.DebugIP, BotConnectionInfo.DebugPort);
            while (true)
            {
                try
                {
                    _debugBotStat = conn.GetStatus();
                    
                }
                catch (Exception e)
                {
                    _debugBotStat = new StatusResponseInfo { BotName = e.Message };
                }
                finally
                {
                    Thread.Sleep(800);
                }
            }
        }

        public static List<string> GetStatus => new List<string> {_bot1Stat?.Status, _betaBotStat?.Status};

        public static List<StatusResponseInfo> GetStatusResponses => new List<StatusResponseInfo>
        {
            _betaBotStat,
            _bot1Stat,
            //_bot2Stat,
#if DEBUG
            //_debugBotStat
#endif
        };
    }
}