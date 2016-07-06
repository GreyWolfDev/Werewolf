using System;
using System.Collections.Generic;
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
        
        public static void Initialize()
        {
            Server = new SimpleTcpServer {AutoTrimStrings = false};
            Server.ClientConnected += ServerOnClientConnected;
            Server.ClientDisconnected += ServerOnClientDisconnected;
            Server.DataReceived += ServerOnDataReceived;
            Server.DelimiterDataReceived += ServerOnDelimiterDataReceived;
            new Thread(Ping).Start();
        }

        private static void Ping()
        {
            while (Bot.Running)
            {
                try
                {
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
    }
}
