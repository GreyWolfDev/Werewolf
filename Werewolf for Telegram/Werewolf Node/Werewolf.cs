using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Infrastructure.Annotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Node.Helpers;
using Werewolf_Node.Models;

// ReSharper disable PossibleMultipleEnumeration
#pragma warning disable 4014

namespace Werewolf_Node
{

    class Werewolf : IDisposable
    {
        public long ChatId;
        public int GameDay, GameId;
        public List<IPlayer> Players = new List<IPlayer>();
        public bool IsRunning,
            IsJoining = true,
            KillTimer,
            IsInitializing,
            MessageQueueing = true,
            Chaos;
        private InlineKeyboardMarkup RequestPMButton;
        public DateTime LastPlayersOutput = DateTime.Now;
        public GameTime Time;
        public string Language = "English SFW", ChatGroup;
        public Locale Locale;
        public Group DbGroup;
        private bool PlayerListChanged = true;
        private DateTime TimeStarted;
        #region Constructor
        /// <summary>
        /// Starts a new instance of a werewolf game
        /// </summary>
        /// <param name="chatid">Id of the group starting the game</param>
        /// <param name="u">User that started the game</param>
        /// <param name="chatGroup">Name of the group starting the game</param>
        /// <param name="chaos">Chaos mode yes or no</param>
        public Werewolf(long chatid, User u, string chatGroup, bool chaos = false)
        {
            try
            {
                new Thread(GroupQueue).Start();
                using (var db = new WWContext())
                {
                    ChatGroup = chatGroup;
                    ChatId = chatid;
                    DbGroup = db.Groups.FirstOrDefault(x => x.GroupId == ChatId);

                    if (DbGroup == null)
                    {
                        MessageQueueing = false;
                        Program.RemoveGame(this);
                        return;
                    }
                    try
                    {
                        var memberCount = Program.Bot.GetChatMembersCount(chatid).Result;
                        DbGroup.MemberCount = memberCount;
                        db.SaveChanges();
                    }
                    catch
                    {
                        // ignored
                    }

                    //decide if chaos or not
                    Chaos = DbGroup.Mode == "Player" ? chaos : DbGroup.Mode == "Chaos";

                    LoadLanguage(DbGroup.Language);

                    RequestPMButton = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton("Start Me") { Url = "telegram.me/" + Program.Me.Username } });
                    AddPlayer(u);
                }
                SendGif(GetLocaleString(Chaos ? "PlayerStartedChaosGame" : "PlayerStartedGame", u.FirstName),
                    GetRandomImage(Chaos ? Settings.StartChaosGame : Settings.StartGame));
                new Thread(GameTimer).Start();

            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                Program.Send("Hmm.. something went wrong, please try starting the game again...\n" + ex.Message, chatid);
#if DEBUG
                Send(ex.StackTrace);
#else
                Send(
                    Program.Version.FileVersion +
                    $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}",
                    Program.Para);
#endif
                Program.RemoveGame(this);
            }

        }
        #endregion

        #region Language Helpers
        /// <summary>
        /// Caches the language file in the instance
        /// </summary>
        /// <param name="language">The language filename to load</param>
        public void LoadLanguage(string language)
        {
            try
            {
                var files = Directory.GetFiles(Program.LanguageDirectory);
                var file = files.First(x => Path.GetFileNameWithoutExtension(x) == language);
                {
                    var doc = XDocument.Load(file);
                    Locale = new Locale
                    {
                        Language = Path.GetFileNameWithoutExtension(file),
                        File = doc
                    };
                }
                Language = Locale.Language;
            }
            catch
            {
                if (language != "English")
                    LoadLanguage("English");
            }
        }
        /// <summary>
        /// Gets the matching language string and formats it with parameters
        /// </summary>
        /// <param name="key">The XML Key of the string needed</param>
        /// <param name="args">Any arguments to fill the strings {0} {n}</param>
        /// <returns></returns>
        private string GetLocaleString(string key, params object[] args)
        {
            try
            {
                var strings = Locale.File.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key) ??
                              Program.English.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key);
                if (strings != null)
                {
                    var values = strings.Descendants("value");
                    var choice = Program.R.Next(values.Count());
                    var selected = values.ElementAt(choice);
                    return String.Format(selected.Value.FormatHTML(), args).Replace("\\n", Environment.NewLine);
                }
                else
                {
                    throw new Exception($"Error getting string {key} with parameters {args.Aggregate((a, b) => a + "," + b.ToString())}");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error getting string {key} with parameters {args.Aggregate((a, b) => a + "," + b.ToString())}", e);
            }
        }
        #endregion

        #region Main bits
        /// <summary>
        /// The main timer for the game
        /// </summary>
        private void GameTimer()
        {
            try
            {
                //Send($"{Settings.GameJoinTime} seconds to join!");
                //start with the joining time
                var count = Players.Count;
                for (var i = 0; i < Settings.GameJoinTime; i++)
                {
                    if (Players == null) //killed extra game
                        return;
                    if (KillTimer)
                    {
                        KillTimer = false;
                        break;
                    }
                    if (count != Players.Count)
                    {
                        i = Math.Min(i, Math.Max(120, i - 30));
                        count = Players.Count;
                    }

                    if (i == Settings.GameJoinTime - 60)
                    {
                        SendWithQueue(GetLocaleString("MinuteLeftToJoin"));
                    }
                    if (i == Settings.GameJoinTime - 30)
                    {
                        SendWithQueue(GetLocaleString("SecondsLeftToJoin", "30".ToBold()));
                    }
                    if (i == Settings.GameJoinTime - 10)
                    {
                        SendWithQueue(GetLocaleString("SecondsLeftToJoin", "10".ToBold()));
                    }
                    Thread.Sleep(1000);
                }
                IsJoining = false;
                IsInitializing = true;
                //check we have enough players...
                if (Players.Count < Settings.MinPlayers)
                {
                    SendWithQueue(GetLocaleString("NotEnoughPlayers"));

                    Program.RemoveGame(this);
                    return;
                }

                SendWithQueue(GetLocaleString("StartingGameWait"));

                IsRunning = true;
                AssignRoles();
                //create new game for database
                using (var db = new WWContext())
                {
                    TimeStarted = DateTime.Now;
                    var game = new Game
                    {
                        GroupName = ChatGroup,
                        TimeStarted = TimeStarted,
                        GroupId = ChatId,
                        GrpId = int.Parse(DbGroup.Id.ToString()),
                        Mode = Chaos ? "Chaos" : "Normal"
                    };
                    db.Games.Add(game);
                    db.SaveChanges();

                    foreach (var p in Players)
                    {
                        //make sure they have DB entries
                        var dbp = db.Players.FirstOrDefault(x => x.TelegramId == p.Id);
                        if (dbp == null)
                        {
                            dbp = new Player { TelegramId = p.Id, Language = Language };
                            db.Players.Add(dbp);
                        }
                        dbp.Name = p.Name;
                        dbp.UserName = p.TeleUser.Username;

                        p.Language = dbp.Language;

                        var gamePlayer = new GamePlayer
                        {
                            GameId = game.Id,
                            Survived = true,
                            Role = p.PlayerRole.ToString()
                        };
                        dbp.GamePlayers.Add(gamePlayer);
                        db.SaveChanges();

                        //new Task(() => { ImageHelper.GetUserImage(p.TeleUser.Id); }).Start();
                    }
                }

                using (var db = new WWContext())
                {
                    GameId =
                        db.Games.Where(x => x.GroupId == ChatId).OrderByDescending(x => x.Id).FirstOrDefault()?.Id ?? 0;
                }
                IsInitializing = false;

                //disable Inconspicuous achievement if less than 20 players
                if (Players.Count < 20)
                {
                    foreach (var p in Players.Where(x => x != null))
                        p.HasBeenVoted = true;
                }

                NotifyRoles();

                Time = GameTime.Night;
                while (IsRunning)
                {
                    GameDay++;
                    if (!IsRunning) break;
                    CheckRoleChanges();
                    CheckLongHaul();
                    NightCycle();
                    if (!IsRunning) break;
                    CheckRoleChanges();
                    CheckLongHaul();
                    DayCycle();
                    if (!IsRunning) break;
                    CheckRoleChanges();
                    CheckLongHaul();
                    LynchCycle();
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var msg = "";
                foreach (var ves in ex.EntityValidationErrors)
                {
                    foreach (var ve in ves.ValidationErrors)
                    {
                        msg += $"{ves.Entry.Entity}:{ve.ErrorMessage}\n";
                    }
                }
                Send("Something just went terribly wrong, I had to cancel the game....");
                Send(
                    Program.Version.FileVersion +
                    $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{msg}\n{ex.StackTrace}",
                    Program.Para);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.Para);
                    ex = ex.InnerException;
                }
                Send("Something just went terribly wrong, I had to cancel the game....\n" + ex.Message);
#if DEBUG
                Send(ex.StackTrace);
#else
                Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.Para);
#endif

            }
            finally
            {
                Program.RemoveGame(this);
            }

        }
        /// <summary>
        /// Add (Join) a player to the game
        /// </summary>
        /// <param name="u">Telegram user who is joining</param>
        /// <param name="notify">Should we announce the join?</param>
        public void AddPlayer(User u, bool notify = true)
        {
            try
            {
                if (!IsJoining)
                {
                    //SendWithQueue(GetLocaleString("NoJoinGameRunning"));
                    return;
                }
                //first check the player hasn't already joined
                if (Players.Any(x => x.Id == u.Id))
                {
                    //SendWithQueue(GetLocaleString("AlreadyJoined"));
                    return;
                }


                var p = new IPlayer
                {
                    TeleUser = u,
                    HasPM = false,
                    Name = $"{u.FirstName} {u.LastName}"
                };
                p.Name = p.Name.Replace("\n", "").Trim();
                p.Id = p.TeleUser.Id;
                //if ()
                //{
                //    var dbuser = GetDBPlayer(p);
                //    if (dbuser != null)
                //    {
                //        dbuser.Banned = true;
                //        DB.SaveChanges();
                //        Send(GetLocaleString("BannedForExploit", p.Name));
                //    }
                //}
                if (p.Name.StartsWith("/") || String.IsNullOrEmpty(p.Name) || p.Name.Trim().ToLower() == "skip")
                {
                    SendWithQueue(GetLocaleString("ChangeNameToJoin",
                        String.IsNullOrWhiteSpace(u.Username) ? u.FirstName + " " + u.LastName : "@" + u.Username));
                    return;
                }
                if (Players.Any(x => x.Name == p.Name))
                {
                    SendWithQueue(GetLocaleString("NameExists", p.GetName(), p.TeleUser.Username));
                    return;
                }
                if (Players.Count >= Settings.MaxPlayers)
                {
                    SendWithQueue(GetLocaleString("PlayerLimitReached"));
                    return;
                }
                Players.Add(p);


                //if (!notify) return;

                var msg = GetLocaleString("PlayerJoined", p.GetName(), Players.Count.ToBold(), Settings.MinPlayers.ToBold(),
                    DbGroup.MaxPlayers.ToBold() ?? Settings.MaxPlayers.ToBold());

                bool sendPM = false;

                using (var db = new WWContext())
                {
                    var user = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                    if (user == null)
                    {
                        user = new Player
                        {
                            TelegramId = u.Id,
                            Language = "English",
                            HasPM = false,
                            HasPM2 = false,
                            HasDebugPM = false
                        };
                        db.Players.Add(user);
                    }

                    user.UserName = u.Username;
                    user.Name = $"{u.FirstName} {u.LastName}".Trim();

                    if (user.Achievements == null)
                        user.Achievements = 0;
                    if (ChatId == Settings.VeteranChatId)
                    {
                        if (!((Achievements)user.Achievements).HasFlag(Achievements.Veteran))
                        {
                            Helpers.Helpers.KickChatMember(ChatId, user.TelegramId);
                            Players.Remove(p);
                            return;
                        }
                    }

                    db.SaveChanges();
                    var botname = "@" + Program.Me.Username;
#if RELEASE
                    if (user.HasPM != true)
#elif RELEASE2
                    if (user.HasPM2 != true)
#elif DEBUG
                    if (false)
#elif BETA
                    if (user.HasDebugPM != true)
#endif

                    {
                        msg = GetLocaleString("PMTheBot", p.GetName(), botname);
                        sendPM = true;
                    }
                }

                //now, attempt to PM the player
                try
                {
                    var result = Send(GetLocaleString("YouJoined", ChatGroup.FormatHTML()), u.Id).Result;
                }
                catch (Exception e)
                {
                    var botname = "@" + Program.Me.Username;
                    if (!sendPM)
                        msg = GetLocaleString("PMTheBot", p.GetName(), botname);
                    //unable to PM
                    sendPM = true;
                }

                SendWithQueue(msg, requestPM: sendPM);

                if (sendPM) //don't allow them to join
                {
                    Players.Remove(p);
                    p = null;
                }

                if (Players.Count == (DbGroup.MaxPlayers ?? Settings.MaxPlayers))
                    KillTimer = true;

            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in AddPlayer: {e.Message}");
            }
        }
        /// <summary>
        /// Removes (flees) player from the game
        /// </summary>
        /// <param name="u">The telegram user to remove</param>
        public void RemovePlayer(User u)
        {
            try
            {
                if (DbGroup.DisableFlee == true && !IsJoining && IsRunning)
                {
                    SendWithQueue(GetLocaleString("FleeDisabled"));
                    return;
                }
                var p = Players.FirstOrDefault(x => x.Id == u.Id);
                if (p == null) return;
                if (p.IsDead)
                {
                    SendWithQueue(GetLocaleString("DeadFlee"));
                    return;
                }

                SendWithQueue(GetLocaleString("Flee", p.GetName()));
                if (IsRunning)
                {
                    //kill the player
                    p.IsDead = true;
                    p.TimeDied = DateTime.Now;
                    p.Fled = true;
                    if (DbGroup.ShowRoles != false)
                        SendWithQueue(GetLocaleString("PlayerRoleWas", p.GetName(), GetDescription(p.PlayerRole)));

                    CheckRoleChanges();
                    //add the 'kill'
                    DBKill(p, p, KillMthd.Flee);
                    CheckForGameEnd();
                }
                else if (IsJoining)
                {
                    Players.Remove(p);
                    SendWithQueue(GetLocaleString("CountPlayersRemain", Players.Count.ToBold()));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in RemovePlayer: {e.Message}");
            }
        }
        #endregion

        #region Communications
        public void HandleReply(CallbackQuery query)
        {
            try
            {
                //first off, what was I asking them?
                var args = query.Data.Split('|');
                //0 - vote
                //1 - clientid
                //2 - choiceid
                var player = Players.FirstOrDefault(x => x.Id == query.From.Id);
                if (player?.CurrentQuestion == null)
                {
                    return;
                }

                if (query.Data == null)
                {
                    throw new NullReferenceException("Object was null: query.Data");
                }

                if (args[2] == "-1")
                {
                    player.Choice = -1;
                    Program.MessagesSent++;
                    Program.Bot.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                        GetLocaleString("ChoiceAccepted") + " - Skip");
                    player.CurrentQuestion = null;
                    return;
                }
                player.Choice = int.Parse(args[2]);
                var target = Players.FirstOrDefault(x => x.Id == player.Choice);
                if (target == null)
                {
                    Send(GetLocaleString("NoPlayerName"), query.From.Id);
                    return;
                }



                if (player.PlayerRole == IRole.Wolf && player.CurrentQuestion.QType == QuestionType.Kill)
                {
                    var others =
                        Players.Where(
                            x => !x.IsDead && x.PlayerRole == IRole.Wolf && x.Id != player.Id);
                    foreach (var w in others)
                    {
                        Send(GetLocaleString("WolfVotedKill", player.GetName(), target.GetName()), w.Id);
                    }
                }
                if (player.PlayerRole == IRole.WildChild && player.CurrentQuestion.QType == QuestionType.RoleModel)
                {
                    player.RoleModel = target.Id;
                    player.Choice = -1;
                }
                if (player.PlayerRole == IRole.Cupid && player.CurrentQuestion.QType == QuestionType.Lover1)
                {
                    var lover1 = Players.FirstOrDefault(x => x.Id == player.Choice);

                    if (lover1 != null)
                    {
                        if (lover1.Id == player.Id)
                            AddAchievement(player, Achievements.SelfLoving);
                        lover1.InLove = true;
                        //send menu for second choice....
                        var secondChoices = Players.Where(x => !x.IsDead && x.Id != lover1.Id).ToList();
                        var buttons =
                            secondChoices.Select(
                                x => new[] { new InlineKeyboardButton(x.Name, $"vote|{Program.ClientId}|{x.Id}") }).ToList();
                        player.Choice = 0;
                        Program.MessagesSent++;
                        Program.Bot.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                            GetLocaleString("ChoiceAccepted") + " - " + target.Name);

                        SendMenu(buttons, player, GetLocaleString("AskCupid2"), QuestionType.Lover2);
                    }
                    return;
                }
                if (player.PlayerRole == IRole.Cupid && player.CurrentQuestion.QType == QuestionType.Lover2)
                {
                    var lover11 = Players.FirstOrDefault(x => x.InLove);
                    if (lover11 == null)
                        return;
                    if (lover11.Id == player.Id)
                        AddAchievement(player, Achievements.SelfLoving);
                    lover11.LoverId = player.Choice;
                    lover11.InLove = true;

                    var id = int.Parse(lover11.Id.ToString());
                    var lover2 = Players.FirstOrDefault(x => x.Id == player.Choice);
                    if (lover2 == null)
                        return;
                    lover2.InLove = true;
                    lover2.LoverId = id;
                    player.Choice = -1;
                }

                if (player.PlayerRole == IRole.Doppelgänger && player.CurrentQuestion.QType == QuestionType.RoleModel)
                {
                    player.RoleModel = target.Id;
                    player.Choice = -1;
                }

                if (player.PlayerRole == IRole.Cultist && player.CurrentQuestion.QType == QuestionType.Convert)
                {
                    var others =
                        Players.Where(
                            x => !x.IsDead && x.PlayerRole == IRole.Cultist && x.Id != player.Id);
                    foreach (var w in others)
                    {
                        Send(GetLocaleString("CultistVotedConvert", player.GetName(), target.GetName()), w.Id);
                    }
                }


                if (player.CurrentQuestion.QType == QuestionType.Lynch)
                {
                    SendWithQueue(GetLocaleString("PlayerVotedLynch", player.GetName(), target.GetName()));
                }
                Program.MessagesSent++;
                Program.Bot.EditMessageText(query.Message.Chat.Id, query.Message.MessageId,
                        GetLocaleString("ChoiceAccepted") + " - " + target.GetName(true));
                player.CurrentQuestion = null;
            }
            catch (Exception e)
            {
                //Send(e.Message, query.From.Id);
                Console.WriteLine($"Error in HandleReply: {e.Message} \n{query.From.FirstName} {query.From.LastName} (@{query.From.Username})\n{query.Data}");
            }
        }

        private Task<Telegram.Bot.Types.Message> Send(string message, long id = 0, bool clearKeyboard = false, InlineKeyboardMarkup menu = null)
        {
            if (id == 0)
                id = ChatId;
            return Program.Send(message, id, clearKeyboard, menu, game: this);
        }

        private void SendGif(string text, string image, long id = 0)
        {
            Program.MessagesSent++;
            if (id == 0)
                id = ChatId;
            //Log.WriteLine($"{id} -> {image} {text}");
#if (DEBUG)
            Send(text, id);
#else
            Program.Bot.SendDocument(id, image, text);
#endif
        }

        private void SendWithQueue(string text, string gif = null, bool requestPM = false)
        {
            _messageQueue.Enqueue(new Message(text, gif, requestPM));
        }

        private void SendWithQueue(Message m)
        {
            _messageQueue.Enqueue(m);
        }




        class Message
        {
            public string Msg { get; }
            public string GifId { get; }

            public bool RequestPM { get; }
            public bool PlayerList { get; set; } = false;

            public Message(string msg, string gifid = null, bool requestPM = false)
            {
                Msg = msg;
                GifId = gifid;
                RequestPM = requestPM;
            }
        }

        private readonly Queue<Message> _messageQueue = new Queue<Message>();

        private void GroupQueue()
        {
            string final;
            while (MessageQueueing)
            {
                final = "";
                bool requestPM = false;
                bool byteMax = false;
                bool pList = false;
                var i = 0;
                while (_messageQueue.Count > 0 & !byteMax)
                {
                    i++;
                    var m = _messageQueue.Peek();


                    if (!String.IsNullOrEmpty(m.GifId))
                    {
                        if (!String.IsNullOrEmpty(final))
                            Send(final);
                        Thread.Sleep(500);
#if !DEBUG
                        _messageQueue.Dequeue();
                        SendGif(m.Msg, m.GifId);
                        Thread.Sleep(500);
                        final = "";
#else
                        var temp = final + m.Msg + Environment.NewLine + Environment.NewLine;
                        if (Encoding.UTF8.GetByteCount(temp) > 512 && i > 1)
                        {
                            byteMax = true; //break and send
                        }
                        else
                        {
                            _messageQueue.Dequeue(); //remove the message, we are sending it.
                            final += temp;
                            if (m.RequestPM)
                                requestPM = true;
                            if (m.PlayerList)
                                pList = true;
                        }
#endif

                    }
                    else
                    {
                        var temp = final + m.Msg + Environment.NewLine + Environment.NewLine;
                        if (Encoding.UTF8.GetByteCount(temp) > 512 && i > 1)
                        {
                            byteMax = true; //break and send
                        }
                        else
                        {
                            _messageQueue.Dequeue(); //remove the message, we are sending it.
                            final += m.Msg + Environment.NewLine + Environment.NewLine;
                            if (m.RequestPM)
                                requestPM = true;
                            if (m.PlayerList)
                                pList = true;
                        }

                    }
                }
                if (!String.IsNullOrEmpty(final))
                {
                    if (requestPM)
                    {
                        Send(final, 0, false, RequestPMButton);
                    }
                    else
                    {
                        if (pList)
                        {
                            try
                            {
                                var result = Send(final).Result;
                                PlayerListId = result.MessageId;
                            }
                            catch
                            {
                                PlayerListId = 0;
                            }
                        }
                        else
                            Send(final);
                    }
                    
                }
                Thread.Sleep(4000);

            }
            //do one last send
            final = "";
            while (_messageQueue.Count > 0)
            {
                var m = _messageQueue.Dequeue();
                if (m.GifId != null)
                {
                    if (!String.IsNullOrEmpty(final))
                        Send(final);
                    Thread.Sleep(500);
                    SendGif(m.Msg, m.GifId);
                    Thread.Sleep(500);
                    final = "";
                }
                else
                {
                    final += m.Msg + Environment.NewLine;
                }

            }
            if (!String.IsNullOrEmpty(final))
                Send(final);
        }

        private int PlayerListId = 0;

        private void SendPlayerList()
        {
            if (!PlayerListChanged) return;
            new Thread(() =>
            {
                //Thread.Sleep(4500); //wait a moment before sending
                LastPlayersOutput = DateTime.Now;
                var msg = $"{GetLocaleString("PlayersAlive")}: {Players.Count(x => !x.IsDead)}/{Players.Count()}\n" +
                          Players.OrderBy(x => x.TimeDied)
                              .Aggregate("",
                                  (current, p) =>
                                      current +
                                      ($"{p.GetName()}: {(p.IsDead ? ((p.Fled ? GetLocaleString("RanAway") : GetLocaleString("Dead")) + (DbGroup.ShowRoles != false ? " - " + GetDescription(p.PlayerRole) + (p.InLove ? "❤️" : "") : "")) : GetLocaleString("Alive"))}\n"));
                PlayerListChanged = false;

                SendWithQueue(new Message(msg) { PlayerList = true });

            }).Start();

        }

        public void OutputPlayers()
        {
            if ((DateTime.Now - LastPlayersOutput).TotalSeconds > (10))
            {
                LastPlayersOutput = DateTime.Now;
                if (PlayerListId != 0)
                {
                    Program.Bot.SendTextMessage(ChatId, GetLocaleString("LatestList"), replyToMessageId: PlayerListId);
                }
                else
                {
                    Program.Bot.SendTextMessage(ChatId, GetLocaleString("UnableToGetList"), replyToMessageId: PlayerListId);
                }
            }
        }
        #endregion

        #region Roles
        string GetDescription(IRole en)
        {
            return GetLocaleString(en.ToString()).ToBold();
        }

        private static List<IRole> GetRoleList(int playerCount, bool allowCult = true, bool allowTanner = true, bool allowFool = true)
        {
            var rolesToAssign = new List<IRole>();
            //need to set the max wolves so game doesn't end immediately - 25% max wolf population
            //25% was too much, max it at 5 wolves.
            for (int i = 0; i < Math.Max(playerCount / 5, 1); i++)
                rolesToAssign.Add(IRole.Wolf);
            //add remaining roles to 'card pile'
            foreach (var role in Enum.GetValues(typeof(IRole)).Cast<IRole>())
            {
                switch (role)
                {
                    case IRole.Wolf:
                        break;
                    case IRole.CultistHunter:
                    case IRole.Cultist:
                        if (allowCult != false && playerCount > 10)
                            rolesToAssign.Add(role);
                        break;
                    case IRole.Tanner:
                        if (allowTanner != false)
                            rolesToAssign.Add(role);
                        break;
                    case IRole.Fool:
                        if (allowFool != false)
                            rolesToAssign.Add(role);
                        break;
                    default:
                        rolesToAssign.Add(role);
                        break;
                }
            }

            //add a couple more masons
            rolesToAssign.Add(IRole.Mason);
            rolesToAssign.Add(IRole.Mason);
            //for smaller games, all roles will be available and chosen randomly.  For large games, it will be about the
            //same as it was before....

            //if player count > role count, add another cultist into the mix
            if (rolesToAssign.Any(x => x == IRole.CultistHunter))
            {
                rolesToAssign.Add(IRole.Cultist);
                rolesToAssign.Add(IRole.Cultist);
            }
            //now fill rest of the slots with villagers (for large games)
            for (int i = 0; i < playerCount / 4; i++)
                rolesToAssign.Add(IRole.Villager);
            return rolesToAssign;
        }

        private void AssignRoles()
        {
            try
            {
                var rolesToAssign = new List<IRole>();
                var count = Players.Count;
                if (Chaos)
                {
                    rolesToAssign = GetRoleList(count, DbGroup.AllowCult != false, DbGroup.AllowTanner != false, DbGroup.AllowFool != false);
                }
                else
                {
                    var balanced = false;
                    var attempts = 0;
                    var nonVgRoles = new[] { IRole.Cultist, IRole.SerialKiller, IRole.Tanner, IRole.Wolf };
                    while (!balanced)
                    {
                        attempts++;
                        if (attempts >= 200)
                        {
                            throw new IndexOutOfRangeException("Unable to create a balanced game.  Please try again.\nPlayer count: " + count);
                        }
                        rolesToAssign = GetRoleList(count);
                        rolesToAssign.Shuffle();
                        rolesToAssign = rolesToAssign.Take(count).ToList();
                        //check the balance

                        var villageStrength = rolesToAssign.Where(x => !nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));
                        var wolfStrength = rolesToAssign.Where(x => x == IRole.Wolf).Sum(x => x.GetStrength(rolesToAssign));
                        var skStrength = rolesToAssign.Where(x => x == IRole.SerialKiller)
                            .Sum(x => x.GetStrength(rolesToAssign));
                        var cultStrength = rolesToAssign.Where(x => x == IRole.Cultist).Sum(x => x.GetStrength(rolesToAssign));

                        //check balance
                        var varianceAllowed = (count / 5) + 3;
                        var enemyStrength = (wolfStrength + skStrength + cultStrength);
                        balanced = (Math.Abs(villageStrength - enemyStrength) <= varianceAllowed);
                    }


                    //if (count >= Settings.PlayerCountDetective)
                    //{
                    //    rolesToAssign.Add(IRole.Detective);
                    //}
                    //if (count >= Settings.PlayerCountThirdWolf)
                    //{
                    //    rolesToAssign.Add(IRole.Wolf);
                    //}
                    //if (count >= Settings.PlayerCountSeerCursed)
                    //{
                    //    rolesToAssign.Add(IRole.Cursed);
                    //    rolesToAssign.Add(IRole.Seer);
                    //}
                    //if (count >= Settings.PlayerCountGuardianAngel)
                    //    rolesToAssign.Add(IRole.GuardianAngel);
                    //if (count >= Settings.PlayerCountGunner)
                    //{
                    //    rolesToAssign.Add(IRole.Gunner);
                    //}
                    //if (count >= Settings.PlayerCountTraitor)
                    //{
                    //    rolesToAssign.Add(IRole.Traitor);
                    //}
                    //if (count >= Settings.PlayerCountHarlot)
                    //{
                    //    rolesToAssign.Add(IRole.Harlot);
                    //}

                    //if (count >= Settings.PlayerCountSecondWolf)
                    //{
                    //    rolesToAssign.Add(IRole.Wolf);
                    //}

                    //if (count >= Settings.PlayerCountApprenticeSeer)
                    //{
                    //    rolesToAssign.Add(IRole.ApprenticeSeer);
                    //}

                    //if (count >= Settings.PlayerCountWildChild)
                    //{
                    //    rolesToAssign.Add(IRole.WildChild);
                    //}

                    //if (count >= Settings.PlayerCountCultist && DbGroup.AllowCult != false)
                    //{
                    //    rolesToAssign.Add(IRole.Cultist);
                    //    rolesToAssign.Add(IRole.CultistHunter);
                    //}
                    //if (count >= Settings.PlayerCountSecondCultist)
                    //{
                    //    rolesToAssign.Add(IRole.Cultist);
                    //}

                    //if (count >= Settings.PlayerCountMasons)
                    //{
                    //    rolesToAssign.Add(IRole.Mason);
                    //    rolesToAssign.Add(IRole.Mason);
                    //    rolesToAssign.Add(IRole.Mason);
                    //}

                    //if (count >= Settings.PlayerCountCupid)
                    //{
                    //    rolesToAssign.Add(IRole.Cupid);
                    //}

                    //if (count >= Settings.PlayerCountDoppelGanger)
                    //{
                    //    rolesToAssign.Add(IRole.Doppelgänger);
                    //}

                    //if (count >= Settings.PlayerCountSerialKiller)
                    //{
                    //    rolesToAssign.Add(IRole.SerialKiller);
                    //}

                    //if (count >= Settings.PlayerCountHunter)
                    //{
                    //    rolesToAssign.Add(IRole.Hunter);
                    //}

                    ////add beholder?
                    //if (count >= Settings.PlayerCountBeholderChance)
                    //{
                    //    if (Program.R.Next(100) < Settings.BeholderChance)
                    //    {
                    //        rolesToAssign.Add(IRole.Beholder);
                    //    }
                    //}

                    ////add tanner?
                    //if (Program.R.Next(100) < Settings.TannerChance && DbGroup.AllowTanner != false)
                    //{
                    //    rolesToAssign.Add(IRole.Tanner);
                    //}

                    ////add Fool?
                    //if (count >= Settings.PlayerCountFoolChance)
                    //{
                    //    if (Program.R.Next(100) < Settings.FoolChance && DbGroup.AllowFool != false)
                    //    {
                    //        rolesToAssign.Add(IRole.Fool);
                    //    }
                    //}

                    //rolesToAssign.Add(IRole.Wolf);
                    //rolesToAssign.Add(IRole.Drunk);

                    //while (count > rolesToAssign.Count)
                    //    rolesToAssign.Add(IRole.Villager);

                }



                //shuffle things
                Players.Shuffle();
                Players.Shuffle();
                rolesToAssign.Shuffle();
                rolesToAssign.Shuffle();


                //#if DEBUG
                //                //force roles for testing
                //                rolesToAssign[0] = IRole.Cupid;
                //                rolesToAssign[1] = IRole.Doppelgänger;
                //                rolesToAssign[2] = IRole.WildChild;
                //                rolesToAssign[3] = IRole.Wolf;
                //#endif

                var lastIndex = 0;
                for (var i = 0; i < Players.Count; i++)
                {
                    Players[i].PlayerRole = rolesToAssign[i];
                    lastIndex = i;
                }
                lastIndex++;
                foreach (var p in Players)
                {
                    switch (p.PlayerRole)
                    {
                        case IRole.Villager:
                        case IRole.Cursed:
                        case IRole.Drunk:
                        case IRole.Beholder:
                        case IRole.ApprenticeSeer:
                        case IRole.Traitor:
                        case IRole.Mason:
                        case IRole.Hunter:
                            p.HasDayAction = false;
                            p.HasNightAction = false;
                            p.Team = ITeam.Village;
                            break;
                        case IRole.Fool:
                        case IRole.Harlot:
                        case IRole.CultistHunter:
                        case IRole.Seer:
                        case IRole.GuardianAngel:
                        case IRole.WildChild:
                        case IRole.Cupid:
                            p.Team = ITeam.Village;
                            p.HasNightAction = true;
                            p.HasDayAction = false;
                            break;
                        case IRole.Doppelgänger:
                            p.Team = ITeam.Neutral;
                            p.HasNightAction = true;
                            p.HasDayAction = false;
                            break;
                        case IRole.Detective:
                        case IRole.Gunner:
                            p.Team = ITeam.Village;
                            p.HasDayAction = true;
                            p.HasNightAction = false;
                            break;
                        case IRole.Wolf:
                            p.Team = ITeam.Wolf;
                            p.HasNightAction = true;
                            p.HasDayAction = false;
                            break;
                        case IRole.Tanner:
                            p.Team = ITeam.Tanner;
                            p.HasDayAction = false;
                            p.HasNightAction = false;
                            break;
                        case IRole.Cultist:
                            p.HasDayAction = false;
                            p.HasNightAction = true;
                            p.Team = ITeam.Cult;
                            break;
                        case IRole.SerialKiller:
                            p.HasNightAction = true;
                            p.HasDayAction = false;
                            p.Team = ITeam.SerialKiller;
                            break;

                    }
                }

                if (Chaos)
                {
                    //make sure we at least have more than one team (tanner doesn't count)
                    while (Players.Where(x => x.Team != ITeam.Tanner && x.Team != ITeam.Neutral).Select(x => x.Team).Distinct().Count() < 2)
                    {
                        Players[count - 1].PlayerRole = rolesToAssign[lastIndex];
                        lastIndex++;
                        foreach (var p in Players)
                        {
                            switch (p.PlayerRole)
                            {
                                case IRole.Villager:
                                case IRole.Cursed:
                                case IRole.Drunk:
                                case IRole.Beholder:
                                case IRole.ApprenticeSeer:
                                case IRole.Traitor:
                                case IRole.Mason:
                                case IRole.Hunter:
                                    p.HasDayAction = false;
                                    p.HasNightAction = false;
                                    p.Team = ITeam.Village;
                                    break;
                                case IRole.Fool:
                                case IRole.Harlot:
                                case IRole.CultistHunter:
                                case IRole.Seer:
                                case IRole.GuardianAngel:
                                case IRole.WildChild:
                                case IRole.Cupid:
                                    p.Team = ITeam.Village;
                                    p.HasNightAction = true;
                                    p.HasDayAction = false;
                                    break;
                                case IRole.Doppelgänger:
                                    p.Team = ITeam.Neutral;
                                    p.HasNightAction = true;
                                    p.HasDayAction = false;
                                    break;
                                case IRole.Detective:
                                case IRole.Gunner:
                                    p.Team = ITeam.Village;
                                    p.HasDayAction = true;
                                    p.HasNightAction = false;
                                    break;
                                case IRole.Wolf:
                                    p.Team = ITeam.Wolf;
                                    p.HasNightAction = true;
                                    p.HasDayAction = false;
                                    break;
                                case IRole.Tanner:
                                    p.Team = ITeam.Tanner;
                                    p.HasDayAction = false;
                                    p.HasNightAction = false;
                                    break;
                                case IRole.Cultist:
                                    p.HasDayAction = false;
                                    p.HasNightAction = true;
                                    p.Team = ITeam.Cult;
                                    break;
                                case IRole.SerialKiller:
                                    p.HasNightAction = true;
                                    p.HasDayAction = false;
                                    p.Team = ITeam.SerialKiller;
                                    break;
                            }
                        }
                    }
                }

                //do a shuffle, as the above portion can cause the SK / wolf to always be the last player in the list.
                Players.Shuffle();
                Players.Shuffle();

                //one last check for seer
                if (Players.Any(x => x.PlayerRole == IRole.ApprenticeSeer) &&
                    Players.All(x => x.PlayerRole != IRole.Seer))
                {
                    var s = Players.FirstOrDefault(x => x.PlayerRole == IRole.ApprenticeSeer);
                    if (s != null)
                    {
                        s.PlayerRole = IRole.Seer;
                        s.HasNightAction = true;
                    }
                }

                //check that CH exists if cult exist
                if (Players.Any(x => x.PlayerRole == IRole.Cultist) &&
                    Players.All(x => x.PlayerRole != IRole.CultistHunter))
                {
                    //fix dat shit
                    var ch = Players.First(x => x.PlayerRole != IRole.Cultist && x.PlayerRole != IRole.Wolf);
                    ch.PlayerRole = IRole.CultistHunter;
                    ch.HasDayAction = false;
                    ch.HasNightAction = true;
                    ch.Team = ITeam.Village;
                }



                foreach (var p in Players)
                    p.OriginalRole = p.PlayerRole;
            }
            catch (Exception ex)
            {
                Send($"Error while assigning roles: {ex.Message}\nPlease start a new game");
                Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.Para);
                Thread.Sleep(1000);
                Program.RemoveGame(this);
            }

        }

        private void NotifyRoles()
        {
            if (Players == null) return; //how the hell?
            //notify each player
            foreach (var p in Players.ToList())
            {
                if (p?.PlayerRole == null) continue;
                var msg = GetRoleInfo(p.PlayerRole);
                try
                {
                    var result = Program.Send(msg, p.Id, true).Result;
                }
                catch (AggregateException e)
                {
                    SendWithQueue(GetLocaleString("PlayerNoPM", p.GetName()));
                    FleePlayer(p.TeleUser.Id);
                }
                catch (NullReferenceException ex)
                {
                    Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.Para);
                }
                Thread.Sleep(50);
            }
        }

        private string GetRoleInfo(IRole role)
        {
            var msg = "";
            try
            {
                switch (role)
                {
                    case IRole.Cursed:
                        msg = GetLocaleString("RoleInfoCursed");
                        break;
                    case IRole.Detective:
                        msg = GetLocaleString("RoleInfoDetective");
                        break;
                    case IRole.Drunk:
                        msg = GetLocaleString("RoleInfoDrunk");
                        break;
                    case IRole.GuardianAngel:
                        msg = GetLocaleString("RoleInfoGuardianAngel");
                        break;
                    case IRole.Gunner:
                        msg = GetLocaleString("RoleInfoGunner");
                        break;
                    case IRole.Harlot:
                        msg = GetLocaleString("RoleInfoHarlot");
                        break;
                    case IRole.Traitor:
                        msg = GetLocaleString("RoleInfoTraitor");
                        break;
                    case IRole.Wolf:
                        msg = GetLocaleString("RoleInfoWolf");
                        break;
                    case IRole.Fool:
                    case IRole.Seer:
                        msg = GetLocaleString("RoleInfoSeer");
                        break;
                    case IRole.Villager:
                        msg = GetLocaleString("RoleInfoVillager");
                        break;
                    case IRole.Tanner:
                        msg = GetLocaleString("RoleInfoTanner");
                        break;
                    case IRole.Cultist:
                        msg = GetLocaleString("RoleInfoCultist");
                        break;
                    case IRole.WildChild:
                        msg = GetLocaleString("RoleInfoWildChild");
                        break;
                    case IRole.Beholder:
                        msg = GetLocaleString("RoleInfoBeholder");
                        var seer = Players?.FirstOrDefault(x => x.PlayerRole == IRole.Seer);
                        if (seer != null)
                            msg += GetLocaleString("BeholderSeer", $"{seer.GetName()}");
                        else
                            msg += "  " + GetLocaleString("NoSeer");
                        break;
                    case IRole.ApprenticeSeer:
                        msg = GetLocaleString("RoleInfoApprenticeSeer");
                        break;
                    case IRole.CultistHunter:
                        msg = GetLocaleString("RoleInfoCultistHunter");
                        break;
                    case IRole.Mason:
                        msg = GetLocaleString("RoleInfoMason");
                        if (Players?.Count(x => x?.PlayerRole == IRole.Mason) > 1)
                        {
                            msg += GetLocaleString("MasonTeam",
                                Players.Where(x => x.PlayerRole == IRole.Mason)
                                    .Select(x => x.GetName())
                                    .Aggregate((current, next) => current + ", " + next));
                        }
                        break;
                    case IRole.Cupid:
                        msg = GetLocaleString("RoleInfoCupid");
                        break;
                    case IRole.Doppelgänger:
                        msg = GetLocaleString("RoleInfoDoppelganger");
                        break;
                    case IRole.Hunter:
                        msg = GetLocaleString("RoleInfoHunter");
                        break;
                    case IRole.SerialKiller:
                        msg = GetLocaleString("RoleInfoSerialKiller");
                        break;
                }
            }
            catch (Exception e)
            {
                Send("Error in get role info: \n" + e.Message + "\n" + e.StackTrace, Program.Para);
            }
            return msg;
        }

        public void CheckRoleChanges()
        {
            if (Players == null) return;
            //check Apprentice Seer
            var aps = Players?.FirstOrDefault(x => x.PlayerRole == IRole.ApprenticeSeer & !x.IsDead);
            if (aps != null)
            {
                //check if seer is alive
                if (!Players.Any(x => x.PlayerRole == IRole.Seer & !x.IsDead))
                {
                    //get the dead seer
                    var ds = Players.FirstOrDefault(x => x.PlayerRole == IRole.Seer && x.IsDead);

                    //seer is dead, promote app seer
                    aps.HasDayAction = false;
                    aps.HasNightAction = true;
                    aps.OriginalRole = IRole.ApprenticeSeer;
                    aps.PlayerRole = IRole.Seer;
                    aps.ChangedRolesCount++;
                    //notify
                    Send(GetLocaleString("ApprenticeNowSeer", ds?.GetName() ?? GetDescription(IRole.Seer)), aps.Id);
                    var beholder = Players.FirstOrDefault(x => x.PlayerRole == IRole.Beholder & !x.IsDead);
                    if (beholder != null)
                        Send(GetLocaleString("BeholderNewSeer", $"{aps.GetName()}", ds?.GetName() ?? GetDescription(IRole.Seer)), beholder.Id);
                }

            }
            CheckWildChild();
            CheckDoppelganger();

            var wolves = Players.Where(x => x.PlayerRole == IRole.Wolf & !x.IsDead);
            if (wolves.Count() >= 7)
            {
                foreach (var w in wolves)
                {
                    AddAchievement(w, Achievements.PackHunter);
                }
            }

        }

        private void ValidateSpecialRoleChoices()
        {
            if (GameDay != 1) return;
            //Wild Child
            var wc = Players.FirstOrDefault(x => x.PlayerRole == IRole.WildChild);
            if (wc != null && wc.RoleModel == 0)
            {
                var choiceid = ChooseRandomPlayerId(wc);
                var choice = Players.FirstOrDefault(x => x.Id == choiceid);
                if (choice != null)
                {
                    wc.RoleModel = choice.Id;
                    Send(GetLocaleString("RoleModelChosen", choice.GetName()), wc.Id);
                }
            }

            var dg = Players.FirstOrDefault(x => x.PlayerRole == IRole.Doppelgänger);
            if (dg != null && dg.RoleModel == 0)
            {
                var choiceid = ChooseRandomPlayerId(dg);
                var choice = Players.FirstOrDefault(x => x.Id == choiceid);
                if (choice != null)
                {
                    dg.RoleModel = choice.Id;
                    Send(GetLocaleString("RoleModelChosen", choice.GetName()), dg.Id);
                }
            }

            //first, make sure there even IS a cupid
            if (Players.Any(x => x.PlayerRole == IRole.Cupid))
            {
                CreateLovers();
                NotifyLovers();
            }
        }

        private void CreateLovers()
        {

            //REDO
            //how many lovers do we have?
            var count = Players.Count(x => x.InLove);
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{count} Lovers found");
            Console.ForegroundColor = ConsoleColor.Gray;
            if (count == 2)
            {
                return;
            }
            if (count > 2) //how?!?
            {
                var lovers = Players.Where(x => x.InLove).ToList(); //to list, we have broken off
                var l1 = Players.FirstOrDefault(x => x.Id == lovers[0].Id);
                var l2 = Players.FirstOrDefault(x => x.Id == lovers[1].Id);
                if (l1 == null || l2 == null)
                {
                    //WTF IS GOING ON HERE?!
                    if (l1 != null)
                        AddLover(l1);
                    if (l2 != null)
                        AddLover(l2);
                    //if both are null..
                    if (l1 == null && l2 == null)
                    {
                        //so lost....
                        l1 = AddLover();
                        l2 = AddLover(l1);
                    }
                }
                l1.LoverId = l2.Id;
                l2.LoverId = l1.Id;
                foreach (var p in lovers.Skip(2))
                {
                    var foreverAlone = Players.FirstOrDefault(x => x.Id == p.Id);
                    if (foreverAlone != null)
                    {
                        foreverAlone.InLove = false;
                        foreverAlone.LoverId = 0;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Step 1: {Players.Count(x => x.InLove)} Lovers found");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            if (count < 2)
            {
                //ok, missing lovers.
                var exist = Players.FirstOrDefault(x => x.InLove) ?? AddLover();
                if (exist == null)
                    exist = AddLover();

                AddLover(exist);
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Step 2: {Players.Count(x => x.InLove)} Lovers found");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            ////cupid stuffs
            //var lovers = Players.Where(x => x.InLove);
            //while (lovers.Count() != 2)
            //{
            //    //ok, missing lover, create one
            //    var choiceid = ChooseRandomPlayerId(lovers);
            //    var newLover = Players.FirstOrDefault(x => x.Id == choiceid);
            //    if (newLover != null)
            //    {
            //        newLover.InLove = true;
            //        var otherLover = lovers.FirstOrDefault(x => x.Id != newLover.Id);
            //        if (otherLover != null)
            //        {
            //            otherLover.LoverId = newLover.Id;
            //            newLover.LoverId = otherLover.Id;
            //        }
            //    }
            //}
        }

        private void NotifyLovers()
        {
            var loversNotify = Players.Where(x => x.InLove).ToList();
            while (loversNotify.Count != 2)
            {
                CreateLovers();
                loversNotify = Players.Where(x => x.InLove).ToList();
            }

            Send(GetLocaleString("CupidChosen", loversNotify[0].GetName()), loversNotify[1].Id);
            Send(GetLocaleString("CupidChosen", loversNotify[1].GetName()), loversNotify[0].Id);

        }

        private IPlayer AddLover(IPlayer existing = null)
        {
            var lover = Players.FirstOrDefault(x => x.Id == ChooseRandomPlayerId(existing));
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"AddLover: {lover?.Name} picked");
            Console.ForegroundColor = ConsoleColor.Gray;
            if (lover == null) return null;
            lover.InLove = true;
            if (existing == null) return lover;
            existing.LoverId = lover.Id;
            lover.LoverId = existing.Id;
            return lover;
        }

        private void CheckWildChild()
        {
            var wc = Players?.FirstOrDefault(x => x.PlayerRole == IRole.WildChild & !x.IsDead);

            // Check Wild Child
            if (wc != null)
            {
                var rm = Players.FirstOrDefault(x => x.Id == wc.RoleModel);
                if (rm != null)
                {
                    if (rm.IsDead)
                    {
                        var teammates = "";
                        //notify other wolves
                        foreach (var w in Players.Where(x => x.PlayerRole == IRole.Wolf & !x.IsDead))
                        {
                            Send(GetLocaleString("WildChildToWolves", $"{wc.GetName()}"), w.Id);
                            teammates += $"{w.GetName()}" + ", ";
                        }
                        wc.PlayerRole = IRole.Wolf;
                        wc.Team = ITeam.Wolf;
                        wc.ChangedRolesCount++;
                        wc.HasNightAction = true;
                        wc.HasDayAction = false;
                        Send(GetLocaleString("WildChildTransform", rm.GetName(), teammates), wc.Id);
                    }
                }
            }
        }

        private void CheckDoppelganger()
        {
            var p = Players?.FirstOrDefault(x => x.PlayerRole == IRole.Doppelgänger & !x.IsDead);
            //var aps = Players.FirstOrDefault(x => x.PlayerRole == IRole.ApprenticeSeer & !x.IsDead);
            //var traitor = Players.FirstOrDefault(x => x.PlayerRole == IRole.Traitor & !x.IsDead);

            // Check DG
            if (p != null)
            {
                var rm = Players.FirstOrDefault(x => x.Id == p.RoleModel);
                if (rm != null)
                {
                    if (rm.IsDead)
                    {

                        var teammates = "";
                        //notify other wolves

                        p.PlayerRole = rm.OriginalRole;
                        if (rm.OriginalRole == IRole.ApprenticeSeer || rm.OriginalRole == IRole.WildChild || rm.OriginalRole == IRole.Traitor || rm.OriginalRole == IRole.Cursed)
                            if (rm.PlayerRole != IRole.Cultist)
                                p.PlayerRole = rm.PlayerRole;
                        p.ChangedRolesCount++;
                        switch (p.PlayerRole)
                        {
                            case IRole.Villager:
                            case IRole.Cursed:
                            case IRole.Drunk:
                                p.HasDayAction = false;
                                p.HasNightAction = false;
                                p.Team = ITeam.Village;
                                break;
                            case IRole.Beholder:
                                p.HasDayAction = false;
                                p.HasNightAction = false;
                                p.Team = ITeam.Village;
                                var seer = Players.FirstOrDefault(x => x.PlayerRole == IRole.Seer);
                                if (seer != null)
                                    Send(GetLocaleString("BeholderSeer",
                                        $"{seer.GetName()}"),
                                        p.Id);
                                else
                                    Send(GetLocaleString("NoSeer"), p.Id);
                                break;
                            case IRole.ApprenticeSeer:
                                p.HasDayAction = false;
                                p.HasNightAction = false;
                                p.Team = ITeam.Village;
                                if (Players.Count(x => !x.IsDead && x.PlayerRole == IRole.Seer) == 0)
                                {
                                    p.PlayerRole = IRole.Seer;
                                    p.HasNightAction = true;
                                    var beholder = Players.FirstOrDefault(x => x.PlayerRole == IRole.Beholder & !x.IsDead);
                                    if (beholder != null)
                                        Send(GetLocaleString("BeholderNewSeer", $"{p.GetName()}", rm.GetName() ?? GetDescription(IRole.Seer)), beholder.Id);
                                }
                                break;
                            case IRole.Traitor:
                                p.HasDayAction = false;
                                p.HasNightAction = false;
                                p.Team = ITeam.Village;
                                if (Players.Count(x => !x.IsDead && x.PlayerRole == IRole.Wolf) == 0)
                                {
                                    p.HasNightAction = true;
                                    p.PlayerRole = IRole.Wolf;
                                    p.Team = ITeam.Wolf;
                                }
                                break;
                            case IRole.Mason:
                                p.HasDayAction = false;
                                p.HasNightAction = false;
                                p.Team = ITeam.Village;
                                foreach (var w in Players.Where(x => x.PlayerRole == IRole.Mason & !x.IsDead && x.Id != p.Id))
                                {
                                    Send(GetLocaleString("DGToMason", $"{p.GetName()}"), w.Id);
                                    teammates += $"{w.GetName()}" + ", ";
                                }
                                Send(GetLocaleString("DGTransformToMason", rm.GetName(), teammates), p.Id);
                                break;
                            case IRole.Hunter:
                                p.HasDayAction = false;
                                p.HasNightAction = false;
                                p.Team = ITeam.Village;
                                break;
                            case IRole.Fool:
                            case IRole.Harlot:
                            case IRole.CultistHunter:
                                p.Team = ITeam.Village;
                                p.HasNightAction = true;
                                p.HasDayAction = false;
                                break;
                            case IRole.Seer:
                                p.Team = ITeam.Village;
                                p.HasNightAction = true;
                                p.HasDayAction = false;
                                var bh = Players.FirstOrDefault(x => x.PlayerRole == IRole.Beholder & !x.IsDead);
                                if (bh != null)
                                    Send(GetLocaleString("BeholderNewSeer", $"{p.GetName()}", rm.GetName() ?? GetDescription(IRole.Seer)), bh.Id);
                                break;
                            case IRole.GuardianAngel:

                                p.Team = ITeam.Village;
                                p.HasNightAction = true;
                                p.HasDayAction = false;
                                break;
                            case IRole.WildChild:
                                p.RoleModel = rm.RoleModel;
                                p.Team = ITeam.Village;
                                p.HasNightAction = true;
                                p.HasDayAction = false;
                                Send(GetLocaleString("NewWCRoleModel", Players.FirstOrDefault(x => x.Id == p.RoleModel)?.GetName() ?? "None was chosen!"), p.Id);
                                break;
                            case IRole.Cupid:
                            case IRole.Doppelgänger:
                                p.Team = ITeam.Village;
                                p.HasNightAction = false;
                                p.HasDayAction = false;
                                break;
                            case IRole.Detective:
                            case IRole.Gunner:
                                p.Bullet = 2;
                                p.Team = ITeam.Village;
                                p.HasDayAction = true;
                                p.HasNightAction = false;
                                break;
                            case IRole.Wolf:
                                p.Team = ITeam.Wolf;
                                p.HasNightAction = true;
                                p.HasDayAction = false;
                                foreach (var w in Players.Where(x => x.PlayerRole == IRole.Wolf & !x.IsDead && x.Id != p.Id))
                                {
                                    Send(GetLocaleString("DGToWolf", $"{p.GetName()}"), w.Id);
                                    teammates += $"{w.GetName()}" + ", ";
                                }
                                Send(GetLocaleString("DGTransformToWolf", rm.GetName(), teammates), p.Id);
                                break;
                            case IRole.Tanner:
                                p.Team = ITeam.Tanner;
                                p.HasDayAction = false;
                                p.HasNightAction = false;
                                break;
                            case IRole.Cultist:
                                p.HasDayAction = false;
                                p.HasNightAction = true;
                                p.Team = ITeam.Cult;
                                foreach (var w in Players.Where(x => x.PlayerRole == IRole.Cultist & !x.IsDead && x.Id != p.Id))
                                {
                                    Send(GetLocaleString("DGToCult", $"{p.GetName()}"), w.Id);
                                    teammates += $"{w.GetName()}" + ", ";
                                }
                                Send(GetLocaleString("DGTransformToCult", rm.GetName(), teammates), p.Id);
                                break;
                            case IRole.SerialKiller:
                                p.HasNightAction = true;
                                p.HasDayAction = false;
                                p.Team = ITeam.SerialKiller;
                                break;
                        }

                        if (p.PlayerRole != IRole.Mason && p.PlayerRole != IRole.Wolf && p.PlayerRole != IRole.Cultist && p.PlayerRole != IRole.WildChild)
                        {
                            //tell them their new role
                            Send(GetRoleInfo(p.PlayerRole), p.Id);
                        }
                    }
                }
            }
        }

        private void ConvertToCult(IPlayer target, IEnumerable<IPlayer> voteCult)
        {
            target.OriginalRole = target.PlayerRole;
            target.PlayerRole = IRole.Cultist;
            target.Team = ITeam.Cult;
            target.HasDayAction = false;
            target.HasNightAction = true;
            target.DayCult = GameDay;
            Send(GetLocaleString("CultConvertYou"), target.Id);
            Send(GetLocaleString("CultTeam", voteCult.Select(x => x.GetName()).Aggregate((a, b) => a + ", " + b)), target.Id);
            foreach (var c in voteCult)
                Send(GetLocaleString("CultJoin", $"{target.GetName()}"), c.Id);
        }
        #endregion

        #region Cycles
        public void ForceStart()
        {
            KillTimer = true;
        }

        private void LynchCycle()
        {
            if (!IsRunning) return;
            Time = GameTime.Lynch;
            if (Players == null) return;
            foreach (var p in Players)
                p.CurrentQuestion = null;

            if (CheckForGameEnd()) return;
            SendWithQueue(GetLocaleString("LynchTime", DbGroup.LynchTime.ToBold() ?? Settings.TimeLynch.ToBold()));
            SendPlayerList();
            SendLynchMenu();

            for (var i = 0; i < (DbGroup.LynchTime ?? Settings.TimeLynch); i++)
            {
                Thread.Sleep(1000);
                if (CheckForGameEnd()) return;
                //check if all votes are cast
                var livePlayers = Players.Where(x => !x.IsDead);
                if (livePlayers.All(x => x.Choice != 0))
                    break;
            }
            //remove menus
            try
            {
                foreach (var p in Players.Where(x => x.CurrentQuestion != null))
                {
                    try
                    {
                        if (p.CurrentQuestion.MessageId != 0)
                        {
                            Program.MessagesSent++;
                            Program.Bot.EditMessageText(p.Id, p.CurrentQuestion.MessageId, GetLocaleString("TimesUp"));
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    p.CurrentQuestion = null;
                }
            }
            catch
            {
                // ignored
            }


            //Log.WriteLine("Lynch time ended, adding up votes");
            if (CheckForGameEnd()) return;
            foreach (var p in Players.Where(x => !x.IsDead))
            {
                if (p.Choice != 0 && p.Choice != -1)
                {
                    var target = Players.FirstOrDefault(x => x.Id == p.Choice);
                    if (target != null)
                    {
                        target.HasBeenVoted = true;
                        target.Votes++;
                        DBAction(p, target, "Lynch");
                    }
                    p.NonVote = 0;
                }
                else if (!p.IsDead)
                {
                    p.NonVote++;
                    if (p.NonVote < 2) continue;
                    var idles24 = 0;
                    try
                    {
                        using (var db = new WWContext())
                        {
                            idles24 = db.GetIdleKills24Hours(p.Id).FirstOrDefault() ?? 0;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    SendWithQueue(GetLocaleString("IdleKill", p.GetName(), (DbGroup.ShowRoles == false ? "" : $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}\n") + GetLocaleString("IdleCount", p.GetName(), idles24 + 1)));

                    //if hunter has died from AFK, too bad....
                    p.IsDead = true;
                    p.TimeDied = DateTime.Now;
                    CheckRoleChanges();
                    //update the database
                    DBKill(p, p, KillMthd.Idle);
                }
            }


            try
            {
                var maxVotes = Players.Max(x => x.Votes);
                var choices = Players.Where(x => x.Votes == maxVotes).ToList();
                IPlayer lynched = new IPlayer() { Votes = -1 };
                if (choices.Count > 1)
                {
                    //Log.WriteLine("Lynch tie");
                    if (Settings.RandomLynch)
                    {
                        //select one at random now
                        choices.Shuffle();
                        choices.Shuffle();
                        lynched = Players.First(x => x.Id == choices.First().Id);
                    }
                }
                else
                {
                    lynched = Players.First(x => x.Id == choices.First().Id);
                }

                //Log.WriteLine("lynched Votes = " + lynched.Votes);

                if (lynched.Votes > 0)
                {
                    lynched.IsDead = true;
                    lynched.TimeDied = DateTime.Now;
                    if (lynched.PlayerRole == IRole.Seer && GameDay == 1)
                        AddAchievement(lynched, Achievements.LackOfTrust);
                    SendWithQueue(GetLocaleString("LynchKill", lynched.GetName(), DbGroup.ShowRoles == false ? "" : $"{lynched.GetName()} {GetLocaleString("Was")} {GetDescription(lynched.PlayerRole)}"));
                    if (lynched.InLove)
                        KillLover(lynched);
                    //update the database
                    foreach (var pl in Players.Where(x => x.Choice == lynched.Id))
                    {
                        DBKill(pl, lynched, KillMthd.Lynch);
                    }
                    //add the 'kill'
                    if (lynched.PlayerRole == IRole.Tanner)
                    {
                        //check for overkill
                        if (Players.Where(x => !x.IsDead).All(x => x.Choice == lynched.Id))
                            AddAchievement(lynched, Achievements.TannerOverkill);
                        //end game
                        DoGameEnd(ITeam.Tanner);
                        return;
                    }
                    //need to do the hunter!

                    if (lynched.PlayerRole == IRole.Hunter)
                    {
                        HunterFinalShot(lynched, KillMthd.Lynch);
                    }
                    CheckRoleChanges();
                }
                else if (lynched.Votes == -1)
                {
                    SendWithQueue(GetLocaleString("LynchTie"));
                    var t = choices.FirstOrDefault(x => x.PlayerRole == IRole.Tanner);
                    if (t != null)
                        AddAchievement(t, Achievements.SoClose);
                }
                else
                {
                    SendWithQueue(GetLocaleString("NoLynchVotes"));
                }
            }
            catch (Exception ex)
            {
                var e = ex;
                while (e.InnerException != null)
                    e = e.InnerException;


                Send("Well shit. I broke. Check the logs...\n" + e.Message);
#if DEBUG
                Send(ex.StackTrace);
#else
                Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.Para);
#endif
                Program.RemoveGame(this);
            }
        }

        private void DayCycle()
        {
            if (!IsRunning) return;
            Time = GameTime.Day;

            //see who died over night
            if (Players == null) return;
            foreach (var p in Players)
                p.CurrentQuestion = null;
            if (Players.Any(x => x.DiedLastNight))
            {
                if (Players == null) return;
                //OLD CODE
                //if (DbGroup.ShowRoles != false)
                //{
                //    foreach (var p in Players.Where(x => x.DiedLastNight && x.DiedFromWolf && !x.DiedGuardingWolf))
                //    {
                //        string msg;
                //        switch (p.PlayerRole)
                //        {
                //            case IRole.Detective:
                //                msg = GetLocaleString("DetectiveEaten", p.GetName());
                //                break;
                //            case IRole.GuardianAngel:
                //                msg = GetLocaleString("GuardianEaten", p.GetName());
                //                break;
                //            case IRole.Gunner:
                //                msg = GetLocaleString("GunnerEaten", p.GetName());
                //                break;
                //            case IRole.Harlot:
                //                msg = null;
                //                break;
                //            case IRole.Seer:
                //                msg = GetLocaleString("SeerEaten", p.GetName());
                //                break;
                //            case IRole.Drunk:
                //                msg = GetLocaleString("DrunkEaten", p.GetName());
                //                break;
                //            case IRole.Fool:
                //                msg = GetLocaleString("FoolEaten", p.GetName());
                //                break;
                //            case IRole.Mason:
                //                msg = GetLocaleString("MasonEaten", p.GetName());
                //                break;
                //            default:
                //                msg = GetLocaleString("DefaultEaten", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
                //                break;
                //        }
                //        SendWithQueue(msg);
                //        p.DiedLastNight = false;
                //    }
                //    foreach (var p in Players.Where(x => x.DiedLastNight & x.DiedHuntingSK))
                //    {
                //        SendWithQueue(GetLocaleString("DefaultKilled", p.GetName(), $"{GetDescription(p.PlayerRole)} {GetLocaleString("IsDead")}"));
                //    }
                //    foreach (var p in Players.Where(x => x.DiedLastNight & !x.DiedFromWolf & !x.DiedFromKiller & !x.DiedFromHunter))
                //    {
                //        SendWithQueue(GetLocaleString("HunterKilledCultist", p.GetName()));
                //        p.DiedLastNight = false;
                //    }
                //}
                //else
                //{
                //    foreach (var p in Players.Where(x => x.DiedLastNight && x.PlayerRole != IRole.Harlot & !x.DiedFromKiller))
                //    {
                //        SendWithQueue(GetLocaleString("GenericDeathNoReveal", p.GetName()));
                //        p.DiedLastNight = false;
                //    }
                //}
                var secret = (DbGroup.ShowRoles == false);
                foreach (var p in Players.Where(x => x.DiedLastNight))
                {
                    var msg = "";
                    if (secret)
                    {
                        msg = GetLocaleString("GenericDeathNoReveal", p.GetName());
                    }
                    else
                    {
                        if (p.DiedFromWolf)
                        {
                            switch (p.PlayerRole)
                            {
                                case IRole.Detective:
                                    msg = GetLocaleString("DetectiveEaten", p.GetName());
                                    break;
                                case IRole.Drunk:
                                    msg = GetLocaleString("DrunkEaten", p.GetName());
                                    break;
                                case IRole.Fool:
                                    msg = GetLocaleString("FoolEaten", p.GetName());
                                    break;
                                case IRole.GuardianAngel:
                                    msg = GetLocaleString("GuardianEaten", p.GetName());
                                    break;
                                case IRole.Gunner:
                                    msg = GetLocaleString("GunnerEaten", p.GetName());
                                    break;
                                case IRole.Harlot:
                                    msg = GetLocaleString("HarlotEaten", p.GetName());
                                    break;
                                case IRole.Mason:
                                    msg = GetLocaleString("MasonEaten", p.GetName());
                                    break;
                                case IRole.Seer:
                                    msg = GetLocaleString("SeerEaten", p.GetName());
                                    break;
                                default:
                                    msg = GetLocaleString("DefaultEaten", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
                                    break;
                            }
                        }
                        else if (p.DiedFromKiller)
                        {
                            msg = GetLocaleString("DefaultKilled", p.GetName(), $"{GetDescription(p.PlayerRole)} {GetLocaleString("IsDead")}");
                            if (p.PlayerRole == IRole.Hunter)
                                HunterFinalShot(p, KillMthd.SerialKilled);
                        }
                        if (p.DiedFromWrongChoice)
                            switch (p.PlayerRole)
                            {
                                case IRole.Cultist:
                                    if (p.DiedFromHunter)
                                        msg = GetLocaleString("HunterKilledVisiter", p.GetName(), $"{GetDescription(p.PlayerRole)} {GetLocaleString("IsDead")}");
                                    else if (p.DiedFromKiller)
                                    {
                                        //TODO: Add string "CultistVisitedKiller"
                                    }
                                    //else
                                    //TODO: Maybe add a "CultistVisitedCH" too?
                                    break;
                                case IRole.CultistHunter:
                                    //TODO: Add string "CHVisitedSK"
                                    break;
                                case IRole.GuardianAngel:
                                    //TODO: Add strings "GAGuardedWolf", "GAGuardedSK"
                                    break;
                                case IRole.Harlot:
                                    if (p.DiedFromWolf)
                                        msg = GetLocaleString("HarlotFuckedWolfPublic", p.GetName());
                                    else if (p.DiedFromKiller)
                                    {
                                        //TODO: Use HarlotFuckedKillerPublic here
                                    }
                                    else //died from visiting wolves' victim...
                                    {
                                        var victim = Players.FirstOrDefault(x => x.DiedFromWolf && x.DiedLastNight && !x.DiedFromWrongChoice); //hoping it's not null...
                                        msg = GetLocaleString("HarlotFuckedVictimPublic", p.GetName(), victim.GetName());
                                    }
                                    break;
                                case IRole.Wolf:
                                    if (p.DiedFromKiller)
                                        msg = GetLocaleString("SerialKillerKilledWolf", p.GetName());
                                    else if (p.DiedFromHunter)
                                    {
                                        //if the wolves who were alive last night are > 1... maybe there's some cleaner / simpler way to do this
                                        if (Players.Where(x => x.PlayerRole == IRole.Wolf && (!x.IsDead || x.DiedLastNight)).Count() > 1)
                                            msg = GetLocaleString("HunterShotWolfMulti", p.GetName());
                                        else
                                            GetLocaleString("HunterShotWolf", p.GetName());
                                    }
                                    break;
                            }
                        if (p.PlayerRole == IRole.Cultist && !p.DiedFromWolf && !p.DiedFromKiller && !p.DiedFromHunter)
                            msg = GetLocaleString("HunterKilledCultist", p.GetName());
                    }
                    SendWithQueue(msg);
                    if (p.InLove)
                        KillLover(p);
                    p.DiedLastNight = false;
                }
            }
            else
            {
                if (IsRunning)
                    SendWithQueue(GetLocaleString("NoAttack"));
            }

            if (CheckForGameEnd()) return;
            //reset choices
            foreach (var p in Players)
            {
                p.Choice = 0;
                p.HasUsedAbility = false;
                p.Drunk = false;
            }
            var timeToAdd = Math.Max(((Players.Count(x => !x.IsDead) / 5) - 1) * 30, 60);

            SendWithQueue(GetLocaleString("DayTime", ((DbGroup.DayTime ?? Settings.TimeDay) + timeToAdd).ToBold()));
            SendWithQueue(GetLocaleString("Day", GameDay.ToBold()));
            SendPlayerList();

            SendDayActions();
            //incremental sleep time for large players....
            Thread.Sleep(TimeSpan.FromSeconds((DbGroup.LynchTime ?? Settings.TimeLynch) + timeToAdd));

            if (!IsRunning) return;
            try
            {
                foreach (var p in Players.Where(x => x.CurrentQuestion != null))
                {
                    try
                    {
                        if (p.CurrentQuestion.MessageId != 0)
                        {
                            Program.MessagesSent++;
                            Program.Bot.EditMessageText(p.Id, p.CurrentQuestion.MessageId, GetLocaleString("TimesUp"));
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    p.CurrentQuestion = null;
                }
            }
            catch
            {
                // ignored
            }
            //check detective
            if (Players == null) return;
            var detect = Players.FirstOrDefault(x => x.PlayerRole == IRole.Detective & !x.IsDead && x.Choice != 0 && x.Choice != -1);
            if (detect != null)
            {
                //first off, chance to tell wolves
                if (Program.R.Next(100) < Settings.ChanceDetectiveCaught)
                {
                    foreach (var w in Players.Where(x => !x.IsDead && x.PlayerRole == IRole.Wolf))
                    {
                        Send(GetLocaleString("DetectiveCaught", $"{detect.GetName()}"), w.Id);
                    }
                }

                var check = Players.FirstOrDefault(x => x.Id == detect.Choice);
                if (check != null)
                {
                    DBAction(detect, check, "Detect");
                    Send(GetLocaleString("DetectiveSnoop", check.GetName(), GetDescription(check.PlayerRole)), detect.Id);
                }
            }

            var gunner = Players.FirstOrDefault(x => x.PlayerRole == IRole.Gunner & !x.IsDead && x.Choice != 0 && x.Choice != -1);
            if (gunner != null)
            {
                var check = Players.FirstOrDefault(x => x.Id == gunner.Choice);
                if (check != null)
                {
                    //kill them
                    gunner.Bullet--;
                    check.IsDead = true;
                    if (!new[] { IRole.Wolf, IRole.Cultist, IRole.SerialKiller }.Contains(check.PlayerRole))
                        gunner.BulletHitVillager = true;
                    check.TimeDied = DateTime.Now;
                    //update database
                    DBKill(gunner, check, KillMthd.Shoot);
                    DBAction(gunner, check, "Shoot");
                    switch (check.PlayerRole)
                    {
                        case IRole.Harlot:
                            SendWithQueue(DbGroup.ShowRoles != false
                                ? GetLocaleString("HarlotShot", gunner.GetName(), check.GetName())
                                : GetLocaleString("DefaultShot", gunner.GetName(), check.GetName(), ""));
                            break;
                        case IRole.Hunter:
                            SendWithQueue(GetLocaleString("DefaultShot", gunner.GetName(), check.GetName(), DbGroup.ShowRoles == false ? "" : $"{GetDescription(check.PlayerRole)} {GetLocaleString("IsDead")}"));
                            HunterFinalShot(check, KillMthd.Shoot);
                            break;
                        default:
                            SendWithQueue(GetLocaleString("DefaultShot", gunner.GetName(), check.GetName(), DbGroup.ShowRoles == false ? "" : $"{GetDescription(check.PlayerRole)} {GetLocaleString("IsDead")}"));
                            break;
                    }
                    //check if dead was in love
                    if (check.InLove)
                        KillLover(check);
                }
            }
            CheckRoleChanges();
        }

        private void NightCycle()
        {
            if (!IsRunning) return;
            //FUN!
            Time = GameTime.Night;
            foreach (var p in Players)
                p.CurrentQuestion = null;
            if (CheckForGameEnd()) return;
            foreach (var p in Players)
            {
                p.Choice = 0;
                p.HasUsedAbility = false;
                p.Votes = 0;
                p.DiedLastNight = false;
            }
            var nightTime = (DbGroup.NightTime ?? Settings.TimeNight);
            if (GameDay == 1)
                if (Players.Any(x => new[] { IRole.Cupid, IRole.Doppelgänger, IRole.WildChild }.Contains(x.PlayerRole)))
                    nightTime = Math.Max(nightTime, 120);

            SendWithQueue(GetLocaleString("NightTime", nightTime.ToBold()));
            SendPlayerList();
            SendNightActions();

            var nightPlayers = Players.Where(x => !x.IsDead & !x.Drunk && x.HasNightAction);
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            for (var i = 0; i < nightTime; i++)
            {
                Thread.Sleep(1000);
                if (Players == null)
                {
                    CheckForGameEnd();
                    return;
                }
                //check if all votes are cast

                if (nightPlayers.All(x => x.CurrentQuestion == null))
                    break;
            }
            try
            {
                foreach (var p in Players.Where(x => x.CurrentQuestion != null))
                {
                    try
                    {
                        if (p.CurrentQuestion.MessageId != 0)
                        {
                            Program.MessagesSent++;
                            Program.Bot.EditMessageText(p.Id, p.CurrentQuestion.MessageId, GetLocaleString("TimesUp"));
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    p.CurrentQuestion = null;
                }
            }
            catch
            {
                // ignored
            }

            //if first night, make sure cupid / wc / dg have picked
            ValidateSpecialRoleChoices();


            var wolves = Players?.Where(x => x.PlayerRole == IRole.Wolf && !x.IsDead).ToList();

            if (CheckForGameEnd()) return;
            var ga = Players.FirstOrDefault(x => x.PlayerRole == IRole.GuardianAngel & !x.IsDead && x.Choice != 0 && x.Choice != -1);
            var voteWolves = Players.Where(x => x.PlayerRole == IRole.Wolf & !x.IsDead & !x.Drunk);

            if (voteWolves.Any())
            {

                foreach (var w in voteWolves)
                {
                    var wchoice = Players.FirstOrDefault(x => x.Id == w.Choice);
                    if (wchoice != null)
                    {
                        DBAction(w, wchoice, "Eat");
                    }
                }
                var votechoice = voteWolves.Where(x => x.Choice != 0 && x.Choice != 1);
                int choice = 0;
                if (votechoice.Any())
                {
                    choice = votechoice.GroupBy(x => x.Choice).OrderByDescending(x => x.Count()).First().Key;
                }

                if (choice != 0 && choice != -1)
                {
                    var target = Players.FirstOrDefault(x => x.Id == choice & !x.IsDead);
                    if (target != null)
                    {
                        {
                            if (ga?.Choice == target.Id)
                            {
                                foreach (var wolf in voteWolves)
                                    Send(GetLocaleString("GuardBlockedWolf", target.GetName()), wolf.Id);
                                //Send(GetLocaleString("GuardSaved", target.Name), ga.Id);
                                //Send(GetLocaleString("GuardSavedYou"), target.Id);
                                target.DiedLastNight = true;
                                target.DiedFromWolf = true;
                            }
                            else
                            {
                                //check if they are the harlot, and were home
                                switch (target.PlayerRole)
                                {
                                    case IRole.Harlot:
                                        if (target.Choice == 0 || target.Choice == -1)
                                        {
                                            target.DiedLastNight = true;
                                            target.IsDead = true;
                                            target.TimeDied = DateTime.Now;
                                            target.DiedFromWolf = true;
                                            DBKill(voteWolves, target, KillMthd.Eat);
                                            //SendWithQueue(DbGroup.ShowRoles != false
                                            //    ? GetLocaleString("HarlotEaten", target.GetName())
                                            //    : GetLocaleString("GenericDeathNoReveal", target.GetName()));
                                        }
                                        else
                                        {
                                            foreach (var wolf in voteWolves)
                                                Send(GetLocaleString("HarlotNotHome", target.GetName()), wolf.Id);
                                        }
                                        break;
                                    case IRole.Cursed:
                                        target.PlayerRole = IRole.Wolf;
                                        target.Team = ITeam.Wolf;
                                        target.ChangedRolesCount++;
                                        target.HasNightAction = true;
                                        target.HasDayAction = false;
                                        Send(GetLocaleString("CursedBitten"), target.Id);
                                        try
                                        {
                                            Send(
                                                GetLocaleString("WolfTeam",
                                                    wolves.Select(x => x.GetName())
                                                        .Aggregate((current, w) => current + ", " + w)),
                                                target.Id);
                                        }
                                        catch
                                        {
                                            // ignored
                                        }
                                        foreach (var w in wolves)
                                            Send(GetLocaleString("CursedBittenToWolves", $"{target.GetName()}"), w.Id);
                                        break;
                                    case IRole.Drunk:
                                        target.DiedLastNight = true;
                                        target.DiedFromWolf = true;
                                        target.IsDead = true;
                                        target.TimeDied = DateTime.Now;
                                        DBKill(voteWolves, target, KillMthd.Eat);
                                        SendGif(GetLocaleString("WolvesEatYou"),
                                            GetRandomImage(Settings.VillagerDieImages), target.Id);
                                        foreach (var w in voteWolves)
                                        {
                                            Send(GetLocaleString("WolvesEatDrunk", target.GetName()), w.Id);
                                            w.Drunk = true;
                                        }
                                        break;
                                    case IRole.Hunter:
                                        //hunter has a chance to kill....

                                        //figure out what chance they have...
                                        var chance = Settings.HunterKillWolfChanceBase + ((voteWolves.Count() - 1) * 20);
                                        if (Program.R.Next(100) < chance)
                                        {
                                            //wolf dies!
                                            IPlayer shotWuff;
                                            try
                                            {
                                                shotWuff = voteWolves.ElementAt(Program.R.Next(voteWolves.Count()));
                                            }
                                            catch
                                            {
                                                shotWuff = voteWolves.FirstOrDefault();
                                            }
                                            if (shotWuff != null)
                                            {
                                                if (voteWolves.Count() > 1)
                                                {
                                                    //SendWithQueue(GetLocaleString("HunterShotWolfMulti", shotWuff.GetName()));
                                                    SendGif(GetLocaleString("WolvesEatYou"),
                                                        GetRandomImage(Settings.VillagerDieImages), target.Id);
                                                    DBKill(voteWolves, target, KillMthd.Eat);
                                                    target.DiedFromWolf = true;
                                                    target.IsDead = true;
                                                    target.TimeDied = DateTime.Now;
                                                    target.DiedLastNight = true;
                                                }
                                                else
                                                {
                                                    //SendWithQueue(GetLocaleString("HunterShotWolf", shotWuff.GetName()));
                                                }
                                                shotWuff.IsDead = true;
                                                shotWuff.TimeDied = DateTime.Now;
                                                shotWuff.DiedFromWrongChoice = true;
                                                shotWuff.DiedFromHunter = true;
                                                shotWuff.DiedLastNight = true;
                                                DBKill(target, shotWuff, KillMthd.HunterShot);

                                            }

                                        }
                                        else
                                        {
                                            SendGif(GetLocaleString("WolvesEatYou"), GetRandomImage(Settings.VillagerDieImages), target.Id);
                                            DBKill(voteWolves, target, KillMthd.Eat);
                                            target.DiedFromWolf = true;
                                            target.IsDead = true;
                                            target.TimeDied = DateTime.Now;
                                            target.DiedLastNight = true;
                                        }


                                        break;
                                    case IRole.SerialKiller:
                                        //serial killer has 80% of winning the fight....
                                        if (Program.R.Next(100) < 80)
                                        {
                                            //serial killer wins...
                                            var shotWuff = voteWolves.ElementAt(Program.R.Next(voteWolves.Count()));
                                            shotWuff.IsDead = true;
                                            shotWuff.TimeDied = DateTime.Now;
                                            shotWuff.DiedFromWrongChoice = true;
                                            shotWuff.DiedFromKiller = true;
                                            shotWuff.DiedLastNight = true;
                                            //SendWithQueue(GetLocaleString("SerialKillerKilledWolf", shotWuff.GetName()));
                                            DBKill(target, shotWuff, KillMthd.SerialKilled);
                                        }
                                        else
                                        {
                                            target.DiedFromWolf = true;
                                            target.IsDead = true;
                                            target.TimeDied = DateTime.Now;
                                            target.DiedLastNight = true;
                                            DBKill(voteWolves, target, KillMthd.Eat);
                                            SendGif(GetLocaleString("WolvesEatYou"), GetRandomImage(Settings.VillagerDieImages), target.Id);
                                        }
                                        break;
                                    default:
                                        target.DiedFromWolf = true;
                                        target.IsDead = true;
                                        target.TimeDied = DateTime.Now;
                                        target.DiedLastNight = true;
                                        DBKill(voteWolves, target, KillMthd.Eat);
                                        SendGif(GetLocaleString("WolvesEatYou"), GetRandomImage(Settings.VillagerDieImages), target.Id);

                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        //no choice
                    }
                }
            }

            //give serial killer a chance!
            var sk = Players.FirstOrDefault(x => x.PlayerRole == IRole.SerialKiller & !x.IsDead);
            if (sk != null)
            {
                var skilled = Players.FirstOrDefault(x => x.Id == sk.Choice && !x.IsDead);
                if (skilled != null)
                {
                    skilled.DiedLastNight = true;
                    skilled.IsDead = true;
                    skilled.TimeDied = DateTime.Now;
                    skilled.DiedFromWolf = false;
                    skilled.DiedFromKiller = true;
                    if (skilled.PlayerRole == IRole.Wolf)
                        sk.SerialKilledWolvesCount++;
                    if (ga?.Choice == skilled.Id)
                    {

                        Send(GetLocaleString("GuardBlockedKiller", skilled.GetName()), sk.Id);
                        //Send(GetLocaleString("GuardSaved", target.Name), ga.Id);
                        //Send(GetLocaleString("GuardSavedYou"), target.Id);
                        skilled.IsDead = false;
                        skilled.TimeDied = DateTime.MaxValue;
                    }
                    else
                    {
                        DBKill(sk, skilled, KillMthd.SerialKilled);
                        //if (DbGroup.ShowRoles != false)
                        //    SendWithQueue(GetLocaleString("DefaultKilled", skilled.GetName(), DbGroup.ShowRoles == false ? "" : $"{GetDescription(skilled.PlayerRole)} {GetLocaleString("IsDead")}"));
                        //else
                        //    SendWithQueue(GetLocaleString("GenericDeathNoReveal", skilled.GetName()));
                        //if (skilled.PlayerRole == IRole.Hunter)
                        //{
                        //    HunterFinalShot(skilled, KillMthd.SerialKilled);
                        //}
                    }
                }
            }

            if (Players == null)
                return;
            //start with the cult...
            var voteCult = Players.Where(x => x.PlayerRole == IRole.Cultist & !x.IsDead);

            if (voteCult.Any())
            {
                foreach (var c in voteCult)
                {
                    var cchoice = Players.FirstOrDefault(x => x.Id == c.Choice);
                    if (cchoice != null)
                    {
                        DBAction(c, cchoice, "Convert");
                    }
                }
                var votechoice = voteCult.Where(x => x.Choice != 0 && x.Choice != 1);
                int choice = 0;
                if (votechoice.Any())
                {
                    choice = votechoice.GroupBy(x => x.Choice).OrderByDescending(x => x.Count()).First().Key;
                }

                if (choice != 0 && choice != -1)
                {
                    var target = Players.FirstOrDefault(x => x.Id == choice & !x.IsDead);
                    if (target != null)
                    {
                        if (!target.IsDead)
                        {
                            var newbie = voteCult.OrderByDescending(x => x.DayCult).First();
                            //check if they are the hunter
                            switch (target.PlayerRole)
                            {
                                case IRole.Hunter:
                                    //first, check if they got converted....
                                    if (Program.R.Next(100) < Settings.HunterConversionChance)
                                    {
                                        ConvertToCult(target, voteCult);
                                    }
                                    else
                                    {
                                        if (Program.R.Next(100) < Settings.HunterKillCultChance)
                                        {

                                            newbie.DiedLastNight = true;
                                            newbie.IsDead = true;
                                            newbie.TimeDied = DateTime.Now;
                                            newbie.DiedFromHunter = true;
                                            newbie.DiedFromWrongChoice = true;
                                            DBKill(target, newbie, KillMthd.HunterCult);
                                            //notify everyone
                                            foreach (var c in voteCult)
                                            {
                                                Send(GetLocaleString("CultConvertHunter", newbie.GetName(), target.GetName()), c.Id);
                                            }
                                            //if (DbGroup.ShowRoles ?? true)
                                            //{
                                            //    SendWithQueue(GetLocaleString("HunterKilledVisiter", newbie.GetName(),
                                            //        $"{GetDescription(newbie.PlayerRole)} {GetLocaleString("IsDead")}"));
                                            //}
                                            //else
                                            //{
                                            //    SendWithQueue(GetLocaleString("GenericDeathNoReveal", newbie.GetName()));
                                            //}
                                        }
                                        else
                                        {
                                            foreach (var c in voteCult)
                                            {
                                                Send(GetLocaleString("CultUnableToConvert", newbie.GetName(), target.GetName()), c.Id);
                                            }
                                            Send(GetLocaleString("CultAttempt"), target.Id);
                                        }
                                    }
                                    break;
                                case IRole.SerialKiller:
                                    //kill newest cult
                                    newbie.DiedLastNight = true;
                                    newbie.IsDead = true;
                                    newbie.TimeDied = DateTime.Now;
                                    newbie.DiedFromKiller = true;
                                    newbie.DiedFromWrongChoice = true;
                                    DBKill(target, newbie, KillMthd.SerialKilled);
                                    foreach (var c in voteCult)
                                    {
                                        Send(GetLocaleString("CultConvertSerialKiller", newbie.GetName(), target.GetName()), c.Id);
                                    }
                                    //SendWithQueue(GetLocaleString("DefaultKilled", newbie.GetName(), DbGroup.ShowRoles == false ? "" : $"{GetDescription(newbie.PlayerRole)} {GetLocaleString("IsDead")}"));
                                    break;
                                case IRole.CultistHunter:
                                    //kill the newest cult member
                                    newbie.DiedLastNight = true;
                                    newbie.IsDead = true;
                                    newbie.DiedFromWrongChoice = true;
                                    AddAchievement(newbie, Achievements.CultFodder);
                                    newbie.TimeDied = DateTime.Now;
                                    DBKill(target, newbie, KillMthd.Hunt);
                                    //notify everyone
                                    foreach (var c in voteCult)
                                    {
                                        Send(GetLocaleString("CultConvertCultHunter", newbie.GetName(), target.GetName()), c.Id);
                                    }
                                    Send(GetLocaleString("CultHunterKilledCultVisit", newbie.GetName(), voteCult.Count()), target.Id);
                                    break;
                                case IRole.Seer:
                                    if (Program.R.Next(100) < Settings.SeerConversionChance)
                                        ConvertToCult(target, voteCult);
                                    else
                                    {
                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultUnableToConvert", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                        Send(GetLocaleString("CultAttempt"), target.Id);
                                    }
                                    break;
                                case IRole.GuardianAngel:
                                    if (target.Choice == 0 || target.Choice == -1) // stayed home
                                    {
                                        if (Program.R.Next(100) < Settings.GuardianAngelConversionChance)
                                            ConvertToCult(target, voteCult);
                                        else
                                        {
                                            foreach (var c in voteCult)
                                            {
                                                Send(GetLocaleString("CultUnableToConvert", newbie.GetName(), target.GetName()), c.Id);
                                            }
                                            Send(GetLocaleString("CultAttempt"), target.Id);
                                        }
                                    }
                                    else
                                    {
                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultVisitEmpty", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                        Send(GetLocaleString("CultAttempt"), target.Id);
                                    }
                                    break;
                                case IRole.Detective:
                                    if (Program.R.Next(100) < Settings.DetectiveConversionChance)
                                        ConvertToCult(target, voteCult);
                                    else
                                    {
                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultUnableToConvert", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                        Send(GetLocaleString("CultAttempt"), target.Id);
                                    }
                                    break;
                                case IRole.Cursed:
                                    if (Program.R.Next(100) < Settings.CursedConversionChance)
                                        ConvertToCult(target, voteCult);
                                    else
                                    {
                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultUnableToConvert", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                        Send(GetLocaleString("CultAttempt"), target.Id);
                                    }
                                    break;
                                case IRole.Harlot:
                                    if (target.Choice == 0 || target.Choice == -1) // stayed home
                                    {
                                        if (Program.R.Next(100) < Settings.HarlotConversionChance)
                                            ConvertToCult(target, voteCult);
                                        else
                                        {
                                            foreach (var c in voteCult)
                                            {
                                                Send(GetLocaleString("CultUnableToConvert", newbie.GetName(), target.GetName()), c.Id);
                                            }
                                            Send(GetLocaleString("CultAttempt"), target.Id);
                                        }
                                    }
                                    else
                                    {
                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultVisitEmpty", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                        Send(GetLocaleString("CultAttempt"), target.Id);
                                    }
                                    break;
                                case IRole.Mason:
                                    //notify other masons....
                                    ConvertToCult(target, voteCult);
                                    foreach (var m in Players.Where(x => x.PlayerRole == IRole.Mason & !x.IsDead))
                                        Send(GetLocaleString("MasonConverted", target.GetName()), m.Id);
                                    break;
                                case IRole.Doppelgänger:
                                    foreach (var c in voteCult)
                                    {
                                        Send(GetLocaleString("CultUnableToConvert", newbie.GetName(), target.GetName()), c.Id);
                                    }
                                    Send(GetLocaleString("CultAttempt"), target.Id);
                                    break;
                                case IRole.Wolf:
                                    if (voteWolves.Any(x => x.Choice != 0 && x.Choice != 1)) //did wolves go eating?
                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultVisitEmpty", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                    else //stayed home!
                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultUnableToConvert", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                    Send(GetLocaleString("CultAttempt"), target.Id);
                                    break;
                                default:
                                    ConvertToCult(target, voteCult);
                                    break;
                            }
                        }
                        else
                        {
                            foreach (var c in voteCult)
                                Send(GetLocaleString("CultTargetDead", target.GetName()), c.Id);
                        }
                    }
                    else
                    {
                        //no choice
                    }
                    if (CheckForGameEnd()) return;
                }
            }

            //cult hunter
            var hunter = Players.FirstOrDefault(x => x.PlayerRole == IRole.CultistHunter & !x.IsDead);
            if (hunter != null)
            {
                var hunted = Players.FirstOrDefault(x => x.Id == hunter.Choice);
                if (hunted != null)
                {
                    DBAction(hunter, hunted, "Hunt");
                    if (hunted.PlayerRole == IRole.SerialKiller)
                    {
                        //awwwwww CH gets popped
                        DBKill(hunted, hunter, KillMthd.SerialKilled);
                        hunter.IsDead = true;
                        hunter.TimeDied = DateTime.Now;
                        hunter.DiedLastNight = true;
                        hunter.DiedFromKiller = true;
                        hunter.DiedFromWrongChoice = true;
                    }
                    else if (hunted.IsDead && hunted.DiedFromWolf && hunted.DiedFromKiller) //added DiedFromSK here... but, does this really matter? isn't IsDead enough?
                    {
                        Send(GetLocaleString("HunterVisitDead", hunted.GetName()), hunter.Id);
                    }
                    else if (hunted.PlayerRole == IRole.Cultist)
                    {
                        Send(GetLocaleString("HunterFindCultist", hunted.GetName()), hunter.Id);
                        hunted.IsDead = true;
                        hunted.TimeDied = DateTime.Now;
                        hunted.DiedLastNight = true;
                        DBKill(hunter, hunted, KillMthd.Hunt);
                    }
                    else
                    {
                        Send(GetLocaleString("HunterFailedToFind", hunted.GetName()), hunter.Id);
                    }
                }
            }

            if (Players == null)
            {
                CheckForGameEnd();
                return;
            }
            if (ga != null)
            {
                var save = Players.FirstOrDefault(x => x.Id == ga.Choice);
                if (save != null)
                    DBAction(ga, save, "Guard");

                if (save.DiedLastNight)
                //removed !save.DiedFromLove here... because if they are DiedFromLove they aren't DiedLastNight. This was actually the only use of DiedFromLove, so I removed it from the class
                {
                    if (save.PlayerRole == IRole.Cultist & !save.DiedFromWolf & !save.DiedFromKiller) // -> has been killed by CH
                    {
                        Send(GetLocaleString("GuardEmptyHouse", save.GetName()), ga.Id);
                    }
                    else
                    {
                        save.IsDead = false;
                        save.TimeDied = DateTime.MaxValue;
                        save.DiedLastNight = false;
                        save.DiedFromWolf = false;
                        save.DiedFromKiller = false;
                        save.DiedFromWrongChoice = false;
                        using (var db = new WWContext())
                        {
                            var dbsp = GetDBGamePlayer(save, db);
                            if (dbsp != null)
                                dbsp.Survived = true;
                            db.SaveChanges();
                        }
                        Send(GetLocaleString("GuardSaved", save.GetName()), ga.Id);
                        Send(GetLocaleString("GuardSavedYou"), save.Id);
                    }
                }
                if (save.PlayerRole == IRole.Wolf) //removed "else" here, otherwise GA can't die guarding WW from SK or vice versa
                {
                    if (Program.R.Next(100) > 50)
                    {
                        ga.IsDead = true;
                        ga.TimeDied = DateTime.Now;
                        ga.DiedLastNight = true;
                        ga.DiedFromWolf = true;
                        ga.DiedFromWrongChoice = true;
                        DBKill(save, ga, KillMthd.GuardWolf);
                        Send(GetLocaleString("GuardWolf"), ga.Id);
                    }
                    else
                    {
                        Send(GetLocaleString("GuardNoAttack", save.GetName()), ga.Id);
                    }
                }
                else if (save.PlayerRole == IRole.SerialKiller)
                {
                    //oops, GA is dead
                    ga.IsDead = true;
                    ga.TimeDied = DateTime.Now;
                    ga.DiedLastNight = true;
                    ga.DiedFromKiller = true;
                    ga.DiedFromWrongChoice = true;
                    DBKill(save, ga, KillMthd.GuardKiller);
                    Send(GetLocaleString("GuardKiller"), ga.Id);
                    //SendWithQueue(GetLocaleString("DefaultKilled", ga.GetName(), DbGroup.ShowRoles == false ? "" : $"{GetDescription(ga.PlayerRole)} {GetLocaleString("IsDead")}"));
                }
                else if (!save.DiedLastNight)
                {
                    Send(GetLocaleString("GuardNoAttack", save.GetName()), ga.Id);
                }
            }
            //let the seer know
            var seers = Players.Where(x => x.PlayerRole == IRole.Seer & !x.IsDead);
            if (seers.Any())
            {
                foreach (var seer in seers)
                {
                    var target = Players.FirstOrDefault(x => x.Id == seer.Choice);
                    if (target != null)
                    {
                        DBAction(seer, target, "See");
                    }
                    if (!seer.IsDead && seer.Choice != 0 && seer.Choice != -1)
                    {
                        if (target != null)
                        {
                            var role = target.PlayerRole;
                            if (target.PlayerRole == IRole.Beholder)
                                AddAchievement(seer, Achievements.ShouldHaveKnown);
                            if (target.PlayerRole == IRole.Traitor)
                            {
                                role = Program.R.Next(100) > 50 ? IRole.Wolf : IRole.Villager;
                            }
                            Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(role)), seer.Id);
                        }
                    }
                }
            }
            var fool = Players.FirstOrDefault(x => x.PlayerRole == IRole.Fool & !x.IsDead);
            if (fool != null)
                if (!fool.IsDead && fool.Choice != 0 && fool.Choice != -1)
                {
                    var target = Players.FirstOrDefault(x => x.Id == fool.Choice);
                    var possibleRoles = Players.Where(x => !x.IsDead && x.Id != fool.Id && x.PlayerRole != IRole.Seer).Select(x => x.PlayerRole).ToList();
                    possibleRoles.Shuffle();
                    possibleRoles.Shuffle();
                    possibleRoles.Shuffle();
                    possibleRoles.Shuffle();
                    possibleRoles.Shuffle();
                    if (possibleRoles.Any())
                    {
                        //check if it's accurate
                        if (possibleRoles[0] == target.PlayerRole)
                            fool.FoolCorrectSeeCount++;
                        Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(possibleRoles[0])), fool.Id);
                    }
                }


            //let the harlot know
            var harlot = Players.FirstOrDefault(x => x.PlayerRole == IRole.Harlot & !x.IsDead);
            if (harlot != null)
            {
                var target = Players.FirstOrDefault(x => x.Id == harlot.Choice);
                if (target != null)
                {
                    DBAction(harlot, target, "Fuck");
                    if (harlot.PlayersVisited.Contains(target.TeleUser.Id))
                        harlot.HasRepeatedVisit = true;

                    harlot.PlayersVisited.Add(target.TeleUser.Id);
                }
                else
                {
                    //stayed home D:
                    harlot.HasStayedHome = true;
                }
                if (!harlot.IsDead)
                {

                    if (target != null)
                    {
                        if (target.PlayerRole == IRole.Wolf ||
                            (target.DiedLastNight && target.DiedFromWolf && !target.DiedFromWrongChoice)) //it's wolves who ate them, not them choosing the wolves
                        //if choice is 0, they did not leave their house last night, so presumably they are not the wolf
                        {
                            harlot.IsDead = true;
                            harlot.TimeDied = DateTime.Now;
                            harlot.DiedLastNight = true;
                            harlot.DiedFromWrongChoice = true;
                            if (target.PlayerRole == IRole.Wolf)
                            {
                                DBKill(target, harlot, KillMthd.VisitWolf);
                                harlot.DiedFromWolf = true;
                                //Send($"You slip into a villagers house, and are confronted by a large wolf.  He sees you, a slutty harlot, and you notice his sheath expand in excitement.  He grabs you, throws you on the bed, and proceeds to ram you until he is done...  then eats you as an after snack.", harlot.Id);
                                Send(GetLocaleString("HarlotFuckWolf", target.GetName()), harlot.Id);
                                //SendWithQueue(DbGroup.ShowRoles != false
                                //    ? GetLocaleString("HarlotFuckedWolfPublic", harlot.GetName())
                                //    : GetLocaleString("GenericDeathNoReveal", harlot.GetName()));
                            }
                            else
                            {
                                DBKill(target, harlot, KillMthd.VisitVictim);
                                //Send($"{harlot.Name} slips into {target.Name}'s house, ready to have a little fun to ease off the stress.  However, as they do, they find themselves staring at a werewolf, tearing apart {target.Name}'s corpse, his huge knot hanging out in his excitement.  He turns and sees them, grabs their face and shoves it between his legs as he continues gorging himself on the corpse.\nAs he cums, he presses them so hard into his fur, they suffocate.  Subsequently, {harlot.Name} is eated.");
                                //SendWithQueue(DbGroup.ShowRoles != false
                                //    ? GetLocaleString("HarlotFuckedVictimPublic", harlot.GetName(), target.GetName())
                                //    : GetLocaleString("GenericDeathNoReveal", harlot.GetName()));
                            }
                        }
                        else if (target.PlayerRole == IRole.Cultist)
                        {
                            Send(
                                Program.R.Next(100) < Settings.HarlotDiscoverCultChance
                                    ? GetLocaleString("HarlotDiscoverCult", target.GetName())
                                    : GetLocaleString("HarlotVisitNonWolf", target.GetName()), harlot.Id);
                        }
                        else if (target.PlayerRole == IRole.SerialKiller)
                        {
                            //oops, harlot dead....
                            harlot.IsDead = true;
                            harlot.TimeDied = DateTime.Now;
                            harlot.DiedLastNight = true;
                            harlot.DiedFromWrongChoice = true;
                            harlot.DiedFromKiller = true;
                            DBKill(target, harlot, KillMthd.VisitKiller);
                            Send(GetLocaleString("HarlotFuckKiller", target.GetName()), harlot.Id);
                            //if (DbGroup.ShowRoles != false)
                            //    SendWithQueue(GetLocaleString("DefaultKilled", harlot.GetName(),
                            //        $"{harlot.GetName()} {GetLocaleString("Was")} {GetDescription(harlot.PlayerRole)}"));
                            //else
                            //    SendWithQueue(GetLocaleString("GenericDeathNoReveal", harlot.GetName()));
                        }
                        else
                        {
                            Send(GetLocaleString("HarlotVisitNonWolf", target.GetName()), harlot.Id);
                        }
                        Send(GetLocaleString("HarlotVisitYou"), target.Id);
                    }
                }
            }
            CheckRoleChanges();

            //reset everything
            foreach (var p in Players)
            {
                p.Choice = 0;
                p.HasUsedAbility = false;
                p.Votes = 0;
            }
        }

        private bool CheckForGameEnd()
        {
            if (Players == null)
                return true;

            if (Players.All(x => x.IsDead))
                return DoGameEnd(ITeam.NoOne);
            if (Players.Where(x => !x.IsDead).All(x => x.InLove) && Players.Count(x => !x.IsDead) == 2)
            {
                return DoGameEnd(ITeam.Lovers);
            }
            //are all wolves dead?
            if (Players != null && Players.Where(x => x.Team == ITeam.Wolf).All(x => x.IsDead))
            {
                var traitor = Players.FirstOrDefault(x => x.PlayerRole == IRole.Traitor & !x.IsDead);
                if (traitor != null)
                {
                    traitor.PlayerRole = IRole.Wolf;
                    traitor.Team = ITeam.Wolf;
                    traitor.HasDayAction = false;
                    traitor.HasNightAction = true;
                    traitor.ChangedRolesCount++;
                    Send(GetLocaleString("TraitorTurnWolf"), traitor.Id);
                }
                else if (Players.Count(x => x.PlayerRole == IRole.Cultist & !x.IsDead) == 0 && Players.Count(x => !x.IsDead && x.PlayerRole == IRole.SerialKiller) == 0)
                {
                    return DoGameEnd(ITeam.Village);
                }
            }
            //do the wolves outnumber the villagers? (1:1 ratio)
            if ((Players != null && Players.Count(x => x.Team != ITeam.Wolf & !x.IsDead) <= Players.Count(x => x.Team == ITeam.Wolf && !x.IsDead)) && Players.Where(x => !x.IsDead).All(x => x.PlayerRole != IRole.SerialKiller))
            {
                if (Players.Any(x => x.PlayerRole == IRole.Gunner && x.Bullet > 0 & !x.IsDead))
                {
                    // do nothing, gunner is alive
                    return false;
                }
                if (Players.Any(x => !x.IsDead && x.PlayerRole == IRole.Wolf & x.Drunk) &&
                    Players.Count(x => x.IsDead) > 2)  //what the hell was my logic here....  damn myself for not commenting this line. why would it matter if 2 players ARE dead?
                {
                    //do nothing
                    // ^ why? Commenting next line out until we find out
                    //return false;
                }
                return DoGameEnd(ITeam.Wolf);
            }

            //is everyone left a cultist?
            if (Players != null && Players.Where(x => !x.IsDead).All(x => x.Team == ITeam.Cult))
                return DoGameEnd(ITeam.Cult);



            if (Players != null && Players.Count(x => !x.IsDead) == 2)
            {
                if (Players.Any(x => !x.IsDead && x.PlayerRole == IRole.Cultist) & !Players.Any(x => !x.IsDead && x.PlayerRole == IRole.CultistHunter) & !Players.Any(x => !x.IsDead && x.PlayerRole == IRole.Wolf))
                {
                    if (Players.Any(x => !x.IsDead && x.PlayerRole == IRole.SerialKiller))
                        return DoGameEnd(ITeam.SerialKiller);
                    //cult outnumbers, win
                    return DoGameEnd(ITeam.Cult);
                }
                if (Players.Any(x => !x.IsDead && x.PlayerRole == IRole.SerialKiller))
                {
                    //check for hunter
                    if (Players.Any(x => !x.IsDead && x.PlayerRole == IRole.Hunter))
                    {
                        return DoGameEnd(ITeam.SKHunter);
                    }
                    return DoGameEnd(ITeam.SerialKiller);
                }

            }

            if (Players != null && Players.Count(x => !x.IsDead) == 1)
            {
                var firstOrDefault = Players.FirstOrDefault(x => !x.IsDead);
                return firstOrDefault != null && DoGameEnd(firstOrDefault.Team);
            }
            return false;
        }

        private bool DoGameEnd(ITeam team)
        {
            using (var db = new WWContext())
            {
                //Log.WriteLine($"Doing game end.  IsRunning: {IsRunning}");
                if (!IsRunning) return true;
                IsRunning = false;
                var msg = "";

                var game = db.Games.FirstOrDefault(x => x.Id == GameId) ?? new Game();
                game.TimeEnded = DateTime.Now;

                if (team == ITeam.Lovers)
                {
                    var lovers = Players.Where(x => x.InLove);
                    var forbidden = lovers.Any(x => x.PlayerRole == IRole.Wolf) && lovers.Any(x => x.PlayerRole == IRole.Villager);
                    foreach (var w in lovers)
                    {
                        if (forbidden)
                            AddAchievement(w, Achievements.ForbiddenLove);
                        w.Won = true;
                        var p = GetDBGamePlayer(w, db);
                        p.Won = true;
                    }
                }
                else
                {
                    foreach (var w in Players.Where(x => x.Team == team))
                    {
                        //for sk, only let the one that is alive win
                        if (team == ITeam.SerialKiller && w.IsDead)
                            continue;

                        //same with tanner, but this is a little trickier
                        if (team == ITeam.Tanner && Players.Count(x => x.PlayerRole == IRole.Tanner) > 1)
                        {
                            //get the last tanner alive
                            var lastTanner =
                                Players.Where(x => x.PlayerRole == IRole.Tanner)
                                    .OrderByDescending(x => x.TimeDied)
                                    .Select(x => x.Id).FirstOrDefault();
                            //compare to this player
                            if (w.Id != lastTanner)
                                continue;
                        }


                        w.Won = true;
                        var p = GetDBGamePlayer(w, db);
                        p.Won = true;
                        if (w.InLove)
                        {
                            //find lover
                            var lover = Players.FirstOrDefault(x => x.Id == w.LoverId);
                            if (lover != null)
                            {
                                lover.Won = true;
                                GetDBGamePlayer(lover, db).Won = true;
                            }
                        }
                    }
                }
                switch (team)
                {
                    case ITeam.NoOne:
                        msg += GetLocaleString("NoWinner");
                        game.Winner = "NoOne";
                        SendWithQueue(msg, GetRandomImage(Settings.NoWinner));
                        break;

                    case ITeam.Wolf:
                        if (Players.Count(x => !x.IsDead && x.PlayerRole == IRole.Wolf) > 1)
                        {
                            msg += GetLocaleString("WolvesWin");
                            game.Winner = "Wolves";
                            SendWithQueue(msg, GetRandomImage(Settings.WolvesWin));
                        }
                        else
                        {
                            msg += GetLocaleString("WolfWins");
                            game.Winner = "Wolf";
                            SendWithQueue(msg, GetRandomImage(Settings.WolfWin));
                        }
                        break;
                    case ITeam.Tanner:
                        msg += GetLocaleString("TannerWins");
                        game.Winner = "Tanner";
                        SendWithQueue(msg, GetRandomImage(Settings.TannerWin));
                        break;
                    case ITeam.Cult:
                        msg += GetLocaleString("CultWins");
                        game.Winner = "Cult";
                        SendWithQueue(msg, GetRandomImage(Settings.CultWins)); //, GetRandomImage(Program.VillagersWin));
                        break;
                    case ITeam.SerialKiller:
                        if (Players.Count(x => !x.IsDead) > 1)
                        {
                            var alive = Players.Where(x => !x.IsDead);
                            var otherPerson = alive.FirstOrDefault(x => x.PlayerRole != IRole.SerialKiller);
                            var sk = alive.FirstOrDefault(x => x.PlayerRole == IRole.SerialKiller);
                            SendWithQueue(GetLocaleString("SerialKillerWinsOverpower", sk.GetName(), otherPerson.GetName()));
                            DBKill(sk, otherPerson, KillMthd.SerialKilled);
                            if (otherPerson != null)
                            {
                                otherPerson.IsDead = true;
                                otherPerson.TimeDied = DateTime.Now;
                            }
                        }
                        msg += GetLocaleString("SerialKillerWins");
                        game.Winner = "SerialKiller";
                        SendWithQueue(msg, GetRandomImage(Settings.SerialKillerWins));
                        break;
                    case ITeam.Lovers:
                        msg += GetLocaleString("LoversWin");
                        game.Winner = "Lovers";
                        SendWithQueue(msg, GetRandomImage(Settings.LoversWin));
                        break;
                    case ITeam.SKHunter:
                        var skhunter = Players.Where(x => !x.IsDead);
                        var hunter = skhunter.FirstOrDefault(x => x.PlayerRole != IRole.SerialKiller);
                        var skh = skhunter.FirstOrDefault(x => x.PlayerRole == IRole.SerialKiller);
                        msg += GetLocaleString("NoWinner");
                        game.Winner = "NoOne";
                        AddAchievement(skh, Achievements.DoubleKill);
                        AddAchievement(hunter, Achievements.DoubleKill);
                        DBKill(skh, hunter, KillMthd.SerialKilled);
                        DBKill(hunter, skh, KillMthd.HunterShot);
                        if (skh != null)
                        {
                            skh.IsDead = true;
                            skh.TimeDied = DateTime.Now;
                            if (hunter != null)
                            {
                                hunter.IsDead = true;
                                hunter.TimeDied = DateTime.Now;
                                SendWithQueue(GetLocaleString("SKHunterEnd", skh.GetName(), hunter.GetName()));
                            }
                        }
                        SendWithQueue(msg, GetRandomImage(Settings.NoWinner));
                        break;
                    default: //village
                        msg += GetLocaleString("VillageWins");
                        game.Winner = "Village";
                        SendWithQueue(msg, GetRandomImage(Settings.VillagersWin));
                        break;
                }
                db.SaveChanges();
                switch (DbGroup.ShowRolesEnd)
                {
                    case "None":
                        msg = $"{GetLocaleString("PlayersAlive")}: {Players.Count(x => !x.IsDead)} / {Players.Count()}\n" +
                       Players.OrderBy(x => x.TimeDied)
                           .Aggregate(msg,
                               (current, p) => current + $"\n{p.GetName()}");
                        break;
                    case "All":
                        msg = $"{GetLocaleString("PlayersAlive")}: {Players.Count(x => !x.IsDead)} / {Players.Count()}\n" +
                             Players.OrderBy(x => x.TimeDied)
                                 .Aggregate("",
                                     (current, p) =>
                                         current +
                                         ($"{p.GetName()}: {(p.IsDead ? (p.Fled ? GetLocaleString("RanAway") : GetLocaleString("Dead")) : GetLocaleString("Alive")) + " - " + GetDescription(p.PlayerRole) + (p.InLove ? "❤️" : "")} {(p.Won ? GetLocaleString("Won") : GetLocaleString("Lost"))}\n"));
                        break;
                    default:
                        msg = GetLocaleString("RemainingPlayersEnd") + Environment.NewLine;
                        msg = Players.Where(x => !x.IsDead)
                            .OrderBy(x => x.Team)
                            .Aggregate(msg,
                                (current, p) => current + $"\n{p.GetName()}: {GetDescription(p.PlayerRole)} ({p.Team} Team) {(p.InLove ? "❤️" : "")} {GetLocaleString(p.Won ? "Won" : "Lost")}");
                        break;
                }
                if (game.TimeStarted.HasValue)
                {
                    var endGame = game.TimeEnded.Value - game.TimeStarted.Value;
                    msg += "\n" + GetLocaleString("EndTime", endGame.ToString(@"hh\:mm\:ss"));
                }
                SendWithQueue(msg);
                Program.Bot.SendTextMessage(ChatId,
                    "[Enjoy playing? Support the developers and get some swag!](https://teespring.com/stores/werewolf-for-telegram)",
                    parseMode: ParseMode.Markdown, disableWebPagePreview: true);
                UpdateAchievements();

                Thread.Sleep(10000);
                Program.RemoveGame(this);
                return true;
            }
        }



        #endregion

        #region Send Menus

        private void SendLynchMenu()
        {
            if (Players == null)
                return;
            foreach (var player in Players.Where(x => !x.IsDead).OrderBy(x => x.Name))
            {
                player.CurrentQuestion = null;
                player.Choice = 0;
                var choices = Players.Where(x => !x.IsDead && x.Id != player.Id).Select(x => new[] { new InlineKeyboardButton(x.Name, $"vote|{Program.ClientId.ToString()}|{x.Id}") }).ToList();
                SendMenu(choices, player, GetLocaleString("AskLynch"), QuestionType.Lynch);
                Thread.Sleep(100);
            }
        }

        private void SendMenu(List<InlineKeyboardButton[]> choices, IPlayer to, string text, QuestionType qtype)
        {
            choices = choices.ToList();
            var skip = choices.FirstOrDefault(x => x[0].Text == "Skip");

            if (skip != null)
            {
                var index = choices.IndexOf(skip);
                skip = choices[index];
                choices.Remove(skip);
                choices.Add(skip);
            }
            var menu = new InlineKeyboardMarkup(choices.ToArray());
            try
            {
                var msgId = 0;

                try
                {
                    var result = Program.Send(text, to.Id, false, menu).Result;
                    msgId = result.MessageId;
                }
                catch (AggregateException ex)
                {
                    var e = ex.InnerExceptions.First();
                    Console.WriteLine($"Error getting menu send result: {e.Message} {to.TeleUser.Username} {to.Name}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error getting menu send result: {e.Message} {to.TeleUser.Username} {to.Name}");
                }
                to.CurrentQuestion = (new QuestionAsked
                {
                    QType = qtype,
                    ValidAnswers = choices.Select(x => x[0].Text).ToArray(),
                    MessageId = msgId
                });
            }
            catch (AggregateException ex)
            {
                var e = ex.InnerExceptions.First();
                Send(e.Message, to.Id);
                Console.WriteLine($"Error sending menu: {e.Message}");
            }
            catch (Exception e)
            {
                Send(e.Message, to.Id);
                Console.WriteLine($"Error sending menu: {e.Message}");
            }
        }

        private void SendDayActions()
        {
            if (Players == null) return;
            foreach (var p in Players)
            {
                p.CurrentQuestion = null;
                p.Choice = 0;
            }
            Thread.Sleep(1000); //sleep to let any clear keyboard messages go through....
            //really, the only day action is the detective, if there is one...
            var detective = Players.FirstOrDefault(x => x != null && x.PlayerRole == IRole.Detective & !x.IsDead);
            if (detective != null)
            {
                detective.Choice = 0;
                var options = Players.Where(x => !x.IsDead && x.Id != detective.Id).ToList();
                if (options.Any())
                {
                    var choices = options.Select(x => new[] { new InlineKeyboardButton(x.Name, $"vote|{Program.ClientId}|{x.Id}") }).ToList();
                    choices.Add(new[] { new InlineKeyboardButton("Skip", $"vote|{Program.ClientId}|-1") });
                    SendMenu(choices, detective, GetLocaleString("AskDetect"), QuestionType.Detect);
                }
            }

            var gunner = Players.FirstOrDefault(x => x.PlayerRole == IRole.Gunner & !x.IsDead);

            if (gunner != null)
            {
                gunner.Choice = 0;
                if (gunner.Bullet > 0)
                {
                    var options = Players.Where(x => !x.IsDead && x.Id != gunner.Id).ToList();
                    if (options.Any())
                    {
                        var choices = options.Select(x => new[] { new InlineKeyboardButton(x.Name, $"vote|{Program.ClientId}|{x.Id}") }).ToList();
                        choices.Add(new[] { new InlineKeyboardButton("Skip", $"vote|{Program.ClientId}|-1") });
                        SendMenu(choices, gunner, GetLocaleString("AskShoot", gunner.Bullet), QuestionType.Shoot);
                    }
                }
            }
        }

        private void SendNightActions()
        {

            Thread.Sleep(1000); //sleep to let any clear keyboard messages go through....
            foreach (var player in Players.Where(x => !x.IsDead))
            {
                player.CurrentQuestion = null;
                player.Choice = 0;
                string msg = "";
                var targetBase = Players.Where(x => !x.IsDead && x.Id != player.Id).ToList();

                List<IPlayer> targets = new List<IPlayer>();

                QuestionType qtype = QuestionType.Lynch;
                switch (player.PlayerRole)
                {
                    case IRole.SerialKiller:
                        targets = targetBase.ToList();
                        msg = GetLocaleString("AskKill");
                        qtype = QuestionType.SerialKill;
                        break;
                    case IRole.Harlot:
                        targets = targetBase.ToList();
                        msg = GetLocaleString("AskVisit");
                        qtype = QuestionType.Visit;
                        break;
                    case IRole.Fool:
                    case IRole.Seer:
                        targets = targetBase.ToList();
                        msg = GetLocaleString("AskSee");
                        qtype = QuestionType.See;
                        break;
                    case IRole.GuardianAngel:
                        targets = targetBase.ToList();
                        msg = GetLocaleString("AskGuard");
                        qtype = QuestionType.Guard;
                        break;
                    case IRole.Wolf:
                        targets = targetBase.Where(x => x.PlayerRole != IRole.Wolf).ToList();
                        msg = GetLocaleString("AskEat");
                        var others = targetBase.Where(x => x.PlayerRole == IRole.Wolf & !x.Drunk).ToList();
                        if (others.Any())
                        {
                            var andStr = $" {GetLocaleString("And").Trim()} ";
                            msg += GetLocaleString("DiscussWith", others.Select(x => x.GetName(true)).Aggregate((current, a) => current + andStr + a));
                        }
                        qtype = QuestionType.Kill;
                        break;
                    case IRole.Cultist:
                        //if (GameDay % 2 == 1)
                        {
                            targets =
                                targetBase.Where(x => x.PlayerRole != IRole.Cultist)
                                    .ToList();
                            msg = GetLocaleString("AskConvert");
                            var otherCults = targetBase.Where(x => x.PlayerRole == IRole.Cultist).ToList();
                            if (otherCults.Any())
                            {
                                var andStr = GetLocaleString("And");
                                msg += GetLocaleString("DiscussWith", otherCults.Select(x => x.GetName(true)).Aggregate((current, a) => current + andStr + a));
                            }
                            qtype = QuestionType.Convert;
                        }
                        //else
                        //{
                        //    player.Choice = -1;
                        //}
                        break;
                    case IRole.CultistHunter:
                        targets = targetBase.ToList();
                        msg = GetLocaleString("AskHunt");
                        qtype = QuestionType.Hunt;
                        break;
                    case IRole.WildChild:
                        if (GameDay == 1)
                        {
                            targets = targetBase.ToList();
                            msg = GetLocaleString("AskRoleModel");
                            qtype = QuestionType.RoleModel;
                        }
                        else player.Choice = -1;
                        break;
                    case IRole.Doppelgänger:
                        if (GameDay == 1)
                        {
                            targets = targetBase.ToList();
                            msg = GetLocaleString("AskDoppelganger");
                            qtype = QuestionType.RoleModel;
                        }
                        else player.Choice = -1;
                        break;
                    case IRole.Cupid:
                        //this is a bit more difficult....
                        if (GameDay == 1)
                        {
                            targets = Players.Where(x => !x.IsDead).ToList();
                            msg = GetLocaleString("AskCupid1");
                            qtype = QuestionType.Lover1;
                        }
                        else player.Choice = -1;
                        break;
                    default:
                        continue;
                }
                var buttons =
                    targets.Select(
                        x => new[] { new InlineKeyboardButton(x.Name, $"vote|{Program.ClientId}|{x.Id}") }).ToList();
                if (player.PlayerRole != IRole.WildChild && player.PlayerRole != IRole.Cupid && player.PlayerRole != IRole.Doppelgänger)
                    buttons.Add(new[] { new InlineKeyboardButton("Skip", $"vote|{Program.ClientId}|-1") });

                if (!player.Drunk && !String.IsNullOrWhiteSpace(msg))
                {
                    SendMenu(buttons, player, msg, qtype);
                    Thread.Sleep(100);
                }
                else
                {
                    player.Choice = -1;
                }
                //reset drunk status
                player.Drunk = false;
            }
        }

        #endregion

        #region Helpers
        public void FleePlayer(int banid)
        {
            var p = Players?.FirstOrDefault(x => x.Id == banid);
            if (p != null)
            {

                if (p.IsDead)
                {
                    return;
                }
                SendWithQueue(GetLocaleString("Flee", p.GetName()));

                if (IsRunning)
                {
                    //kill the player
                    p.IsDead = true;
                    p.TimeDied = DateTime.Now;
                    p.Fled = true;
                    if (DbGroup.ShowRoles != false)
                        SendWithQueue(GetLocaleString("PlayerRoleWas", p.GetName(), GetDescription(p.PlayerRole)));
                    CheckRoleChanges();

                    //add the 'kill'
                    DBKill(p, p, KillMthd.Flee);
                    CheckForGameEnd();
                }
                else if (IsJoining)
                {
                    PlayerListChanged = true;
                    Players.Remove(p);
                    SendWithQueue(GetLocaleString("CountPlayersRemain", Players.Count.ToBold()));
                }
            }
        }

        public void Kill()
        {
            //forces game to exit
            Send("Game removed");
            Program.RemoveGame(this);
        }

        private string GetRandomImage(List<string> input)
        {
            return input[Program.R.Next(0, input.Count)];
        }

        public void SkipVote()
        {
            foreach (var p in Players.Where(x => x.Choice == 0))
                p.Choice = -1;
        }

        public void HunterFinalShot(IPlayer hunter, KillMthd method)
        {
            CheckRoleChanges();

            //send a menu to the hunter, asking who he wants to kill as he is hung....
            var hunterChoices = new List<InlineKeyboardButton[]>();
            hunterChoices.AddRange(
                Players.Where(x => !x.IsDead).Select(x => new[] { new InlineKeyboardButton(x.Name, $"vote|{Program.ClientId}|{x.Id}") }));

            //raise hunter from dead long enough to shoot
            hunter.IsDead = false;

            SendMenu(hunterChoices, hunter, GetLocaleString(method == KillMthd.Lynch ? "HunterLynchedChoice" : "HunterShotChoice"), QuestionType.HunterKill);

            hunter.Choice = 0;
            //hunter gets 30 seconds to choose
            for (int i = 0; i < 30; i++)
            {
                if (hunter.Choice != 0)
                {
                    i = 30;
                }
                Thread.Sleep(1000);
            }
            hunter.IsDead = true;
            if (hunter.Choice == 0)
            {

                SendWithQueue(GetLocaleString(method == KillMthd.Lynch ? "HunterNoChoiceLynched" : "HunterNoChoiceShot", hunter.GetName()));
            }
            else
            {
                if (hunter.Choice == -1)
                {
                    SendWithQueue(GetLocaleString(method == KillMthd.Lynch ? "HunterSkipChoiceLynched" : "HunterSkipChoiceShot", hunter.GetName()));
                }
                else
                {
                    //someone has been killed.....
                    var killed = Players.FirstOrDefault(x => x.Id == hunter.Choice);
                    if (killed != null)
                    {
                        SendWithQueue(GetLocaleString(method == KillMthd.Lynch ? "HunterKilledFinalLynched" : "HunterKilledFinalShot", hunter.GetName(), killed.GetName(), DbGroup.ShowRoles == false ? "" : $"{killed.GetName()} {GetLocaleString("Was")} {GetDescription(killed.PlayerRole)}"));
                        killed.IsDead = true;
                        killed.TimeDied = DateTime.Now;
                        if (killed.PlayerRole == IRole.Wolf || killed.PlayerRole == IRole.SerialKiller)
                            AddAchievement(hunter, Achievements.HeyManNiceShot);

                        DBKill(hunter, killed, KillMthd.HunterShot);
                        if (killed.InLove)
                            KillLover(killed);

                        CheckRoleChanges();
                        if (killed.PlayerRole == IRole.Hunter)
                            HunterFinalShot(killed, KillMthd.HunterShot);
                    }
                }
            }
        }

        public void Dispose()
        {
            Players?.Clear();
            Players = null;
            MessageQueueing = false;

        }

        internal enum GameTime
        {
            Day,
            Lynch,
            Night
        }

        private int ChooseRandomPlayerId(IPlayer exclude)
        {
            try
            {
                var possible = exclude != null ? Players.Where(x => x.Id != exclude.Id).ToList() : Players.ToList();
                possible.Shuffle();
                possible.Shuffle();
                return possible[0].Id;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
                Send(
                   Program.Version.FileVersion +
                   $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}",
                   Program.Para);
                return -1;
            }
        }

        private int ChooseRandomPlayerId(IEnumerable<IPlayer> exclude)
        {
            try
            {
                var possible = Players.Where(x => exclude.All(y => y?.TeleUser.Id != x?.TeleUser.Id)).ToList();
                possible.Shuffle();
                possible.Shuffle();
                return possible[0].Id;
            }
            catch
            {
                return -1;
            }
        }
        #endregion

        #region Database Helpers
        private void DBAction(IPlayer initator, IPlayer receiver, string action)
        {
            return; //dropping actions.  We never use them, they just take up a massive amount of space in the database
            //using (var db = new WWContext())
            //{
            //    try
            //    {
            //        var initid = initator.DBPlayerId;
            //        if (initid == 0)
            //        {
            //            initid = GetDBPlayer(initator, db).Id;
            //        }
            //        var recid = receiver.DBPlayerId;
            //        if (recid == 0)
            //            recid = GetDBPlayer(receiver, db).Id;
            //        if (DBGameId == 0)
            //        {
            //            DBGameId = db.Games.FirstOrDefault(x => x.Id == GameId)?.Id ?? 0;
            //        }
            //        var a = new Action
            //        {
            //            ActionTaken = action,
            //            GameId = DBGameId,
            //            InitiatorId = initid,
            //            ReceiverId = recid,
            //            TimeStamp = DateTime.Now,
            //            Day = GameDay
            //        };
            //        db.Actions.Add(a);

            //        db.SaveChanges();
            //    }
            //    catch (Exception)
            //    {
            //        //Log.WriteLine(e.Message + "\n" + e.StackTrace, LogLevel.Error, fileName: "error.log");
            //    }
            //}
        }

        private void DBKill(IPlayer killer, IPlayer victim, KillMthd method)
        {
            PlayerListChanged = true;
            using (var db = new WWContext())
            {
                try
                {
                    var killerid = killer.DBPlayerId;
                    if (killerid == 0)
                    {
                        var dbpKiller = GetDBPlayer(killer, db);
                        killerid = dbpKiller.Id;
                    }
                    Player dbpVictim = null;
                    var victimid = victim.DBPlayerId;
                    if (victimid == 0)
                    {
                        dbpVictim = GetDBPlayer(victim, db);
                        victimid = dbpVictim.Id;
                    }
                    if (DBGameId == 0)
                    {
                        DBGameId = db.Games.FirstOrDefault(x => x.Id == GameId)?.Id ?? 0;
                    }

                    var dbgp = dbpVictim == null ? GetDBGamePlayer(victim, db) : GetDBGamePlayer(dbpVictim);
                    dbgp.Survived = false;
                    db.SaveChanges();
                    var gk = new GameKill
                    {
                        Day = GameDay,
                        GameId = DBGameId,
                        KillMethodId = (int)method,
                        KillerId = killerid,
                        TimeStamp = DateTime.Now,
                        VictimId = victimid
                    };
                    db.GameKills.Add(gk);

                    db.SaveChanges();
                }
                catch (Exception)
                {
                    //Log.WriteLine(e.Message + "\n" + e.StackTrace, LogLevel.Error, fileName: "error.log");
                }
            }

            if (victim.LoverId == killer.Id && GameDay == 1 && Time == GameTime.Night && method != KillMthd.LoverDied)
                AddAchievement(killer, Achievements.OhShi);
        }

        private void KillLover(IPlayer victim)
        {
            var p = Players.FirstOrDefault(x => x.Id == victim.LoverId & !x.IsDead);
            if (p != null)
            {
                SendWithQueue(GetLocaleString("LoverDied", victim.GetName(), p.GetName(), DbGroup.ShowRoles == false ? "" : $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}"));
                DBKill(victim, p, KillMthd.LoverDied);
                p.IsDead = true;
                p.TimeDied = DateTime.Now;
            }
            //maybe we should CheckRoleChanges(); here?
            if (p?.PlayerRole == IRole.Hunter)
            {
                HunterFinalShot(p, KillMthd.LoverDied);
            }
        }

        private bool LongHaulReached = false;
        private void CheckLongHaul()
        {
            if (Players == null) return;
            if ((DateTime.Now - TimeStarted).Hours >= 1 & !LongHaulReached)
            {
                foreach (var p in Players.Where(x => !x.IsDead && !x.Fled))
                {
                    AddAchievement(p, Achievements.LongHaul);
                }
                LongHaulReached = true;
            }
        }

        public int DBGameId { get; set; }

        private void DBKill(IEnumerable<IPlayer> killers, IPlayer victim, KillMthd method)
        {
            foreach (var killer in killers)
                DBKill(killer, victim, method);
        }

        private Player GetDBPlayer(IPlayer player, WWContext db)
        {
            if (player.DBPlayerId == 0)
            {
                var p = db.Players.FirstOrDefault(x => x.TelegramId == player.Id);
                player.DBPlayerId = p?.Id ?? 0;
                return p;
            }
            try
            {
                return db.Players.Find(player.DBPlayerId);
            }
            catch
            {
                return null;
            }
        }

        private GamePlayer GetDBGamePlayer(Player player)
        {
            return player?.GamePlayers.FirstOrDefault(x => x.GameId == GameId);
        }

        private GamePlayer GetDBGamePlayer(IPlayer player, WWContext db)
        {
            if (player.DBGamePlayerId == 0)
            {
                var p = GetDBGamePlayer(GetDBPlayer(player, db));
                player.DBGamePlayerId = p?.Id ?? 0;
                return p;
            }

            try
            {
                return db.GamePlayers.Find(player.DBGamePlayerId);
            }
            catch
            {
                return null;
            }

        }

        private void UpdateAchievements()
        {
            if (Players == null) return;
            using (var db = new WWContext())
            {
                //check for convention
                var convention = Players.Count(x => x.PlayerRole == IRole.Cultist & !x.IsDead) >= 10;
                foreach (var player in Players.Where(x => !x.Fled)) //flee / afk? no achievements for you.
                {
                    var p = GetDBPlayer(player, db);



                    if (p != null)
                    {
                        Achievements newAch = Achievements.None;
                        //var gp = GetDBGamePlayer(p);

                        if (p.Achievements == null)
                            p.Achievements = 0;
                        var ach = (Achievements)p.Achievements;

                        //calculate achievements
                        //automatically get welcome to hell
                        if (!ach.HasFlag(Achievements.WelcomeToHell))
                            newAch = newAch | Achievements.WelcomeToHell;
                        if (!ach.HasFlag(Achievements.WelcomeToAsylum) && Chaos)
                            newAch = newAch | Achievements.WelcomeToAsylum;
                        if (!ach.HasFlag(Achievements.AlzheimerPatient) && Language.Contains("Amnesia"))
                            newAch = newAch | Achievements.AlzheimerPatient;
                        if (!ach.HasFlag(Achievements.OHAIDER) && Players.Any(x => x.TeleUser.Id == Program.Para))
                            newAch = newAch | Achievements.OHAIDER;
                        if (!ach.HasFlag(Achievements.SpyVsSpy) && DbGroup.ShowRoles == false)
                            newAch = newAch | Achievements.SpyVsSpy;
                        if (!ach.HasFlag(Achievements.NoIdeaWhat) && DbGroup.ShowRoles == false && Language.Contains("Amnesia"))
                            newAch = newAch | Achievements.NoIdeaWhat;
                        if (!ach.HasFlag(Achievements.Enochlophobia) && Players.Count == 35)
                            newAch = newAch | Achievements.Enochlophobia;
                        if (!ach.HasFlag(Achievements.Introvert) && Players.Count == 5)
                            newAch = newAch | Achievements.Introvert;
                        if (!ach.HasFlag(Achievements.Naughty) && Language.Contains("NSFW"))
                            newAch = newAch | Achievements.Naughty;
                        if (!ach.HasFlag(Achievements.Dedicated) && p.GamePlayers.Count() >= 100)
                            newAch = newAch | Achievements.Dedicated;
                        if (!ach.HasFlag(Achievements.Obsessed) && p.GamePlayers.Count() >= 1000)
                            newAch = newAch | Achievements.Obsessed;
                        if (!ach.HasFlag(Achievements.Veteran) && p.GamePlayers.Count() >= 500)
                            newAch = newAch | Achievements.Veteran;
                        if (!ach.HasFlag(Achievements.Masochist) && player.Won && player.PlayerRole == IRole.Tanner)
                            newAch = newAch | Achievements.Masochist;
                        if (!ach.HasFlag(Achievements.Wobble) && !player.IsDead && player.PlayerRole == IRole.Drunk && Players.Count >= 10)
                            newAch = newAch | Achievements.Wobble;
                        if (!ach.HasFlag(Achievements.Survivalist) && p.GamePlayers.Count(x => x.Survived) >= 100)
                            newAch = newAch | Achievements.Survivalist;
                        if (!ach.HasFlag(Achievements.MasonBrother) && player.PlayerRole == IRole.Mason &&
                            Players.Count(x => x.PlayerRole == IRole.Mason & !x.IsDead) >= 2)
                            newAch = newAch | Achievements.MasonBrother;
                        if (!ach.HasFlag(Achievements.ChangingSides) && player.OriginalRole != player.PlayerRole && player.Won)
                            newAch = newAch | Achievements.ChangingSides;
                        if (!ach.HasFlag(Achievements.LoneWolf) && Players.Count >= 10 && player.PlayerRole == IRole.Wolf &&
                            Players.Count(x => x.PlayerRole == IRole.Wolf) == 1 && player.Won)
                            newAch = newAch | Achievements.LoneWolf;
                        if (!ach.HasFlag(Achievements.Inconspicuous) && !player.HasBeenVoted & !player.IsDead)
                            newAch = newAch | Achievements.Inconspicuous;
                        if (!ach.HasFlag(Achievements.Promiscuous) && !player.HasStayedHome & !player.HasRepeatedVisit && player.PlayersVisited.Count >= 5)
                            newAch = newAch | Achievements.Promiscuous;
                        if (!ach.HasFlag(Achievements.DoubleShifter) && player.ChangedRolesCount >= 2)
                            newAch = newAch | Achievements.DoubleShifter;
                        if (!ach.HasFlag(Achievements.BrokenClock) && player.FoolCorrectSeeCount >= 2)
                            newAch = newAch | Achievements.BrokenClock;
                        if (!ach.HasFlag(Achievements.SmartGunner) && player.PlayerRole == IRole.Gunner & !player.BulletHitVillager && player.Bullet == 0)
                            newAch = newAch | Achievements.SmartGunner;
                        if (!ach.HasFlag(Achievements.CultCon) && player.PlayerRole == IRole.Cultist && convention)
                            newAch = newAch | Achievements.CultCon;
                        if (!ach.HasFlag(Achievements.SerialSamaritan) && player.PlayerRole == IRole.SerialKiller && player.SerialKilledWolvesCount >= 3)
                            newAch = newAch | Achievements.SerialSamaritan;

                        //now save
                        p.Achievements = (long)(ach | newAch);
                        db.SaveChanges();

                        //notify
                        var newFlags = newAch.GetUniqueFlags().ToList();
                        if (newAch == Achievements.None) continue;
                        var msg = "New Unlocks!".ToBold() + Environment.NewLine;
                        msg = newFlags.Aggregate(msg, (current, a) => current + $"{a.GetName().ToBold()}\n{a.GetDescription()}\n\n");
                        Send(msg, p.TelegramId);
                    }
                }
            }
        }

        private void AddAchievement(IPlayer player, Achievements a)
        {
            using (var db = new WWContext())
            {
                var p = GetDBPlayer(player, db);
                if (p != null)
                {
                    if (p.Achievements == null)
                        p.Achievements = 0;
                    var ach = (Achievements)p.Achievements;
                    if (ach.HasFlag(a)) return; //no point making another db call if they already have it
                    ach = ach | a;
                    p.Achievements = (long)ach;
                    db.SaveChanges();
                    Send($"Achievement Unlocked!\n{a.GetName().ToBold()}\n{a.GetDescription()}", player.Id);
                }
            }

        }

        #endregion
    }
}
