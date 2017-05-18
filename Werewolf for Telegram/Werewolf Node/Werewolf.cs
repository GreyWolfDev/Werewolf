using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public int GameDay, GameId,
            SecondsToAdd = 0;
        public List<IPlayer> Players = new List<IPlayer>();
        public bool IsRunning,
            IsJoining = true,
            KillTimer,
            IsInitializing,
            MessageQueueing = true,
            Chaos, WolfCubKilled,
            NoOneCastLynch;
        public Guid Guid = Guid.NewGuid();
        private readonly InlineKeyboardMarkup _requestPMButton;
        public DateTime LastPlayersOutput = DateTime.Now;
        public GameTime Time;
        public string Language = "English SFW", ChatGroup;
        public Locale Locale;
        public Group DbGroup;
        private bool _playerListChanged = true, _silverSpread;
        private DateTime _timeStarted;
        public readonly IRole[] WolfRoles = { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub };
        public List<long> HaveExtended = new List<long>();
        private int _joinMsgId;
        private string FirstMessage = "";
        private DateTime LastJoinButtonShowed = DateTime.MinValue;
        private InlineKeyboardMarkup _joinButton;
        private List<int> _joinButtons = new List<int>();

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

                    _requestPMButton = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton("Start Me") { Url = "telegram.me/" + Program.Me.Username } });
                    //AddPlayer(u);
                    SendPlayerList(true);
                }

                var deeplink = $"{Program.ClientId.ToString("N")}{Guid.ToString("N")}";
                //create our button
                _joinButton = new InlineKeyboardMarkup(new[]
                {
                    new InlineKeyboardButton(GetLocaleString("JoinButton")){Url = $"https://t.me/{Program.Me.Username}?start=" + deeplink }
                });
                FirstMessage = GetLocaleString(Chaos ? "PlayerStartedChaosGame" : "PlayerStartedGame", u.FirstName);
                //#if DEBUG
                //                _joinMsgId = Program.Bot.SendDocument(chatid, "CgADAwADmAIAAnQXsQdKO62ILjJQMQI", FirstMessage, replyMarkup: _joinButton).Result.MessageId;
                //#else
                _joinMsgId = Program.Bot.SendDocument(chatid, GetRandomImage(Chaos ? Settings.StartChaosGame : Settings.StartGame), FirstMessage, replyMarkup: _joinButton).Result.MessageId;
                //#endif

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
                    Program.ErrorGroup);
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
                var strings = Locale.File.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key) ??
                              Program.English.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                if (strings != null)
                {
                    var values = strings.Descendants("value");
                    var choice = Program.R.Next(values.Count());
                    var selected = values.ElementAt(choice).Value;

                    //disable bluetexting /join!
                    if (selected.ToLower().Contains("/join"))
                        throw new Exception("/join found in the string, using the English file.");

                    return String.Format(selected.FormatHTML(), args).Replace("\\n", Environment.NewLine);
                }
                else
                {
                    throw new Exception($"Error getting string {key} with parameters {(args != null && args.Length > 0 ? args.Aggregate((a, b) => a + "," + b.ToString()) : "none")}");
                }
            }
            catch (Exception e)
            {
                try
                {
                    //try the english string to be sure
                    var strings =
                        Program.English.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                    var values = strings?.Descendants("value");
                    if (values != null)
                    {
                        var choice = Program.R.Next(values.Count());
                        var selected = values.ElementAt(choice).Value;
                        selected = selected.Replace("/join", "join").Replace("/Join", "Join");
                        // ReSharper disable once AssignNullToNotNullAttribute
                        return String.Format(selected.FormatHTML(), args).Replace("\\n", Environment.NewLine);
                    }
                    else
                        throw new Exception("Cannot load english string for fallback");
                }
                catch
                {
                    throw new Exception(
                        $"Error getting string {key} with parameters {(args != null && args.Length > 0 ? args.Aggregate((a, b) => a + "," + b.ToString()) : "none")}",
                        e);
                }
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
                    try
                    {
                        Telegram.Bot.Types.Message r = null;
                        if (i == Settings.GameJoinTime - 60)
                        {
                            r = Program.Bot.SendTextMessage(ChatId, GetLocaleString("MinuteLeftToJoin"), parseMode: ParseMode.Html, replyMarkup: _joinButton).Result;
                        }
                        else if (i == Settings.GameJoinTime - 30)
                        {
                            r = Program.Bot.SendTextMessage(ChatId, GetLocaleString("SecondsLeftToJoin", "30".ToBold()), parseMode: ParseMode.Html, replyMarkup: _joinButton).Result;
                        }
                        else if (i == Settings.GameJoinTime - 10)
                        {
                            r = Program.Bot.SendTextMessage(ChatId, GetLocaleString("SecondsLeftToJoin", "10".ToBold()), parseMode: ParseMode.Html, replyMarkup: _joinButton).Result;
                        }
                        if (r != null)
                        {
                            _joinButtons.Add(r.MessageId);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    if (SecondsToAdd != 0)
                    {
                        i = Math.Max(i - SecondsToAdd, Settings.GameJoinTime - Settings.MaxJoinTime);
                        var msg = "";
                        var remaining = TimeSpan.FromSeconds(Settings.GameJoinTime - i);
                        if (SecondsToAdd > 0)
                            msg = GetLocaleString("SecondsAdded", SecondsToAdd.ToString().ToBold(), remaining.ToString(@"mm\:ss").ToBold());
                        else
                        {
                            SecondsToAdd = -SecondsToAdd;
                            msg = GetLocaleString("SecondsRemoved", SecondsToAdd.ToString().ToBold(), remaining.ToString(@"mm\:ss").ToBold());
                        }
                        if (Settings.GameJoinTime > i)
                            Program.Bot.SendTextMessage(ChatId, msg, parseMode: ParseMode.Html, replyToMessageId: _joinMsgId);

                        SecondsToAdd = 0;
                    }
                    Thread.Sleep(1000);
                }
                Program.Bot.EditMessageCaption(ChatId, _joinMsgId, FirstMessage, null);
                IsJoining = false;
                IsInitializing = true;

                Thread.Sleep(2000); //wait for last second joins
                CleanupButtons();
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
                    _timeStarted = DateTime.Now;
                    var game = new Game
                    {
                        GroupName = ChatGroup,
                        TimeStarted = _timeStarted,
                        GroupId = ChatId,
                        GrpId = int.Parse(DbGroup.Id.ToString()),
                        Mode = Chaos ? "Chaos" : "Normal"
                    };

                    db.SaveChanges();
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

                        db.SaveChanges();
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

                    GameId = db.Games.Where(x => x.GroupId == ChatId).OrderByDescending(x => x.Id).FirstOrDefault()?.Id ?? 0;

                    db.Database.ExecuteSqlCommand($"DELETE FROM NotifyGame WHERE GroupId = {ChatId}");
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
                    Program.ErrorGroup);
            }
            catch (Exception ex)
            {
                do
                {
                    Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.ErrorGroup);
                    ex = ex.InnerException;
                } while (ex != null);
                Send("Something just went terribly wrong, I had to cancel the game....\n" + ex.Message);
#if DEBUG
                Send(ex.StackTrace);
#else
                Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.ErrorGroup);
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
                if (!IsJoining || IsInitializing)
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
                //check one more time
                if (IsInitializing || !IsJoining) return;
                //add player
                Players.Add(p);
                var groupname = String.IsNullOrWhiteSpace(DbGroup.GroupLink) ? ChatGroup : $"<a href=\"{DbGroup.GroupLink}\">{ChatGroup.FormatHTML()}</a>";
                Send(GetLocaleString("YouJoined", groupname), p.Id);

                //if (!notify) return;

                //var msg = GetLocaleString("PlayerJoined", p.GetName(), Players.Count.ToBold(), Settings.MinPlayers.ToBold(),
                //    DbGroup.MaxPlayers.ToBold() ?? Settings.MaxPlayers.ToBold());

                //bool sendPM = false;

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
                }

                //now, attempt to PM the player
                //try
                //{
                // ReSharper disable once UnusedVariable
                //var result = Send(GetLocaleString("YouJoined", ChatGroup.FormatHTML()), u.Id).Result;
                //}
                //catch (Exception)
                //{
                //var botname = "@" + Program.Me.Username;
                //if (!sendPM)
                //    msg = GetLocaleString("PMTheBot", p.GetName(), botname);
                ////unable to PM
                //sendPM = true;
                //}

                //SendWithQueue(msg, requestPM: sendPM);

                //if (sendPM) //don't allow them to join
                //{
                //    Players.Remove(p);
                //}
                _requestPlayerListUpdate = true;
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
                if (IsInitializing) throw new Exception("Cannot flee while game is initializing.  Try again once game is done starting.");
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
                else if (IsJoining & !IsInitializing)// really, should never be both joining and initializing but....
                {
                    Players.Remove(p);
                    _requestPlayerListUpdate = true;
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

                if (player == null) return;

                if (player.PlayerRole == IRole.Mayor && args[2] == "reveal" && player.HasUsedAbility == false)
                {
                    player.HasUsedAbility = true;
                    SendWithQueue(GetLocaleString("MayorReveal", player.GetName()));

                    Program.MessagesSent++;
                    ReplyToCallback(query,
                        GetLocaleString("ChoiceAccepted"));

                    return;
                }
                else if (player.PlayerRole == IRole.Mayor && args[2] == "reveal" && player.HasUsedAbility == true)
                    return;

                if (player.PlayerRole == IRole.Blacksmith && player.CurrentQuestion.QType == QuestionType.SpreadSilver)
                {
                    if (args[2] == "yes")
                    {
                        player.HasUsedAbility = true;
                        _silverSpread = true;
                        SendWithQueue(GetLocaleString("BlacksmithSpreadSilver", player.GetName()));
                    }

                    ReplyToCallback(query,
                        GetLocaleString("ChoiceAccepted"));
                    player.CurrentQuestion = null;
                    return;
                }

                if (player.CurrentQuestion == null)
                {
                    return;
                }

                if (query.Data == null)
                {
                    throw new NullReferenceException("Object was null: query.Data");
                }

                if (args[2] == "-1")
                {
                    if (player.CurrentQuestion.QType == QuestionType.Kill2)
                        player.Choice2 = -1;
                    else
                        player.Choice = -1;
                    Program.MessagesSent++;
                    ReplyToCallback(query,
                        GetLocaleString("ChoiceAccepted") + " - Skip");
                    player.CurrentQuestion = null;
                    return;
                }


                if (player.CurrentQuestion.QType == QuestionType.Kill2)
                    player.Choice2 = int.Parse(args[2]);
                else
                    player.Choice = int.Parse(args[2]);

                if (player.PlayerRole == IRole.ClumsyGuy && player.CurrentQuestion.QType == QuestionType.Lynch)
                {
                    if (Program.R.Next(100) < 50)
                    {
                        //pick a random target
                        var clumsy = ChooseRandomPlayerId(player, false);
                        if (clumsy == player.Choice) player.ClumsyCorrectLynchCount++;
                        player.Choice = clumsy;
                    }
                    else
                    {
                        player.ClumsyCorrectLynchCount++;
                    }
                }


                var target = Players.FirstOrDefault(x => player.CurrentQuestion.QType == QuestionType.Kill2 ? x.Id == player.Choice2 : x.Id == player.Choice);
                if (target == null)
                {
                    Send(GetLocaleString("NoPlayerName"), query.From.Id);
                    return;
                }

                if (WolfRoles.Contains(player.PlayerRole) && player.CurrentQuestion.QType == QuestionType.Kill2)
                {
                    var others = Players.GetPlayersForRoles(WolfRoles, exceptPlayer: player);
                    foreach (var w in others)
                    {
                        Send(GetLocaleString("WolfVotedKill", player.GetName(), target.GetName()), w.Id);
                    }
                }

                var clearCurrent = true;
                if (WolfRoles.Contains(player.PlayerRole) && player.CurrentQuestion.QType == QuestionType.Kill)
                {
                    var others = Players.GetPlayersForRoles(WolfRoles, exceptPlayer: player);
                    foreach (var w in others)
                    {
                        Send(GetLocaleString("WolfVotedKill", player.GetName(), target.GetName()), w.Id);
                    }
                    if (WolfCubKilled)
                    {
                        //need to let them have another menu for second kill
                        var targets = Players.Where(x => !WolfRoles.Contains(x.PlayerRole) & !x.IsDead && x.Id != player.Choice).ToList();
                        var msg = GetLocaleString("AskEat");
                        var qtype = QuestionType.Kill2;
                        var buttons = targets.Select(x => new[] { new InlineKeyboardButton(x.Name, $"vote|{Program.ClientId}|{x.Id}") }).ToList();
                        buttons.Add(new[] { new InlineKeyboardButton("Skip", $"vote|{Program.ClientId}|-1") });
                        SendMenu(buttons, player, msg, qtype);
                        clearCurrent = false;
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
                        ReplyToCallback(query,
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

                    if (DbGroup.EnableSecretLynch != true)
                    {
                        var msg = GetLocaleString("PlayerVotedLynch", player.GetName(), target.GetName());
                        SendWithQueue(msg);
                    }
                    else
                    {
                        var msg = GetLocaleString("PlayerVoteCounts", Players.Count(x => !x.IsDead && x.Choice != 0), Players.Count(x => !x.IsDead));
                        SendWithQueue(msg);
                    }

                    if (NoOneCastLynch)
                    {
                        player.FirstStone++;
                        NoOneCastLynch = false;
                    }
                    else
                        player.FirstStone = 0;

                    if (player.FirstStone == 5)
                    {
                        AddAchievement(player, Achievements.FirstStone);
                    }
                }
                Program.MessagesSent++;
                ReplyToCallback(query,
                        GetLocaleString("ChoiceAccepted") + " - " + target.GetName(true));
                if (clearCurrent)
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



        //TODO move the message queue elsewhere
        class Message
        {
            public string Msg { get; }
            public string GifId { get; }

            public bool RequestPM { get; }
            public bool PlayerList { get; set; }
            public bool Joining { get; set; } = false;

            public Message(string msg, string gifid = null, bool requestPM = false)
            {
                Msg = msg;
                GifId = gifid;
                RequestPM = requestPM;
            }
        }

        private readonly Queue<Message> _messageQueue = new Queue<Message>();
        private bool _requestPlayerListUpdate = false;
        private void GroupQueue()
        {
            string final;
            while (MessageQueueing)
            {
                if (_requestPlayerListUpdate)
                {
                    SendPlayerList(true);
                    _requestPlayerListUpdate = false;
                }
                final = "";
                bool requestPM = false;
                bool byteMax = false;
                bool pList = false;
                var i = 0;
                while (_messageQueue.Count > 0 & !byteMax)
                {



                    i++;
                    var m = _messageQueue.Peek();

                    if (m.Joining)
                    {
                        _messageQueue.Dequeue();
                        if (_playerListId == 0)
                            _playerListId = Send(m.Msg).Result.MessageId;
                        else
                            Program.Bot.EditMessageText(ChatId, _playerListId, m.Msg, ParseMode.Html, disableWebPagePreview: true);
                        continue;
                    }

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
                            final += m.Msg + Environment.NewLine + Environment.NewLine;
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
                        if ((Encoding.UTF8.GetByteCount(temp) > 512 && i > 1))
                        {
                            byteMax = true; //break and send
                        }
                        else
                        {
                            _messageQueue.Dequeue(); //remove the message, we are sending it.
                            final += m.Msg + Environment.NewLine;
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
                        Send(final, 0, false, _requestPMButton);
                    }
                    else
                    {
                        if (pList)
                        {
                            try
                            {
                                var result = Send(final).Result;
                                _playerListId = result.MessageId;
                            }
                            catch
                            {
                                _playerListId = 0;
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

        private int _playerListId;

        private void SendPlayerList(bool joining = false)
        {
            if (!_playerListChanged & !joining) return;
            if (Players == null) return;
            try
            {
                new Thread(() =>
                {
                    var msg = "";
                    if (joining)
                    {
                        msg = $"#players: {Players.Count}\n" +
                        Players.Aggregate("", (current, p) => current + ($"{p.GetName()}\n"));
                    }
                    else
                    {
                        //Thread.Sleep(4500); //wait a moment before sending
                        LastPlayersOutput = DateTime.Now;
                        msg =
                            $"{GetLocaleString("PlayersAlive")}: {Players.Count(x => !x.IsDead)}/{Players.Count}\n" +
                            Players.OrderBy(x => x.TimeDied)
                                .Aggregate("",
                                    (current, p) =>
                                        current +
                                        ($"{p.GetName()}: {(p.IsDead ? ((p.Fled ? GetLocaleString("RanAway") : GetLocaleString("Dead")) + (DbGroup.ShowRoles != false ? " - " + GetDescription(p.PlayerRole) + (p.InLove ? "❤️" : "") : "")) : GetLocaleString("Alive"))}\n"));
                        //{(p.HasUsedAbility & !p.IsDead && new[] { IRole.Prince, IRole.Mayor, IRole.Gunner, IRole.Blacksmith }.Contains(p.PlayerRole) ? " - " + GetDescription(p.PlayerRole) : "")}  //OLD CODE SHOWING KNOWN ROLES
                        _playerListChanged = false;
                    }
                    SendWithQueue(new Message(msg) { PlayerList = true, Joining = joining });

                }).Start();
            }
            catch (Exception ex)
            {
                Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.ErrorGroup);
            }
        }

        public void OutputPlayers()
        {
            if (!((DateTime.Now - LastPlayersOutput).TotalSeconds > (10))) return;
            LastPlayersOutput = DateTime.Now;
            Program.Bot.SendTextMessage(ChatId, GetLocaleString(_playerListId != 0 ? "LatestList" : "UnableToGetList"), parseMode: ParseMode.Html, replyToMessageId: _playerListId);
        }

        public async void ShowJoinButton()
        {
            if (!IsJoining) return;
            if (!((DateTime.Now - LastJoinButtonShowed).TotalSeconds > (15))) return;
            LastJoinButtonShowed = DateTime.Now;
            try
            {
                var r = await Program.Bot.SendTextMessage(ChatId, GetLocaleString("JoinByButton"), parseMode: ParseMode.Html, replyMarkup: _joinButton);
                _joinButtons.Add(r.MessageId);
            }
            catch
            {
                // ignored
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
                        if (allowCult && playerCount > 10)
                            rolesToAssign.Add(role);
                        break;
                    case IRole.Tanner:
                        if (allowTanner)
                            rolesToAssign.Add(role);
                        break;
                    case IRole.Fool:
                        if (allowFool)
                            rolesToAssign.Add(role);
                        break;
                    case IRole.WolfCub:
                    case IRole.AlphaWolf: //don't add more wolves, just replace
                        if (rolesToAssign.Remove(IRole.Wolf))
                            rolesToAssign.Add(role);
                        break;
                    default:
                        rolesToAssign.Add(role);
                        break;
                }
            }

            //we want the possibility to have normal wolves too!
            if (!rolesToAssign.Contains(IRole.Wolf))
            {
                var possiblewolves = new List<IRole>() { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub };
                var wolftoadd = possiblewolves[Program.R.Next(3)];

                if (rolesToAssign.Remove(IRole.AlphaWolf))
                    rolesToAssign.Add(wolftoadd);
                //else throw an Exception? it should never happen that rolesToAssign does not contain both Alpha Wolf and Wolf, but...

                if (playerCount >= 10) //there was WolfCub too, since IRole.AlphaWolf < IRole.WolfCub
                {
                    //note that at this point we might have two WolfCubs. However, we are removing one, and if the other wolf was a cub, we're not readding it.
                    if (rolesToAssign.Remove(IRole.WolfCub))
                    {
                        //replace the wolftoadd with a Wolf in the possiblewolves, to keep probability consistency
                        possiblewolves.Remove(wolftoadd);
                        possiblewolves.Add(IRole.Wolf);

                        rolesToAssign.Add(possiblewolves[Program.R.Next(3)]); //actually replace the wolf
                    }
                    //else throw an Exception?
                }
            }

            //add a couple more masons
            rolesToAssign.Add(IRole.Mason);
            rolesToAssign.Add(IRole.Mason);
            //for smaller games, all roles will be available and chosen randomly.  For large games, it will be about the
            //same as it was before....


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
                List<IRole> rolesToAssign;
                var count = Players.Count;

                var balanced = false;
                var attempts = 0;
                var nonVgRoles = new[] { IRole.Cultist, IRole.SerialKiller, IRole.Tanner, IRole.Wolf, IRole.AlphaWolf, IRole.Sorcerer, IRole.WolfCub };

                do
                {
                    attempts++;
                    if (attempts >= 500)
                    {
                        throw new IndexOutOfRangeException("Unable to create a balanced game.  Please try again.\nPlayer count: " + count);
                    }


                    //determine which roles should be assigned
                    rolesToAssign = GetRoleList(count, DbGroup.AllowCult != false, DbGroup.AllowTanner != false,
                        DbGroup.AllowFool != false);
                    rolesToAssign.Shuffle();
                    rolesToAssign = rolesToAssign.Take(count).ToList();



                    //let's fix some roles that should or shouldn't be there...

                    //sorcerer or traitor, without wolves, are pointless. change one of them to wolf
                    if ((rolesToAssign.Contains(IRole.Sorcerer) || rolesToAssign.Contains(IRole.Traitor)) &&
                        !rolesToAssign.Any(x => WolfRoles.Contains(x)))
                    {
                        var towolf = rolesToAssign.FindIndex(x => x == IRole.Sorcerer || x == IRole.Traitor); //if there are both, the random order of rolesToAssign will choose for us which one to substitute
                        rolesToAssign[towolf] = WolfRoles[Program.R.Next(3)]; //choose randomly from WolfRoles
                    }

                    //appseer without seer -> seer
                    if (rolesToAssign.Contains(IRole.ApprenticeSeer) && !rolesToAssign.Contains(IRole.Seer))
                    {
                        //substitute with seer
                        var apps = rolesToAssign.IndexOf(IRole.ApprenticeSeer);
                        rolesToAssign[apps] = IRole.Seer;
                    }

                    //cult without CH -> add CH
                    if (rolesToAssign.Contains(IRole.Cultist) && !rolesToAssign.Contains(IRole.CultistHunter))
                    {
                        //just pick a vg, and turn them to CH
                        var vg = rolesToAssign.FindIndex(x => !nonVgRoles.Contains(x));
                        rolesToAssign[vg] = IRole.CultistHunter;
                    }


                    //make sure that we have at least two teams
                    if (
                        rolesToAssign.Any(x => !nonVgRoles.Contains(x)) //make sure we have VGs
                        && rolesToAssign.Any(x => nonVgRoles.Contains(x) && x != IRole.Sorcerer && x != IRole.Tanner) //make sure we have at least one enemy
                    )
                        balanced = true;
                    //else, redo role assignment. better to rely on randomness, than trying to fix it

                    //the roles to assign are good, now if it's not a chaos game we need to check if they're balanced
                    if (!Chaos)
                    {
                        var villageStrength =
                            rolesToAssign.Where(x => !nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));
                        var enemyStrength =
                            rolesToAssign.Where(x => nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));

                        //check balance
                        var varianceAllowed = (count / 4) + 1;
                        balanced = balanced && (Math.Abs(villageStrength - enemyStrength) <= varianceAllowed);
                    }
                } while (!balanced);


                //shuffle things
                Players.Shuffle();
                Players.Shuffle();
                Players.Shuffle();
                Players.Shuffle();
                Players.Shuffle();
                rolesToAssign.Shuffle();
                rolesToAssign.Shuffle();


#if DEBUG
                //force roles for testing
                //rolesToAssign[0] = IRole.WolfCub;
                //rolesToAssign[1] = IRole.WolfCub;
                //rolesToAssign[2] = IRole.AlphaWolf;
                //rolesToAssign[3] = IRole.WolfCub;
                //if (rolesToAssign.Count >= 5)
                //    rolesToAssign[4] = IRole.Villager;
#endif


                //assign the roles 
                for (var i = 0; i < Players.Count; i++)
                {
                    Players[i].PlayerRole = rolesToAssign[i];
                }

                SetRoleAttributes();

                //shuffle again
                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        Players.Shuffle();
                    }
                    catch
                    {
                        // ignored
                    }
                }

                foreach (var p in Players)
                    p.OriginalRole = p.PlayerRole;
            }
            catch (Exception ex)
            {
                Send($"Error while assigning roles: {ex.Message}\nPlease start a new game");
                Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.ErrorGroup);
                Thread.Sleep(1000);
                Program.RemoveGame(this);
            }
        }

        private void SetRoleAttributes()
        {
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
                    case IRole.Mayor:
                    case IRole.ClumsyGuy:
                    case IRole.Prince:
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
                    case IRole.Blacksmith:
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
                    case IRole.Sorcerer:
                    case IRole.AlphaWolf:
                    case IRole.WolfCub:
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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
                    // ReSharper disable once UnusedVariable
                    var result = Program.Send(msg, p.Id, true).Result;
                }
                catch (NullReferenceException ex)
                {
                    Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.ErrorGroup);
                }
                catch (Exception e)
                {
                    //SendWithQueue(GetLocaleString("PlayerNoPM", p.GetName()));
                    //FleePlayer(p.TeleUser.Id);
                    do
                    {
                        Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{e.Message}\n{e.StackTrace}", Program.ErrorGroup);
                        e = e.InnerException;
                    } while (e != null);

                }
                Thread.Sleep(50);
            }
        }

        private string GetRoleInfo(IRole role)
        {
            try
            {
                string msg;
                switch (role)
                {
                    case IRole.Fool:
                        return GetLocaleString("RoleInfoSeer");
                    case IRole.Beholder:
                        msg = GetLocaleString("RoleInfoBeholder");
                        var seer = Players?.FirstOrDefault(x => x.PlayerRole == IRole.Seer);
                        if (seer != null)
                            msg += GetLocaleString("BeholderSeer", $"{seer.GetName()}");
                        else
                            msg += "  " + GetLocaleString("NoSeer");
                        return msg;
                    case IRole.Mason:
                        msg = GetLocaleString("RoleInfoMason");
                        if (Players?.Count(x => x?.PlayerRole == IRole.Mason) > 1)
                        {
                            msg += GetLocaleString("MasonTeam", Players.Where(x => x.PlayerRole == IRole.Mason).Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next));
                        }
                        return msg;
                    case IRole.Doppelgänger:
                        return GetLocaleString("RoleInfoDoppelganger");
                    default:
                        return GetLocaleString($"RoleInfo{role}");
                }
            }
            catch (Exception e)
            {
                Send("Error in get role info: \n" + e.Message + "\n" + e.StackTrace, Program.ErrorGroup);
            }
            return "";
        }

        public void CheckRoleChanges(bool checkbitten = false)
        {
            if (Players == null) return;
            //check Apprentice Seer
            var aps = Players?.FirstOrDefault(x => x.PlayerRole == IRole.ApprenticeSeer & !x.IsDead);
            if (aps != null && (!checkbitten || !aps.Bitten))
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
            CheckWildChild(checkbitten);
            CheckDoppelganger(checkbitten);

            var wolves = Players.GetPlayersForRoles(WolfRoles);
            if (wolves.Count() >= 7)
            {
                foreach (var w in wolves)
                {
                    AddAchievement(w, Achievements.PackHunter);
                }
            }

            var seers = Players.GetPlayersForRoles(new[] { IRole.Seer });
            if (seers.Count() > 1)
            {
                foreach (var s in seers)
                    AddAchievement(s, Achievements.DoubleVision);
            }
        }

        private void ValidateSpecialRoleChoices()
        {
            if (GameDay != 1) return;
            //Wild Child
            var wc = Players.GetPlayerForRole(IRole.WildChild);
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
                if (l1 != null && l2 != null)
                {
                    l1.LoverId = l2.Id;
                    l2.LoverId = l1.Id;
                }
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
                var exist = (Players.FirstOrDefault(x => x.InLove) ?? AddLover()) ?? AddLover();
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

            foreach (var lover in loversNotify)
            {
                if (lover.SpeedDating)
                    AddAchievement(lover, Achievements.OnlineDating);
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
            lover.SpeedDating = true;
            if (existing == null) return lover;
            existing.LoverId = lover.Id;
            lover.LoverId = existing.Id;
            return lover;
        }

        private void CheckWildChild(bool checkbitten = false)
        {
            var wc = Players?.FirstOrDefault(x => x.PlayerRole == IRole.WildChild & !x.IsDead);

            // Check Wild Child
            if (wc != null && (!checkbitten || !wc.Bitten))
            {
                var rm = Players.FirstOrDefault(x => x.Id == wc.RoleModel);
                if (rm != null)
                {
                    if (rm.IsDead)
                    {
                        var teammates = "";
                        //notify other wolves
                        foreach (var w in Players.GetPlayersForRoles(WolfRoles))
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

        private void CheckDoppelganger(bool checkbitten = false)
        {
            var p = Players?.FirstOrDefault(x => x.PlayerRole == IRole.Doppelgänger & !x.IsDead);
            //var aps = Players.FirstOrDefault(x => x.PlayerRole == IRole.ApprenticeSeer & !x.IsDead);
            //var traitor = Players.FirstOrDefault(x => x.PlayerRole == IRole.Traitor & !x.IsDead);

            // Check DG
            if (p != null && (!checkbitten || !p.Bitten))
            {
                var rm = Players.FirstOrDefault(x => x.Id == p.RoleModel);
                if (rm != null && rm.IsDead)
                {
                    var teammates = "";
                    //notify other wolves
                    p.PlayerRole = rm.OriginalRole;
                    if (rm.OriginalRole == IRole.ApprenticeSeer || rm.OriginalRole == IRole.WildChild || rm.OriginalRole == IRole.Traitor || rm.OriginalRole == IRole.Cursed)
                    {
                        //if (rm.OriginalRole == IRole.ApprenticeSeer || rm.OriginalRole == IRole.Cursed)
                        if (rm.OriginalRole == IRole.ApprenticeSeer)     //if cursed turned wolf before dying, should DG turn cursed or directly wolf? use the above line if DG should turn cursed
                            if (rm.PlayerRole != IRole.Wolf)
                                p.PlayerRole = rm.PlayerRole;
                        if (rm.PlayerRole != IRole.Cultist)
                            p.PlayerRole = rm.PlayerRole;
                    }
                    p.ChangedRolesCount++;

                    if (!new[] { IRole.Mason, IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Cultist, IRole.WildChild }.Contains(p.PlayerRole))
                    {
                        //tell them their new role
                        Send(GetRoleInfo(p.PlayerRole), p.Id);
                    }
                    switch (p.PlayerRole)
                    {
                        case IRole.Villager:
                        case IRole.Cursed:
                        case IRole.Drunk:
                        case IRole.Prince:
                        case IRole.ClumsyGuy:
                            p.HasDayAction = false;
                            p.HasNightAction = false;
                            p.Team = ITeam.Village;
                            p.HasUsedAbility = false;
                            break;
                        case IRole.Beholder:
                            p.HasDayAction = false;
                            p.HasNightAction = false;
                            p.Team = ITeam.Village;
                            var seer = Players.FirstOrDefault(x => x.PlayerRole == IRole.Seer);
                            Send(
                                seer != null
                                    ? GetLocaleString("BeholderSeer", $"{seer.GetName()}")
                                    : GetLocaleString("NoSeer"), p.Id);
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
                            if (Players.Count(x => !x.IsDead && WolfRoles.Contains(x.PlayerRole)) == 0)
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
                        case IRole.Blacksmith:
                        case IRole.Gunner:
                            p.Bullet = 2;
                            p.Team = ITeam.Village;
                            p.HasDayAction = true;
                            p.HasNightAction = false;
                            break;
                        case IRole.AlphaWolf:
                        case IRole.WolfCub:
                        case IRole.Wolf:
                            p.Team = ITeam.Wolf;
                            p.HasNightAction = true;
                            p.HasDayAction = false;
                            foreach (var w in Players.GetPlayersForRoles(WolfRoles, exceptPlayer: p))
                            {
                                Send(GetLocaleString("DGToWolf", $"{p.GetName()}"), w.Id);
                                teammates += $"{w.GetName()}" + ", ";
                            }
                            switch (p.PlayerRole)
                            {
                                case IRole.AlphaWolf:
                                    Send(GetLocaleString("DGTransformToAlpha", rm.GetName(), teammates), p.Id);
                                    break;
                                case IRole.WolfCub:
                                    Send(GetLocaleString("DGTransformToWolfCub", rm.GetName(), teammates), p.Id);
                                    break;
                                case IRole.Wolf:
                                    Send(GetLocaleString("DGTransformToWolf", rm.GetName(), teammates), p.Id);
                                    break;
                            }
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
                        case IRole.Sorcerer:
                            p.Team = ITeam.Wolf;
                            p.HasNightAction = true;
                            p.HasDayAction = false;
                            break;
                        case IRole.Mayor:
                            p.HasUsedAbility = false;
                            p.Team = ITeam.Village;
                            p.HasNightAction = false;
                            p.HasDayAction = false;
                            var choices = new[] { new[] { new InlineKeyboardButton(GetLocaleString("Reveal"), $"vote|{Program.ClientId}|reveal") } }.ToList();
                            SendMenu(choices, p, GetLocaleString("AskMayor"), QuestionType.Mayor);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }


                }
            }
        }

        private void BitePlayer(IPlayer target, IEnumerable<IPlayer> voteWolves, string alpha)
        {
            target.Bitten = true;
            foreach (var wolf in voteWolves)
                Send(
                    GetLocaleString("PlayerBittenWolves", target.GetName(),
                        alpha), wolf.Id);
        }

        private void ConvertToCult(IPlayer target, IEnumerable<IPlayer> voteCult, int chance = 100)
        {
            if (Program.R.Next(100) < chance)
            {
                if (target.PlayerRole == IRole.Harlot)
                    foreach (var c in voteCult)
                        AddAchievement(c, Achievements.DontStayHome);

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
            else
            {
                foreach (var c in voteCult)
                {
                    Send(GetLocaleString("CultUnableToConvert", voteCult.OrderByDescending(x => x.DayCult).First().GetName(), target.GetName()), c.Id);
                }
                Send(GetLocaleString("CultAttempt"), target.Id);
            }
        }



        #endregion

        #region Cycles

        public void ForceStart()
        {
            KillTimer = true;
        }
        public void ExtendTime(long id, bool admin, int seconds)
        {
            if (!IsJoining) return;
            var p = Players.FirstOrDefault(x => x.TeleUser.Id == id);
            if (p != null)
            {
                if (HaveExtended.Contains(p.TeleUser.Id) && !admin)
                {
                    SendWithQueue(GetLocaleString("CantExtend"));
                    return;
                }
                SecondsToAdd = seconds;
                HaveExtended.Add(p.TeleUser.Id);
            }
            return;
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
                        if (p.PlayerRole == IRole.Mayor && p.HasUsedAbility) //Mayor counts twice
                        {
                            p.MayorLynchAfterRevealCount++;
                            target.Votes++;
                        }
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
                    SendWithQueue(GetLocaleString("IdleKill", p.GetName(), (DbGroup.ShowRoles == false ? "" : $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}\n") + GetLocaleString("IdleCount", p.GetName() + $"(id: <code>{p.TeleUser.Id}</code>)", idles24 + 1)));

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
                    if (lynched.PlayerRole == IRole.Prince & !lynched.HasUsedAbility) //can only do this once
                    {
                        SendWithQueue(GetLocaleString("PrinceLynched", lynched.GetName()));
                        lynched.HasUsedAbility = true;
                    }
                    else
                    {
                        lynched.IsDead = true;
                        lynched.TimeDied = DateTime.Now;
                        if (lynched.PlayerRole == IRole.Seer && GameDay == 1)
                            AddAchievement(lynched, Achievements.LackOfTrust);
                        if (lynched.PlayerRole == IRole.Prince && lynched.HasUsedAbility)
                            AddAchievement(lynched, Achievements.SpoiledRichBrat);
                        SendWithQueue(GetLocaleString("LynchKill", lynched.GetName(), DbGroup.ShowRoles == false ? "" : $"{lynched.GetName()} {GetLocaleString("Was")} {GetDescription(lynched.PlayerRole)}"));

                        if (lynched.InLove)
                            KillLover(lynched);

                        //effects on game depending on the lynched's role
                        switch (lynched.PlayerRole)
                        {
                            case IRole.WolfCub:
                                WolfCubKilled = true;
                                break;
                            case IRole.Tanner:
                                //check for overkill
                                if (Players.Where(x => !x.IsDead).All(x => x.Choice == lynched.Id))
                                    AddAchievement(lynched, Achievements.TannerOverkill);
                                //end game
                                lynched.DiedLastNight = true; //store the tanner who should win (DG is too complicated to handle)
                                DoGameEnd(ITeam.Tanner);
                                return;
                            case IRole.Hunter:
                                HunterFinalShot(lynched, KillMthd.Lynch);
                                break;
                        }

                        //update the database
                        DBKill(Players.Where(x => x.Choice == lynched.Id), lynched, KillMthd.Lynch);

                        CheckRoleChanges(true);
                    }
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

                if (CheckForGameEnd(true)) return;
            }
            catch (Exception ex)
            {
                var e = ex;
                while (e.InnerException != null)
                    e = e.InnerException;


                Send("Oh no, something went wrong :( Error report is being sent to the developers\n" + e.Message);
#if DEBUG
                Send(ex.StackTrace);
#else
                Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.ErrorGroup);
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
            var timeToAdd = Math.Max(((Players.Count(x => !x.IsDead) / 5) - 1) * 30, 60);
#if DEBUG
            Settings.TimeDay = 20;
            timeToAdd = 0;
#endif
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
                        if (p.CurrentQuestion.MessageId != 0 && p.CurrentQuestion.QType != QuestionType.Mayor)
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
                    foreach (var w in Players.Where(x => !x.IsDead && (x.PlayerRole == IRole.Wolf || x.PlayerRole == IRole.AlphaWolf || x.PlayerRole == IRole.WolfCub)))
                    {
                        Send(GetLocaleString("DetectiveCaught", $"{detect.GetName()}"), w.Id);
                    }
                }

                var check = Players.FirstOrDefault(x => x.Id == detect.Choice);
                if (check != null)
                {
                    DBAction(detect, check, "Detect");
                    Send(GetLocaleString("DetectiveSnoop", check.GetName(), GetDescription(check.PlayerRole)), detect.Id);

                    //if snooped non-bad-roles:
                    if (!new[] { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Cultist, IRole.SerialKiller }.Contains(check.PlayerRole))
                        detect.CorrectSnooped.Clear();     //clear correct snoop list
                    else
                    {
                        if (detect.CorrectSnooped.Contains(check.Id))     //check if it is a re-snoop of correct roles
                            detect.CorrectSnooped.Clear();             //clear the correct snoop list
                        detect.CorrectSnooped.Add(check.Id);              //add the current snoop to list

                        //if snooped 4 times correct continously
                        if (detect.CorrectSnooped.Count() >= 4)
                        {
                            AddAchievement(detect, Achievements.Streetwise);
                            detect.CorrectSnooped.Clear();
                        }
                    }
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
                    gunner.HasUsedAbility = true;
                    check.IsDead = true;
                    if (check.PlayerRole == IRole.WolfCub)
                        WolfCubKilled = true;
                    if (!new[] { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Cultist, IRole.SerialKiller }.Contains(check.PlayerRole))
                        gunner.BulletHitVillager = true;
                    check.TimeDied = DateTime.Now;
                    //update database
                    DBKill(gunner, check, KillMthd.Shoot);
                    DBAction(gunner, check, "Shoot");
                    switch (check.PlayerRole)
                    {
                        case IRole.Harlot:
                            SendWithQueue(DbGroup.ShowRoles != false ? GetLocaleString("HarlotShot", gunner.GetName(), check.GetName()) : GetLocaleString("DefaultShot", gunner.GetName(), check.GetName(), ""));
                            break;
                        case IRole.Hunter:
                            SendWithQueue(GetLocaleString("DefaultShot", gunner.GetName(), check.GetName(), DbGroup.ShowRoles == false ? "" : $"{check.GetName()} {GetLocaleString("Was")} {GetDescription(check.PlayerRole)}"));
                            HunterFinalShot(check, KillMthd.Shoot);
                            break;
                        default:
                            SendWithQueue(GetLocaleString("DefaultShot", gunner.GetName(), check.GetName(), DbGroup.ShowRoles == false ? "" : $"{check.GetName()} {GetLocaleString("Was")} {GetDescription(check.PlayerRole)}"));
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
            var nightStart = DateTime.Now;
            if (CheckForGameEnd(true)) return;
            foreach (var p in Players)
            {
                p.Choice = 0;
                p.Choice2 = 0;
                p.CurrentQuestion = null;
                p.Votes = 0;
                p.DiedLastNight = false;
                p.BeingVisitedSameNightCount = 0;
                if (p.Bitten && !p.IsDead && !WolfRoles.Contains(p.PlayerRole))
                {
                    if (p.PlayerRole == IRole.Mason)
                        foreach (var m in Players.Where(x => x.PlayerRole == IRole.Mason & !x.IsDead && x.Id != p.Id))
                            Send(GetLocaleString("MasonConverted", p.GetName()), m.Id);

                    p.Bitten = false;
                    p.PlayerRole = IRole.Wolf;
                    p.Team = ITeam.Wolf;
                    p.HasDayAction = false;
                    p.HasNightAction = true;
                    p.RoleModel = 0;
                    p.ChangedRolesCount++;  //add count for double-shifter achv after converting to wolf
                    var msg = GetLocaleString("BittenTurned") + "\n";
                    var others = Players.GetPlayersForRoles(WolfRoles, exceptPlayer: p).Where(x => !x.IsDead).ToList();
                    if (others.Any())
                    {
                        var andStr = $" {GetLocaleString("And").Trim()} ";
                        msg += GetLocaleString("WolfTeam", others.Select(x => x.GetName(true)).Aggregate((current, a) => current + andStr + a));
                    }
                    Players.GetPlayerForRole(IRole.AlphaWolf, false).AlphaConvertCount++;

                    Send(msg, p.Id);

                }
            }
            CheckRoleChanges();     //so maybe if seer got converted to wolf, appseer will promote here
            if (CheckForGameEnd()) return;
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


            /* Role priority:
             * Wolf
             * Serial Killer
             * Cultist Hunter
             * Cult
             * Harlot
             * Seer
             * Fool
             * GA
             */

            #region Wolf Night

            var wolves = nightPlayers.GetPlayersForRoles(WolfRoles).ToList();

            if (CheckForGameEnd()) return;
            var ga = Players.FirstOrDefault(x => x.PlayerRole == IRole.GuardianAngel & !x.IsDead && x.Choice != 0 && x.Choice != -1);
            var voteWolves = wolves.Where(x => !x.Drunk);
            var voteWolvesCount = voteWolves.Count();

            if (voteWolves.Any())
            {
                //foreach (var w in voteWolves)
                //{
                //    var wchoice = Players.FirstOrDefault(x => x.Id == w.Choice);
                //    if (wchoice != null)
                //    {
                //        DBAction(w, wchoice, "Eat");
                //    }
                //    wchoice = Players.FirstOrDefault(x => x.Id == w.Choice2);
                //    if (wchoice != null)
                //    {
                //        DBAction(w, wchoice, "Eat");
                //    }
                //}
                var votechoice = voteWolves.Where(x => (x.Choice != 0 && x.Choice != -1) || (x.Choice2 != 0 && x.Choice2 != -1));

                List<int> choices = new List<int>();

                //choice1
                foreach (var w in votechoice)
                {
                    var p = Players.Where(x => x.Id == w.Choice);
                    foreach (var pl in p)
                        pl.Votes++;
                }
                choices.Add(Players.Where(x => x.Votes > 0).OrderByDescending(x => x.Votes).FirstOrDefault()?.Id ?? 0);


                //choice2 (will be 0 if wolfcub wasn't killed)
                foreach (var p in Players)
                    p.Votes = 0;
                foreach (var w in votechoice)
                {
                    var p = Players.Where(x => x.Id == w.Choice2 && x.Id != choices[0]);
                    foreach (var pl in p)
                        pl.Votes++;
                }
                choices.Add(Players.Where(x => x.Votes > 0).OrderByDescending(x => x.Votes).FirstOrDefault()?.Id ?? 0);
                int eatCount = 0;
                foreach (var choice in choices.Where(x => x != 0 && x != -1))
                {
                    if (!voteWolves.Any()) break; //if wolf dies from first choice, and was alone...
                    var target = Players.FirstOrDefault(x => x.Id == choice & !x.IsDead);
                    if (target != null)
                    {
                        target.BeingVisitedSameNightCount++;
                        if (ga?.Choice == target.Id &&
                            !(target.PlayerRole == IRole.Harlot && (target.Choice == 0 || target.Choice == -1))) //doesn't apply to harlot not home
                        {
                            foreach (var wolf in voteWolves)
                                Send(GetLocaleString("GuardBlockedWolf", target.GetName()), wolf.Id);
                            //Send(GetLocaleString("GuardSaved", target.Name), ga.Id);
                            //Send(GetLocaleString("GuardSavedYou"), target.Id);
                            target.WasSavedLastNight = true;
                        }
                        else
                        {
                            var bitten = voteWolves.Any(x => x.PlayerRole == IRole.AlphaWolf) && Program.R.Next(100) < Settings.AlphaWolfConversionChance;
                            var alpha = "";
                            if (bitten)
                                alpha = voteWolves.FirstOrDefault(x => x.PlayerRole == IRole.AlphaWolf).GetName();
                            //check if they are the harlot, and were home
                            switch (target.PlayerRole)
                            {
                                case IRole.Harlot:
                                    if (target.Choice == 0 || target.Choice == -1) //stayed home
                                    {
                                        if (bitten)
                                        {
                                            BitePlayer(target, voteWolves, alpha);
                                        }
                                        else
                                        {
                                            target.DiedLastNight = true;
                                            target.IsDead = true;
                                            target.TimeDied = DateTime.Now;
                                            target.KilledByRole = IRole.Wolf;
                                            DBKill(voteWolves, target, KillMthd.Eat);
                                            //SendWithQueue(DbGroup.ShowRoles != false
                                            //    ? GetLocaleString("HarlotEaten", target.GetName())
                                            //    : GetLocaleString("GenericDeathNoReveal", target.GetName()));
                                        }
                                        foreach (var w in voteWolves)
                                            AddAchievement(w, Achievements.DontStayHome);
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
                                        Send(GetLocaleString("WolfTeam", wolves.Select(x => x.GetName()).Aggregate((current, w) => current + ", " + w)), target.Id);
                                    }
                                    catch
                                    {
                                        // ignored
                                    }
                                    foreach (var w in wolves)
                                        Send(GetLocaleString("CursedBittenToWolves", $"{target.GetName()}"), w.Id);
                                    break;
                                case IRole.Drunk:
                                    if (bitten)
                                    {
                                        BitePlayer(target, voteWolves, alpha);
                                    }
                                    else
                                    {
                                        target.DiedLastNight = true;
                                        target.KilledByRole = IRole.Wolf;
                                        target.IsDead = true;
                                        target.TimeDied = DateTime.Now;
                                        DBKill(voteWolves, target, KillMthd.Eat);
                                        SendGif(GetLocaleString("WolvesEatYou"),
                                            GetRandomImage(Settings.VillagerDieImages), target.Id);
                                        foreach (var w in voteWolves)
                                        {
                                            var secondvictim = Players.FirstOrDefault(x => x.Id == choices[1]);
                                            Send(
                                                target != (secondvictim ?? target) ? //if the drunk is the first victim out of two, they block the second one. let's tell wolves
                                                GetLocaleString("WolvesEatDrunkBlockSecondKill", target.GetName(), secondvictim.GetName()) :
                                                GetLocaleString("WolvesEatDrunk", target.GetName()), w.Id);
                                            w.Drunk = true;
                                        }
                                    }
                                    break;
                                case IRole.Hunter:
                                    //hunter has a chance to kill....
                                    voteWolvesCount = voteWolves.Count();
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
                                                if (bitten)
                                                {
                                                    BitePlayer(target, voteWolves, alpha);
                                                }
                                                else
                                                {
                                                    SendGif(GetLocaleString("WolvesEatYou"),
                                                        GetRandomImage(Settings.VillagerDieImages), target.Id);
                                                    DBKill(voteWolves, target, KillMthd.Eat);
                                                    target.KilledByRole = IRole.Wolf;
                                                    target.IsDead = true;
                                                    target.TimeDied = DateTime.Now;
                                                    target.DiedLastNight = true;
                                                }
                                            }
                                            shotWuff.IsDead = true;
                                            if (shotWuff.PlayerRole == IRole.WolfCub)
                                                WolfCubKilled = true;
                                            shotWuff.TimeDied = DateTime.Now;
                                            shotWuff.DiedByVisitingKiller = true;
                                            shotWuff.KilledByRole = IRole.Hunter;
                                            shotWuff.DiedLastNight = true;
                                            DBKill(target, shotWuff, KillMthd.HunterShot);
                                        }
                                    }
                                    else
                                    {
                                        if (bitten)
                                        {
                                            BitePlayer(target, voteWolves, alpha);
                                        }
                                        else
                                        {
                                            SendGif(GetLocaleString("WolvesEatYou"),
                                                GetRandomImage(Settings.VillagerDieImages), target.Id);
                                            DBKill(voteWolves, target, KillMthd.Eat);
                                            target.KilledByRole = IRole.Wolf;
                                            target.IsDead = true;
                                            target.TimeDied = DateTime.Now;
                                            target.DiedLastNight = true;
                                        }
                                    }
                                    break;
                                case IRole.SerialKiller:
                                    //serial killer has 80% of winning the fight....
                                    if (Program.R.Next(100) < 80)
                                    {
                                        //serial killer wins...
                                        IPlayer shotWuff;
                                        try
                                        {
                                            shotWuff = voteWolves.ElementAt(Program.R.Next(voteWolves.Count()));
                                        }
                                        catch
                                        {
                                            shotWuff = voteWolves.FirstOrDefault();
                                        }
                                        shotWuff.IsDead = true;
                                        if (shotWuff.PlayerRole == IRole.WolfCub)
                                            WolfCubKilled = true;
                                        shotWuff.TimeDied = DateTime.Now;
                                        shotWuff.DiedByVisitingKiller = true;
                                        shotWuff.KilledByRole = IRole.SerialKiller;
                                        shotWuff.DiedLastNight = true;
                                        //SendWithQueue(GetLocaleString("SerialKillerKilledWolf", shotWuff.GetName()));
                                        DBKill(target, shotWuff, KillMthd.SerialKilled);
                                    }
                                    else
                                    {
                                        if (bitten)
                                        {
                                            BitePlayer(target, voteWolves, alpha);
                                        }
                                        else
                                        {
                                            target.KilledByRole = IRole.Wolf;
                                            target.IsDead = true;
                                            target.TimeDied = DateTime.Now;
                                            target.DiedLastNight = true;
                                            DBKill(voteWolves, target, KillMthd.Eat);
                                            SendGif(GetLocaleString("WolvesEatYou"),
                                                GetRandomImage(Settings.VillagerDieImages), target.Id);
                                        }
                                    }
                                    break;
                                default:
                                    if (bitten)
                                    {
                                        BitePlayer(target, voteWolves, alpha);
                                    }
                                    else
                                    {
                                        target.KilledByRole = IRole.Wolf;
                                        target.IsDead = true;
                                        target.TimeDied = DateTime.Now;
                                        target.DiedLastNight = true;
                                        if (target.PlayerRole == IRole.Sorcerer)
                                        {
                                            foreach (var w in voteWolves)
                                                AddAchievement(w, Achievements.NoSorcery);
                                        }
                                        DBKill(voteWolves, target, KillMthd.Eat);
                                        SendGif(GetLocaleString("WolvesEatYou"),
                                            GetRandomImage(Settings.VillagerDieImages), target.Id);
                                    }
                                    break;
                            }
                        }
                        eatCount++;
                    }
                    else
                    {
                        //no choice
                    }
                }
                if (eatCount == 2)
                {
                    var cub = Players.GetPlayerForRole(IRole.WolfCub, false);
                    if (cub != null)
                        AddAchievement(cub, Achievements.IHelped);
                }
                eatCount = 0;
            }
            WolfCubKilled = false;
            #endregion

            #region Serial Killer Night

            //give serial killer a chance!
            var sk = Players.FirstOrDefault(x => x.PlayerRole == IRole.SerialKiller & !x.IsDead);
            if (sk != null)
            {
                var skilled = Players.FirstOrDefault(x => x.Id == sk.Choice && !x.IsDead);
                if (skilled != null)
                {
                    skilled.BeingVisitedSameNightCount++;
                    if (ga?.Choice == skilled.Id)
                    {
                        Send(GetLocaleString("GuardBlockedKiller", skilled.GetName()), sk.Id);
                        skilled.WasSavedLastNight = true;
                        DBKill(sk, skilled, KillMthd.SerialKilled);
                        skilled.BeingVisitedSameNightCount++;
                    }
                    else
                    {
                        skilled.DiedLastNight = true;
                        skilled.IsDead = true;
                        if (skilled.PlayerRole == IRole.WolfCub)
                            WolfCubKilled = true;
                        skilled.TimeDied = DateTime.Now;
                        skilled.KilledByRole = IRole.SerialKiller;
                        DBKill(sk, skilled, KillMthd.SerialKilled);
                        if (WolfRoles.Contains(skilled.PlayerRole))
                            sk.SerialKilledWolvesCount++;
                    }
                }
            }

            #endregion

            if (Players == null)
                return;

            #region Cult Hunter Night

            //cult hunter
            var hunter = Players.GetPlayerForRole(IRole.CultistHunter);
            if (hunter != null)
            {
                var hunted = Players.FirstOrDefault(x => x.Id == hunter.Choice);
                if (hunted != null)
                {
                    hunted.BeingVisitedSameNightCount++;
                    DBAction(hunter, hunted, "Hunt");
                    if (hunted.PlayerRole == IRole.SerialKiller)
                    {
                        //awwwwww CH gets popped
                        DBKill(hunted, hunter, KillMthd.SerialKilled);
                        hunter.IsDead = true;
                        hunter.TimeDied = DateTime.Now;
                        hunter.DiedLastNight = true;
                        hunter.KilledByRole = IRole.SerialKiller;
                        hunter.DiedByVisitingKiller = true;
                    }
                    else if (hunted.IsDead)
                    {
                        Send(GetLocaleString("HunterVisitDead", hunted.GetName()), hunter.Id);
                    }
                    else if (hunted.PlayerRole == IRole.Cultist)
                    {
                        Send(GetLocaleString("HunterFindCultist", hunted.GetName()), hunter.Id);
                        hunted.IsDead = true;
                        hunted.TimeDied = DateTime.Now;
                        hunted.DiedLastNight = true;
                        hunter.CHHuntedCultCount++;
                        hunted.KilledByRole = IRole.CultistHunter;
                        DBKill(hunter, hunted, KillMthd.Hunt);
                    }
                    else
                    {
                        Send(GetLocaleString("HunterFailedToFind", hunted.GetName()), hunter.Id);
                    }
                }
            }

            #endregion

            #region Cult Night

            //CULT
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
                var votechoice = voteCult.Where(x => x.Choice != 0 && x.Choice != -1);
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
                        target.BeingVisitedSameNightCount++;
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
                                            newbie.KilledByRole = IRole.Hunter;
                                            newbie.DiedByVisitingKiller = true;
                                            DBKill(target, newbie, KillMthd.HunterCult);
                                            //notify everyone
                                            foreach (var c in voteCult)
                                            {
                                                Send(GetLocaleString("CultConvertHunter", newbie.GetName(), target.GetName()), c.Id);
                                            }
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
                                    newbie.KilledByRole = IRole.SerialKiller;
                                    newbie.DiedByVisitingKiller = true;
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
                                    newbie.KilledByRole = IRole.CultistHunter;
                                    newbie.DiedByVisitingKiller = true;
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
                                case IRole.Mason:
                                    //notify other masons....
                                    ConvertToCult(target, voteCult);
                                    foreach (var m in Players.Where(x => x.PlayerRole == IRole.Mason & !x.IsDead))
                                        Send(GetLocaleString("MasonConverted", target.GetName()), m.Id);
                                    break;
                                case IRole.Wolf:
                                case IRole.AlphaWolf:
                                case IRole.WolfCub:
                                    if (voteWolves.Any(x => (x.Choice != 0 && x.Choice != -1) || (x.Choice2 != 0 && x.Choice2 != -1))) //did wolves go eating?
                                    {
                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultVisitEmpty", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                    }
                                    else //stayed home!
                                    {
                                        //kill the newest cult member
                                        newbie.DiedLastNight = true;
                                        newbie.IsDead = true;
                                        newbie.TimeDied = DateTime.Now;
                                        newbie.KilledByRole = IRole.Wolf;
                                        newbie.DiedByVisitingKiller = true;
                                        DBKill(target, newbie, KillMthd.Eat);

                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultConvertWolf", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                        Send(GetLocaleString("CultAttempt"), target.Id); //only notify if they were home
                                    }
                                    break;
                                case IRole.GuardianAngel:
                                    if (target.Choice == 0 || target.Choice == -1) // stayed home
                                        ConvertToCult(target, voteCult, Settings.GuardianAngelConversionChance);
                                    else
                                    {
                                        foreach (var c in voteCult)
                                            Send(GetLocaleString("CultVisitEmpty", newbie.GetName(), target.GetName()), c.Id);
                                        //Send(GetLocaleString("CultAttempt"), target.Id);
                                    }
                                    break;
                                case IRole.Harlot:
                                    if (target.Choice == 0 || target.Choice == -1) // stayed home
                                        ConvertToCult(target, voteCult, Settings.HarlotConversionChance);
                                    else
                                    {
                                        foreach (var c in voteCult)
                                            Send(GetLocaleString("CultVisitEmpty", newbie.GetName(), target.GetName()), c.Id);
                                        //Send(GetLocaleString("CultAttempt"), target.Id);
                                    }
                                    break;
                                case IRole.Seer:
                                    ConvertToCult(target, voteCult, Settings.SeerConversionChance);
                                    break;
                                case IRole.Sorcerer:
                                    ConvertToCult(target, voteCult, Settings.SorcererConversionChance);
                                    break;
                                case IRole.Blacksmith:
                                    ConvertToCult(target, voteCult, Settings.BlacksmithConversionChance);
                                    break;
                                case IRole.Detective:
                                    ConvertToCult(target, voteCult, Settings.DetectiveConversionChance);
                                    break;
                                case IRole.Cursed:
                                    ConvertToCult(target, voteCult, Settings.CursedConversionChance);
                                    break;
                                case IRole.Prince:
                                    ConvertToCult(target, voteCult); //TODO: Decide conversion chances for Prince and Mayor!!
                                    break;
                                case IRole.Mayor:
                                    ConvertToCult(target, voteCult); //TODO: Decide conversion chances for Prince and Mayor!!
                                    break;
                                case IRole.Doppelgänger:
                                    ConvertToCult(target, voteCult, 0);
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

            #endregion

            if (Players == null)
            {
                CheckForGameEnd();
                return;
            }

            #region Harlot Night

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
                        target.BeingVisitedSameNightCount++;
                        switch (target.PlayerRole)
                        {
                            case IRole.Wolf:
                            case IRole.AlphaWolf:
                            case IRole.WolfCub:
                                harlot.IsDead = true;
                                harlot.TimeDied = DateTime.Now;
                                harlot.DiedLastNight = true;
                                harlot.DiedByVisitingKiller = true;
                                harlot.KilledByRole = IRole.Wolf;
                                DBKill(target, harlot, KillMthd.VisitWolf);
                                Send(GetLocaleString("HarlotFuckWolf", target.GetName()), harlot.Id);
                                break;
                            case IRole.SerialKiller:
                                harlot.IsDead = true;
                                harlot.TimeDied = DateTime.Now;
                                harlot.DiedLastNight = true;
                                harlot.DiedByVisitingKiller = true;
                                harlot.KilledByRole = IRole.SerialKiller;
                                DBKill(target, harlot, KillMthd.VisitKiller);
                                Send(GetLocaleString("HarlotFuckKiller", target.GetName()), harlot.Id);
                                break;
                            default:
                                if (target.DiedLastNight && (WolfRoles.Contains(target.KilledByRole) || target.KilledByRole == IRole.SerialKiller) && !target.DiedByVisitingKiller)
                                {
                                    harlot.IsDead = true;
                                    harlot.TimeDied = DateTime.Now;
                                    harlot.DiedLastNight = true;
                                    harlot.DiedByVisitingVictim = true;
                                    harlot.KilledByRole = target.KilledByRole;
                                    harlot.RoleModel = target.Id; //store who they visited
                                    DBKill(target, harlot, KillMthd.VisitVictim);
                                }
                                else
                                {
                                    Send(
                                        (target.PlayerRole == IRole.Cultist && Program.R.Next(100) < Settings.HarlotDiscoverCultChance) ?
                                            GetLocaleString("HarlotDiscoverCult", target.GetName()) :
                                            GetLocaleString("HarlotVisitNonWolf", target.GetName()),
                                        harlot.Id);
                                    if (!target.IsDead)
                                        Send(GetLocaleString("HarlotVisitYou"), target.Id);
                                }
                                break;
                        }
                    }
                }
            }

            #endregion

            #region Seer / Fool

            //let the seer know
            var seers = Players.Where(x => x.PlayerRole == IRole.Seer && !x.IsDead);
            if (seers.Any())
            {
                foreach (var seer in seers)
                {
                    var target = Players.FirstOrDefault(x => x.Id == seer.Choice);
                    if (target != null)
                    {
                        DBAction(seer, target, "See");
                        var role = target.PlayerRole;
                        switch (role)
                        {
                            case IRole.Beholder:
                                AddAchievement(seer, Achievements.ShouldHaveKnown);
                                break;
                            case IRole.Traitor:
                                role = Program.R.Next(100) > 50 ? IRole.Wolf : IRole.Villager;
                                break;
                            case IRole.WolfCub: //seer doesn't see wolf type
                            case IRole.AlphaWolf:
                                role = IRole.Wolf;
                                break;
                        }
                        Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(role)), seer.Id);
                    }
                }
            }
            var sorcerer = Players.FirstOrDefault(x => x.PlayerRole == IRole.Sorcerer && !x.IsDead);
            if (sorcerer != null)
            {
                var target = Players.FirstOrDefault(x => x.Id == sorcerer.Choice);
                if (target != null)
                {
                    DBAction(sorcerer, target, "See");
                    var role = target.PlayerRole;
                    switch (role)
                    {
                        case IRole.AlphaWolf:
                        case IRole.Wolf:
                        case IRole.WolfCub:
                            Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(IRole.Wolf)), sorcerer.Id);
                            break;
                        case IRole.Seer:
                            Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(role)), sorcerer.Id);
                            break;
                        default:
                            Send(GetLocaleString("SorcererOther", target.GetName()), sorcerer.Id);
                            break;
                    }
                }

            }
            var fool = Players.FirstOrDefault(x => x.PlayerRole == IRole.Fool && !x.IsDead);
            if (fool != null)
            {
                var target = Players.FirstOrDefault(x => x.Id == fool.Choice);
                if (target != null)
                {
                    var possibleRoles = Players.Where(x => !x.IsDead && x.Id != fool.Id && x.PlayerRole != IRole.Seer).Select(x => x.PlayerRole).ToList();
                    possibleRoles.Shuffle();
                    possibleRoles.Shuffle();
                    if (possibleRoles.Any())
                    {

                        //don't see wolf type!
                        if (WolfRoles.Contains(possibleRoles[0]))
                            possibleRoles[0] = IRole.Wolf;

                        //check if it's accurate
                        try
                        {
                            if (possibleRoles[0] == target.PlayerRole || (possibleRoles[0] == IRole.Wolf && WolfRoles.Contains(target.PlayerRole)))
                                fool.FoolCorrectSeeCount++;
                        }
                        catch
                        {
                            // ignored
                        }

                        Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(possibleRoles[0])), fool.Id);
                    }
                }
            }

            #endregion

            #region GA Night

            if (ga != null)
            {
                var save = Players.FirstOrDefault(x => x.Id == ga.Choice);
                if (save != null)
                {
                    //if (save != null)
                    //    DBAction(ga, save, "Guard");
                    save.BeingVisitedSameNightCount++;
                    if (save.WasSavedLastNight)
                    {
                        Send(GetLocaleString("GuardSaved", save.GetName()), ga.Id);
                        Send(GetLocaleString("GuardSavedYou"), save.Id);
                    }
                    else if (save.DiedLastNight)
                    {
                        Send(GetLocaleString("GuardEmptyHouse", save.GetName()), ga.Id);
                    }

                    //check for save's role, even if they weren't attacked!
                    switch (save.PlayerRole)
                    {
                        case IRole.AlphaWolf:
                        case IRole.WolfCub:
                        case IRole.Wolf:
                            if (Program.R.Next(100) > 50)
                            {
                                ga.IsDead = true;
                                ga.TimeDied = DateTime.Now;
                                ga.DiedLastNight = true;
                                ga.DiedByVisitingKiller = true;
                                ga.KilledByRole = IRole.Wolf;
                                DBKill(save, ga, KillMthd.GuardWolf);
                                Send(GetLocaleString("GuardWolf"), ga.Id);
                            }
                            else if (!save.WasSavedLastNight && !save.DiedLastNight)
                            //only send if GA survived and wolf wasn't attacked
                            {
                                Send(GetLocaleString("GuardNoAttack", save.GetName()), ga.Id);
                                ga.GAGuardWolfCount++;
                            }
                            break;
                        case IRole.SerialKiller:
                            ga.IsDead = true;
                            ga.TimeDied = DateTime.Now;
                            ga.DiedLastNight = true;
                            ga.DiedByVisitingKiller = true;
                            ga.KilledByRole = IRole.SerialKiller;
                            DBKill(save, ga, KillMthd.GuardKiller);
                            Send(GetLocaleString("GuardKiller"), ga.Id);
                            break;
                        default:
                            if (!save.WasSavedLastNight && !save.DiedLastNight) //only send if save wasn't attacked
                                Send(GetLocaleString("GuardNoAttack", save.GetName()), ga.Id);
                            break;
                    }

                    save.WasSavedLastNight = false;
                }
            }

            #endregion

            CheckRoleChanges();

            #region Night Death Notifications to Group


            var secret = DbGroup.ShowRoles == false;
            if (Players.Any(x => x.DiedLastNight))
            {
                foreach (var p in Players.Where(x => x.DiedLastNight))
                {
                    var msg = "";
                    if (secret)
                    {
                        SendWithQueue(GetLocaleString("GenericDeathNoReveal", p.GetName()));
                    }
                    else
                    {
                        //Killed by wolf
                        if (p.KilledByRole == IRole.Wolf && !p.DiedByVisitingKiller && !p.DiedByVisitingVictim)
                        {
                            switch (p.PlayerRole)
                            {
                                case IRole.ApprenticeSeer:
                                case IRole.Detective:
                                case IRole.Drunk:
                                case IRole.Fool:
                                case IRole.Gunner:
                                case IRole.Harlot:
                                case IRole.Mason:
                                case IRole.Seer:
                                case IRole.Sorcerer:
                                case IRole.WildChild:
                                    msg = GetLocaleString(p.PlayerRole.ToString() + "Eaten", p.GetName());
                                    break;
                                case IRole.GuardianAngel:
                                    msg = GetLocaleString("GuardianEaten", p.GetName());
                                    break;
                                default:
                                    msg = GetLocaleString("DefaultEaten", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
                                    break;
                            }
                        }
                        //killed by SK
                        else if (p.KilledByRole == IRole.SerialKiller && !p.DiedByVisitingKiller && !p.DiedByVisitingVictim)
                        {
                            switch (p.PlayerRole)
                            {
                                case IRole.Blacksmith:
                                case IRole.Cultist:
                                case IRole.Cupid:
                                case IRole.Drunk:
                                case IRole.GuardianAngel:
                                case IRole.Gunner:
                                case IRole.Mayor:
                                case IRole.Prince:
                                case IRole.Seer:
                                    msg = GetLocaleString(p.PlayerRole.ToString() + "Killed", p.GetName());
                                    break;

                                case IRole.Hunter:
                                    msg = null;
                                    SendWithQueue(GetLocaleString("DefaultKilled", p.GetName(),
                                        $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}"));
                                    HunterFinalShot(p, KillMthd.SerialKilled);
                                    break;
                                default:
                                    msg = GetLocaleString("DefaultKilled", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
                                    break;
                            }
                        }
                        //died by visiting
                        else
                        {
                            switch (p.PlayerRole)
                            {
                                case IRole.WolfCub:
                                case IRole.AlphaWolf:
                                case IRole.Wolf: //sk and hunter can kill
                                    if (p.PlayerRole == IRole.WolfCub)
                                        WolfCubKilled = true;
                                    if (p.KilledByRole == IRole.SerialKiller)
                                        msg = GetLocaleString("SerialKillerKilledWolf", p.GetName());
                                    else //died from hunter
                                        msg = GetLocaleString(voteWolvesCount > 1 ? "HunterShotWolfMulti" : "HunterShotWolf", p.GetName()) + " " + GetLocaleString("PlayerRoleWas", p.GetName(), GetDescription(p.PlayerRole));
                                    break;
                                case IRole.CultistHunter: //killed by sk
                                    msg = GetLocaleString("SerialKillerKilledCH", p.GetName());
                                    break;
                                case IRole.Cultist:
                                    switch (p.KilledByRole)
                                    {
                                        case IRole.CultistHunter:
                                            msg = GetLocaleString("HunterKilledCultist", p.GetName());
                                            break;
                                        case IRole.Hunter:
                                            msg = GetLocaleString("HunterKilledVisiter", p.GetName(), $"{GetDescription(p.PlayerRole)} {GetLocaleString("IsDead")}");
                                            break;
                                        case IRole.Wolf:
                                            msg = GetLocaleString("CultConvertWolfPublic", p.GetName());
                                            break;
                                        case IRole.SerialKiller:
                                            msg = GetLocaleString("CultConvertKillerPublic", p.GetName());
                                            break;
                                    }
                                    break;
                                case IRole.Harlot:
                                    switch (p.KilledByRole)
                                    {
                                        case IRole.Wolf:
                                            if (p.DiedByVisitingKiller)
                                                msg = GetLocaleString("HarlotFuckedWolfPublic", p.GetName());
                                            else if (p.DiedByVisitingVictim)
                                                msg = GetLocaleString("HarlotFuckedVictimPublic", p.GetName(), Players.FirstOrDefault(x => x.Id == p.RoleModel).GetName());
                                            break;
                                        case IRole.SerialKiller:
                                            if (p.DiedByVisitingKiller)
                                                msg = GetLocaleString("HarlotFuckKillerPublic", p.GetName());
                                            else if (p.DiedByVisitingVictim)
                                                msg = GetLocaleString("HarlotFuckedKilledPublic", p.GetName(), Players.FirstOrDefault(x => x.Id == p.RoleModel).GetName());
                                            break;
                                    }
                                    break;
                                case IRole.GuardianAngel:
                                    if (p.KilledByRole == IRole.Wolf)
                                        msg = GetLocaleString("GAGuardedWolf", p.GetName());
                                    else if (p.KilledByRole == IRole.SerialKiller)
                                        msg = GetLocaleString("GAGuardedKiller", p.GetName());
                                    break;
                            }
                        }
                    }
                    if (!String.IsNullOrEmpty(msg))
                        SendWithQueue(msg);
                    if (p.InLove)
                        KillLover(p);
                }

                var bloodyVictims = Players.Where(x => x.TimeDied > nightStart && x.IsDead);

                if (bloodyVictims.Count() >= 4)
                    foreach (var p in bloodyVictims)
                        AddAchievement(p, Achievements.BloodyNight);
            }
            else
            {
                if (IsRunning)
                    SendWithQueue(GetLocaleString("NoAttack"));
            }

            #endregion

            if (CheckForGameEnd()) return;

            //reset everything
            foreach (var p in Players)
            {
                p.DiedLastNight = false;
                p.Choice = 0;
                p.Votes = 0;
                if (p.BeingVisitedSameNightCount >= 3)
                {
                    p.BusyNight = true;
                }
                if (p.Bitten & !p.IsDead)
                {
                    Send(GetLocaleString("PlayerBitten"), p.Id);
                }
                p.BeingVisitedSameNightCount = 0;
            }

        }

        private bool CheckForGameEnd(bool checkbitten = false)
        {
            if (Players == null)
                return true;
            if (!IsRunning) return true;
            var alivePlayers = Players.Where(x => !x.IsDead);

            //first of all, check for traitor!
            if (alivePlayers.All(x => !WolfRoles.Contains(x.PlayerRole)))
            {
                var traitor = alivePlayers.FirstOrDefault(x => x.PlayerRole == IRole.Traitor);
                if (traitor != null)
                {
                    if (!checkbitten || alivePlayers.All(x => !x.Bitten)) //traitor should not turn wolf if bitten is about to turn
                    {
                        //traitor turns wolf!
                        traitor.PlayerRole = IRole.Wolf;
                        traitor.Team = ITeam.Wolf;
                        traitor.HasDayAction = false;
                        traitor.HasNightAction = true;
                        traitor.ChangedRolesCount++;
                        Send(GetLocaleString("TraitorTurnWolf"), traitor.Id);
                    }
                    else return false; //bitten is turning wolf! game doesn't end
                }
            }

            switch (alivePlayers?.Count())
            {
                case 0:
                    return DoGameEnd(ITeam.NoOne);
                case 1:
                    var p = alivePlayers.FirstOrDefault();
                    if (p.PlayerRole == IRole.Tanner || p.PlayerRole == IRole.Sorcerer)
                        return DoGameEnd(ITeam.NoOne);
                    else
                        return DoGameEnd(p.Team);
                case 2:
                    //check for lovers
                    if (alivePlayers.All(x => x.InLove))
                        return DoGameEnd(ITeam.Lovers);
                    //check for Hunter + SK / Wolf
                    if (alivePlayers.Any(x => x.PlayerRole == IRole.Hunter))
                    {
                        var other = alivePlayers.FirstOrDefault(x => x.PlayerRole != IRole.Hunter);
                        if (other == null)
                            return DoGameEnd(ITeam.Village);
                        if (other.PlayerRole == IRole.SerialKiller)
                            return DoGameEnd(ITeam.SKHunter);
                        if (WolfRoles.Contains(other.PlayerRole))
                        {
                            var hunter = alivePlayers.First(x => x.PlayerRole == IRole.Hunter);
                            if (Program.R.Next(100) < Settings.HunterKillWolfChanceBase)
                            {
                                SendWithQueue(GetLocaleString("HunterKillsWolfEnd", hunter.GetName(), other.GetName()));
                                other.IsDead = true;
                                DBKill(hunter, other, KillMthd.HunterShot);
                                return DoGameEnd(ITeam.Village);
                            }
                            else
                            {
                                SendWithQueue(GetLocaleString("WolfKillsHunterEnd", hunter.GetName(), other.GetName()));
                                hunter.IsDead = true;
                                DBKill(other, hunter, KillMthd.Eat);
                                return DoGameEnd(ITeam.Wolf);
                            }
                        }
                    }
                    //check for SK
                    if (alivePlayers.Any(x => x.PlayerRole == IRole.SerialKiller))
                        return DoGameEnd(ITeam.SerialKiller);
                    //check for cult
                    if (alivePlayers.Any(x => x.PlayerRole == IRole.Cultist))
                    {
                        var other = alivePlayers.FirstOrDefault(x => x.PlayerRole != IRole.Cultist);
                        if (other == null) //two cults
                            return DoGameEnd(ITeam.Cult);
                        switch (other.PlayerRole)
                        {
                            case IRole.Wolf:
                            case IRole.WolfCub:
                            case IRole.AlphaWolf:
                                return DoGameEnd(ITeam.Wolf);
                            case IRole.CultistHunter:
                                var cultist = alivePlayers.FirstOrDefault(x => x.PlayerRole == IRole.Cultist);
                                SendWithQueue(GetLocaleString("CHKillsCultistEnd", cultist.GetName(), other.GetName()));
                                DBKill(other, cultist, KillMthd.Hunt);
                                return DoGameEnd(ITeam.Village);
                            default:
                                //autoconvert the other
                                if (other.PlayerRole != IRole.Doppelgänger)
                                {
                                    other.PlayerRole = IRole.Cultist;
                                    other.Team = ITeam.Cult;
                                }
                                return DoGameEnd(ITeam.Cult);
                        }
                    }
                    break;
                default:
                    break;
            }


            if (alivePlayers.Any(x => x.Team == ITeam.SerialKiller)) //there is still SK alive, do nothing (surely more than two players)
                return false;

            //is everyone left a cultist?
            if (alivePlayers.All(x => x.Team == ITeam.Cult))
                return DoGameEnd(ITeam.Cult);

            //do the wolves outnumber the others?
            if (alivePlayers.Count(x => WolfRoles.Contains(x.PlayerRole)) >= alivePlayers.Count(x => !WolfRoles.Contains(x.PlayerRole)))
            {
                if (alivePlayers.Any(x => x.PlayerRole == IRole.Gunner && x.Bullet > 0))
                {
                    // do nothing, gunner is alive
                    foreach (var p in alivePlayers.Where(x => x.Team == ITeam.Village))
                        AddAchievement(p, Achievements.GunnerSaves);
                    return false;
                }
                return DoGameEnd(ITeam.Wolf);
            }

            if (alivePlayers.All(x => !WolfRoles.Contains(x.PlayerRole) && x.PlayerRole != IRole.Cultist && x.PlayerRole != IRole.SerialKiller)) //checks for cult and SK are actually useless...
                //no wolf, no cult, no SK... VG wins!
                return DoGameEnd(ITeam.Village);


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
                    var forbidden = lovers.Any(x => WolfRoles.Contains(x.PlayerRole)) && lovers.Any(x => x.PlayerRole == IRole.Villager);
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

                        //the winning tanner is the only one with DiedLastNight == true
                        if (team == ITeam.Tanner && !w.DiedLastNight)
                            continue;

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
                        if (Players.Count(x => !x.IsDead && WolfRoles.Contains(x.PlayerRole)) > 1)
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
                        msg = $"{GetLocaleString("PlayersAlive")}: {Players.Count(x => !x.IsDead)} / {Players.Count}\n" + Players.OrderBy(x => x.TimeDied).Aggregate(msg, (current, p) => current + $"\n{p.GetName()}");
                        break;
                    case "All":
                        msg = $"{GetLocaleString("PlayersAlive")}: {Players.Count(x => !x.IsDead)} / {Players.Count}\n" + Players.OrderBy(x => x.TimeDied).Aggregate("", (current, p) => current + ($"{p.GetName()}: {(p.IsDead ? (p.Fled ? GetLocaleString("RanAway") : GetLocaleString("Dead")) : GetLocaleString("Alive")) + " - " + GetDescription(p.PlayerRole) + (p.InLove ? "❤️" : "")} {(p.Won ? GetLocaleString("Won") : GetLocaleString("Lost"))}\n"));
                        break;
                    default:
                        msg = GetLocaleString("RemainingPlayersEnd") + Environment.NewLine;
                        msg = Players.Where(x => !x.IsDead).OrderBy(x => x.Team).Aggregate(msg, (current, p) => current + $"\n{p.GetName()}: {GetDescription(p.PlayerRole)} {GetLocaleString(p.Team + "TeamEnd")} {(p.InLove ? "❤️" : "")} {GetLocaleString(p.Won ? "Won" : "Lost")}");
                        break;
                }
                if (game.TimeStarted.HasValue)
                {
                    var endGame = game.TimeEnded.Value - game.TimeStarted.Value;
                    msg += "\n" + GetLocaleString("EndTime", endGame.ToString(@"hh\:mm\:ss"));
                }
                SendWithQueue(msg);
                //Program.Bot.SendTextMessage(ChatId, "[Enjoy playing? Support the developers and get some swag!](https://teespring.com/stores/werewolf-for-telegram)", parseMode: ParseMode.Markdown, disableWebPagePreview: true);
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
            NoOneCastLynch = true;
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

            var mayor = Players.FirstOrDefault(x => x.PlayerRole == IRole.Mayor & !x.IsDead);
            if (mayor != null && GameDay == 1)
            {
                var choices = new[]
                {
                    new[]
                    {
                        new InlineKeyboardButton(GetLocaleString("Reveal"), $"vote|{Program.ClientId}|reveal")
                    }
                }.ToList();
                SendMenu(choices, mayor, GetLocaleString("AskMayor"), QuestionType.Mayor);
            }

            var blacksmith = Players.FirstOrDefault(x => x.PlayerRole == IRole.Blacksmith & !x.IsDead & !x.HasUsedAbility);

            if (blacksmith != null)
            {
                var choices = new[]
                {
                    new[]
                    {
                        new InlineKeyboardButton(GetLocaleString("Yes"), $"vote|{Program.ClientId}|yes"), new InlineKeyboardButton(GetLocaleString("No"), $"vote|{Program.ClientId}|no")
                    }
                }.ToList();
                SendMenu(choices, blacksmith, GetLocaleString("SpreadDust"), QuestionType.SpreadSilver);
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
            if (Players == null) return;
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
                    case IRole.Sorcerer:
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
                    case IRole.AlphaWolf:
                    case IRole.WolfCub:
                        if (!_silverSpread)
                        {
                            targets = targetBase.Where(x => !WolfRoles.Contains(x.PlayerRole)).ToList();
                            msg = GetLocaleString("AskEat");
                            var others = targetBase.GetPlayersForRoles(WolfRoles, exceptPlayer: player).Where(x => !x.Drunk).ToList();
                            if (others.Any())
                            {
                                var andStr = $" {GetLocaleString("And").Trim()} ";
                                msg += GetLocaleString("DiscussWith", others.Select(x => x.GetName()).Aggregate((current, a) => current + andStr + a));
                            }
                            qtype = QuestionType.Kill;
                        }
                        else
                            msg = null;
                        break;
                    case IRole.Cultist:
                        //if (GameDay % 2 == 1)
                        {
                            targets = targetBase.Where(x => x.PlayerRole != IRole.Cultist).ToList();
                            msg = GetLocaleString("AskConvert");
                            var otherCults = targetBase.Where(x => x.PlayerRole == IRole.Cultist).ToList();
                            if (otherCults.Any())
                            {
                                var andStr = GetLocaleString("And");
                                msg += GetLocaleString("DiscussWith", otherCults.Select(x => x.GetName()).Aggregate((current, a) => current + andStr + a));
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
                var buttons = targets.Select(x => new[] { new InlineKeyboardButton(x.Name, $"vote|{Program.ClientId}|{x.Id}") }).ToList();
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
            } // alive players foreach
            _silverSpread = false;
        }

        #endregion

        #region Helpers
        public void CleanupButtons()
        {
            foreach (var id in _joinButtons)
            {
                Program.Bot.DeleteMessage(ChatId, id);
                Thread.Sleep(500);
            }
        }
        public void FleePlayer(int banid)
        {
            if (IsInitializing)
            {
                SendWithQueue("Cannot flee while game is initializing.  Try again once game is done starting.");
                return;
            }
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
                    _playerListChanged = true;
                    Players.Remove(p);
                    _requestPlayerListUpdate = true;
                    //SendWithQueue(GetLocaleString("CountPlayersRemain", Players.Count.ToBold()));
                }
            }
        }

        public void Kill()
        {
            //forces game to exit
            try
            {
                if (IsJoining) //try to remove the joining button...
                    Program.Bot.EditMessageCaption(ChatId, _joinMsgId, "", null);
            }
            catch
            {
                //ignored
            }
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
            hunterChoices.AddRange(Players.Where(x => !x.IsDead).Select(x => new[] { new InlineKeyboardButton(x.Name, $"vote|{Program.ClientId}|{x.Id}") }));
            hunterChoices.Add(new[] { new InlineKeyboardButton("Skip", $"vote|{Program.ClientId}|-1") });

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
                        if (killed.PlayerRole == IRole.WolfCub)
                            WolfCubKilled = true;
                        killed.TimeDied = DateTime.Now;
                        if (killed.PlayerRole == IRole.Wolf || killed.PlayerRole == IRole.AlphaWolf || killed.PlayerRole == IRole.WolfCub || killed.PlayerRole == IRole.SerialKiller)
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

        private int ChooseRandomPlayerId(IPlayer exclude, bool all = true)
        {
            try
            {
                var possible = exclude != null ? Players.Where(x => x.Id != exclude.Id).ToList() : Players.ToList();
                if (!all)
                    possible = possible.Where(x => !x.IsDead).ToList();
                possible.Shuffle();
                possible.Shuffle();
                return possible[0].Id;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
                Send("Unable to choose random player\n" + Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.ErrorGroup);
                return -1;
            }
        }

        //private int ChooseRandomPlayerId(IEnumerable<IPlayer> exclude)
        //{
        //    try
        //    {
        //        var possible = Players.Where(x => exclude.All(y => y?.TeleUser.Id != x?.TeleUser.Id)).ToList();
        //        possible.Shuffle();
        //        possible.Shuffle();
        //        return possible[0].Id;
        //    }
        //    catch
        //    {
        //        return -1;
        //    }
        //}

        internal static void ReplyToCallback(CallbackQuery query, string text = null, bool edit = true, bool showAlert = false, InlineKeyboardMarkup replyMarkup = null)
        {
            //first answer the callback
            Program.Bot.AnswerCallbackQuery(query.Id, edit ? null : text, showAlert);
            //edit the original message
            if (edit)
                Edit(query, text, replyMarkup);
        }

        internal static Task<Telegram.Bot.Types.Message> Edit(CallbackQuery query, string text, InlineKeyboardMarkup replyMarkup = null)
        {
            return Edit(query.Message.Chat.Id, query.Message.MessageId, text, replyMarkup);
        }

        internal static Task<Telegram.Bot.Types.Message> Edit(long id, int msgId, string text, InlineKeyboardMarkup replyMarkup = null)
        {
            Program.MessagesSent++;
            return Program.Bot.EditMessageText(id, msgId, text, replyMarkup: replyMarkup);
        }


        #endregion

        #region Database Helpers

        // ReSharper disable UnusedParameter.Local
        private void DBAction(IPlayer initator, IPlayer receiver, string action)
        {
            //return; //dropping actions.  We never use them, they just take up a massive amount of space in the database
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
            _playerListChanged = true;
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

            if (victim.LoverId == killer.Id && Time == GameTime.Night && method != KillMthd.LoverDied)
            {
                if (GameDay == 1) //killed lover on first night
                    AddAchievement(killer, Achievements.OhShi);
                else if (WolfRoles.Contains(killer.PlayerRole)) //wolf pack killed lover, not on first night
                    AddAchievement(killer, Achievements.ShouldveMentioned);
            }

        }

        private void KillLover(IPlayer victim)
        {
            var p = Players.FirstOrDefault(x => x.Id == victim.LoverId & !x.IsDead);
            if (p != null)
            {
                SendWithQueue(GetLocaleString("LoverDied", victim.GetName(), p.GetName(), DbGroup.ShowRoles == false ? "" : $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}"));
                DBKill(victim, p, KillMthd.LoverDied);
                p.IsDead = true;
                if (p.PlayerRole == IRole.WolfCub)
                    WolfCubKilled = true;
                p.TimeDied = DateTime.Now;
            }
            CheckRoleChanges();
            if (p?.PlayerRole == IRole.Hunter)
            {
                HunterFinalShot(p, KillMthd.LoverDied);
            }
        }

        private bool _longHaulReached;

        private void CheckLongHaul()
        {
            if (Players == null) return;
            if ((DateTime.Now - _timeStarted).Hours >= 1 & !_longHaulReached)
            {
                foreach (var p in Players.Where(x => !x.IsDead && !x.Fled))
                {
                    AddAchievement(p, Achievements.LongHaul);
                }
                _longHaulReached = true;
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
                        //if (!ach.HasFlag(Achievements.OHAIDER) && Players.Any(x => x.TeleUser.Id == Program.Para))
                        //    newAch = newAch | Achievements.OHAIDER;
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
                        if (!ach.HasFlag(Achievements.Dedicated) && p.GamePlayers.Count >= 100)
                            newAch = newAch | Achievements.Dedicated;
                        if (!ach.HasFlag(Achievements.Obsessed) && p.GamePlayers.Count >= 1000)
                            newAch = newAch | Achievements.Obsessed;
                        if (!ach.HasFlag(Achievements.Veteran) && p.GamePlayers.Count >= 500)
                            newAch = newAch | Achievements.Veteran;
                        if (!ach.HasFlag(Achievements.Masochist) && player.Won && player.PlayerRole == IRole.Tanner)
                            newAch = newAch | Achievements.Masochist;
                        if (!ach.HasFlag(Achievements.Wobble) && !player.IsDead && player.PlayerRole == IRole.Drunk && Players.Count >= 10)
                            newAch = newAch | Achievements.Wobble;
                        if (!ach.HasFlag(Achievements.Survivalist) && p.GamePlayers.Count(x => x.Survived) >= 100)
                            newAch = newAch | Achievements.Survivalist;
                        if (!ach.HasFlag(Achievements.MasonBrother) && player.PlayerRole == IRole.Mason && Players.Count(x => x.PlayerRole == IRole.Mason & !x.IsDead) >= 2)
                            newAch = newAch | Achievements.MasonBrother;
                        if (!ach.HasFlag(Achievements.ChangingSides) && player.OriginalRole != player.PlayerRole && player.Won)
                            newAch = newAch | Achievements.ChangingSides;
                        if (!ach.HasFlag(Achievements.LoneWolf) && Players.Count >= 10 && WolfRoles.Contains(player.PlayerRole) && Players.GetPlayersForRoles(WolfRoles, false).Count() == 1 && player.Won)
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
                        if (!ach.HasFlag(Achievements.CultistTracker) && player.PlayerRole == IRole.CultistHunter && player.CHHuntedCultCount >= 3)
                            newAch = newAch | Achievements.CultistTracker;
                        if (!ach.HasFlag(Achievements.ImNotDrunk) && player.PlayerRole == IRole.ClumsyGuy && player.ClumsyCorrectLynchCount >= 3)
                            newAch = newAch | Achievements.ImNotDrunk;
                        if (!ach.HasFlag(Achievements.WuffieCult) && player.PlayerRole == IRole.AlphaWolf && player.AlphaConvertCount >= 3)
                            newAch = newAch | Achievements.WuffieCult;
                        if (!ach.HasFlag(Achievements.DidYouGuardYourself) && player.PlayerRole == IRole.GuardianAngel && player.GAGuardWolfCount >= 3)
                            newAch = newAch | Achievements.DidYouGuardYourself;
                        if (!ach.HasFlag(Achievements.ThreeLittleWolves) && player.PlayerRole == IRole.Sorcerer && Players.GetPlayersForRoles(WolfRoles, true).Count() >= 3)
                            newAch = newAch | Achievements.ThreeLittleWolves;
                        if (!ach.HasFlag(Achievements.President) && player.PlayerRole == IRole.Mayor && player.MayorLynchAfterRevealCount >= 3)
                            newAch = newAch | Achievements.President;
                        if (!ach.HasFlag(Achievements.ItWasABusyNight) && player.BusyNight)
                            newAch = newAch | Achievements.ItWasABusyNight;

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
