using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Win32;
using Newtonsoft.Json;
using TcpFramework;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Node.Models;

namespace Werewolf_Node
{
    class Program
    {
        internal static SimpleTcpClient Client;
        internal static Guid ClientId;
        internal static bool Running = true;
        internal static HashSet<Werewolf> Games = new HashSet<Werewolf>();
        internal static Client Bot;
        internal static Random R = new Random();
        internal static bool IsShuttingDown = false;
        internal static List<long> GroupInitializing = new List<long>();
        //private static werewolfEntities DB;
        internal static readonly DateTime StartupTime = DateTime.Now;
        internal static DateTime IgnoreTime = DateTime.UtcNow.AddSeconds(10);
        internal static bool SendGifIds = false;
        internal static int CommandsReceived = 0;
        internal static int GamesStarted = 0;
        internal static int Para = 129046388;
        internal static int DupGamesKilled = 0;
        internal static int TotalPlayers = 0;
        internal static string APIToken;
        internal static string LanguageDirectory => Path.GetFullPath(Path.Combine(RootDirectory, @"..\Languages"));
        internal static string TempLanguageDirectory => Path.GetFullPath(Path.Combine(RootDirectory, @"..\TempLanguageFiles"));
        internal static XDocument English;
        static void Main(string[] args)
        {
            //set up exception logging.  It appears nodes are crashing and I'm not getting any output
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                var ex = eventArgs.ExceptionObject as Exception;
                using (var sw = new StreamWriter(Path.Combine(RootDirectory, "..\\Logs\\NodeFatalError.log"), true))
                {
                    
                    sw.WriteLine($"{DateTime.Now} - {Version} - {ex.Message}");
                    sw.WriteLine(ex.StackTrace);
                    while (ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                        sw.WriteLine($"{ex.Message}");
                        sw.WriteLine(ex.StackTrace);
                    }
                    sw.WriteLine("--------------------------------------------------------");
                }
            };
            English = XDocument.Load(Path.Combine(LanguageDirectory, "English.xml"));

            //get api token from registry
            var key =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey("SOFTWARE\\Werewolf");
#if DEBUG
            APIToken = key.GetValue("DebugAPI").ToString();
#else
            APIToken = key.GetValue("ProductionAPI").ToString();
#endif
            Bot = new Client(APIToken);
            ClientId = Guid.NewGuid();
            new Thread(KeepAlive).Start();
            Console.Title = $"{ClientId} - {Version.FileVersion}";
            Thread.Sleep(-1);
        }

        private static void ClientOnDelimiterDataReceived(object sender, Message message)
        {
            //if (message.MessageString != "ping")
            //    Console.WriteLine(message.MessageString);
        }

        private static void ClientOnDataReceived(object sender, Message message)
        {
            try
            {
                var messages = message.MessageString.Split('\u0013');
                foreach (var msg in messages)
                {
                    if (msg == "ping" || String.IsNullOrWhiteSpace(msg)) return; //ignore

                    string t = null;
                    try
                    {
                        dynamic m = JsonConvert.DeserializeObject(msg);
                        t = m.JType?.ToString();
                    }
                    catch (Exception e)
                    {
                        //Bot.SendTextMessage(Settings.MainChatId, e.Message);
                        continue;
                    }
                    Werewolf game;
                    if (t != null)
                    {
                        Console.WriteLine(t);
                        switch (t)
                        {
                            case "PlayerJoinInfo":
                                var pji = JsonConvert.DeserializeObject<PlayerJoinInfo>(msg);
                                game = Games.FirstOrDefault(x => x.ChatId == pji.GroupId);
                                game?.AddPlayer(pji.User);
                                break;
                            case "GameStartInfo":
                                var gsi = JsonConvert.DeserializeObject<GameStartInfo>(msg);
                                //double check we don't already have a game...
                                game = Games.FirstOrDefault(x => x.ChatId == gsi.Chat.Id);
                                if (game != null)
                                {
                                    game.AddPlayer(gsi.User);
                                }
                                else
                                {
                                    game = new Werewolf(gsi.Chat.Id, gsi.User, gsi.Chat.Title,
                                        gsi.Chaos);
                                    Games.Add(game);
                                    GamesStarted++;
                                }
                                break;
                            case "ForceStartInfo":
                                var fsi = JsonConvert.DeserializeObject<ForceStartInfo>(msg);
                                game = Games.FirstOrDefault(x => x.ChatId == fsi.GroupId);
                                game?.ForceStart();
                                break;
                            //case "ReplyInfo":
                            //    var ri = JsonConvert.DeserializeObject<ReplyInfo>(msg);
                            //    game =
                            //        Games.FirstOrDefault(
                            //            x => x.Players.Any(p => p.TeleUser.Id == ri.Update.Message.From.Id && !p.IsDead));
                            //    game?.HandleReply(ri.Update);
                            //    break;
                            case "CallbackInfo":
                                var ci = JsonConvert.DeserializeObject<CallbackInfo>(msg);
                                game =
                                    Games.FirstOrDefault(
                                        x => x.Players.Any(p => !p.IsDead && p.TeleUser.Id == ci.Query.From.Id));
                                game?.HandleReply(ci.Query);
                                break;
                            case "PlayerListRequestInfo":
                                var plri = JsonConvert.DeserializeObject<PlayerListRequestInfo>(msg);
                                game = Games.FirstOrDefault(x => x.ChatId == plri.GroupId);
                                game?.OutputPlayers();
                                break;
                            case "PlayerFleeInfo":
                                var pfi = JsonConvert.DeserializeObject<PlayerFleeInfo>(msg);
                                game = Games.FirstOrDefault(x => x.ChatId == pfi.GroupId);
                                game?.RemovePlayer(pfi.User);
                                break;
                            case "LoadLangInfo":
                                var lli = JsonConvert.DeserializeObject<LoadLangInfo>(msg);
                                game = Games.FirstOrDefault(x => x.ChatId == lli.GroupId);
                                game?.LoadLanguage(lli.FileName);
                                break;
                            case "PlayerSmiteInfo":
                                var psi = JsonConvert.DeserializeObject<PlayerSmiteInfo>(msg);
                                game = Games.FirstOrDefault(x => x.ChatId == psi.GroupId);
                                game?.FleePlayer(psi.UserId);
                                break;
                            case "UpdateNodeInfo":
                                IsShuttingDown = true;
                                break;
                            case "SkipVoteInfo":
                                var svi = JsonConvert.DeserializeObject<SkipVoteInfo>(msg);
                                game = Games.FirstOrDefault(x => x.ChatId == svi.GroupId);
                                game?.SkipVote();
                                break;
                            default:
                                Console.WriteLine(msg);
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + message.MessageString);
            }

        }

        public static void RemoveGame(Werewolf werewolf)
        {
            try
            {
                if (werewolf.Players != null)
                {
                    TotalPlayers += werewolf.Players.Count();
                }
                Games.Remove(werewolf);
                //kill the game completely
                werewolf.Dispose();
                werewolf = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveGame: " + ex.Message);
            }
        }

        internal static void Send(string message, long id, bool clearKeyboard = false, ReplyKeyboardMarkup customMenu = null, Werewolf game = null)
        {
            //message = message.Replace("`",@"\`");
            if (clearKeyboard)
            {
                var menu = new ReplyKeyboardHide { HideKeyboard = true };
                Bot.SendTextMessage(id, message, replyMarkup: menu, disableWebPagePreview: true, parseMode: ParseMode.Html);
            }
            else if (customMenu != null)
            {
                Bot.SendTextMessage(id, message, replyMarkup: customMenu, disableWebPagePreview: true, parseMode: ParseMode.Html);
            }
            else
            {
                Bot.SendTextMessage(id, message, disableWebPagePreview: true, parseMode: ParseMode.Html);
            }
        }

        internal static FileVersionInfo Version
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fvi;
            }
        }

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

        internal static void Connect()
        {
            Client = new SimpleTcpClient();
            Client.DataReceived += ClientOnDataReceived;
            Client.DelimiterDataReceived += ClientOnDelimiterDataReceived;
            //connection lost, let's try to reconnect
            while (Client.TcpClient == null || !Client.TcpClient.Connected)
            {
                try
                {
                    Client.Connect(Settings.ServerIP, Settings.Port);
                    var regInfo = new ClientRegistrationInfo { ClientId = ClientId };
                    var json = JsonConvert.SerializeObject(regInfo);
                    Client.WriteLine(json);
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                        ex = ex.InnerException;
                    Console.WriteLine($"Error in reconnect: {ex.Message}\n{ex.StackTrace}\n");
                }
                Thread.Sleep(100);
            }
        }

        public static void KeepAlive()
        {
            Connect();
            while (Running)
            {
                if (Games == null || (IsShuttingDown && Games.Count == 0))
                {
                    Thread.Sleep(10000);
                    Running = false;
                    Environment.Exit(0);
                    return;
                }
                //monitor the tcp connection to keep it open
                try
                {
                    if (Games == null)
                    {
                        //uhhhhhhhhh  ok.....
                        continue;
                    }
                    var games = Games.ToList();
                    var info = new NodeInfo
                    {
                        Games = new HashSet<GameInfo>(),
                        ClientId = ClientId,
                        CurrentGames = games.Count,
                        CurrentPlayers = games.Sum(x => x.Players?.Count??0),
                        DuplicateGamesRemoved = DupGamesKilled,
                        ThreadCount = Process.GetCurrentProcess().Threads.Count,
                        TotalGames = GamesStarted,
                        TotalPlayers = games.Sum(x => x.Players?.Count ?? 0) + TotalPlayers,
                        Uptime = DateTime.Now - StartupTime,
                        Version = Version.FileVersion,
                        ShuttingDown = IsShuttingDown
                    };

                    foreach (var g in games)
                    {
                        var gi = new GameInfo
                        {
                            Language = g.Language,
                            ChatGroup = g.ChatGroup,
                            GroupId = g.ChatId,
                            NodeId = ClientId,
                            State = g.IsRunning ? GameState.Running : g.IsJoining ? GameState.Joining : GameState.Dead,
                            Users = new HashSet<int>(g.Players?.Where(x => !x.IsDead).Select(x => x.TeleUser.Id)??new[] {0})
                        };
                        info.Games.Add(gi);
                    }

                    var json = JsonConvert.SerializeObject(info);
                    Client.WriteLine(json);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    Console.WriteLine($"Error in KeepAlive: {e.Message}\n{e.StackTrace}\n");
                    if (Client != null)
                    {
                        try
                        {
                            Client.DataReceived -= ClientOnDataReceived;
                            Client.DelimiterDataReceived -= ClientOnDelimiterDataReceived;
                            Client.Disconnect();
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    Connect();
                }
                Thread.Sleep(100);
            }
        }
    }
}
