using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TcpFramework;
using TcpFramework.Server;
using Werewolf_Control.Models;

namespace Werewolf_Control.Helpers
{
    internal static class TCP
    {
        public static SimpleTcpServer Server;
        public static SimpleTcpServer StatusServer;
        
        public static void Initialize()
        {
            Server = new SimpleTcpServer {AutoTrimStrings = false};
            Server.ClientConnected += ServerOnClientConnected;
            Server.ClientDisconnected += ServerOnClientDisconnected;
            Server.DataReceived += ServerOnDataReceived;
            Server.DelimiterDataReceived += ServerOnDelimiterDataReceived;
            new Thread(Ping).Start();

            //open up the status server 
            StatusServer = new SimpleTcpServer {AutoTrimStrings = false};
            StatusServer.ClientConnected += StatusServerOnClientConnected;
            StatusServer.ClientDisconnected += StatusServerOnClientDisconnected;
            StatusServer.DataReceived += StatusServerOnDataReceived;
            StatusServer.DelimiterDataReceived += StatusServerOnDelimiterDataReceived;
        }

        private static void StatusServerOnDelimiterDataReceived(object sender, Message message)
        {
            
        }

        private static void StatusServerOnDataReceived(object sender, Message message)
        {
            try
            {
                var messages = message.MessageString.Split('\u0013');
                foreach (var msg in messages)
                {
                    if (String.IsNullOrWhiteSpace(msg) || String.IsNullOrWhiteSpace(msg.Replace("\0", "")))
                        continue;
                    dynamic m = JsonConvert.DeserializeObject(msg);
                    string t = m.JType?.ToString();
                    var nodes = Bot.Nodes.ToList();
                    
                    if (t != null)
                    {
                        Console.WriteLine(t);
                        switch (t)
                        {
                            case "GetStatusInfo":
                                //web api is requesting current status
                                Console.WriteLine("Getting Status");
                                var status = new StatusResponseInfo
                                {
                                    BotName = Bot.Me.Username,
                                    MessagesProcPerSecond = Program.MessagesProcessed.FirstOrDefault(),
                                    MessagesPerSecondOut = Program.MessagesSent.FirstOrDefault(),
                                    MessagesPerSecondIn = Program.MessagesReceived.FirstOrDefault(),
                                    MaxGames = Program.MaxGames,
                                    MaxGamesTime = Program.MaxTime,
                                    Nodes = nodes.Select(n => new NodeResponseInfo
                                    {
                                        MessagesSent = n.MessagesSent,
                                        ClientId = n.ClientId,
                                        CurrentGames = n.CurrentGames,
                                        CurrentPlayers = n.CurrentPlayers,
                                        Games = n.Games.Select(x => new GameListInfo { GroupId = x.GroupId, GroupName = x.ChatGroup, NumPlayers = x.PlayerCount, PlayersAlive = x.Users.Count, State = x.State }).ToList(),
                                        ShuttingDown = n.ShuttingDown,
                                        Uptime = n.Uptime,
                                        Version = n.Version
                                    }).ToList(),
                                    NumGames = nodes.Sum(x => x.CurrentGames),
                                    NumPlayers = nodes.Sum(x => x.CurrentPlayers),
                                    Uptime = DateTime.UtcNow - Bot.StartTime,
                                    Status = Bot.CurrentStatus
                                };
                                message.Reply(JsonConvert.SerializeObject(status));
                                break;
                            case "GetNodeInfo":
                                var gni = JsonConvert.DeserializeObject<GetNodeInfo>(msg);
                                var node = nodes.FirstOrDefault(x => x.ClientId == gni.ClientId);
                                if (node == null)
                                {
                                    message.Reply("null");
                                    return;
                                }
                                var nodeInfo = new NodeResponseInfo
                                {
                                    MessagesSent = node.MessagesSent,
                                    ClientId = node.ClientId,
                                    CurrentGames = node.CurrentGames,
                                    CurrentPlayers = node.CurrentPlayers,
                                    Games = node.Games.Select(x => new GameListInfo { GroupId = x.GroupId, GroupName= x.ChatGroup, NumPlayers = x.PlayerCount, PlayersAlive = x.Users.Count, State = x.State}).ToList(),
                                    ShuttingDown = node.ShuttingDown,
                                    Uptime = node.Uptime,
                                    Version = node.Version
                                };
                                message.Reply(JsonConvert.SerializeObject(nodeInfo));
                                break;

                            case "GetGameInfo":
                                var ggi = JsonConvert.DeserializeObject<GetGameInfo>(msg);
                                //get the node

                                var gamenode = Bot.Nodes.FirstOrDefault(x => x.ClientId == ggi.ClientId);
                                var game = gamenode?.GetGameInfo(ggi);
                                
                                if (game == null)
                                {
                                    message.Reply("null");
                                    return;
                                }
                                var response = JsonConvert.SerializeObject(game);
                                using (var sw = new StreamWriter(Path.Combine(Bot.RootDirectory, "..\\tcpadmin.log"), true))
                                {
                                    sw.WriteLine("Control Replying to GetGameInfo with:\n" + response + "\n\n");
                                }

                                message.Reply(response);
                                break;
                            case "StopNodeRequest":
                                var snr = JsonConvert.DeserializeObject<StopNodeRequest>(msg);
                                Bot.Nodes.FirstOrDefault(x => x.ClientId == snr.ClientId)?.ShutDown();
                                break;
                            default:
                                message.Reply("null");
                                break;
                        }
                    }
                    
                }
            }
            catch(Exception e)
            {
                // ignored
                while (e.InnerException != null)
                    e = e.InnerException;
                Console.WriteLine(e.Message);
                using (var sw = new StreamWriter(Path.Combine(Bot.RootDirectory, "..\\Logs\\tcperror.log"), true))
                    sw.WriteLine(e.Message + "\n" + e.StackTrace + "\n");
            }
            finally
            {

            }
        }

        private static void StatusServerOnClientDisconnected(object sender, ConnectedClient connectedClient)
        {
            
        }

        private static void StatusServerOnClientConnected(object sender, ConnectedClient connectedClient)
        {
            
        }

        private static void Ping()
        {
            while (Bot.Running)
            {
                try
                {
                    if (!StatusServer.IsStarted)
                        StatusServer.Start(IPAddress.Any, Settings.AdminPort);
                    //check server
                    if (!Server.IsStarted)
                        Server.Start(IPAddress.Any, Settings.Port);
                    for (var i = Bot.Nodes.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            Bot.Nodes.ElementAt(i).Broadcast("ping", false);
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(2000);
                            var n = Bot.Nodes.ElementAt(i);
                            if (Bot.Nodes.Count(x => x.ClientId == n.ClientId) > 1)
                                n.Disconnect(false);
                            else
                            {
                                try
                                {
                                    n.Broadcast("ping", false);
                                }
                                catch (Exception)
                                {
                                    n.Disconnect();
                                }
                            }
                        }

                    }
                }
                catch
                {
                    // ignored
                }

                Thread.Sleep(1000);
            }
        }

        private static void ServerOnDelimiterDataReceived(object sender, Message message)
        {
            //MessageBox.Show(message.MessageString);
        }

        private static void ServerOnDataReceived(object sender, Message message)
        {
            try
            {
                var messages = message.MessageString.Split('\u0013');
                foreach (var msg in messages)
                {
                    if (String.IsNullOrWhiteSpace(msg) || String.IsNullOrWhiteSpace(msg.Replace("\0", "")))
                        continue;
                    dynamic m = JsonConvert.DeserializeObject(msg);
                    string t = m.JType?.ToString();
                    if (t != null)
                    {
                        Node node;
                        switch (t)
                        {
                            case "ClientRegistrationInfo":
                                var cri = JsonConvert.DeserializeObject<ClientRegistrationInfo>(msg);
                                //validate the client
                                if (cri.Secret == Settings.TcpSecret)
                                {
                                    //we can register
                                    var n = new Node {ClientId = cri.ClientId, TcpClient = message.TcpClient};
                                    Bot.Nodes.Add(n);
                                    Bot.NodeConnected(n);
                                    //n.Broadcast("Registered");
                                    Program.Log($"Client registered: {cri.ClientId}");
                                }
                                break;
                            case "NodeInfo":
                                var ni = JsonConvert.DeserializeObject<NodeInfo>(msg);
                                node = Bot.Nodes.FirstOrDefault(x => x.ClientId == ni.ClientId);
                                if (node == null)
                                {
                                    node = new Node {ClientId = ni.ClientId, TcpClient = message.TcpClient};
                                    Bot.Nodes.Add(node);
                                    Bot.NodeConnected(node);
                                }
                                node.CurrentGames = ni.CurrentGames;
                                node.CurrentPlayers = ni.CurrentPlayers;
                                node.DuplicateGamesRemoved = ni.DuplicateGamesRemoved;
                                node.ThreadCount = ni.ThreadCount;
                                node.TotalGames = ni.TotalGames;
                                node.TotalPlayers = ni.TotalPlayers;
                                node.Uptime = ni.Uptime;
                                node.Games = ni.Games;
                                node.Version = ni.Version;
                                node.ShuttingDown = ni.ShuttingDown;
                                if (ni.Version.Contains("5984.20648"))
                                    node.ShuttingDown = true;
                                break;
                            case "GameEndInfo":
                                var gei = JsonConvert.DeserializeObject<GameEndInfo>(msg);
                                node = Bot.Nodes.FirstOrDefault(x => x.ClientId == gei.ClientId);
                                node?.EndGame(gei);
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Program.Log($"Error in message received: {e.Message}\n{message.MessageString}", true);
            }
        }

        private static void ServerOnClientDisconnected(object sender, ConnectedClient tcpClient)
        {
            Program.Log($"Client disconnected: {tcpClient.Client.Client.Handle}");
        }

        private static void ServerOnClientConnected(object sender, ConnectedClient tcpClient)
        {
            Program.Log(
                $"Client connected: {tcpClient.Client.Client.Handle} - {((IPEndPoint) tcpClient.Client.Client.RemoteEndPoint).Address}");
        }

        public static void Broadcast(this Node node, string message, bool catchError = true)
        {
            try
            {
                
                Server.BroadcastLine(message, node.TcpClient);
            }
            catch (Exception e)
            {
                if (catchError)
                    Program.Log($"------------\nError sending broadcast!\n{e.Message}\n{message}\n------------", true);
                else throw e;
            }
        }

        public static Message WriteLineAndGetReply(this Node node, string message, bool catchError = true)
        {
            try
            {
                return Server.WriteLineAndGetReply(message, TimeSpan.FromSeconds(30), node.TcpClient);
            }
            catch (Exception e)
            {
                if (catchError)
                    Program.Log($"------------\nError sending broadcast!\n{e.Message}\n{message}\n------------", true);
                else throw e;
            }
            return null;
        }
    }
}
