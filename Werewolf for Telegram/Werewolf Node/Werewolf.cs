using Database;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Node.Helpers;
using Werewolf_Node.Models;
using Shared;
using Telegram.Bot;

// ReSharper disable PossibleMultipleEnumeration warning
#pragma warning disable 4014

namespace Werewolf_Node
{
    public class Werewolf : IDisposable
    {
        public long ChatId;
        public int GameDay, GameId;
        private int _secondsToAdd = 0;
        public List<IPlayer> Players = new List<IPlayer>();
        public bool IsRunning,
            IsJoining = true,
            KillTimer,
            IsInitializing,
            MessageQueueing = true,
            WolfCubKilled,
            NoOneCastLynch;
        public GameMode GameMode;
        public string Guid;
        private readonly InlineKeyboardMarkup _requestPMButton;
        public DateTime LastPlayersOutput = DateTime.Now;
        public GameTime Time;
        public string Language = "English SFW", ChatGroup;
        public Locale Locale;
        public Locale Fallback;
        public Group DbGroup;
        private bool _playerListChanged = true, _silverSpread, _sandmanSleep, _pacifistUsed, _doubleLynch;
        private DateTime _timeStarted;
        private TimeSpan? _timePlayed = null;
        public readonly IRole[] WolfRoles = { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Lycan };
        public List<long> HaveExtended = new List<long>();
        private readonly List<IPlayer> _joined = new List<IPlayer>();
        private readonly int _joinMsgId;
        private readonly string FirstMessage = "";
        private DateTime LastJoinButtonShowed = DateTime.MinValue;
        private readonly InlineKeyboardMarkup _joinButton;
        private readonly List<int> _joinButtons = new List<int>();
        private int _playerListId = 0;
        public bool RandomMode = false;
        public bool ShowRolesOnDeath, SecretLynch, ShowIDs, AllowNSFW, ThiefFull, AllowArsonist, BurningOverkill;
        public bool SecretLynchShowVoters, SecretLynchShowVotes;
        public bool ShufflePlayerList;
        public string ShowRolesEnd;
        private DateTime lastGrave = DateTime.MinValue, secondLastGrave = DateTime.MinValue;
        private List<IRole> PossibleRoles;
        private const string GifPrefix = "https://tgwerewolf.com/gifs/";

        public List<string> VillagerDieImages,
            WolfWin,
            WolvesWin,
            VillagersWin,
            NoWinner,
            StartGame,
            StartChaosGame,
            TannerWin,
            CultWins,
            SerialKillerWins,
            LoversWin,
            SKKilled,
            ArsonistWins,
            BurnToDeath;

        public Dictionary<int, string> CustomWinMessages = new Dictionary<int, string>
        {
            // Permanent
            { 786889199, "Kindness is like snow, it beautifies everything it covers ❄️" }, // As a reward for insane donations to keep up our bots :)

            // Expires 04 May 2021, Christmas Event Prize
            { 927424174, "در تو، من نیست \nو در من سراسر توست\nدر انتها؛ \nآنچه رفته ست میان من و تو\nتو خالی شده ای و من سراسر پُر\nآری!\nتو باخته ای و من سراسر بُرد ...\n\nیه بازی دیگه، یه بار دیگه اثبات پرو بودن ℳεɦ∂ɨ❤️{ ωσℓƒ }❤️ رو مشاهده میکنید...😎\nبار دیگر شاهد برد بی نظیر مهدی و مهارت اون توی بازی بودید\nبرید برا بچه محلاتون تعریف کنید🤞" },

            // Expires 04 May 2021, Christmas Event Prize
            { 588510620, "Whether you win or lose… You can always come out ahead by learning from the experience\n~ Kinnice" }
        };

        #region Constructor
        /// <summary>
        /// Starts a new instance of a werewolf game
        /// </summary>
        /// <param name="chatid">Id of the group starting the game</param>
        /// <param name="u">User that started the game</param>
        /// <param name="chatGroup">Name of the group starting the game</param>
        /// <param name="chaos">Chaos mode yes or no</param>
        public Werewolf(long chatid, User u, string chatGroup, GameMode gameMode)
        {
            try
            {
                do
                {
                    Guid = Convert.ToBase64String(System.Guid.NewGuid().ToByteArray()).Substring(0, 22); // Since a GUID is always 128 bits, we can omit the "==" that we know will always be present at the end
                }
                while (!Guid.All(x => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".Contains(x)));

                VillagerDieImages = Settings.VillagerDieImages.ToList();
                WolfWin = Settings.WolfWin.ToList();
                WolvesWin = Settings.WolvesWin.ToList();
                VillagersWin = Settings.VillagersWin.ToList();
                NoWinner = Settings.NoWinner.ToList();
                StartGame = Settings.StartGame.ToList();
                StartChaosGame = Settings.StartChaosGame.ToList();
                TannerWin = Settings.TannerWin.ToList();
                CultWins = Settings.CultWins.ToList();
                SerialKillerWins = Settings.SerialKillerWins.ToList();
                LoversWin = Settings.LoversWin.ToList();
                SKKilled = Settings.SKKilled.ToList();
                ArsonistWins = Settings.ArsonistWins.ToList();
                BurnToDeath = Settings.BurnToDeath.ToList();

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
                        var memberCount = Program.Bot.GetChatMemberCountAsync(chatId: chatid).Result;
                        DbGroup.MemberCount = memberCount;

                        db.SaveChanges();
                    }
                    catch
                    {
                        // ignored
                    }
                    AllowNSFW = DbGroup.HasFlag(GroupConfig.AllowNSFW);

                    var player = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                    if (player?.CustomGifSet != null)
                    {
                        var gifset = JsonConvert.DeserializeObject<CustomGifData>(player.CustomGifSet);
                        if (gifset.Approved == true)
                        {
                            if (!(gifset.NSFW == true && !AllowNSFW))
                            {
                                if (gifset.StartChaosGame != null)
                                {
                                    StartChaosGame.Clear();
                                    StartChaosGame.Add(gifset.StartChaosGame);
                                }
                                if (gifset.StartGame != null)
                                {
                                    StartGame.Clear();
                                    StartGame.Add(gifset.StartGame);
                                }
                            }
                        }
                    }

                    DbGroup.UpdateFlags();
                    ShowIDs = DbGroup.HasFlag(GroupConfig.ShowIDs);
                    ShufflePlayerList = DbGroup.HasFlag(GroupConfig.ShufflePlayerList);
                    RandomMode = DbGroup.HasFlag(GroupConfig.RandomMode);
                    db.SaveChanges();

                    var modes = Enum.GetValues(typeof(GameMode)).Cast<GameMode>();
                    if (RandomMode)
                    {
                        GameMode = modes.ElementAt(Program.R.Next(modes.Count()));

                        ThiefFull = Program.R.Next(100) < 50;
                        SecretLynch = Program.R.Next(100) < 50;
                        ShowRolesOnDeath = Program.R.Next(100) < 50;
                        SecretLynchShowVotes = Program.R.Next(100) < 50;
                        SecretLynchShowVoters = Program.R.Next(100) < 50;
                        AllowArsonist = Program.R.Next(100) < 50;
                        BurningOverkill = Program.R.Next(100) < 1;
                        var r = Program.R.Next(100);
                        if (r < 33)
                            ShowRolesEnd = "None";
                        else if (r < 67)
                            ShowRolesEnd = "Living";
                        else
                            ShowRolesEnd = "All";

                    }
                    else
                    {
                        //decide if chaos or not
                        GameMode = DbGroup.Mode == "Player" ? gameMode : modes.FirstOrDefault(x => x.ToString() == DbGroup.Mode);
                        ShowRolesEnd = DbGroup.ShowRolesEnd;
                        ThiefFull = DbGroup.HasFlag(GroupConfig.ThiefFull);
                        SecretLynch = DbGroup.HasFlag(GroupConfig.EnableSecretLynch);
                        ShowRolesOnDeath = DbGroup.HasFlag(GroupConfig.ShowRolesDeath);
                        SecretLynchShowVotes = DbGroup.HasFlag(GroupConfig.SecretLynchShowVotes);
                        SecretLynchShowVoters = DbGroup.HasFlag(GroupConfig.SecretLynchShowVoters);
                        AllowArsonist = DbGroup.HasFlag(GroupConfig.AllowArsonist);
                        BurningOverkill = DbGroup.HasFlag(GroupConfig.BurningOverkill);
                    }


                    LoadLanguage(DbGroup.Language, DbGroup.HasFlag(GroupConfig.RandomLangVariant));

                    _requestPMButton = new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithUrl("Start Me", "http://t.me/" + Program.Me.Username) });
                    //AddPlayer(u);
                }

                var deeplink = $"{Program.ClientId}{Guid}";
                //create our button
                _joinButton = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithUrl(GetLocaleString("JoinButton"),$"https://t.me/{Program.Me.Username}?start=join" + deeplink)
                });

                switch (GameMode)
                {
                    case GameMode.Chaos:
                        FirstMessage = GetLocaleString("PlayerStartedChaosGame", u.FirstName);
#if RELEASE
                        _joinMsgId = Program.Bot.SendDocumentAsync(chatId: ChatId, document: new InputFileId(GetRandomImage(StartChaosGame)), caption: FirstMessage, replyMarkup: _joinButton).Result.MessageId;
#else
                        _joinMsgId = Program.Bot.SendTextMessageAsync(chatId: chatid, text: $"<a href='{GifPrefix}{GetRandomImage(StartChaosGame)}.mp4'>\u200C</a>{FirstMessage.FormatHTML()}", replyMarkup: _joinButton, parseMode: ParseMode.Html).Result.MessageId;
#endif
                        break;


                    default:
                        FirstMessage = GetLocaleString("PlayerStartedGame", u.FirstName);
#if RELEASE
                        _joinMsgId = Program.Bot.SendDocumentAsync(chatId: ChatId, document: new InputFileId(GetRandomImage(StartGame)), caption: FirstMessage, replyMarkup: _joinButton).Result.MessageId;
#else
                        _joinMsgId = Program.Bot.SendTextMessageAsync(chatId: chatid, text: $"<a href='{GifPrefix}{GetRandomImage(StartGame)}.mp4'>\u200C</a>{FirstMessage.FormatHTML()}", replyMarkup: _joinButton, parseMode: ParseMode.Html).Result.MessageId;
#endif
                        break;
                }

                // This can stay turned off now I think. Can enable it again if players don't get it at all
                // SendWithQueue(GetLocaleString("NoAutoJoin", u.Username != null ? ("@" + u.Username) : u.FirstName.ToBold()));
                SendPlayerList(true);

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
        public void LoadLanguage(string language, bool randomVariant = false)
        {
            try
            {
                if (randomVariant)
                {
                    var langfiles = Program.Languages.Select(x => x.Value);
                    var langbase = langfiles.First(x => x.FileName == language).Base;
                    var baseFiles = langfiles.Where(x => x.Base == langbase);
                    if (baseFiles.Any(x => x.FileName.IndexOf("nsfw", StringComparison.InvariantCultureIgnoreCase) < 0))
                        baseFiles = baseFiles.Where(x => x.FileName.IndexOf("nsfw", StringComparison.InvariantCultureIgnoreCase) < 0);
                    var chosen = baseFiles.ElementAt(Program.R.Next(baseFiles.Count()));

                    Locale = new Locale
                    {
                        Language = chosen.FileName,
                        Base = chosen.Base,
                        File = chosen.Doc
                    };

#if DEBUG
                    SendWithQueue("Picked Language: " + chosen.FileName + ".xml");
#endif
                }
                else
                {
                    var file = Program.Languages[language];
                    Locale = new Locale
                    {
                        Language = language,
                        Base = file.Base,
                        File = file.Doc
                    };
                }
                Language = Locale.Language;

                // also load fallback file
                using (var db = new WWContext())
                {
                    // if fallback doesn't exist, it will just be ignored
                    Language fallback = db.Language.FirstOrDefault(x => x.Base == Locale.Base && x.FileName != language && x.IsDefault);
                    if (fallback != null)
                    {
                        if (Program.Languages.ContainsKey(fallback.FileName))
                        {
                            var file = Program.Languages[fallback.FileName];
                            Fallback = new Locale
                            {
                                Language = fallback.FileName,
                                Base = file.Base,
                                File = file.Doc
                            };
                        }
                    }
                }
            }
            catch
            {
                if (language != "English" || randomVariant)
                    LoadLanguage("English", false);
            }
        }

        private string GetSpecialString(string key)
        {
            if (SpecialStrings.Strings.TryGetValue(key, out string res))
                return res;
            else
                return null;
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
                var special = GetSpecialString(key);
                if (special != null)
                    return String.Format(special.FormatHTML(), args).Replace("\\n", Environment.NewLine);

                var strings = Locale.File.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key) ??
                              Fallback?.File.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key) ??
                              Program.English.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                if (strings != null)
                {
                    var values = strings.Descendants("value");
                    var choice = Program.R.Next(values.Count());
                    var selected = values.ElementAt(choice).Value;

                    //disable bluetexting /join!
                    if (selected.ToLower().Contains("/join"))
                        throw new Exception("/join found in the string, using the English file.");

                    if (String.IsNullOrWhiteSpace(selected))
                    {
                        strings = Program.English.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                        values = strings.Descendants("value");
                        choice = Program.R.Next(values.Count());
                        selected = values.ElementAt(choice).Value;
                    }

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
                int secondsElapsed = 0;
                for (var i = 0; i < Settings.GameJoinTime; i++)
                {
                    if (Players == null) //killed extra game
                        return;

                    if (KillTimer) //forcestart
                    {
                        KillTimer = false;
                        break;
                    }

                    _requestPlayerListUpdate = true;

                    if (count != Players.Count) //if a player joined, add time
                    {
                        i = Math.Min(i, Math.Max(120, i - 30));
                        count = Players.Count;
                    }

                    if (secondsElapsed++ % 30 == 0 && _joined.Any()) //every 30 seconds, say in group who have joined
                    {
                        SendWithQueue(GetLocaleString("HaveJoined", _joined.Aggregate("", (cur, p) => cur + p.GetName() + (ShowIDs ? $" (ID: <code>{p.TeleUser.Id}</code>)\n" : ", ")).TrimEnd(',', ' ') + (ShowIDs ? "" : " ")));
                        _joined.Clear();
                    }

                    try
                    {
                        Telegram.Bot.Types.Message r = null;

                        var importantSeconds = new[] { 10, 30, 60 }; //notify when time is running out
                        foreach (var s in importantSeconds)
                        {
                            if (i == Settings.GameJoinTime - s)
                            {
                                var str = s == 60 ? GetLocaleString("MinuteLeftToJoin") : GetLocaleString("SecondsLeftToJoin", s.ToString().ToBold());
                                r = Program.Bot.SendTextMessageAsync(chatId: ChatId, text: str, parseMode: ParseMode.Html, replyMarkup: _joinButton).Result;
                                break;
                            }
                        }

                        if (_secondsToAdd != 0)
                        {
                            i = Math.Max(i - _secondsToAdd, Settings.GameJoinTime - Settings.MaxJoinTime);

                            if (Settings.GameJoinTime > i)
                                r = Program.Bot.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: GetLocaleString(
                                        _secondsToAdd > 0 ? "SecondsAdded" : "SecondsRemoved",
                                        Math.Abs(_secondsToAdd).ToString().ToBold(),
                                        TimeSpan.FromSeconds(Settings.GameJoinTime - i).ToString(@"mm\:ss").ToBold()
                                    ), parseMode: ParseMode.Html, replyMarkup: _joinButton
                                ).Result;

                            _secondsToAdd = 0;
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
                    Thread.Sleep(1000);
                }
                Program.Bot.EditMessageReplyMarkupAsync(chatId: ChatId, messageId: _joinMsgId, replyMarkup: null);
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
                _playerListChanged = true;


                if (Players.Count(x => (x.GifPack?.Approved ?? false) && x.DonationLevel >= 10) > 0)
                {
                    var cMsg = "Players with custom gif packs:\n";
                    var customs = Players.Where(x => (x.GifPack?.Approved ?? false) && x.DonationLevel >= 10);
                    if (!AllowNSFW)
                        customs = customs.Where(x => !x.GifPack.NSFW);
                    if (customs.Any(x => x.GifPack.CultWins != null))
                        CultWins = customs.Where(x => x.GifPack.CultWins != null).Select(x => x.GifPack.CultWins).ToList();
                    if (customs.Any(x => x.GifPack.LoversWin != null))
                        LoversWin = customs.Where(x => x.GifPack.LoversWin != null).Select(x => x.GifPack.LoversWin).ToList();
                    if (customs.Any(x => x.GifPack.NoWinner != null))
                        NoWinner = customs.Where(x => x.GifPack.NoWinner != null).Select(x => x.GifPack.NoWinner).ToList();
                    if (customs.Any(x => x.GifPack.SerialKillerWins != null))
                        SerialKillerWins = customs.Where(x => x.GifPack.SerialKillerWins != null).Select(x => x.GifPack.SerialKillerWins).ToList();
                    if (customs.Any(x => x.GifPack.TannerWin != null))
                        TannerWin = customs.Where(x => x.GifPack.TannerWin != null).Select(x => x.GifPack.TannerWin).ToList();
                    if (customs.Any(x => x.GifPack.VillagerDieImage != null))
                        VillagerDieImages = customs.Where(x => x.GifPack.VillagerDieImage != null).Select(x => x.GifPack.VillagerDieImage).ToList();
                    if (customs.Any(x => x.GifPack.VillagersWin != null))
                        VillagersWin = customs.Where(x => x.GifPack.VillagersWin != null).Select(x => x.GifPack.VillagersWin).ToList();
                    if (customs.Any(x => x.GifPack.WolfWin != null))
                        WolfWin = customs.Where(x => x.GifPack.WolfWin != null).Select(x => x.GifPack.WolfWin).ToList();
                    if (customs.Any(x => x.GifPack.WolvesWin != null))
                        WolvesWin = customs.Where(x => x.GifPack.WolvesWin != null).Select(x => x.GifPack.WolvesWin).ToList();
                    if (customs.Any(x => x.GifPack.SKKilled != null))
                        SKKilled = customs.Where(x => x.GifPack.SKKilled != null).Select(x => x.GifPack.SKKilled).ToList();
                    if (customs.Any(x => x.GifPack.ArsonistWins != null))
                        ArsonistWins = customs.Where(x => x.GifPack.ArsonistWins != null).Select(x => x.GifPack.ArsonistWins).ToList();
                    if (customs.Any(x => x.GifPack.BurnToDeath != null))
                        BurnToDeath = customs.Where(x => x.GifPack.BurnToDeath != null).Select(x => x.GifPack.BurnToDeath).ToList();
                    foreach (var p in customs)
                    {
                        cMsg += p.GetName() + Environment.NewLine;
                    }
                    Send(cMsg);
                }

                IsRunning = true;
                AssignRoles();
                //create new game for database
                using (var db = new WWContext())
                {
                    _timeStarted = DateTime.Now;
                    var game = new Database.Game
                    {
                        GroupName = ChatGroup,
                        TimeStarted = _timeStarted,
                        GroupId = ChatId,
                        GrpId = int.Parse(DbGroup.Id.ToString()),
                        Mode = GameMode.ToString(),
#if BETA
                        Beta = true,
#endif
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
                if (ex.InnerException != null)
                    msg += ex.InnerException.Message;
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
                LogAllExceptions(ex);
                Send("Something just went terribly wrong, I had to cancel the game....\n" + ex.Message);
#if DEBUG
                Send(ex.StackTrace);
#else
                //this would be a duplicate
                //Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{ex.Message}\n{ex.StackTrace}", Program.ErrorGroup);
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
        public void AddPlayer(User u)
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
                if (p.Name.StartsWith("/") || String.IsNullOrEmpty(p.Name) || p.Name.Trim().ToLower() == "skip" || p.Name.Trim().ToLower() == GetLocaleString("Skip").ToLower())
                {
                    Send(GetLocaleString("ChangeNameToJoin",
                        String.IsNullOrWhiteSpace(u.Username) ? u.FirstName + " " + u.LastName : "@" + u.Username), p.Id);
                    return;
                }
                if (Players.Any(x => x.Name == p.Name))
                {
                    Send(GetLocaleString("NameExists", p.GetName(), p.TeleUser.Username), p.Id);
                    return;
                }
                if (Players.Count >= Settings.MaxPlayers)
                {
                    Send(GetLocaleString("PlayerLimitReached"), p.Id);
                    return;
                }
                //check one more time
                if (IsInitializing || !IsJoining) return;
                //add player
                Players.Add(p);
                _joined.Add(p);
                var groupname = String.IsNullOrWhiteSpace(DbGroup.GroupLink) ? ChatGroup : $"<a href=\"{DbGroup.GroupLink}\">{ChatGroup.FormatHTML()}</a>";
                Send(GetLocaleString("YouJoined", groupname), p.Id);

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
                    p.DonationLevel = user.DonationLevel ?? 0;
                    p.Founder = user.Founder ?? false;
                    user.UserName = u.Username;
                    user.Name = $"{u.FirstName} {u.LastName}".Trim();
                    if (!String.IsNullOrEmpty(user.CustomGifSet))
                        p.GifPack = JsonConvert.DeserializeObject<CustomGifData>(user.CustomGifSet);
                    if (user.NewAchievements == null)
                        user.NewAchievements = new BitArray(200).ToByteArray();
                    // switch achv system
                    //SwitchAchievementsSystem(p);

                    /* 
                     * Executrix will do this job for now, that will hopefully work better than this did before
                     * 
                    if (ChatId == Settings.VeteranChatId)
                    {
                        if (!(p.NewAchievements.HasFlag(AchievementsReworked.Veteran)))
                        {
                            Helpers.Helpers.KickChatMember(ChatId, user.TelegramId);
                            Players.Remove(p);
                            return;
                        }
                    }
                    */

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
                _playerListChanged = true;
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
                if (!DbGroup.HasFlag(GroupConfig.AllowFlee) && !IsJoining && IsRunning)
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
                    if (DbGroup.HasFlag(GroupConfig.ShowRolesDeath))
                        SendWithQueue(GetLocaleString("PlayerRoleWas", p.GetName(), GetDescription(p.PlayerRole)));

                    CheckRoleChanges();
                    //add the 'kill'
                    DBKill(p, p, KillMthd.Flee);
                    CheckForGameEnd();
                }
                else if (IsJoining && !IsInitializing)// really, should never be both joining and initializing but....
                {
                    Players.Remove(p);
                    _playerListChanged = true;
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
                if (!IsRunning) return; // ignore button presses after the game ended

                //first off, what was I asking them?
                var args = query.Data.Split('|');
                //0 - vote
                //1 - clientid
                //2 - gameid
                //3 - QuestionTypeId
                //4 - choiceid
                var player = Players.FirstOrDefault(x => x.Id == query.From.Id && !x.IsDead);

                QuestionType qtype = (QuestionType)int.Parse(args[3]);
                string choice = args[4];

                if (player == null) return;

                #region Reveal at any time roles
                if (qtype == QuestionType.Mayor && player.PlayerRole == IRole.Mayor && choice == "reveal" && !player.HasUsedAbility)
                {
                    player.HasUsedAbility = true;
                    SendWithQueue(GetLocaleString("MayorReveal", player.GetName()));

                    //Program.MessagesSent++;
                    ReplyToCallback(query,
                        GetLocaleString("ChoiceAccepted"));

                    return;
                }
                else if (qtype == QuestionType.Mayor && player.PlayerRole == IRole.Mayor && choice == "reveal" && player.HasUsedAbility)
                    return;

                if (qtype == QuestionType.Pacifist && player.PlayerRole == IRole.Pacifist && choice == "peace" && !player.HasUsedAbility)
                {
                    player.HasUsedAbility = true;
                    _pacifistUsed = true;
                    _doubleLynch = false; // peace overrides trouble
                    SendWithQueue(GetLocaleString("PacifistNoLynch", player.GetName()));

                    //Program.MessagesSent++;
                    ReplyToCallback(query,
                        GetLocaleString("ChoiceAccepted"));
                    return;
                }
                else if (qtype == QuestionType.Pacifist && player.PlayerRole == IRole.Pacifist && choice == "peace" && player.HasUsedAbility)
                    return;
                #endregion


                if (player.CurrentQuestion == null || player.CurrentQuestion.QType != qtype || !player.CurrentQuestion.ValidAnswers.Contains(query.Data))
                {
                    return;
                }

                if (qtype == QuestionType.SpreadSilver && player.PlayerRole == IRole.Blacksmith && player.CurrentQuestion.QType == QuestionType.SpreadSilver)
                {
                    if (choice == "yes")
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

                if (qtype == QuestionType.Sandman && player.PlayerRole == IRole.Sandman && player.CurrentQuestion.QType == QuestionType.Sandman)
                {
                    if (choice == "yes")
                    {
                        player.HasUsedAbility = true;
                        _sandmanSleep = true;
                        SendWithQueue(GetLocaleString("SandmanSleepAll", player.GetName()));
                    }

                    ReplyToCallback(query,
                        GetLocaleString("ChoiceAccepted"));
                    player.CurrentQuestion = null;
                    return;
                }

                if (qtype == QuestionType.Trouble && player.PlayerRole == IRole.Troublemaker && player.CurrentQuestion.QType == QuestionType.Trouble && !player.HasUsedAbility)
                {
                    if (choice == "yes")
                    {
                        player.HasUsedAbility = true;
                        _doubleLynch = true;
                        _pacifistUsed = false; // trouble overrides peace
                        SendWithQueue(GetLocaleString("TroublemakerDoubleLynch", player.GetName()));
                    }

                    ReplyToCallback(query, GetLocaleString("ChoiceAccepted"));
                    player.CurrentQuestion = null;
                    return;
                }

                if (choice == "-1")
                {
                    if (qtype == QuestionType.Kill2 && player.CurrentQuestion.QType == QuestionType.Kill2)
                        player.Choice2 = -1;
                    else if (qtype == player.CurrentQuestion.QType)
                        player.Choice = -1;
                    //Program.MessagesSent++;
                    ReplyToCallback(query,
                        GetLocaleString("ChoiceAccepted") + $" - {GetLocaleString("Skip")}");
                    player.CurrentQuestion = null;
                    return;
                }


                if (qtype == QuestionType.Kill2 && player.CurrentQuestion.QType == QuestionType.Kill2)
                    player.Choice2 = long.Parse(choice);
                else if (qtype == player.CurrentQuestion.QType)
                    player.Choice = long.Parse(choice);

                if (qtype == QuestionType.Lynch && player.PlayerRole == IRole.ClumsyGuy && player.CurrentQuestion.QType == QuestionType.Lynch)
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

                if (choice == "-2" && qtype == QuestionType.Douse && player.PlayerRole == IRole.Arsonist && player.CurrentQuestion.QType == QuestionType.Douse)
                {
                    player.Choice = -2;
                    //Program.MessagesSent++;
                    ReplyToCallback(query,
                        GetLocaleString("ChoiceAccepted") + $" - {GetLocaleString("Spark")}");
                    player.CurrentQuestion = null;
                    return;
                }

                var target = Players.FirstOrDefault(x => player.CurrentQuestion.QType == QuestionType.Kill2 ? x.Id == player.Choice2 : x.Id == player.Choice);
                if (target == null)
                {
                    Send(GetLocaleString("NoPlayerName"), query.From.Id);
                    return;
                }

                if (qtype == QuestionType.Kill2 && WolfRoles.Contains(player.PlayerRole) && player.CurrentQuestion.QType == QuestionType.Kill2)
                {
                    var others = Players.GetPlayersForRoles(WolfRoles, exceptPlayer: player);
                    foreach (var w in others)
                    {
                        Send(GetLocaleString("WolfVotedKill", player.GetName(), target.GetName()), w.Id);
                    }
                }

                var clearCurrent = true;
                if (qtype == QuestionType.Kill && WolfRoles.Contains(player.PlayerRole) && player.CurrentQuestion.QType == QuestionType.Kill)
                {
                    var others = Players.GetPlayersForRoles(WolfRoles, exceptPlayer: player);
                    foreach (var w in others)
                    {
                        Send(GetLocaleString("WolfVotedKill", player.GetName(), target.GetName()), w.Id);
                    }
                    if (WolfCubKilled)
                    {
                        //need to let them have another menu for second kill
                        var targets = Players.Where(x => !WolfRoles.Contains(x.PlayerRole) & x.PlayerRole != IRole.SnowWolf & !x.IsDead && x.Id != player.Choice).ToList();
                        var msg = GetLocaleString("AskEat");
                        var newqtype = QuestionType.Kill2;
                        var buttons = targets.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"vote|{Program.ClientId}|{Guid}|{(int)newqtype}|{x.Id}") }).ToList();
                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetLocaleString("Skip"), $"vote|{Program.ClientId}|{Guid}|{(int)newqtype}|-1") });
                        SendMenu(buttons, player, msg, newqtype);
                        clearCurrent = false;
                    }
                }
                if (qtype == QuestionType.RoleModel && player.PlayerRole == IRole.WildChild && player.CurrentQuestion.QType == QuestionType.RoleModel)
                {
                    player.RoleModel = target.Id;
                    player.Choice = -1;
                }
                if (qtype == QuestionType.Lover1 && player.PlayerRole == IRole.Cupid && player.CurrentQuestion.QType == QuestionType.Lover1)
                {
                    var lover1 = Players.FirstOrDefault(x => x.Id == player.Choice);

                    if (lover1 != null)
                    {
                        if (lover1.Id == player.Id)
                            AddAchievement(player, AchievementsReworked.SelfLoving);
                        lover1.InLove = true;
                        //send menu for second choice....
                        var secondChoices = Players.Where(x => !x.IsDead && x.Id != lover1.Id).ToList();
                        var buttons =
                            secondChoices.Select(
                                x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Lover2}|{x.Id}") }).ToList();
                        player.Choice = 0;
                        //Program.MessagesSent++;
                        ReplyToCallback(query,
                            GetLocaleString("ChoiceAccepted") + " - " + target.Name);

                        SendMenu(buttons, player, GetLocaleString("AskCupid2"), QuestionType.Lover2);
                    }
                    return;
                }
                if (qtype == QuestionType.Lover2 && player.PlayerRole == IRole.Cupid && player.CurrentQuestion.QType == QuestionType.Lover2)
                {
                    var lover11 = Players.FirstOrDefault(x => x.InLove);
                    if (lover11 == null)
                        return;
                    lover11.LoverId = player.Choice;
                    lover11.InLove = true;

                    var id = lover11.Id;
                    var lover2 = Players.FirstOrDefault(x => x.Id == player.Choice);
                    if (lover2 == null)
                        return;
                    if (lover2.Id == player.Id)
                        AddAchievement(player, AchievementsReworked.SelfLoving);
                    lover2.InLove = true;
                    lover2.LoverId = id;
                    player.Choice = -1;
                }

                if (player.PlayerRole == IRole.Doppelgänger && player.CurrentQuestion.QType == QuestionType.RoleModel)
                {
                    player.RoleModel = target.Id;
                    player.Choice = -1;
                }

                if (qtype == QuestionType.Convert && player.PlayerRole == IRole.Cultist && player.CurrentQuestion.QType == QuestionType.Convert)
                {
                    var others =
                        Players.Where(
                            x => !x.IsDead && x.PlayerRole == IRole.Cultist && x.Id != player.Id);
                    foreach (var w in others)
                    {
                        Send(GetLocaleString("CultistVotedConvert", player.GetName(), target.GetName()), w.Id);
                    }
                }


                if (qtype == QuestionType.Lynch && player.CurrentQuestion.QType == QuestionType.Lynch)
                {
                    if (!DbGroup.HasFlag(GroupConfig.EnableSecretLynch))
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
                    //First Stone counter does not reset its value to 0
                    //else
                    //    player.FirstStone = 0;

                    if (player.FirstStone == 5)
                    {
                        AddAchievement(player, AchievementsReworked.FirstStone);
                    }
                }
                //Program.MessagesSent++;
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

        private Task<Telegram.Bot.Types.Message> Send(string message, long id = 0, bool clearKeyboard = false, InlineKeyboardMarkup menu = null, bool notify = false, bool preview = false)
        {
            if (id == 0)
                id = ChatId;
            return Program.Send(message, id, clearKeyboard, menu, game: this, notify: notify, preview: preview);
        }

        private void SendGif(string text, string image, long id = 0)
        {
            //Program.MessagesSent++;
            if (id == 0)
                id = ChatId;
            //Log.WriteLine($"{id} -> {image} {text}");

            if (!String.IsNullOrWhiteSpace(image))
#if RELEASE
                Program.Bot.SendDocumentAsync(chatId: id, document: new InputFileId(image), caption: text);
#else
                Send($"<a href='{GifPrefix}{image}.mp4'>\u200C</a>{text}", id, preview: true);
#endif
            else
                Send(text, id, preview: false);
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
            public bool Notify { get; set; }

            public Message(string msg, string gifid = null, bool requestPM = false, bool notify = false)
            {
                Msg = msg;
                GifId = gifid;
                RequestPM = requestPM;
                Notify = notify;
            }
        }

        private readonly Queue<Message> _messageQueue = new Queue<Message>();
        private bool _requestPlayerListUpdate = false;
        private void GroupQueue()
        {
            try
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
                    while (_messageQueue.Count > 0 && !byteMax)
                    {
                        i++;
                        var m = _messageQueue.Peek();
                        if (m == null) continue;

                        if (m.Joining)
                        {
                            _messageQueue.Dequeue();
                            if (_playerListId == 0)
                            {
                                try
                                {
                                    _playerListId = Send(m.Msg).Result.MessageId;
                                }
                                catch
                                {
                                    //ignored
                                }
                            }
                            else
                                Program.Bot.EditMessageTextAsync(chatId: ChatId, messageId: _playerListId, text: m.Msg, parseMode: ParseMode.Html, disableWebPagePreview: true);
                            continue;
                        }

                        if (!String.IsNullOrEmpty(m.GifId))
                        {
                            if (!String.IsNullOrEmpty(final))
                                Send(final);
                            Thread.Sleep(1000);

                            _messageQueue.Dequeue();
                            SendGif(m.Msg, m.GifId);
                            Thread.Sleep(1000);
                            final = "";
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
                                Send(final, notify: true, preview: true);
                        }

                    }
                    Thread.Sleep(4000);

                }
                //do one last send
                final = "";
                while (_messageQueue.Count > 0)
                {
                    var m = _messageQueue.Dequeue();
                    if (m == null) continue;
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
                    Send(final, preview: true);
            }
            catch (Exception e)
            {
                LogAllExceptions(e);
            }
        }

        private void SendPlayerList(bool joining = false)
        {
            if (!_playerListChanged && (!ShufflePlayerList || joining)) return;
            if (Players == null) return;
            try
            {
                new Thread(() =>
                {
                    try
                    {
                        var players = new List<IPlayer>(Players);
                        var msg = "";
                        if (joining)
                        {
                            msg = $"#players: {players.Count}\n" +
                            players.Aggregate("", (current, p) => current + ($"{p.GetName()}\n"));
                        }
                        else
                        {
                            LastPlayersOutput = DateTime.Now;
                            msg =
                                $"{GetLocaleString("PlayersAlive")}: {players.Count(x => !x.IsDead)}/{players.Count}\n";
                            var loveEmojiList = new List<string> { "❤️", "🧡", "💛", "💚", "💙", "💜", "🤎" };
                            var loveEmoji = IsDateAnywhere(14, 2, 2021) ? loveEmojiList[Program.R.Next(0, loveEmojiList.Count)] : "❤️";
                            if (ShufflePlayerList)
                            {
                                msg += players.Where(x => x.IsDead).OrderBy(x => x.TimeDied)
                                    .Aggregate("",
                                        (current, p) =>
                                            current +
                                            p.GetName(dead: true) + ": " + (p.Fled ? GetLocaleString("RanAway") : GetLocaleString("Dead")) + (DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? " - " + GetDescription(p.PlayerRole) + (p.InLove ? loveEmoji : "") : "") + "\n");

                                msg += players.Where(x => !x.IsDead).OrderBy(x => Program.R.Next())
                                    .Aggregate("",
                                        (current, p) =>
                                            current +
                                            p.GetName() + ": " + GetLocaleString("Alive") + "\n");
                            }
                            else
                            {
                                //Thread.Sleep(4500); //wait a moment before sending
                                msg +=
                                   players.OrderBy(x => x.TimeDied)
                                       .Aggregate("",
                                           (current, p) =>
                                               current +
                                               ($"{p.GetName(dead: p.IsDead)}: {(p.IsDead ? ((p.Fled ? GetLocaleString("RanAway") : GetLocaleString("Dead")) + (DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? " - " + GetDescription(p.PlayerRole) + (p.InLove ? "❤️" : "") : "")) : GetLocaleString("Alive"))}\n"));
                                //{(p.HasUsedAbility & !p.IsDead && new[] { IRole.Prince, IRole.Mayor, IRole.Gunner, IRole.Blacksmith }.Contains(p.PlayerRole) ? " - " + GetDescription(p.PlayerRole) : "")}  //OLD CODE SHOWING KNOWN ROLES
                            }
                        }
                        _playerListChanged = false;
                        SendWithQueue(new Message(msg) { PlayerList = true, Joining = joining });
                    }
                    catch (Exception exc)
                    {
                        LogException(exc);
                    }

                }).Start();
            }
            catch (Exception ex)
            {
                LogAllExceptions(ex);
            }
        }

        public void OutputPlayers()
        {
            if (!((DateTime.Now - LastPlayersOutput).TotalSeconds > (10))) return;
            LastPlayersOutput = DateTime.Now;
            try
            {
                Program.Bot.SendTextMessageAsync(chatId: ChatId, text: GetLocaleString(_playerListId != 0 ? "LatestList" : "UnableToGetList"), parseMode: ParseMode.Html, replyToMessageId: _playerListId);
            }
            catch { }
        }

        public async void ShowJoinButton()
        {
            if (!IsJoining) return;
            if (!((DateTime.Now - LastJoinButtonShowed).TotalSeconds > (15))) return;
            LastJoinButtonShowed = DateTime.Now;
            try
            {
                var r = await Program.Bot.SendTextMessageAsync(chatId: ChatId, text: GetLocaleString("JoinByButton"), parseMode: ParseMode.Html, replyMarkup: _joinButton);
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

        private void AssignRoles()
        {
            try
            {
                var roleflags = (IRole)(DbGroup.RoleFlags ?? 0);
                var rolesToAssign = GameBalancing.Balance(roleflags, Players.Count, GameMode == Shared.GameMode.Chaos, BurningOverkill, out PossibleRoles);

                //shuffle things
                Players.Shuffle();
                Players.Shuffle();
                Players.Shuffle();
                Players.Shuffle();
                Players.Shuffle();
                rolesToAssign.Shuffle();
                rolesToAssign.Shuffle();


                // special roles for events
                // valentines this time
                if (IsDateAnywhere(14, 02, 2021) && !rolesToAssign.Any(x => x == IRole.Cupid))
                {
                    var toReplace = rolesToAssign.FindIndex(x => x == IRole.Villager || x == IRole.Mason);
                    if (toReplace != -1) rolesToAssign[toReplace] = IRole.Cupid;
                }


#if DEBUG
                //force roles for testing
                IRole[] requiredRoles = new IRole[]
                {
                    IRole.Wolf,
                    IRole.Gunner
                };
                int requiredCount = requiredRoles.Length;

                for (int i = 0; i < rolesToAssign.Count; i++)
                {
                    if (i < requiredCount)
                        rolesToAssign[i] = requiredRoles[i];
                    else
                        rolesToAssign[i] = IRole.Villager;
                }

                SendWithQueue("<b>DEBUG MODE!</b>\nAssigned roles:\n" + 
                    string.Join("\n", Enumerable.Range(0, Players.Count).Select(i => $"{Players[i].GetName()}: {rolesToAssign[i]}")));
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
                {
                    p.CultLeader = p.PlayerRole == IRole.Cultist;
                }

            }
            catch (Exception ex)
            {
                Send($"Error while assigning roles: {ex.Message}\nPlease start a new game");
                LogException(ex);
                Thread.Sleep(1000);
                Program.RemoveGame(this);
            }
        }

        private void SetRoleAttributes()
        {
            foreach (var p in Players)
            {
                SetTeam(p);
            }
        }

        private static void SetTeam(IPlayer p)
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
                case IRole.WolfMan:
                case IRole.Pacifist:
                case IRole.WiseElder:
                case IRole.Blacksmith:
                case IRole.Troublemaker:
                case IRole.Fool:
                case IRole.Harlot:
                case IRole.CultistHunter:
                case IRole.Seer:
                case IRole.GuardianAngel:
                case IRole.WildChild:
                case IRole.Cupid:
                case IRole.Sandman:
                case IRole.Oracle:
                case IRole.Chemist:
                case IRole.Detective:
                case IRole.Gunner:
                case IRole.Spumpkin:
                case IRole.Augur:
                case IRole.GraveDigger:
                    p.Team = ITeam.Village;
                    break;
                case IRole.Doppelgänger:
                case IRole.Thief:
                    p.Team = ITeam.Thief;
                    break;
                case IRole.Sorcerer:
                case IRole.AlphaWolf:
                case IRole.WolfCub:
                case IRole.Wolf:
                case IRole.Lycan:
                case IRole.SnowWolf:
                    p.Team = ITeam.Wolf;
                    break;
                case IRole.Tanner:
                    p.Team = ITeam.Tanner;
                    break;
                case IRole.Cultist:
                    p.Team = ITeam.Cult;
                    break;
                case IRole.SerialKiller:
                    p.Team = ITeam.SerialKiller;
                    break;
                case IRole.Arsonist:
                    p.Team = ITeam.Arsonist;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void NotifyRoles()
        {
            if (Players == null) return; //how the hell?

            if (ChatId == -1001341772435) //publish roles in alpha testing group
            {
                SendWithQueue(string.Join("\n", Players.Select(x => x.Name + ": " + x.PlayerRole.ToString())));
            }

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
                catch (AggregateException ae)
                {
                    LogException(ae);
                }
                catch (Exception e)
                {
                    LogAllExceptions(e);
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
                        var seer = Players?.FirstOrDefault(x => x.PlayerRole == IRole.Seer && !x.IsDead);
                        if (seer != null)
                            msg += GetLocaleString("BeholderSeer", $"{seer.GetName()}");
                        else
                            msg += "  " + GetLocaleString("NoSeer");
                        return msg;
                    case IRole.Mason:
                        msg = GetLocaleString("RoleInfoMason");
                        if (Players?.Count(x => x?.PlayerRole == IRole.Mason && !x.IsDead) > 1)
                        {
                            msg += " " + GetLocaleString("MasonTeam", Players.Where(x => x.PlayerRole == IRole.Mason && !x.IsDead).Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next)).Trim();
                        }
                        return msg;
                    case IRole.Doppelgänger:
                        return GetLocaleString("RoleInfoDoppelganger");
                    case IRole.Thief:
                        if (ThiefFull)
                            return GetLocaleString("RoleInfoThiefFull");
                        else
                            return GetLocaleString("RoleInfoThief");
                    case IRole.Wolf:
                    case IRole.WolfCub:
                    case IRole.AlphaWolf:
                    case IRole.Lycan:
                    case IRole.SnowWolf:
                        msg = GetLocaleString($"RoleInfo{role}");
                        var wolves = Players?.Where(x => !x.IsDead && (WolfRoles.Contains(x.PlayerRole) || x.PlayerRole == IRole.SnowWolf));
                        if (wolves?.Count() > 1)
                        {
                            if (wolves.Any(x => x.PlayerRole == IRole.SnowWolf))
                            {
                                msg += " " + GetLocaleString("WolfPackSnowList",
                                    wolves.Where(x => WolfRoles.Contains(x.PlayerRole)).Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next),
                                    wolves.First(x => x.PlayerRole == IRole.SnowWolf).GetName());
                            }
                            else
                            {
                                msg += " " + GetLocaleString("WolfPackList",
                                    wolves.Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next));
                            }
                        }
                        return msg;
                    case IRole.Cultist:
                        msg = GetLocaleString("RoleInfoCultist");
                        if (Players?.Count(x => x.PlayerRole == IRole.Cultist && !x.IsDead) > 1)
                        {
                            msg += " " + GetLocaleString("CultistsList",
                                Players?.Where(x => x.PlayerRole == IRole.Cultist && !x.IsDead).Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next));
                        }
                        return msg;
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
                    Transform(aps, IRole.Seer, TransformationMethod.ApprenticeSeer, roleModel: ds);
                }
            }

            CheckWildChild(checkbitten);
            CheckDoppelganger(checkbitten);

            var wolves = Players.GetPlayersForRoles(WolfRoles.Concat(new[] { IRole.SnowWolf }).ToArray());
            if (wolves.Count() >= 7)
            {
                foreach (var w in wolves)
                {
                    AddAchievement(w, AchievementsReworked.PackHunter);
                }
            }

            var seers = Players.GetPlayersForRoles(new[] { IRole.Seer });
            if (seers.Count() > 1)
            {
                foreach (var s in seers)
                    AddAchievement(s, AchievementsReworked.DoubleVision);
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
                //CreateLovers();, will already be called in NotifyLovers
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
                var exist = Players.FirstOrDefault(x => x.InLove) ?? AddLover();
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
            if (loversNotify.Count != 2)
            {
                CreateLovers();
                loversNotify = Players.Where(x => x.InLove).ToList();
            }

            foreach (var lover in loversNotify)
            {
                if (lover.SpeedDating)
                    AddAchievement(lover, AchievementsReworked.OnlineDating);
                if (lover.PlayerRole == IRole.Doppelgänger && lover.RoleModel == lover.LoverId)
                    AddAchievement(lover, AchievementsReworked.DeepLove);
                if (loversNotify.Any(x => x.PlayerRole == IRole.Seer) && loversNotify.Any(x => x.PlayerRole == IRole.Sorcerer))
                    AddAchievement(lover, AchievementsReworked.SeeingBetweenTeams);
            }

            Send(GetLocaleString("CupidChosen", loversNotify[0].GetName()), loversNotify[1].Id);
            Send(GetLocaleString("CupidChosen", loversNotify[1].GetName()), loversNotify[0].Id);
        }

        private IPlayer AddLover(IPlayer existing = null)
        {
            var loverId = ChooseRandomPlayerId(existing);
            var lover = Players.FirstOrDefault(x => x.Id == loverId);
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
                        Transform(wc, IRole.Wolf, TransformationMethod.WildChild, roleModel: rm);
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
                    Transform(p, rm.PlayerRole, TransformationMethod.Doppelgänger,
                        newRoleModel: rm.RoleModel, bullet: new[] { IRole.Spumpkin, IRole.Gunner }.Contains(rm.PlayerRole) ? (int?)2 : null, hasUsedAbility: false, roleModel: rm);
                }
            }
        }

        private void BitePlayer(IPlayer target, string alpha)
        {
            target.Bitten = true;
            foreach (var wolf in Players.GetPlayersForRoles(WolfRoles.Concat(new[] { IRole.SnowWolf }).ToArray()))
                Send(
                    GetLocaleString("PlayerBittenWolves", target.GetName(),
                        alpha), wolf.Id);
        }

        /// <summary>
        /// Transforms the player to a new role, checks for achievements before and after and notifies the player and possible teammates
        /// </summary>
        /// <param name="p">The player whose role is to be changed</param>
        /// <param name="toRole">New role for the player</param>
        /// <param name="method">Method of transformation</param>
        /// <param name="newRoleModel">New role model of player. Since role models are mandatory if there are any, defaults to 0 (no role model)</param>
        /// <param name="newTeamMembers">Team members to notify, depending on transformation method, if any</param>
        /// <param name="bullet">new bullet count of player, or null if it shouldn't change</param>
        /// <param name="hasUsedAbility">new HasUsedAbility value of player, or null if it shouldn't change</param>
        /// <param name="roleModel">The role model of the player to transform if they were DG or WC, or the old seer if they were apprentice, or the victim if they were thief</param>
        private void Transform(IPlayer p, IRole toRole, TransformationMethod method, long newRoleModel = 0, IEnumerable<IPlayer> newTeamMembers = null, int? bullet = null, bool? hasUsedAbility = null, IPlayer roleModel = null, IEnumerable<IPlayer> oldTeamMates = null)
        {
            if (p.IsDead)
            {
                // most transformations don't work for dead people
                switch (method)
                {
                    case TransformationMethod.KillElder:
                    case TransformationMethod.AutoConvertToCult:
                        break;
                    default:
                        return;
                }
            }
            if (toRole == IRole.Thief && !ThiefFull) toRole = IRole.Villager;
            // increase change roles count
            p.ChangedRolesCount++;

            // check for possible achievements before transformation
            if (p.PlayerRole == IRole.WiseElder)
                AddAchievement(p, AchievementsReworked.ILostMyWisdom);
            if (p.PlayerRole == IRole.WolfMan && method == TransformationMethod.AlphaBitten)
                AddAchievement(p, AchievementsReworked.JustABeardyGuy);
            if (p.Id == newRoleModel)
                AddAchievement(p, AchievementsReworked.Indestructible);

            // transformation specific additions
            if (method == TransformationMethod.ConvertToCult)
            {
                p.ConvertedToCult = true;
                p.DayCult = GameDay;
            }
            if (method == TransformationMethod.AutoConvertToCult)
                p.ConvertedToCult = true;

            // notify masons if given
            if (p.PlayerRole == IRole.Mason && toRole != IRole.Mason && oldTeamMates != null)
                foreach (var m in oldTeamMates)
                    Send(GetLocaleString("MasonConverted", p.GetName()), m.Id);

            // notify cultists if given
            if (p.PlayerRole == IRole.Cultist && toRole != IRole.Cultist && oldTeamMates != null)
                foreach (var c in oldTeamMates)
                    Send(GetLocaleString("CultistBitten", p.GetName()), c.Id);

            // actually transform the player
            if (bullet.HasValue) p.Bullet = bullet.Value;
            if (hasUsedAbility.HasValue) p.HasUsedAbility = hasUsedAbility.Value;
            p.RoleModel = newRoleModel;
            p.PlayerRole = toRole;
            SetTeam(p);

            // role specific after-actions
            #region Method-specific
            switch (method)
            {
                #region Cursed
                case TransformationMethod.BiteCursed:
                    var msg = GetLocaleString("CursedBitten");
                    var snowwolf = Players.GetPlayerForRole(IRole.SnowWolf);
                    if (snowwolf != null)
                    {
                        Send(GetLocaleString("CursedBittenToWolves", p.GetName()), snowwolf.Id);
                        msg += " " + GetLocaleString("WolfPackSnowList",
                            newTeamMembers.Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next),
                            snowwolf.GetName());
                    }
                    else
                    {
                        msg += " " + GetLocaleString("WolfPackList",
                            newTeamMembers.Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next));
                    }
                    try
                    {
                        Send(msg, p.Id);
                    }
                    catch
                    {
                        //ignored
                    }
                    foreach (var w in newTeamMembers)
                        Send(GetLocaleString("CursedBittenToWolves", p.GetName()), w.Id);
                    break;
                #endregion
                #region Traitor
                case TransformationMethod.Traitor:
                    Send(GetLocaleString("TraitorTurnWolf"), p.Id);
                    break;
                #endregion
                #region Apprentice Seer
                case TransformationMethod.ApprenticeSeer:
                    Send(GetLocaleString("ApprenticeNowSeer", roleModel?.GetName() ?? GetDescription(IRole.Seer)), p.Id);
                    var beholder = Players.FirstOrDefault(x => x.PlayerRole == IRole.Beholder & !x.IsDead);
                    if (beholder != null)
                        Send(GetLocaleString("BeholderNewSeer", p.GetName(), roleModel?.GetName() ?? GetDescription(IRole.Seer)), beholder.Id);
                    break;
                #endregion
                #region Cult
                case TransformationMethod.ConvertToCult:
                    var cultists = Players.GetPlayersForRoles(new[] { IRole.Cultist }, exceptPlayer: p);
                    Send(GetLocaleString("CultConvertYou"), p.Id);
                    Send(GetLocaleString("CultTeam", cultists.Select(x => x.GetName()).Aggregate((a, b) => a + ", " + b)), p.Id);
                    var cultMsg = GetLocaleString("CultJoin", $"{p.GetName()}");
                    cultMsg += "\n" + GetLocaleString("CultistsList", Players?.Where(x => x.PlayerRole == IRole.Cultist && !x.IsDead).Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next));
                    foreach (var c in cultists)
                        Send(cultMsg, c.Id);
                    break;
                #endregion
                #region Wild Child
                case TransformationMethod.WildChild:
                    var wolves = Players.GetPlayersForRoles(WolfRoles, exceptPlayer: p);
                    var snowwolf1 = Players.GetPlayerForRole(IRole.SnowWolf);
                    var teammates = string.Join(", ", wolves.Select(x => x.GetName()));

                    //notify other wolves
                    foreach (var w in wolves)
                    {
                        Send(GetLocaleString("WildChildToWolves", p.GetName()), w.Id);
                    }

                    if (snowwolf1 != null)
                    {
                        Send(GetLocaleString("WildChildToWolves", p.GetName()), snowwolf1.Id);
                        Send(GetLocaleString("WildChildSnowTransform", roleModel.GetName(), teammates, snowwolf1.GetName()), p.Id);
                    }
                    else Send(GetLocaleString("WildChildTransform", roleModel.GetName(), teammates), p.Id);
                    break;
                #endregion
                #region Doppelgänger
                case TransformationMethod.Doppelgänger:
                    var teammates2 = "";
                    if (!new[] { IRole.Mason, IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Cultist, IRole.WildChild, IRole.Lycan, IRole.SnowWolf }.Contains(p.PlayerRole))
                    {
                        //tell them their new role
                        Send(GetRoleInfo(p.PlayerRole), p.Id);
                    }
                    switch (p.PlayerRole)
                    {
                        case IRole.Mason:
                            foreach (var w in Players.Where(x => x.PlayerRole == IRole.Mason & !x.IsDead && x.Id != p.Id))
                            {
                                Send(GetLocaleString("DGToMason", $"{p.GetName()}"), w.Id);
                                teammates2 += $"{w.GetName()}" + ", ";
                            }
                            Send(GetLocaleString("DGTransformToMason", roleModel.GetName(), teammates2), p.Id);
                            break;
                        case IRole.Seer:
                            var bh = Players.FirstOrDefault(x => x.PlayerRole == IRole.Beholder & !x.IsDead);
                            if (bh != null)
                                Send(GetLocaleString("BeholderNewSeer", $"{p.GetName()}", roleModel.GetName() ?? GetDescription(IRole.Seer)), bh.Id);
                            break;
                        case IRole.AlphaWolf:
                        case IRole.WolfCub:
                        case IRole.Wolf:
                        case IRole.Lycan:
                            var wolves2 = Players.GetPlayersForRoles(WolfRoles, exceptPlayer: p);
                            var snowwolf2 = Players.GetPlayerForRole(IRole.SnowWolf);
                            teammates2 = string.Join(", ", wolves2.Select(x => x.GetName()));

                            foreach (var w in wolves2)
                            {
                                Send(GetLocaleString($"DGToWolf", p.GetName()), w.Id);
                            }

                            if (snowwolf2 != null)
                            {
                                Send(GetLocaleString($"DGToWolf", p.GetName()), snowwolf2.Id);
                                switch (p.PlayerRole)
                                {
                                    case IRole.AlphaWolf:
                                        Send(GetLocaleString("DGTransformToAlphaWithSnow", roleModel.GetName(), teammates2, snowwolf2.GetName()), p.Id);
                                        break;
                                    case IRole.WolfCub:
                                        Send(GetLocaleString("DGTransformToWolfCubWithSnow", roleModel.GetName(), teammates2, snowwolf2.GetName()), p.Id);
                                        break;
                                    case IRole.Wolf:
                                        Send(GetLocaleString("DGTransformToWolfWithSnow", roleModel.GetName(), teammates2, snowwolf2.GetName()), p.Id);
                                        break;
                                    case IRole.Lycan:
                                        Send(GetLocaleString("DGTransformToLycanWithSnow", roleModel.GetName(), teammates2, snowwolf2.GetName()), p.Id);
                                        break;
                                }
                            }
                            else
                            {
                                switch (p.PlayerRole)
                                {
                                    case IRole.AlphaWolf:
                                        Send(GetLocaleString("DGTransformToAlpha", roleModel.GetName(), teammates2), p.Id);
                                        break;
                                    case IRole.WolfCub:
                                        Send(GetLocaleString("DGTransformToWolfCub", roleModel.GetName(), teammates2), p.Id);
                                        break;
                                    case IRole.Wolf:
                                        Send(GetLocaleString("DGTransformToWolf", roleModel.GetName(), teammates2), p.Id);
                                        break;
                                    case IRole.Lycan:
                                        Send(GetLocaleString("DGTransformToLycan", roleModel.GetName(), teammates2), p.Id);
                                        break;
                                }
                            }
                            break;
                        case IRole.SnowWolf:
                            wolves = Players.GetPlayersForRoles(WolfRoles);
                            teammates2 = string.Join(", ", wolves.Select(x => x.GetName()));
                            foreach (var w in wolves)
                            {
                                Send(GetLocaleString($"DGToSnowWolf", p.GetName()), w.Id);
                            }
                            Send(GetLocaleString("DGTransformToSnowWolf", roleModel.GetName(), teammates2), p.Id);
                            break;
                        case IRole.Cultist:
                            foreach (var w in Players.Where(x => x.PlayerRole == IRole.Cultist & !x.IsDead && x.Id != p.Id))
                            {
                                Send(GetLocaleString("DGToCult", p.GetName()), w.Id);
                                teammates2 += $"{w.GetName()}" + ", ";
                            }
                            Send(GetLocaleString("DGTransformToCult", roleModel.GetName(), teammates2), p.Id);
                            break;
                        default:
                            break;
                    }
                    break;
                #endregion
                #region Alpha
                case TransformationMethod.AlphaBitten:
                    if (p.PlayerRole == IRole.Cultist)
                        foreach (var m in Players.Where(x => x.PlayerRole == IRole.Cultist & !x.IsDead && x.Id != p.Id))
                            Send(GetLocaleString("CultistBitten", p.GetName()), m.Id);
                    var msg1 = GetLocaleString("BittenTurned");
                    if (newTeamMembers?.Count() > 1)
                    {
                        if (newTeamMembers.Any(x => x.PlayerRole == IRole.SnowWolf))
                        {
                            msg1 += " " + GetLocaleString("WolfPackSnowList",
                                newTeamMembers.Where(x => WolfRoles.Contains(x.PlayerRole)).Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next),
                                newTeamMembers.First(x => x.PlayerRole == IRole.SnowWolf).GetName());
                        }
                        else
                        {
                            msg1 += " " + GetLocaleString("WolfPackList",
                                newTeamMembers.Where(x => WolfRoles.Contains(x.PlayerRole)).Select(x => x.GetName()).Aggregate((current, next) => current + ", " + next));
                        }
                    }
                    Players.GetPlayerForRole(IRole.AlphaWolf, false).AlphaConvertCount++;

                    Send(msg1, p.Id);
                    break;
                #endregion
                #region Thief
                case TransformationMethod.ThiefSteal:
                    Send(GetLocaleString("ThiefStoleRole", roleModel.GetName()), p.Id);
                    Send(GetRoleInfo(p.PlayerRole), p.Id);
                    switch (p.PlayerRole)
                    {
                        case IRole.Mason:
                            foreach (var w in Players.Where(x => x.PlayerRole == IRole.Mason & !x.IsDead && x.Id != p.Id))
                            {
                                Send(GetLocaleString("ThiefToMason", roleModel.GetName(), p.GetName()), w.Id);
                            }
                            break;
                        case IRole.Seer:
                            var bh = Players.FirstOrDefault(x => x.PlayerRole == IRole.Beholder & !x.IsDead);
                            if (bh != null)
                                Send(GetLocaleString("BeholderSeerStolen", p.GetName(), roleModel.GetName()), bh.Id);
                            break;
                        case IRole.AlphaWolf:
                        case IRole.WolfCub:
                        case IRole.Wolf:
                        case IRole.Lycan:
                            foreach (var w in Players.Where(x => !x.IsDead && x.Id != p.Id && (WolfRoles.Contains(x.PlayerRole) || x.PlayerRole == IRole.SnowWolf)))
                            {
                                Send(GetLocaleString("ThiefToWolf", roleModel.GetName(), p.GetName()), w.Id);
                            }
                            break;
                        case IRole.SnowWolf:
                            foreach (var w in Players.GetPlayersForRoles(WolfRoles))
                            {
                                Send(GetLocaleString("ThiefToSnowWolf", roleModel.GetName(), p.GetName()), w.Id);
                            }
                            break;
                        case IRole.Cultist:
                            foreach (var w in Players.Where(x => x.PlayerRole == IRole.Cultist & !x.IsDead && x.Id != p.Id))
                            {
                                Send(GetLocaleString("ThiefToCult", roleModel.GetName(), p.GetName()), w.Id);
                            }
                            break;
                        case IRole.Doppelgänger:
                            Send(GetLocaleString("NewDGRoleModel", Players.First(x => x.Id == p.RoleModel).GetName()), p.Id);
                            break;
                        case IRole.WildChild:
                            Send(GetLocaleString("NewWCRoleModel", Players.First(x => x.Id == p.RoleModel).GetName()), p.Id);
                            break;
                        default:
                            break;
                    }
                    break;
                case TransformationMethod.ThiefStolen:
                    Send(GetLocaleString((ThiefFull == true ? "ThiefStoleYourRoleThief" : "ThiefStoleYourRoleVillager")), p.Id);
                    break;
                    #endregion
            }
            #endregion
            #region General
            switch (p.PlayerRole)
            {
                case IRole.ApprenticeSeer:
                    if (Players.Count(x => !x.IsDead && x.PlayerRole == IRole.Seer) == 0)
                    {
                        Transform(p, IRole.Seer, TransformationMethod.ApprenticeSeer, roleModel: roleModel);
                    }
                    break;
                case IRole.Beholder:
                    var seer = Players.FirstOrDefault(x => x.PlayerRole == IRole.Seer);
                    Send(
                        seer != null
                            ? GetLocaleString("BeholderSeer", $"{seer.GetName()}")
                            : GetLocaleString("NoSeer"), p.Id);
                    break;
                case IRole.Traitor:
                    if (Players.Count(x => !x.IsDead && WolfRoles.Contains(x.PlayerRole)) == 0)
                    {
                        Transform(p, IRole.Wolf, TransformationMethod.Traitor);
                    }
                    break;
                case IRole.WildChild:
                    Send(GetLocaleString("NewWCRoleModel", Players.FirstOrDefault(x => x.Id == p.RoleModel)?.GetName() ?? "None was chosen!"), p.Id);
                    break;
                case IRole.Mayor:
                    if (!p.HasUsedAbility && (GameDay != 1 || Time != GameTime.Night))
                    {
                        var choices = new[] { new[] { InlineKeyboardButton.WithCallbackData(GetLocaleString("Reveal"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Mayor}|reveal") } }.ToList();
                        SendMenu(choices, p, GetLocaleString("AskMayor"), QuestionType.Mayor);
                    }
                    break;
                case IRole.Pacifist:
                    if (!p.HasUsedAbility && (GameDay != 1 || Time != GameTime.Night))
                    {
                        var choices = new[] { new[] { InlineKeyboardButton.WithCallbackData(GetLocaleString("Peace"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Pacifist}|peace") } }.ToList();
                        SendMenu(choices, p, GetLocaleString("AskPacifist"), QuestionType.Pacifist);
                    }
                    break;
            }
            #endregion
        }

        private enum TransformationMethod
        {
            BiteCursed,
            Traitor,
            ApprenticeSeer,
            AutoConvertToCult,
            ConvertToCult,
            WildChild,
            Doppelgänger,
            AlphaBitten,
            ThiefSteal,
            ThiefStolen,
            KillElder,
        }

        /// <summary>
        /// Method to put together all general visiting code (achievements, grave digger...). Does NOT check for guardian angel.
        /// </summary>
        /// <param name="visitor">The visiting player</param>
        /// <param name="visited">The visited player</param>
        /// <returns>true if the player survived the visit</returns>
        private VisitResult VisitPlayer(IPlayer visitor, IPlayer visited)
        {
            if (visited == null) return VisitResult.TargetNull;
            // increment visit count
            visited.BeingVisitedSameNightCount++;
            // If someone's dead, they're dead.
            if (visited.IsDead && !visited.Burning && (ThiefFull || visitor.PlayerRole != IRole.Thief)) return VisitResult.AlreadyDead;
            // A serial killer never misses their target. They might stumble into a grave, though.
            if (visitor.PlayerRole == IRole.SerialKiller && visited.PlayerRole != IRole.GraveDigger) return visited.IsDead ? VisitResult.AlreadyDead : VisitResult.Success;
            // if the visited person is burning, everyone but the SK burns with them
            if (visited.Burning)
            {
                if (visitor.PlayerRole == IRole.SerialKiller) return VisitResult.AlreadyDead;
                KillPlayer(visitor, KillMthd.VisitBurning, killer: Players.GetPlayerForRole(IRole.Arsonist, false), diedByVisitingVictim: true);
                return VisitResult.VisitorDied;
            }
            // if the visited person is a serial killer, say goodbye to your lives, unless you are a wolf and very lucky
            if (visited.PlayerRole == IRole.SerialKiller)
            {
                if ((!WolfRoles.Contains(visitor.PlayerRole) && visitor.PlayerRole != IRole.SnowWolf) || visited.Choice == 0 || visited.Choice == -1 || visited.Frozen || Program.R.Next(100) < 80)
                {
                    KillPlayer(visitor, KillMthd.VisitKiller, killer: visited, diedByVisitingKiller: true);
                    return VisitResult.VisitorDied;
                }
                else return VisitResult.Success;
            }
            if (visitor.PlayerRole == IRole.Thief && !ThiefFull) return VisitResult.Success;
            // A snow wolf can only maybe not freeze a serial killer
            if (visitor.PlayerRole == IRole.SnowWolf) return VisitResult.Success;
            // If the visited player is a wolf, only certain roles might die
            if ((WolfRoles.Contains(visited.PlayerRole) || visited.PlayerRole == IRole.SnowWolf)
                && (new IRole[] { IRole.Harlot, IRole.GuardianAngel }.Contains(visitor.PlayerRole) || (ThiefFull && visitor.PlayerRole == IRole.Thief)))
            {
                switch (visitor.PlayerRole)
                {
                    case IRole.Harlot:
                        KillPlayer(visitor, KillMthd.VisitWolf, killer: visited, diedByVisitingKiller: true, killedByRole: IRole.Wolf);
                        return VisitResult.VisitorDied;
                    case IRole.GuardianAngel:
                        if (!visited.WasSavedLastNight && Program.R.Next(100) < 50)
                        {
                            KillPlayer(visitor, KillMthd.GuardWolf, killer: visited, diedByVisitingKiller: true, killedByRole: IRole.Wolf);
                            return VisitResult.VisitorDied;
                        }
                        else return VisitResult.Success;
                    case IRole.Thief:
                        return VisitResult.Fail;
                }
            }
            // If the visited player is a grave digger, prepare for parkour!
            if (visited.PlayerRole == IRole.GraveDigger)
            {
                if (visited.DugGravesLastNight < 1) return VisitResult.Success;
                if (visitor.PlayerRole == IRole.SerialKiller)
                {
                    visitor.StumbledGrave = GameDay;
                    Send(GetLocaleString("KillerStumbled", visited.GetName()), visitor.Id);
                    return VisitResult.Success;
                }
                // GA won't fall if they protected grave digger
                if (visitor.PlayerRole == IRole.GuardianAngel && visited.WasSavedLastNight) return VisitResult.Success;
                var fallChance = 20 + (30 - (30 * Math.Pow(0.5, visited.DugGravesLastNight - 1)));
                if (visitor.Team == ITeam.Village) fallChance /= 2;
                if (Program.R.Next(100) < fallChance)
                {
                    KillPlayer(visitor, KillMthd.FallGrave, killer: visited, diedByVisitingKiller: true, hunterFinalShot: false);
                    switch (visitor.PlayerRole)
                    {
                        case IRole.AlphaWolf:
                        case IRole.Lycan:
                        case IRole.Wolf:
                        case IRole.WolfCub:
                            Send(GetLocaleString("WolfFell", visitor.GetName()), visited.Id);
                            break;
                        case IRole.CultistHunter:
                            Send(GetLocaleString("HunterFellDigger", visitor.GetName()), visited.Id);
                            break;
                        case IRole.Cultist:
                            Send(GetLocaleString("CultFell", visitor.GetName()), visited.Id);
                            break;
                        case IRole.GuardianAngel:
                            Send(GetLocaleString("GAFellDigger", visitor.GetName()), visited.Id);
                            break;
                        default:
                            Send(GetLocaleString($"{visitor.PlayerRole}FellDigger", visitor.GetName()), visited.Id);
                            break;
                    }
                    return VisitResult.VisitorDied;
                }
                //If an Arsonist is a lucky guy and avoided all the traps of GraveDigger - he will be able to burn him, not fail after avoiding.
                else if (visitor.PlayerRole != IRole.Arsonist)
                    return VisitResult.Fail;
            }
            // An arsonist also usually doesn't care whether harlot or GA or GD are home
            if (visitor.PlayerRole == IRole.Arsonist) return VisitResult.Success;
            // Checks for harlot or GA not home visited
            if ((visited.PlayerRole == IRole.Harlot || (visited.PlayerRole == IRole.GuardianAngel && !WolfRoles.Contains(visitor.PlayerRole))) && visited.Choice != 0 && visited.Choice != -1 && !visited.Frozen)
            {
                if (visitor.PlayerRole == IRole.Thief && !ThiefFull) return VisitResult.Success;
                else return VisitResult.Fail;
            }
            return VisitResult.Success;
        }

        private enum VisitResult
        {
            Success,
            VisitorDied,
            Fail,
            AlreadyDead,
            TargetNull
        }

        private void StealRole(IPlayer thief, IPlayer target)
        {
            if (target.IsDead) // ThiefFull must be false at this point, because of the VisitPlayer method
            {
                Send(GetLocaleString("ThiefStealDead", target.GetName()), thief.Id);

                try
                {
                    int tries = 0;
                    do
                    {
                        tries++;
                        try
                        {
                            var choiceid = ChooseRandomPlayerId(thief, false);
                            target = Players.FirstOrDefault(x => x.Id == choiceid);
                        }
                        catch (Exception e)
                        {
                            LogAllExceptions(e);
                        }
                    } while (target == null && tries < 10);
                    thief.Choice = target.Id;
                    Send(GetLocaleString("ThiefStealChosen", target.GetName()), thief.Id);
                }
                catch (Exception e)
                {
                    LogAllExceptions(e);
                    // just to make sure a target is chosen?
                    target = Players.FirstOrDefault(x => !x.IsDead && x != thief);
                }
            }

            if (VisitPlayer(thief, target) != VisitResult.Success) return;

            //swap roles
            var targetRole = target.PlayerRole;
            var targetRoleModel = target.RoleModel;
            var targetBullet = target.Bullet;
            var targetHasUsedAbility = target.HasUsedAbility;

            // the target first
            Transform(target, ThiefFull ? IRole.Thief : IRole.Villager, TransformationMethod.ThiefStolen);

            // then the thief
            Transform(thief, targetRole, TransformationMethod.ThiefSteal, newRoleModel: targetRoleModel, bullet: targetBullet, hasUsedAbility: targetHasUsedAbility, roleModel: target);
        }

        private void ConvertToCult(IPlayer target, IEnumerable<IPlayer> voteCult, int chance = 100)
        {
            var cultists = Players.GetPlayersForRoles(new[] { IRole.Cultist });

            if (Program.R.Next(100) < chance)
            {
                if (target.PlayerRole == IRole.Harlot)
                    foreach (var c in voteCult)
                        AddAchievement(c, AchievementsReworked.DontStayHome);

                Transform(target, IRole.Cultist, TransformationMethod.ConvertToCult, newTeamMembers: cultists, oldTeamMates: Players.Where(x => x.PlayerRole == IRole.Mason && x.Id != target.Id && !x.IsDead));
            }
            else
            {
                foreach (var c in cultists)
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
                _secondsToAdd = seconds;
                HaveExtended.Add(p.TeleUser.Id);
            }
            return;
        }
        private void LynchCycle()
        {
            if (!IsRunning) return;
            Time = GameTime.Lynch;
            if (Players == null) return;

            int lynchAttempt = 0;
            bool doubleLynch = _doubleLynch;
            _doubleLynch = false;

            do
            {
                lynchAttempt++;

                foreach (var p in Players)
                {
                    p.CurrentQuestion = null;
                    p.VotedBy.Clear();
                    p.Votes = 0;
                }

                if (CheckForGameEnd()) return;
                if (_pacifistUsed)
                {
                    SendWithQueue(GetLocaleString("PacifistNoLynchNow"));
                    _pacifistUsed = false;
                    return;
                }

                if (lynchAttempt == 2) SendWithQueue(GetLocaleString("TroubleDoubleLynchNow"));
                SendWithQueue(GetLocaleString("LynchTime", DbGroup.LynchTime.ToBold() ?? Settings.TimeLynch.ToBold()));
                SendPlayerList();
                SendLynchMenu();

                for (var i = 0; i < (DbGroup.LynchTime ?? Settings.TimeLynch); i++)
                {
                    Thread.Sleep(1000);
                    if (CheckForGameEnd()) return;
                    if (_pacifistUsed)
                    {
                        SendWithQueue(GetLocaleString("PacifistNoLynchNow"));
                        _pacifistUsed = false;
                        foreach (var p in Players.Where(x => x.CurrentQuestion != null))
                        {
                            try
                            {
                                if (p.CurrentQuestion.MessageId != 0)
                                {
                                    //Program.MessagesSent++;
                                    Program.Bot.EditMessageTextAsync(chatId: p.Id, messageId: p.CurrentQuestion.MessageId, text: GetLocaleString("LynchPeaceTimeout"));
                                }
                            }
                            catch { } // ignored
                            p.CurrentQuestion = null;
                        }
                        var pacifist = Players.FirstOrDefault(x => x.PlayerRole == IRole.Pacifist & !x.IsDead);
                        if (pacifist != null)
                        {
                            if (Players.Count(x => x.Choice == pacifist.Id) > (double)Players.Count(x => !x.IsDead) / 2)
                            {
                                AddAchievement(pacifist, AchievementsReworked.EveryManForHimself);
                            }
                            else if (pacifist.LoverId != 0 && Players.Count(x => x.Choice == pacifist.LoverId) > (double)Players.Count(x => !x.IsDead) / 2)
                            {
                                AddAchievement(Players.First(x => x.Id == pacifist.LoverId), AchievementsReworked.MySweetieSoStrong);
                            }
                        }
                        return;
                    }
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
                                //Program.MessagesSent++;
                                Program.Bot.EditMessageTextAsync(chatId: p.Id, messageId: p.CurrentQuestion.MessageId, text: GetLocaleString("TimesUp"));
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

                            if (SecretLynch && SecretLynchShowVotes && SecretLynchShowVoters && !target.VotedBy.ContainsKey(p))
                                target.VotedBy.Add(p, 1);

                            if (p.PlayerRole == IRole.Mayor && p.HasUsedAbility) //Mayor counts twice
                            {
                                p.MayorLynchAfterRevealCount++;
                                target.Votes++;
                                if (SecretLynch && SecretLynchShowVotes && SecretLynchShowVoters)
                                    target.VotedBy[p]++;
                            }
                        }
                        p.NonVote = 0;
                    }
                    else if (!p.IsDead && lynchAttempt < 2)
                    {
                        p.NonVote++;
                        if (p.NonVote < 2) continue;
                        var idles24 = 0;
                        var groupIdles24 = 0;
                        try
                        {
                            using (var db = new WWContext())
                            {
                                idles24 = db.GetIdleKills24Hours(p.Id).FirstOrDefault() ?? 0;
                                groupIdles24 = db.GetGroupIdleKills24Hours(p.Id, ChatId).FirstOrDefault() ?? 0;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                        SendWithQueue(GetLocaleString("IdleKill", p.GetName(), (DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}\n" : "") + GetLocaleString("IdleCount", p.GetName() + $"(id: <code>{p.TeleUser.Id}</code>)", idles24 + 1) + " " + GetLocaleString("GroupIdleCount", groupIdles24 + 1)));

                        //if hunter has died from AFK, too bad....
                        KillPlayer(p, KillMthd.Idle, killer: p, isNight: false, hunterFinalShot: false);
                        CheckRoleChanges();
                    }
                }


                try
                {
                    var maxVotes = Players.Max(x => x.Votes);
                    var choices = Players.Where(x => x.Votes == maxVotes).ToList();
                    IPlayer lynched = new IPlayer() { Votes = -1 }; // -1 = tie // -2 = no votes at all //

                    if (Players.All(x => x.Votes == 0))
                    {
                        lynched.Votes = -2;
                    }
                    else if (choices.Count > 1)
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

                    // if secret lynch is on and show votes
                    if (SecretLynch && SecretLynchShowVotes)
                    {
                        string sendMsg = "";
                        // secret lynch vote results..
                        var playerGotVoted = Players.Where(x => x.Votes > 0).OrderByDescending(x => x.Votes);
                        foreach (IPlayer p in playerGotVoted)
                        {
                            if (SecretLynchShowVoters == true)
                            {
                                List<string> voterList = new List<string>();
                                string voterNames = "";
                                foreach (KeyValuePair<IPlayer, int> pp in p.VotedBy)
                                {
                                    voterList.Add(pp.Value > 1 ? $"{pp.Key.GetName()} ({pp.Value})" : pp.Key.GetName());
                                }
                                voterNames = voterList.Aggregate((x, y) => x + ", " + y);
                                sendMsg += GetLocaleString("SecretLynchResultEach", p.Votes, p.GetName(), voterNames) + "\n";
                            }
                            else
                            {
                                sendMsg += GetLocaleString("SecretLynchResultNumber", p.Votes, p.GetName()) + "\n";
                            }
                        }
                        SendWithQueue(GetLocaleString("SecretLynchResultFull", sendMsg));
                    }

                    if (lynched.Votes > 0)
                    {
                        if (lynched.PlayerRole == IRole.Prince & !lynched.HasUsedAbility) //can only do this once
                        {
                            SendWithQueue(GetLocaleString("PrinceLynched", lynched.GetName()));
                            lynched.HasUsedAbility = true;
                        }
                        else
                        {
                            if (lynched.PlayerRole == IRole.Tanner)
                            {
                                if (Players.Count(x => !x.IsDead) == 3)
                                    AddAchievement(lynched, AchievementsReworked.ThatCameUnexpected);

                                if (lynched.InLove)
                                    AddAchievement(Players.First(x => x.Id == lynched.LoverId), AchievementsReworked.RomeoAndJuliet);
                            }

                            if (lynched.PlayerRole == IRole.Seer && GameDay == 1)
                                AddAchievement(lynched, AchievementsReworked.LackOfTrust);
                            if (lynched.PlayerRole == IRole.Prince && lynched.HasUsedAbility)
                                AddAchievement(lynched, AchievementsReworked.SpoiledRichBrat);
                            SendWithQueue(GetLocaleString("LynchKill", lynched.GetName(), DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? $"{lynched.GetName()} {GetLocaleString("Was")} {GetDescription(lynched.PlayerRole)}" : ""));
                            KillPlayer(lynched, KillMthd.Lynch, killers: Players.Where(x => x.Choice == lynched.Id), isNight: false);
                            //effects on game depending on the lynched's role
                            switch (lynched.PlayerRole)
                            {
                                case IRole.Tanner:
                                    //check for overkill
                                    if (Players.Where(x => !x.IsDead).All(x => x.Choice == lynched.Id))
                                        AddAchievement(lynched, AchievementsReworked.TannerOverkill);
                                    //end game
                                    lynched.DiedLastNight = true; //store the tanner who should win (DG is too complicated to handle)
                                    DoGameEnd(ITeam.Tanner);
                                    return;
                            }

                            CheckRoleChanges(true);
                        }
                    }
                    else if (lynched.Votes == -1) // Lynch tie
                    {
                        SendWithQueue(GetLocaleString("LynchTie"));
                        var t = choices.FirstOrDefault(x => x.PlayerRole == IRole.Tanner);
                        if (t != null && t.Votes > 0)
                            AddAchievement(t, AchievementsReworked.SoClose);
                    }
                    else // if (lynched.Votes == -2) // No lynch votes at all
                    {
                        SendWithQueue(GetLocaleString("NoLynchVotes"));
                    }

                    if (CheckForGameEnd(true)) return;
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;


                    Send("Oh no, something went wrong :( Error report is being sent to the developers\n" + e.Message);
#if DEBUG
                    Send(e.StackTrace);
#else
                    LogException(e);
#endif
                    Program.RemoveGame(this);
                }
            }
            while (lynchAttempt < (doubleLynch ? 2 : 1));
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
            Thread.Sleep(TimeSpan.FromSeconds((DbGroup.DayTime ?? Settings.TimeDay) + timeToAdd));

            if (!IsRunning) return;
            try
            {
                foreach (var p in Players.Where(x => x.CurrentQuestion != null))
                {
                    try
                    {
                        if (p.CurrentQuestion.MessageId != 0 && !new[] { QuestionType.Mayor, QuestionType.Pacifist }.Contains(p.CurrentQuestion.QType))
                        {
                            //Program.MessagesSent++;
                            Program.Bot.EditMessageTextAsync(chatId: p.Id, messageId: p.CurrentQuestion.MessageId, text: GetLocaleString("TimesUp"));
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

            if (Players == null) return;

            if (_sandmanSleep && _silverSpread)
            {
                var bs = Players.First(x => x.PlayerRole == IRole.Blacksmith & !x.IsDead);
                AddAchievement(bs, AchievementsReworked.WastedSilver);
            }

            //check gunner
            var gunner = Players.FirstOrDefault(x => x.PlayerRole == IRole.Gunner & !x.IsDead && x.Choice != 0 && x.Choice != -1);
            if (gunner != null)
            {
                var check = Players.FirstOrDefault(x => x.Id == gunner.Choice);
                if (check != null)
                {
                    //kill them
                    gunner.Bullet--;
                    gunner.HasUsedAbility = true;
                    if (new[] { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Cultist, IRole.SerialKiller, IRole.Lycan, IRole.SnowWolf, IRole.Arsonist }.Contains(check.PlayerRole))
                        gunner.BulletHitBaddies++;
                    //update database
                    switch (check.PlayerRole)
                    {
                        case IRole.Harlot:
                            SendWithQueue(DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? GetLocaleString("HarlotShot", gunner.GetName(), check.GetName()) : GetLocaleString("DefaultShot", gunner.GetName(), check.GetName(), ""));
                            break;
                        case IRole.WiseElder:
                            SendWithQueue(GetLocaleString("DefaultShot", gunner.GetName(), check.GetName(), !DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? "" : $"{check.GetName()} {GetLocaleString("Was")} {GetDescription(check.PlayerRole)}"));
                            SendWithQueue(GetLocaleString("GunnerShotWiseElder", gunner.GetName(), check.GetName()));
                            Transform(gunner, IRole.Villager, TransformationMethod.KillElder, bullet: 0);
                            break;
                        default:
                            SendWithQueue(GetLocaleString("DefaultShot", gunner.GetName(), check.GetName(), !DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? "" : $"{check.GetName()} {GetLocaleString("Was")} {GetDescription(check.PlayerRole)}"));
                            break;
                    }
                    KillPlayer(check, KillMthd.Shoot, killer: gunner, isNight: false);
                }
            }

            //check spumpkin
            var spumpkin = Players.FirstOrDefault(x => x.PlayerRole == IRole.Spumpkin & !x.IsDead && x.Choice != 0 && x.Choice != -1);
            if (spumpkin != null)
            {
                var check = Players.FirstOrDefault(x => x.Id == spumpkin.Choice);
                if (check != null)
                {
                    if (Program.R.Next(100) < 40)
                    {
                        //update database
                        switch (check.PlayerRole)
                        {
                            case IRole.WiseElder:
                                SendWithQueue(GetLocaleString("Detonation", spumpkin.GetName(), check.GetName(), !DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? "" : $"{check.GetName()} {GetLocaleString("Was")} {GetDescription(check.PlayerRole)}"));
                                SendWithQueue(GetLocaleString("DetonatedWiseElder", spumpkin.GetName(), check.GetName()));
                                Transform(spumpkin, IRole.Villager, TransformationMethod.KillElder);
                                break;
                            default:
                                SendWithQueue(GetLocaleString("Detonation", spumpkin.GetName(), check.GetName(), !DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? "" : $"{check.GetName()} {GetLocaleString("Was")} {GetDescription(check.PlayerRole)}"));
                                break;
                        }
                        KillPlayer(spumpkin, killMethod: null, killer: null, isNight: false);
                        KillPlayer(check, KillMthd.Shoot, killer: spumpkin, isNight: false);
                    }
                    else
                    {
                        Send(GetLocaleString("SpumpkinFailDetonate", check.GetName()), spumpkin.Id);
                    }
                }
            }

            //check detective
            var detect = Players.FirstOrDefault(x => x.PlayerRole == IRole.Detective & !x.IsDead && x.Choice != 0 && x.Choice != -1);
            if (detect != null)
            {
                //first off, chance to tell wolves
                if (Program.R.Next(100) < Settings.ChanceDetectiveCaught)
                {
                    IRole[] wolves = new[] { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Lycan, IRole.SnowWolf };
                    foreach (var w in Players.GetPlayersForRoles(wolves))
                    {
                        Send(GetLocaleString("DetectiveCaught", detect.GetName()), w.Id);
                    }
                }

                var check = Players.FirstOrDefault(x => x.Id == detect.Choice);
                if (check != null)
                {
                    Send(GetLocaleString("DetectiveSnoop", check.GetName(), GetDescription(check.PlayerRole)), detect.Id);

                    //if snooped non-bad-roles:
                    if (!new[] { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Lycan, IRole.Cultist, IRole.SerialKiller, IRole.SnowWolf, IRole.Arsonist }.Contains(check.PlayerRole))
                        detect.CorrectSnooped.Clear();     //clear correct snoop list
                    else
                    {
                        if (detect.CorrectSnooped.Contains(check.Id))     //check if it is a re-snoop of correct roles
                            detect.CorrectSnooped.Clear();             //clear the correct snoop list
                        detect.CorrectSnooped.Add(check.Id);              //add the current snoop to list

                        //if snooped 4 times correct continuously
                        if (detect.CorrectSnooped.Count() >= 4)
                        {
                            AddAchievement(detect, AchievementsReworked.Streetwise);
                            detect.CorrectSnooped.Clear();
                        }
                    }
                }
            }
            CheckRoleChanges(); // this stays after detective snooping so he still gets the "old" roles. Since he is using the game day for his snooping when the roles haven't changed yet
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
                if (p.Bitten)
                { // p.Bitten may also still be true if the bitten player was wc or dg and turned ww by rm death the same day - in that case, do nothing
                    p.Bitten = false;
                    if (!p.IsDead && !WolfRoles.Contains(p.PlayerRole) && p.PlayerRole != IRole.SnowWolf)
                    {
                        List<IPlayer> oldTeamMembers = p.PlayerRole == IRole.Cultist
                            ? Players.Where(x => x.PlayerRole == IRole.Cultist && !x.IsDead && x.Id != p.Id).ToList()
                            : Players.Where(x => x.PlayerRole == IRole.Mason && !x.IsDead && x.Id != p.Id).ToList();
                        Transform(p, IRole.Wolf, TransformationMethod.AlphaBitten, oldTeamMates: oldTeamMembers,
                            newTeamMembers: Players.Where(x => !x.IsDead && (WolfRoles.Contains(x.PlayerRole) || x.PlayerRole == IRole.SnowWolf)));
                    }
                }
            }
            CheckRoleChanges();     //so maybe if seer got converted to wolf, appseer will promote here
            if (CheckForGameEnd()) return;
            var nightTime = (DbGroup.NightTime ?? Settings.TimeNight);
            if (GameDay == 1)
            {
                if (Players.Any(x => new[] { IRole.Cupid, IRole.Doppelgänger, IRole.WildChild }.Contains(x.PlayerRole)))
                    nightTime = Math.Max(nightTime, 120);
                if (!ThiefFull && Players.Any(x => x.PlayerRole == IRole.Thief))
                    nightTime = Math.Max(nightTime, 120);
            }
            if (_sandmanSleep)
            {
                _sandmanSleep = false;
                _silverSpread = false; //reset blacksmith
                WolfCubKilled = false; //reset double kill
                foreach (var p in Players) p.Drunk = false; //reset drunk
                SendWithQueue(GetLocaleString("SandmanNight"));
                return;
            }

            var aliveWolves = Players.GetPlayersForRoles(WolfRoles, true);
            if (aliveWolves.Any(x => x.Drunk))
            {
                foreach (var w in aliveWolves.Where(x => !x.Drunk))
                    AddAchievement(w, AchievementsReworked.ThanksJunior);
            }

            SendWithQueue(GetLocaleString("NightTime", nightTime.ToBold()));
            SendPlayerList();
            SendNightActions();

            var nightPlayers = Players.Where(x => !x.IsDead & !x.Drunk);
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            for (var i = 0; i < nightTime; i++)
            {
                Thread.Sleep(1000);
                if (CheckForGameEnd()) return;
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
                            //Program.MessagesSent++;
                            Program.Bot.EditMessageTextAsync(chatId: p.Id, messageId: p.CurrentQuestion.MessageId, text: GetLocaleString("TimesUp"));
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
             * Snow Wolf
             * Grave Digger
             * Arsonist
             * Wolves
             * Serial Killer
             * Cultist Hunter
             * Cult
             * Chemist
             * Harlot
             * Seer
             * Sorcerer
             * Fool
             * Oracle
             * Augur
             * Guardian Angel
             * Thief
             */

            var ga = Players.FirstOrDefault(x => x.PlayerRole == IRole.GuardianAngel & !x.IsDead && x.Choice != 0 && x.Choice != -1);
            #region Snow Wolf Night
            var snowwolf = Players.FirstOrDefault(x => x.PlayerRole == IRole.SnowWolf & !x.IsDead);

            if (snowwolf != null && snowwolf.Choice != -1 && snowwolf.Choice != 0)
            {
                var target = Players.FirstOrDefault(x => x.Id == snowwolf.Choice);
                switch (VisitPlayer(snowwolf, target))
                {
                    case VisitResult.Success:
                        if (target.PlayerRole == IRole.SerialKiller)
                        {
                            target.Frozen = true;
                            Send(GetLocaleString("SKFrozen"), target.Id);
                            Send(GetLocaleString("SuccessfulFreeze", target.GetName()), snowwolf.Id);
                        }
                        else if (ga != null && ga.Choice == target.Id) // GA protects from being frozen
                        {
                            target.WasSavedLastNight = true;
                            Send(GetLocaleString("GuardBlockedSnowWolf", target.GetName()), snowwolf.Id);
                        }
                        else if (target.PlayerRole == IRole.Hunter)
                        {
                            if (Program.R.Next(100) < 50)
                            {
                                target.Frozen = true;
                                Send(GetLocaleString("DefaultFrozen"), target.Id);
                                Send(GetLocaleString("SuccessfulFreeze", target.GetName()), snowwolf.Id);
                            }
                            else
                            {
                                KillPlayer(snowwolf, KillMthd.HunterShot, killer: target, diedByVisitingKiller: true);
                            }
                        }
                        else // go and freeze that player!
                        {
                            target.Frozen = true;
                            switch (target.PlayerRole)
                            {
                                case IRole.Harlot:
                                    snowwolf.FrozeHarlot = true;
                                    Send(GetLocaleString("HarlotFrozen"), target.Id);
                                    break;
                                case IRole.Chemist:
                                    Send(GetLocaleString("ChemistFrozen"), target.Id);
                                    break;
                                case IRole.Cultist:
                                    Send(GetLocaleString("CultistFrozen"), target.Id);
                                    break;
                                case IRole.CultistHunter:
                                    Send(GetLocaleString("CHFrozen"), target.Id);
                                    break;
                                case IRole.Fool:
                                case IRole.Seer:
                                case IRole.Sorcerer:
                                case IRole.Oracle:
                                case IRole.Augur:
                                    Send(GetLocaleString("SeeingFrozen"), target.Id);
                                    break;
                                case IRole.GuardianAngel:
                                    Send(GetLocaleString("GAFrozen"), target.Id);
                                    ga = null;
                                    break;
                                case IRole.Thief:
                                    if (ThiefFull)
                                        Send(GetLocaleString("ThiefFrozen"), target.Id);
                                    else
                                        Send(GetLocaleString("DefaultFrozen"), target.Id);
                                    break;
                                case IRole.GraveDigger:
                                    if (target.DugGravesLastNight < 1)
                                        Send(GetLocaleString("DefaultFrozen"), target.Id);
                                    else
                                    {
                                        lastGrave = secondLastGrave; // tonight's graves have never been dug
                                        Send(GetLocaleString("GraveDiggerFrozen"), target.Id);
                                        target.DugGravesLastNight = 0;
                                    }
                                    break;
                                case IRole.Arsonist:
                                    // Arsonist can act despite being frozen.
                                    // target.Frozen is still true, so the snow wolf cannot freeze them a second time in a row.
                                    Send(GetLocaleString("ArsonistNotFrozen"), target.Id);
                                    break;
                                default:
                                    Send(GetLocaleString("DefaultFrozen"), target.Id);
                                    break;
                            }
                            Send(GetLocaleString("SuccessfulFreeze", target.GetName()), snowwolf.Id);
                        }
                        break;
                }
            }
            #endregion

            #region Arsonist Night
            var arsonist = Players.FirstOrDefault(x => !x.IsDead && x.PlayerRole == IRole.Arsonist);
            if (arsonist != null) // Arsonist can *not* be frozen! Fire beats ice!
            {
                if (arsonist.Choice == -2) //Spark
                {
                    var burning = Players.Where(x => !x.IsDead && x.Doused && x.PlayerRole != IRole.Arsonist).ToList();
                    foreach (var burn in burning)
                    {
                        if (ga?.Choice == burn.Id)
                        {
                            Send(GetLocaleString("GuardSavedYouFromFire"), burn.Id);
                            Send(GetLocaleString("GuardSavedFromFire", burn.GetName()), ga.Id);
                            burn.Doused = false;
                        }
                        else
                        {
                            KillPlayer(burn, KillMthd.Burn, killer: arsonist, hunterFinalShot: false, 
                                dyingSimultaneously: burning.Where(x => (ga?.Choice ?? 0) != x.Id).ToList());
                            burn.Doused = false;
                            burn.Burning = true;
                            SendGif(GetLocaleString("Burn"), GetRandomImage(BurnToDeath), burn.Id);
                        }
                    }

                    if (Players.Count(x => x.Burning) >= 5)
                    {
                        AddAchievement(arsonist, AchievementsReworked.PlayingWithTheFire);
                        if (Players.Count(x => x.Burning) >= 10)
                        {
                            AddAchievement(arsonist, AchievementsReworked.Firework);
                        }
                    }
                }
                else
                {
                    var doused = Players.FirstOrDefault(x => x.Id == arsonist.Choice);
                    if (doused != null)
                    {
                        if (VisitPlayer(arsonist, doused) == VisitResult.Success)
                        {
                            doused.Doused = true;
                            Send(GetLocaleString("Doused", doused.GetName()), arsonist.Id);
                        }
                    }
                }
            }
            #endregion

            #region Wolf Night - Non-snow wolves
            var wolves = nightPlayers.GetPlayersForRoles(WolfRoles).ToList();
            var voteWolves = wolves.Where(x => !x.Drunk);
            var voteWolvesCount = voteWolves.Count();
            WolfCubKilled = false;

            if (voteWolves.Any())
            {
                var votechoice = voteWolves.Where(x => (x.Choice != 0 && x.Choice != -1) || (x.Choice2 != 0 && x.Choice2 != -1));

                List<long> choices = new List<long>();

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
                    var target = Players.FirstOrDefault(x => x.Id == choice);
                    IPlayer visitorWuff;
                    try
                    {
                        visitorWuff = voteWolves.ElementAt(Program.R.Next(voteWolves.Count()));
                    }
                    catch
                    {
                        visitorWuff = voteWolves.FirstOrDefault();
                    }
                    switch (VisitPlayer(visitorWuff, target))
                    {
                        case VisitResult.Success:
                            if (ga?.Choice == target.Id)
                            {
                                foreach (var wolf in voteWolves)
                                    Send(GetLocaleString("GuardBlockedWolf", target.GetName()), wolf.Id);
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
                                        foreach (var w in voteWolves)
                                            AddAchievement(w, AchievementsReworked.DontStayHome);
                                        goto default;
                                    case IRole.Cursed:
                                        Transform(target, IRole.Wolf, TransformationMethod.BiteCursed, newTeamMembers: wolves);
                                        break;
                                    case IRole.Drunk:
                                        if (bitten)
                                        {
                                            AddAchievement(voteWolves.First(x => x.PlayerRole == IRole.AlphaWolf), AchievementsReworked.LuckyDay);
                                            BitePlayer(target, alpha);
                                        }
                                        else
                                        {
                                            KillPlayer(target, KillMthd.Eat, killers: voteWolves, killedByRole: IRole.Wolf);
                                            SendGif(GetLocaleString("WolvesEatYou"),
                                                GetRandomImage(VillagerDieImages), target.Id);
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
                                                    //commented out: we don't want the hunter to be bitten if he shot.
                                                    //if (bitten)
                                                    //{
                                                    //    BitePlayer(target, voteWolves, alpha);
                                                    //}
                                                    //else
                                                    {
                                                        SendGif(GetLocaleString("WolvesEatYou"),
                                                            GetRandomImage(VillagerDieImages), target.Id);
                                                        KillPlayer(target, KillMthd.Eat, killers: voteWolves, killedByRole: IRole.Wolf, hunterFinalShot: false);
                                                    }
                                                }
                                                KillPlayer(shotWuff, KillMthd.HunterShot, killer: target, diedByVisitingKiller: true);
                                            }
                                        }
                                        else
                                        {
                                            goto default;
                                        }
                                        break;
                                    case IRole.SerialKiller:
                                        if (bitten)
                                        {
                                            BitePlayer(target, alpha);
                                            voteWolves.First(x => x.PlayerRole == IRole.AlphaWolf).StrongestAlpha = true;
                                        }
                                        else
                                        {
                                            goto default;
                                        }
                                        break;
                                    case IRole.WiseElder:
                                        if (bitten)
                                            BitePlayer(target, alpha);
                                        else
                                        {
                                            // If WiseElder was eaten once already
                                            if (target.HasUsedAbility)
                                            {
                                                goto default;
                                            }
                                            else
                                            {
                                                target.HasUsedAbility = true;
                                                foreach (var wolf in voteWolves)
                                                    Send(GetLocaleString("WolvesTriedToEatWiseElder", target.GetName()), wolf.Id);
                                                Send(GetLocaleString("WolvesAteWiseElderPM"), target.Id);
                                            }
                                        }
                                        break;
                                    case IRole.Traitor:
                                        if (bitten)
                                        {
                                            BitePlayer(target, alpha);
                                        }
                                        else
                                        {
                                            if (Players.Count(x => !x.IsDead && (WolfRoles.Contains(x.PlayerRole) || x.PlayerRole == IRole.SnowWolf)) == 1) // just looking for voteWolves is not enough because of drunk wolves
                                                AddAchievement(voteWolves.First(), AchievementsReworked.ConditionRed);

                                            goto default;
                                        }
                                        break;
                                    default:
                                        if (bitten)
                                        {
                                            BitePlayer(target, alpha);
                                        }
                                        else
                                        {
                                            KillPlayer(target, KillMthd.Eat, killers: voteWolves, killedByRole: IRole.Wolf, hunterFinalShot: false);
                                            if (target.PlayerRole == IRole.Sorcerer)
                                            {
                                                foreach (var w in voteWolves)
                                                    AddAchievement(w, AchievementsReworked.NoSorcery);
                                            }
                                            SendGif(GetLocaleString("WolvesEatYou"),
                                                GetRandomImage(VillagerDieImages), target.Id);
                                        }
                                        break;
                                }
                            }
                            eatCount++;
                            break;
                        case VisitResult.Fail:
                            //no success, why?
                            switch (target.PlayerRole)
                            {
                                case IRole.Harlot:
                                    foreach (var wolf in voteWolves)
                                        Send(GetLocaleString("HarlotNotHome", target.GetName()), wolf.Id);
                                    break;
                                case IRole.GraveDigger:
                                    foreach (var wolf in voteWolves)
                                        Send(GetLocaleString("GraveDiggerNotHome", target.GetName()), wolf.Id);
                                    break;
                            }
                            break;
                        case VisitResult.VisitorDied:
                            // The poor visiting wolf died...
                            if (target.Burning) foreach (var wolf in voteWolves) Send(GetLocaleString("WolvesVisitBurn", target.GetName(), visitorWuff.GetName()), wolf.Id);
                            else
                                switch (target.PlayerRole)
                                {
                                    case IRole.SerialKiller:
                                        // no need to notify
                                        break;
                                    case IRole.GraveDigger:
                                        foreach (var wolf in voteWolves) Send(GetLocaleString("WolfFellWolves", target.GetName(), visitorWuff.GetName()), wolf.Id);
                                        break;
                                }
                            break;
                    }
                    var gd = Players.FirstOrDefault(x => x.PlayerRole == IRole.GraveDigger && !x.IsDead && x.DugGravesLastNight > 0);
                    if (gd != null)
                    {
                        //give wolves a chance to spot and kill grave digger
                        var spotChance = (20 + (30 - (30 * Math.Pow(0.5, gd.DugGravesLastNight - 1)))) / 2;
                        if (Program.R.Next(100) < spotChance)
                        {
                            KillPlayer(gd, KillMthd.Spotted, killers: voteWolves, diedByVisitingKiller: true, killedByRole: IRole.Wolf);
                            foreach (var w in voteWolves)
                                Send(GetLocaleString("WolvesSpotted", gd.GetName()), w.Id);
                            SendGif(GetLocaleString("WolvesSpottedYou"), GetRandomImage(VillagerDieImages), gd.Id);
                        }
                    }
                }
                if (eatCount == 2)
                {
                    var cub = Players.GetPlayersForRoles(new[] { IRole.WolfCub }, false).OrderByDescending(x => x.TimeDied).FirstOrDefault(x => x.IsDead);
                    if (cub != null)
                        AddAchievement(cub, AchievementsReworked.IHelped);
                    if (Players.Count(x => choices.Contains(x.Id) && x.Bitten) == 2)
                        AddAchievement(voteWolves.FirstOrDefault(x => x.PlayerRole == IRole.AlphaWolf), AchievementsReworked.IncreaseThePack);
                }

                eatCount = 0;
            }
            #endregion

            #region Serial Killer Night

            //give serial killer a chance!
            var sk = Players.FirstOrDefault(x => x.PlayerRole == IRole.SerialKiller & !x.IsDead);
            if (sk != null && !sk.Frozen)
            {
                var skilled = Players.FirstOrDefault(x => x.Id == sk.Choice);
                switch (VisitPlayer(sk, skilled))
                {
                    // otherwise player is already dead
                    case VisitResult.Success:
                        IPlayer oldSkilled = null;
                        if (sk.StumbledGrave > 0 && sk.StumbledGrave + 1 == GameDay && Program.R.Next(100) < 50)
                        {
                            oldSkilled = skilled;
                            skilled = Players.Where(x => x.PlayerRole != IRole.SerialKiller && !x.IsDead).ElementAt(Program.R.Next(Players.Count(x => x.PlayerRole != IRole.SerialKiller && !x.IsDead)));
                            VisitPlayer(sk, skilled);
                            Send(GetLocaleString("KillerRandomKill", oldSkilled.GetName(), skilled.GetName()), sk.Id);
                        }
                        if (ga?.Choice == skilled.Id && skilled.PlayerRole != IRole.Harlot) //GA doesn't find Harlot at home, therefore can't protect them
                        {
                            if (oldSkilled == null)
                                Send(GetLocaleString("GuardBlockedKiller", skilled.GetName()), sk.Id);
                            else
                            {
                                Send(GetLocaleString("GuardBlockedRandomKiller", skilled.GetName()), sk.Id);
                                AddAchievement(sk, AchievementsReworked.ReallyBadLuck);
                            }
                            skilled.WasSavedLastNight = true;
                        }
                        else
                        {
                            KillPlayer(skilled, KillMthd.SerialKilled, killer: sk);
                            if (WolfRoles.Contains(skilled.PlayerRole) || skilled.PlayerRole == IRole.SnowWolf)
                                sk.SerialKilledWolvesCount++;
                            SendGif(GetLocaleString("SerialKillerKilledYou"), GetRandomImage(SKKilled), skilled.Id);
                        }
                        break;
                }
                var gd = Players.FirstOrDefault(x => x.PlayerRole == IRole.GraveDigger && !x.IsDead && x.DugGravesLastNight > 0);
                if (gd != null)
                {
                    //give sk a chance to spot and kill grave digger
                    var spotChance = (20 + (30 - (30 * Math.Pow(0.5, gd.DugGravesLastNight - 1)))) / 2;
                    if (Program.R.Next(100) < spotChance)
                    {
                        KillPlayer(gd, KillMthd.Spotted, killer: sk, diedByVisitingKiller: true);
                        Send(GetLocaleString("SerialKillerSpotted", gd.GetName()), sk.Id);
                        SendGif(GetLocaleString("SerialKillerSpottedYou"), GetRandomImage(SKKilled), gd.Id);
                    }
                }
            }

            #endregion

            if (Players == null)
                return;

            #region Cult Hunter Night

            //cult hunter
            var hunter = Players.GetPlayerForRole(IRole.CultistHunter);
            if (hunter != null && !hunter.Frozen)
            {
                var hunted = Players.FirstOrDefault(x => x.Id == hunter.Choice);
                // if ch died, everything will already be handled
                switch (VisitPlayer(hunter, hunted))
                {
                    case VisitResult.Success:
                        if (hunted.PlayerRole == IRole.Cultist)
                        {
                            Send(GetLocaleString("HunterFindCultist", hunted.GetName()), hunter.Id);
                            Send(GetLocaleString("CHHuntedYou"), hunted.Id);
                            KillPlayer(hunted, KillMthd.Hunt, killer: hunter);
                            hunter.CHHuntedCultCount++;
                        }
                        else
                        {
                            goto fail;
                        }
                        break;
                    case VisitResult.Fail:
                    fail:
                        Send(GetLocaleString("HunterFailedToFind", hunted.GetName()), hunter.Id);
                        break;
                    case VisitResult.AlreadyDead:
                        Send(GetLocaleString("HunterVisitDead", hunted.GetName()), hunter.Id);
                        break;
                }
            }

            #endregion

            #region Cult Night

            //CULT
            var voteCult = Players.Where(x => x.PlayerRole == IRole.Cultist & !x.IsDead & !x.Frozen);

            if (voteCult.Any())
            {
                var votechoice = voteCult.Where(x => x.Choice != 0 && x.Choice != -1);
                long choice = 0;
                if (votechoice.Any())
                {
                    choice = votechoice.GroupBy(x => x.Choice).OrderByDescending(x => x.Count()).First().Key;
                }

                if (choice != 0 && choice != -1)
                {
                    var target = Players.FirstOrDefault(x => x.Id == choice);
                    var newbie = voteCult.OrderByDescending(x => x.DayCult).First();
                    switch (VisitPlayer(newbie, target))
                    {
                        case VisitResult.Success:
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
                                            KillPlayer(newbie, KillMthd.HunterCult, killer: target, diedByVisitingKiller: true);
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
                                case IRole.CultistHunter:
                                    //kill the newest cult member
                                    KillPlayer(newbie, KillMthd.Hunt, killer: target, diedByVisitingKiller: true);
                                    AddAchievement(newbie, AchievementsReworked.CultFodder);
                                    //notify everyone
                                    foreach (var c in voteCult)
                                    {
                                        Send(GetLocaleString("CultConvertCultHunter", newbie.GetName(), target.GetName()), c.Id);
                                    }
                                    Send(GetLocaleString("CultHunterKilledCultVisit", newbie.GetName(), CountCultistsAlive()), target.Id);
                                    break;
                                case IRole.Wolf:
                                case IRole.AlphaWolf:
                                case IRole.WolfCub:
                                case IRole.Lycan:
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
                                        KillPlayer(newbie, KillMthd.VisitWolf, killer: target, diedByVisitingKiller: true, killedByRole: IRole.Wolf);

                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultConvertWolf", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                        Send(GetLocaleString("CultAttempt"), target.Id); //only notify if they were home
                                    }
                                    break;
                                case IRole.SnowWolf:
                                    if (target.Choice != -1 && target.Choice != 0) // did snow wolf go freezing?
                                    {
                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultVisitEmpty", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                    }
                                    else // stayed home!
                                    {
                                        KillPlayer(newbie, KillMthd.VisitWolf, killer: target, diedByVisitingKiller: true, killedByRole: IRole.Wolf);

                                        foreach (var c in voteCult)
                                        {
                                            Send(GetLocaleString("CultConvertWolf", newbie.GetName(), target.GetName()), c.Id);
                                        }
                                        Send(GetLocaleString("CultAttempt"), target.Id); //only notify if they were home
                                    }
                                    break;
                                case IRole.GuardianAngel:
                                    ConvertToCult(target, voteCult, Settings.GuardianAngelConversionChance);
                                    break;
                                case IRole.Harlot:
                                    ConvertToCult(target, voteCult, Settings.HarlotConversionChance);
                                    break;
                                case IRole.Arsonist:
                                    if (target.Choice == -1 || target.Frozen)
                                        ConvertToCult(target, voteCult, 0);
                                    else
                                    {
                                        foreach (var c in voteCult)
                                            Send(GetLocaleString("CultVisitEmpty", newbie.GetName(), target.GetName()), c.Id);
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
                                case IRole.Doppelgänger:
                                case IRole.Thief:
                                case IRole.Spumpkin:
                                    ConvertToCult(target, voteCult, 0);
                                    break;
                                case IRole.Oracle:
                                    ConvertToCult(target, voteCult, Settings.OracleConversionChance);
                                    break;
                                case IRole.Sandman:
                                    ConvertToCult(target, voteCult, Settings.SandmanConversionChance);
                                    break;
                                case IRole.WiseElder:
                                    ConvertToCult(target, voteCult, Settings.WiseElderConversionChance);
                                    break;
                                case IRole.Pacifist:
                                    ConvertToCult(target, voteCult, Settings.PacifistConversionChance);
                                    break;
                                case IRole.GraveDigger:
                                    ConvertToCult(target, voteCult, Settings.GraveDiggerConversionChance);
                                    break;
                                case IRole.Augur:
                                    ConvertToCult(target, voteCult, Settings.AugurConversionChance);
                                    break;
                                default:
                                    ConvertToCult(target, voteCult);
                                    break;
                            }
                            break;
                        case VisitResult.AlreadyDead:
                            if (!target.Burning)
                            {
                                foreach (var c in voteCult)
                                    Send(GetLocaleString("CultTargetDead", target.GetName()), c.Id);
                            }
                            break;
                        case VisitResult.VisitorDied:
                            switch (target.PlayerRole)
                            {
                                case IRole.SerialKiller:
                                    foreach (var c in voteCult) Send(GetLocaleString("CultConvertSerialKiller", newbie.GetName(), target.GetName()), c.Id);
                                    break;
                                case IRole.GraveDigger:
                                    foreach (var c in voteCult) Send(GetLocaleString("CultConvertGraveDigger", newbie.GetName(), target.GetName()), c.Id);
                                    break;
                            }
                            break;
                        case VisitResult.Fail:
                            foreach (var c in voteCult)
                                Send(GetLocaleString("CultVisitEmpty", newbie.GetName(), target.GetName()), c.Id);
                            break;
                    }
                }
            }
            #endregion

            #region Chemist Night
            var chemist = Players.FirstOrDefault(x => x.PlayerRole == IRole.Chemist & !x.IsDead);
            if (chemist != null && !chemist.Frozen)
            {
                var target = Players.FirstOrDefault(x => x.Id == chemist.Choice);
                switch (VisitPlayer(chemist, target))
                {
                    case VisitResult.Success:
                        if (Program.R.Next(100) < Settings.ChemistSuccessChance) // chemist kills his target
                        {
                            chemist.HasUsedAbility = false;
                            KillPlayer(target, KillMthd.Chemistry, killer: chemist);
                            target.ChemistFailed = false;
                            Send(GetLocaleString("ChemistVisitYouSuccess"), target.Id);
                            Send(GetLocaleString("ChemistSuccess", target.GetName()), chemist.Id);
                            if (++chemist.ChemistVisitSurviveCount == 3)
                                AddAchievement(chemist, AchievementsReworked.GoodChoiceForYou);
                        }
                        else // chemist commits suicide by accident... oops!
                        {
                            chemist.HasUsedAbility = false;
                            KillPlayer(chemist, KillMthd.Chemistry, killer: chemist);
                            chemist.ChemistFailed = true;
                            Send(GetLocaleString("ChemistVisitYouFail", chemist.GetName()), target.Id);
                            Send(GetLocaleString("ChemistFail", target.GetName()), chemist.Id);
                        }
                        break;
                    case VisitResult.AlreadyDead:
                        Send(GetLocaleString("ChemistTargetDead", target.GetName()), chemist.Id);
                        break;
                    case VisitResult.Fail:
                        Send(GetLocaleString("ChemistTargetEmpty", target.GetName()), chemist.Id);
                        break;
                    case VisitResult.VisitorDied:
                        switch (target.PlayerRole)
                        {
                            case IRole.SerialKiller:
                                Send(GetLocaleString("ChemistVisitYouSK", chemist.GetName()), target.Id);
                                Send(GetLocaleString("ChemistSK", target.GetName()), chemist.Id);
                                break;
                            case IRole.GraveDigger:
                                Send(GetLocaleString("ChemistFellDigger", chemist.GetName()), target.Id);
                                Send(GetLocaleString("ChemistFell", target.GetName()), chemist.Id);
                                break;
                        }
                        break;
                }
            }
            #endregion

            #region Harlot Night

            //let the harlot know
            var harlot = Players.FirstOrDefault(x => x.PlayerRole == IRole.Harlot & !x.IsDead);
            if (harlot != null && !harlot.Frozen)
            {
                var target = Players.FirstOrDefault(x => x.Id == harlot.Choice);
                if (target != null)
                {
                    if (harlot.LoverId == target.Id)
                        AddAchievement(harlot, AchievementsReworked.Affectionate);
                    if (harlot.PlayersVisited.Contains(target.TeleUser.Id))
                        harlot.HasRepeatedVisit = true;
                    harlot.PlayersVisited.Add(target.TeleUser.Id);
                }
                else harlot.HasStayedHome = true;

                switch (VisitPlayer(harlot, target))
                {
                    case VisitResult.Success:
                        Send(
                            (target.PlayerRole == IRole.Cultist && Program.R.Next(100) < Settings.HarlotDiscoverCultChance) ?
                                GetLocaleString("HarlotDiscoverCult", target.GetName()) :
                                GetLocaleString("HarlotVisitNonWolf", target.GetName()),
                            harlot.Id);
                        if (!target.IsDead)
                            Send(GetLocaleString("HarlotVisitYou"), target.Id);
                        break;
                    case VisitResult.VisitorDied:
                        if (!target.Burning)
                            switch (target.PlayerRole)
                            {
                                case IRole.Wolf:
                                case IRole.AlphaWolf:
                                case IRole.WolfCub:
                                case IRole.Lycan:
                                case IRole.SnowWolf:
                                    Send(GetLocaleString("HarlotFuckWolf", target.GetName()), harlot.Id);
                                    break;
                                case IRole.SerialKiller:
                                    Send(GetLocaleString("HarlotFuckKiller", target.GetName()), harlot.Id);
                                    break;
                                case IRole.GraveDigger:
                                    Send(GetLocaleString("HarlotFell", target.GetName()), harlot.Id);
                                    break;
                            }
                        break;
                    case VisitResult.AlreadyDead:
                        if (target.DiedLastNight && (WolfRoles.Contains(target.KilledByRole) || target.KilledByRole == IRole.SerialKiller) && !target.DiedByVisitingKiller && !target.DiedByVisitingVictim)
                        {
                            KillPlayer(harlot, KillMthd.VisitVictim, killer: target, diedByVisitingVictim: true, killedByRole: target.KilledByRole);
                            harlot.RoleModel = target.Id; //store who they visited
                        }
                        else
                        {
                            Send(GetLocaleString("HarlotVisitNotHome", target.GetName()), harlot.Id);
                        }
                        break;
                    case VisitResult.Fail:
                        switch (target.PlayerRole)
                        {
                            case IRole.GraveDigger:
                                Send(GetLocaleString("HarlotVisitDigger", target.GetName()), harlot.Id);
                                break;
                            default:
                                Send(GetLocaleString("HarlotVisitNotHome", target.GetName()), harlot.Id);
                                break;
                        }
                        break;
                }
            }

            #endregion

            #region Seer / Fool

            //let the seer know
            var seers = Players.Where(x => x.PlayerRole == IRole.Seer && !x.IsDead && !x.Frozen);
            if (seers.Any())
            {
                foreach (var seer in seers)
                {
                    var target = Players.FirstOrDefault(x => x.Id == seer.Choice);
                    if (target != null)
                    {
                        var role = target.PlayerRole;
                        switch (role)
                        {
                            case IRole.Beholder:
                                AddAchievement(seer, AchievementsReworked.ShouldHaveKnown);
                                break;
                            case IRole.Traitor:
                                role = Program.R.Next(100) < 50 ? IRole.Wolf : IRole.Villager;
                                break;
                            case IRole.WolfCub: //seer doesn't see wolf type
                            case IRole.AlphaWolf:
                                role = IRole.Wolf;
                                break;
                            case IRole.WolfMan: //poor wolf man, is just a villager!
                                role = IRole.Wolf;
                                target.Trustworthy = true;
                                break;
                            case IRole.Lycan: //sneaky wuff
                                role = IRole.Villager;
                                break;
                        }
                        Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(role)), seer.Id);
                    }
                }
            }
            var sorcerer = Players.FirstOrDefault(x => x.PlayerRole == IRole.Sorcerer && !x.IsDead);
            if (sorcerer != null && !sorcerer.Frozen)
            {
                var target = Players.FirstOrDefault(x => x.Id == sorcerer.Choice);
                if (target != null)
                {
                    var role = target.PlayerRole;
                    switch (role)
                    {
                        case IRole.AlphaWolf:
                        case IRole.Wolf:
                        case IRole.WolfCub:
                            Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(IRole.Wolf)), sorcerer.Id);
                            break;
                        case IRole.Seer:
                            Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(IRole.Seer)), sorcerer.Id);
                            break;
                        case IRole.SnowWolf:
                            Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(IRole.SnowWolf)), sorcerer.Id);
                            break;
                        default:
                            Send(GetLocaleString("SorcererOther", target.GetName()), sorcerer.Id);
                            break;
                    }
                }

            }
            var fool = Players.FirstOrDefault(x => x.PlayerRole == IRole.Fool && !x.IsDead);
            if (fool != null && !fool.Frozen)
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
                            if (possibleRoles[0] == IRole.Beholder && target.PlayerRole == IRole.Beholder)
                                fool.FoolCorrectlySeenBH = true;
                        }
                        catch
                        {
                            // ignored
                        }

                        Send(GetLocaleString("SeerSees", target.GetName(), GetDescription(possibleRoles[0])), fool.Id);
                    }
                }
            }

            var oracle = Players.FirstOrDefault(x => x.PlayerRole == IRole.Oracle && !x.IsDead);
            if (oracle != null && !oracle.Frozen)
            {
                var target = Players.FirstOrDefault(x => x.Id == oracle.Choice);
                if (target != null)
                {
                    var possibleRoles = Players.Where(x => !x.IsDead && x.Id != oracle.Id && x.PlayerRole != target.PlayerRole).Select(x => x.PlayerRole).ToList();
                    possibleRoles.Shuffle();
                    possibleRoles.Shuffle();
                    if (possibleRoles.Any())
                    {
                        Send(GetLocaleString("NegSeerSees", target.GetName(), GetDescription(possibleRoles[0])), oracle.Id);
                    }
                    else
                    {
                        AddAchievement(oracle, AchievementsReworked.NowImBlind);
                    }
                }
            }


            #endregion

            #region Augur
            var augur = Players.FirstOrDefault(x => !x.IsDead && x.PlayerRole == IRole.Augur);
            if (augur != null && !augur.Frozen)
            {
                bool isNotInGame(IRole x) => !augur.SawRoles.Contains(x) && !Players.Any(y => (!y.IsDead || y.DiedLastNight) && y.PlayerRole == x);
                PossibleRoles.Shuffle();
                if (PossibleRoles.Any(isNotInGame))
                {
                    var roleToSee = PossibleRoles.FirstOrDefault(isNotInGame);
                    if (roleToSee == IRole.Seer && Players.Any(x => !x.IsDead && x.PlayerRole == IRole.ApprenticeSeer) && !Players.Any(x => x.PlayerRole == IRole.Seer && !x.IsDead)) roleToSee = IRole.ApprenticeSeer;  //replace Seer by AppSeer if they are about to transform
                    Send(GetLocaleString("AugurSees", GetDescription(roleToSee)), augur.Id);
                    augur.SawRoles.Add(roleToSee);
                }
                else
                {
                    Send(GetLocaleString("AugurSeesNothing"), augur.Id);
                }
            }
            #endregion

            #region GA Night

            if (ga != null && !ga.Frozen && !ga.IsDead)
            {
                var save = Players.FirstOrDefault(x => x.Id == ga.Choice);
                switch (VisitPlayer(ga, save))
                {
                    case VisitResult.Success:
                        if (WolfRoles.Contains(save.PlayerRole) && !save.WasSavedLastNight) ga.GAGuardWolfCount++;
                        bool cleanedDoused = false;
                        if (save.WasSavedLastNight)
                        {
                            Send(GetLocaleString("GuardSaved", save.GetName()), ga.Id);
                            Send(GetLocaleString("GuardSavedYou"), save.Id);
                        }
                        else if (save.Doused)
                        {
                            Send(GetLocaleString("CleanDoused", save.GetName()), ga.Id);
                            save.Doused = false;
                            cleanedDoused = true;
                        }
                        if (!save.WasSavedLastNight && !save.DiedLastNight && !cleanedDoused) //only send if save wasn't attacked
                            Send(GetLocaleString("GuardNoAttack", save.GetName()), ga.Id);
                        save.WasSavedLastNight = false;
                        break;
                    case VisitResult.Fail:
                        Send(GetLocaleString("GuardEmptyHouse", save.GetName()), ga.Id);
                        break;
                    case VisitResult.VisitorDied:
                        switch (save.PlayerRole)
                        {
                            case IRole.AlphaWolf:
                            case IRole.WolfCub:
                            case IRole.Wolf:
                            case IRole.Lycan:
                            case IRole.SnowWolf:
                                Send(GetLocaleString("GuardWolf"), ga.Id);
                                break;
                            case IRole.SerialKiller:
                                Send(GetLocaleString("GuardKiller"), ga.Id);
                                break;
                            case IRole.GraveDigger:
                                Send(GetLocaleString("GAFell", save.GetName()), ga.Id);
                                break;
                        }
                        break;
                }
            }

            #endregion

            CheckRoleChanges();

            #region Thief Night
            var thief = Players.FirstOrDefault(x => x.PlayerRole == IRole.Thief && !x.IsDead);
            if (thief != null)
            {
                if (!ThiefFull) // Not full thief, steal no matter if frozen or not
                {
                    if (GameDay == 1)
                    {
                        var target = Players.FirstOrDefault(x => x.Id == thief.Choice);
                        int tries = 0;
                        while (target == null && tries < 10)
                        {
                            tries++;
                            try
                            {
                                var choiceid = ChooseRandomPlayerId(thief, false);
                                target = Players.FirstOrDefault(x => x.Id == choiceid);
                            }
                            catch (Exception e)
                            {
                                LogAllExceptions(e);
                            }
                        }
                        if (tries > 0) Send(GetLocaleString("ThiefStealChosen", target.GetName()), thief.Id);
                        switch (VisitPlayer(thief, target))
                        {
                            case VisitResult.Success:
                                StealRole(thief, target);
                                break;
                            case VisitResult.VisitorDied:
                                // don't steal from SKs :P
                                break;
                        }
                    }
                }
                else if (!thief.Frozen)
                {
                    var target = Players.FirstOrDefault(x => x.Id == thief.Choice);
                    switch (VisitPlayer(thief, target))
                    {
                        case VisitResult.Success:
                            if (Program.R.Next(100) < Settings.ThiefStealChance && !WolfRoles.Contains(target.PlayerRole) && target.PlayerRole != IRole.Cultist && target.PlayerRole != IRole.SnowWolf)
                                StealRole(thief, target);
                            else goto fail;
                            break;
                        case VisitResult.VisitorDied:
                            if (!target.Burning && target.PlayerRole == IRole.GraveDigger) Send(GetLocaleString("ThiefFell", target.GetName()), thief.Id);
                            else if (target.PlayerRole == IRole.SerialKiller) Send(GetLocaleString("StealKiller"), thief.Id);
                            break;
                        case VisitResult.Fail:
                        fail:
                            Send(GetLocaleString("ThiefStealFailed", target.GetName()), thief.Id);
                            break;
                        case VisitResult.AlreadyDead:
                            Send(GetLocaleString("ThiefStealDead", target.GetName()), thief.Id);
                            break;
                    }
                }
            }
            #endregion

            #region Night Death Notifications to Group
            Dictionary<IPlayer, KillMthd> hunterFinalShot = new Dictionary<IPlayer, KillMthd>();
            var secret = !DbGroup.HasFlag(GroupConfig.ShowRolesDeath);
            if (Players.Any(x => x.DiedLastNight))
            {
                if (!secret && Players.Any(x => x.DiedLastNight && !x.DiedByVisitingVictim && x.KilledByRole == IRole.Arsonist))
                {
                    var burnDeaths = Players.Where(x => x.DiedLastNight && !x.DiedByVisitingVictim && x.KilledByRole == IRole.Arsonist);
                    SendWithQueue(GetLocaleString("Burning", string.Join("\n", burnDeaths.Select(x => $"{x.GetName()} {GetLocaleString("Was")} {GetDescription(x.PlayerRole)}"))));
                    foreach (var p in burnDeaths.Where(x => x.InLove && !burnDeaths.Any(y => y.Id == x.LoverId) && Players.Any(y => !string.IsNullOrEmpty(y.LoverMsg) && y.Id == x.LoverId)))
                    {
                        var lover = Players.First(x => x.Id == p.LoverId);
                        SendWithQueue(lover.LoverMsg);
                        if (lover.PlayerRole == IRole.Hunter)
                            hunterFinalShot.Add(lover, KillMthd.LoverDied);
                    }
                    foreach (var p in burnDeaths.Where(x => x.FinalShotDelay.HasValue))
                        hunterFinalShot.Add(p, p.FinalShotDelay.Value);
                }
                //notify of arsonist victims separately, if mode is not secret
                foreach (var p in Players.Where(x => x.DiedLastNight && (secret || !(!x.DiedByVisitingVictim && x.KilledByRole == IRole.Arsonist))))
                {
                    var msg = "";
                    var msg2 = "";
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
                                default:
                                    msg = GetLocaleString("DefaultKilled", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
                                    break;
                            }
                        }
                        else if (p.KilledByRole == IRole.Chemist) //killed by chemist
                        {
                            if (p.ChemistFailed) // player is chemist and accidentally suicided
                            {
                                msg = GetLocaleString("ChemistFailPublic", p.GetName());
                            }
                            else // player was successfully killed by chemist
                            {
                                msg = GetLocaleString("ChemistSuccessPublic", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");

                                if (p.PlayerRole == IRole.WiseElder && chemist != null && !chemist.IsDead)
                                {
                                    chemist.PlayerRole = IRole.Villager;
                                    chemist.ChangedRolesCount++;
                                    msg2 = GetLocaleString("ChemistKillWiseElder");
                                }
                            }
                        }
                        //killed by visiting grave digger
                        else if (p.KilledByRole == IRole.GraveDigger)
                        {
                            switch (p.PlayerRole)
                            {
                                case IRole.Wolf:
                                case IRole.AlphaWolf:
                                case IRole.Lycan:
                                case IRole.WolfCub:
                                    msg = GetLocaleString("WolfFellPublic", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
                                    break;
                                case IRole.Harlot:
                                case IRole.Cultist:
                                case IRole.Thief:
                                    msg = GetLocaleString(p.PlayerRole.ToString() + "FellPublic", p.GetName());
                                    break;
                                default:
                                    msg = GetLocaleString("DefaultFellPublic", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
                                    break;
                            }
                        }
                        //killed by arsonist
                        else if (p.KilledByRole == IRole.Arsonist && p.DiedByVisitingVictim)
                        {
                            switch (p.PlayerRole)
                            {
                                case IRole.AlphaWolf:
                                case IRole.Lycan:
                                case IRole.WolfCub:
                                case IRole.Wolf:
                                    msg = GetLocaleString("WolfVisitBurn", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
                                    break;
                                default:
                                    msg = GetLocaleString("DefaultVisitBurn", p.GetName(), $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
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
                                case IRole.Lycan:
                                case IRole.Wolf: //sk and hunter can kill
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
                                case IRole.Thief:
                                    if (p.KilledByRole == IRole.SerialKiller)
                                        msg = GetLocaleString("ThiefStoleKiller", p.GetName());
                                    break;
                                case IRole.Chemist:
                                    if (p.KilledByRole == IRole.SerialKiller)
                                        msg = GetLocaleString("ChemistSKPublic", p.GetName());
                                    break;
                                case IRole.SnowWolf:
                                    if (p.KilledByRole == IRole.SerialKiller)
                                        msg = GetLocaleString("SnowFrozeKiller", p.GetName());
                                    else // died from hunter
                                        msg = GetLocaleString("SnowFrozeHunter", p.GetName());
                                    break;
                                case IRole.Arsonist:
                                    //can only die by visiting sk
                                    msg = GetLocaleString("ArsonistVisitKiller", p.GetName());
                                    break;
                                case IRole.GraveDigger:
                                    if (p.KilledByRole == IRole.SerialKiller)
                                        msg = GetLocaleString("KillerSpottedDiggerPublic", p.GetName());
                                    else
                                        msg = GetLocaleString("WolvesSpottedDiggerPublic", p.GetName());
                                    break;
                            }
                        }
                    }
                    if (!String.IsNullOrEmpty(msg))
                        SendWithQueue(msg);
                    if (!String.IsNullOrEmpty(msg2))
                        SendWithQueue(msg2);
                    var lover = Players.FirstOrDefault(x => x.Id == p.LoverId && !string.IsNullOrEmpty(x.LoverMsg));
                    if (lover != null) SendWithQueue(lover.LoverMsg);
                    if (p.FinalShotDelay.HasValue) hunterFinalShot.Add(p, p.FinalShotDelay.Value);
                    if (lover?.FinalShotDelay.HasValue ?? false) hunterFinalShot.Add(lover, lover.FinalShotDelay.Value);
                }

                foreach (var h in hunterFinalShot)
                    HunterFinalShot(h.Key, h.Value);

                var bloodyVictims = Players.Where(x => x.TimeDied > nightStart && x.IsDead);

                if (bloodyVictims.Count() >= 4)
                    foreach (var p in bloodyVictims)
                        AddAchievement(p, AchievementsReworked.BloodyNight);
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

        private int CountCultistsAlive()
        {
            int cultists = 0;
            // check for alive cultists
            cultists += Players.Count(x => x.PlayerRole == IRole.Cultist && !x.IsDead);
            // check for dg about to transform
            cultists += Players.Count(x => x.PlayerRole == IRole.Doppelgänger && !x.IsDead && Players.FirstOrDefault(rm => rm.Id == x.RoleModel && rm.IsDead)?.PlayerRole == IRole.Cultist);
            // return the sum
            return cultists;
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
                var snowwolf = alivePlayers.FirstOrDefault(x => x.PlayerRole == IRole.SnowWolf);
                if (snowwolf != null)
                {
                    if (!checkbitten || alivePlayers.All(x => !x.Bitten)) //snowwolf should not turn wolf if bitten is about to turn
                    {
                        //snowwolf becomes normal wolf!
                        snowwolf.PlayerRole = IRole.Wolf;
                        snowwolf.ChangedRolesCount++;
                        Send(GetLocaleString("SnowWolfTurnWolf"), snowwolf.Id);
                    }
                    else return false; //bitten is turning wolf! game doesn't end
                }
                else
                {
                    var traitor = alivePlayers.FirstOrDefault(x => x.PlayerRole == IRole.Traitor);
                    if (traitor != null)
                    {
                        if (!checkbitten || alivePlayers.All(x => !x.Bitten)) //traitor should not turn wolf if bitten is about to turn
                        {
                            //traitor turns wolf!
                            Transform(traitor, IRole.Wolf, TransformationMethod.Traitor);
                        }
                        else return false; //bitten is turning wolf! game doesn't end
                    }
                }
            }

            switch (alivePlayers?.Count())
            {
                case 0:
                    return DoGameEnd(ITeam.NoOne);
                case 1:
                    var p = alivePlayers.FirstOrDefault();
                    if (p.PlayerRole == IRole.Tanner || p.PlayerRole == IRole.Sorcerer || p.PlayerRole == IRole.Thief || p.PlayerRole == IRole.Doppelgänger)
                        return DoGameEnd(ITeam.NoOne);
                    else
                        return DoGameEnd(p.Team);
                case 2:
                    //check for lovers
                    if (alivePlayers.All(x => x.InLove))
                        return DoGameEnd(ITeam.Lovers);
                    //check for Tanner + Sorcerer + Thief + Doppelgänger
                    if (alivePlayers.Select(x => x.PlayerRole).All(x => new IRole[] { IRole.Sorcerer, IRole.Tanner, IRole.Thief, IRole.Doppelgänger }.Contains(x)))
                        return DoGameEnd(ITeam.NoOne);
                    //check for Hunter + SK / Wolf
                    if (alivePlayers.Any(x => x.PlayerRole == IRole.Hunter))
                    {
                        var other = alivePlayers.FirstOrDefault(x => x.PlayerRole != IRole.Hunter);
                        if (other == null)
                            return DoGameEnd(ITeam.Village);
                        if (other.PlayerRole == IRole.SerialKiller)
                            return DoGameEnd(ITeam.SKHunter);
                        if (WolfRoles.Contains(other.PlayerRole) || other.PlayerRole == IRole.SnowWolf)
                        {
                            var hunter = alivePlayers.First(x => x.PlayerRole == IRole.Hunter);
                            if (Program.R.Next(100) < Settings.HunterKillWolfChanceBase)
                            {
                                SendWithQueue(GetLocaleString("HunterKillsWolfEnd", hunter.GetName(), other.GetName()));
                                KillPlayer(other, KillMthd.HunterShot, killer: hunter, isNight: false);
                                return DoGameEnd(ITeam.Village);
                            }
                            else
                            {
                                SendWithQueue(GetLocaleString("WolfKillsHunterEnd", hunter.GetName(), other.GetName()));
                                KillPlayer(hunter, KillMthd.Eat, killer: other, isNight: false, hunterFinalShot: false);
                                return DoGameEnd(ITeam.Wolf);
                            }
                        }
                    }
                    //check for SK
                    if (alivePlayers.Any(x => x.PlayerRole == IRole.SerialKiller))
                        return DoGameEnd(ITeam.SerialKiller);
                    //check for Arso
                    if (alivePlayers.Any(x => x.PlayerRole == IRole.Arsonist) && !alivePlayers.Any(x => x.PlayerRole == IRole.Gunner && x.Bullet > 0))
                        return DoGameEnd(ITeam.Arsonist);
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
                            case IRole.Lycan:
                            case IRole.SnowWolf: // shouldn't happen but well
                                return DoGameEnd(ITeam.Wolf);
                            case IRole.CultistHunter:
                                var cultist = alivePlayers.FirstOrDefault(x => x.PlayerRole == IRole.Cultist);
                                SendWithQueue(GetLocaleString("CHKillsCultistEnd", cultist.GetName(), other.GetName()));
                                DBKill(other, cultist, KillMthd.Hunt);
                                return DoGameEnd(ITeam.Village);
                            default:
                                //autoconvert the other
                                if (other.PlayerRole != IRole.Doppelgänger && other.PlayerRole != IRole.Thief)
                                {
                                    Transform(other, IRole.Cultist, TransformationMethod.AutoConvertToCult);
                                }
                                return DoGameEnd(ITeam.Cult);
                        }
                    }
                    break;
                case 3:
                    if (alivePlayers.Select(x => x.PlayerRole).All(x => new IRole[] { IRole.Sorcerer, IRole.Thief, IRole.Doppelgänger }.Contains(x)))
                        return DoGameEnd(ITeam.NoOne);
                    break;
                default:
                    break;
            }


            if (alivePlayers.Any(x => x.Team == ITeam.SerialKiller)) //there is still SK alive, do nothing (surely more than two players)
                return false;

            if (alivePlayers.Any(x => x.Team == ITeam.Arsonist)) //there is still Arsonist alive, do nothing (surely more than two players)
                return false;
            //is everyone left a cultist?
            if (alivePlayers.All(x => x.Team == ITeam.Cult))
                return DoGameEnd(ITeam.Cult);

            //do the wolves outnumber the others?
            if (alivePlayers.Count(x => WolfRoles.Contains(x.PlayerRole) || x.PlayerRole == IRole.SnowWolf) >= alivePlayers.Count(x => !WolfRoles.Contains(x.PlayerRole) && x.PlayerRole != IRole.SnowWolf))
            {
                if (alivePlayers.Any(x => x.PlayerRole == IRole.Gunner && x.Bullet > 0))
                {
                    var wolves = alivePlayers.Where(x => WolfRoles.Contains(x.PlayerRole) || x.PlayerRole == IRole.SnowWolf);
                    var others = alivePlayers.Where(x => !WolfRoles.Contains(x.PlayerRole) && x.PlayerRole != IRole.SnowWolf);
                    // gunner makes the difference only if wolves are exactly as many as the others, or two wolves are in love and the gunner can kill two of them at once
                    var gunnermakesthedifference = (wolves.Count() == others.Count()) || (wolves.Count() == others.Count() + 1 && wolves.Count(x => x.InLove) == 2);
                    if (gunnermakesthedifference)
                    {
                        // do nothing, gunner can still make VGs win
                        foreach (var p in alivePlayers.Where(x => x.Team == ITeam.Village))
                            AddAchievement(p, AchievementsReworked.GunnerSaves);
                        return false;
                    }
                }
                return DoGameEnd(ITeam.Wolf);
            }

            if (alivePlayers.All(x => !WolfRoles.Contains(x.PlayerRole) && x.PlayerRole != IRole.SnowWolf && x.PlayerRole != IRole.Cultist && x.PlayerRole != IRole.SerialKiller && x.PlayerRole != IRole.Arsonist)) //checks for SK and snow wolf are actually useless...
                //no wolf, no cult, no SK, no Arsonist... VG wins!
                if (!checkbitten || alivePlayers.All(x => !x.Bitten)) //unless bitten is about to turn into a wolf
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
                CheckLongHaul();
                var msg = "";

                var game = db.Games.FirstOrDefault(x => x.Id == GameId) ?? new Database.Game();
                game.TimeEnded = DateTime.Now;

                if (team == ITeam.Lovers)
                {
                    var lovers = Players.Where(x => x.InLove);
                    var forbidden = lovers.Any(x => WolfRoles.Contains(x.PlayerRole)) && lovers.Any(x => x.PlayerRole == IRole.Villager);
                    foreach (var w in lovers)
                    {
                        if (forbidden)
                            AddAchievement(w, AchievementsReworked.ForbiddenLove);
                        if (IsDateAnywhere(14, 02, 2020))
                            AddAchievement(w, AchievementsReworked.TodaysSpecial);
                        w.Won = true;
                        var p = GetDBGamePlayer(w, db);
                        p.Won = true;
                    }
                }
                else
                {
                    foreach (var w in Players.Where(x => x.Team == team))
                    {
                        //for sk and arsonist, only let the one that is alive win
                        if ((team == ITeam.SerialKiller || team == ITeam.Arsonist) && w.IsDead)
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
                        var alives = Players.Where(x => !x.IsDead);
                        var deathmessage = "";
                        switch (alives.Count())
                        {
                            case 3:
                                // Thief, Doppelganger, Sorcerer
                                if (new[] { IRole.Sorcerer, IRole.Thief, IRole.Doppelgänger }.All(x => alives.Any(y => y.PlayerRole == x)))
                                {
                                    var doppelganger = alives.FirstOrDefault(x => x.PlayerRole == IRole.Doppelgänger);
                                    var thief = alives.FirstOrDefault(x => x.PlayerRole == IRole.Thief);
                                    var sorc = alives.FirstOrDefault(x => x.PlayerRole == IRole.Sorcerer);
                                    
                                    if (doppelganger != null && thief != null && sorc != null)
                                    {
                                        AddAchievement(sorc, AchievementsReworked.TimeToRetire);
                                        deathmessage = GetLocaleString("SorcererEnd", sorc.GetName()) + Environment.NewLine;
                                        deathmessage += Environment.NewLine + GetLocaleString("DoppelgangerEnd", doppelganger.GetName()) + Environment.NewLine;
                                        deathmessage += Environment.NewLine + GetLocaleString("ThiefEnd", thief.GetName());
                                    }
                                }
                                break;
                            case 2:
                                // Tanner and sorcerer/thief/doppelgänger, let first sorcerer/thief/doppelgänger, then tanner die.
                                if (alives.Any(x => x.PlayerRole == IRole.Tanner) && new IRole[] { IRole.Sorcerer, IRole.Thief, IRole.Doppelgänger }.Contains(alives.First(x => x.PlayerRole != IRole.Tanner).PlayerRole))
                                {
                                    var sorcOrThiefOrDG = alives.FirstOrDefault(x => x.PlayerRole == IRole.Sorcerer || x.PlayerRole == IRole.Thief || x.PlayerRole == IRole.Doppelgänger);
                                    var tann = alives.FirstOrDefault(x => x.PlayerRole == IRole.Tanner);

                                    if (sorcOrThiefOrDG != null && tann != null)
                                    {
                                        KillPlayer(tann, KillMthd.Suicide, killer: tann, isNight: false);
                                        deathmessage = GetLocaleString("TannerEnd", tann.GetName()) + Environment.NewLine;
                                        
                                        //check if the tanner is role model of doppelganger or not. If yes, also kill the doppelganger after his transform
                                        if (sorcOrThiefOrDG.PlayerRole == IRole.Doppelgänger)
                                        {
                                            CheckDoppelganger();
                                            if (sorcOrThiefOrDG.PlayerRole == IRole.Tanner)
                                            {
                                                KillPlayer(sorcOrThiefOrDG, KillMthd.Suicide, killer: sorcOrThiefOrDG, isNight: false);
                                                deathmessage += Environment.NewLine + GetLocaleString("TannerEnd", sorcOrThiefOrDG.GetName());
                                            }
                                            else
                                            {
                                                deathmessage += Environment.NewLine + GetLocaleString("DoppelgangerEnd", sorcOrThiefOrDG.GetName());
                                            }
                                        }
                                        else
                                        {
                                            deathmessage += Environment.NewLine + GetLocaleString($"{sorcOrThiefOrDG.PlayerRole}End", sorcOrThiefOrDG.GetName());
                                        }
                                        
                                        if (sorcOrThiefOrDG.PlayerRole == IRole.Sorcerer)
                                        {
                                            AddAchievement(sorcOrThiefOrDG, AchievementsReworked.TimeToRetire);
                                        }
                                    }
                                }
                                // thief/doppelganger and sorcerer
                                else if (alives.Any(x => x.PlayerRole == IRole.Sorcerer) && (alives.First(x => x.PlayerRole != IRole.Sorcerer).PlayerRole == IRole.Thief || alives.First(x => x.PlayerRole != IRole.Sorcerer).PlayerRole == IRole.Doppelgänger))
                                {
                                    var sorcerer = alives.FirstOrDefault(x => x.PlayerRole == IRole.Sorcerer);
                                    var thiefOrDG = alives.FirstOrDefault(x => x.PlayerRole == IRole.Thief || x.PlayerRole == IRole.Doppelgänger);

                                    if (sorcerer != null && thiefOrDG != null)
                                    {
                                        deathmessage = GetLocaleString("SorcererEnd", sorcerer.GetName()) + Environment.NewLine;
                                        AddAchievement(sorcerer, AchievementsReworked.TimeToRetire);
                                        if (thiefOrDG.PlayerRole == IRole.Doppelgänger)
                                        {
                                            deathmessage += Environment.NewLine + GetLocaleString("DoppelgangerEnd", thiefOrDG.GetName());
                                        }
                                        else
                                        {
                                            deathmessage += Environment.NewLine + GetLocaleString("ThiefEnd", thiefOrDG.GetName());
                                        }
                                    }
                                }
                                //thief and doppelganger
                                else if (alives.Any(x => x.PlayerRole == IRole.Thief) && alives.First(x => x.PlayerRole != IRole.Thief).PlayerRole == IRole.Doppelgänger)
                                {
                                    var theThief = alives.FirstOrDefault(x => x.PlayerRole == IRole.Thief);
                                    var DG = alives.FirstOrDefault(x => x.PlayerRole == IRole.Doppelgänger);
                                    
                                    if (theThief != null && DG != null)
                                    {
                                        deathmessage = GetLocaleString("ThiefEnd", theThief.GetName()) + Environment.NewLine;
                                        deathmessage += Environment.NewLine + GetLocaleString("DoppelgangerEnd", DG.GetName());
                                    }
                                }
                                break;

                            case 1: // Tanner or sorcerer or thief or doppelgänger
                                var lastone = alives.FirstOrDefault(x => x.PlayerRole == IRole.Sorcerer || x.PlayerRole == IRole.Thief || x.PlayerRole == IRole.Doppelgänger);
                                if (lastone != null)
                                {
                                    if (lastone.PlayerRole == IRole.Tanner)
                                        KillPlayer(lastone, KillMthd.Suicide, killer: lastone, isNight: false);
                                    else if (lastone.PlayerRole == IRole.Sorcerer)
                                        AddAchievement(lastone, AchievementsReworked.TimeToRetire);
                                    
                                    if (lastone.PlayerRole == IRole.Doppelgänger)
                                        deathmessage = GetLocaleString("DoppelgangerEnd", lastone.GetName());
                                    else
                                        deathmessage = GetLocaleString($"{lastone.PlayerRole}End", lastone.GetName());
                                }
                                break;

                            default:
                                break;
                        }

                        if (!string.IsNullOrEmpty(deathmessage)) SendWithQueue(deathmessage);

                        msg += GetLocaleString("NoWinner");
                        game.Winner = "NoOne";
                        SendWithQueue(msg, GetRandomImage(NoWinner));
                        break;

                    case ITeam.Wolf:
                        if (Players.Count(x => !x.IsDead && (WolfRoles.Contains(x.PlayerRole)) || x.PlayerRole == IRole.SnowWolf) > 1)
                        {
                            msg += GetLocaleString("WolvesWin");
                            game.Winner = "Wolves";
                            SendWithQueue(msg, GetRandomImage(WolvesWin));
                        }
                        else
                        {
                            msg += GetLocaleString("WolfWins");
                            game.Winner = "Wolf";
                            SendWithQueue(msg, GetRandomImage(WolfWin));
                        }
                        break;
                    case ITeam.Tanner:
                        msg += GetLocaleString("TannerWins");
                        game.Winner = "Tanner";
                        SendWithQueue(msg, GetRandomImage(TannerWin));
                        break;
                    case ITeam.Arsonist:
                        if (Players.Count(x => !x.IsDead) > 1)
                        {
                            var alive = Players.Where(x => !x.IsDead);
                            var otherPerson = alive.FirstOrDefault(x => x.PlayerRole != IRole.Arsonist);
                            var arsonist = alive.FirstOrDefault(x => x.PlayerRole == IRole.Arsonist);
                            SendWithQueue(GetLocaleString("ArsonistWinsOverpower", arsonist.GetName(), otherPerson.GetName()));
                            DBKill(arsonist, otherPerson, KillMthd.Burn);
                            if (otherPerson != null)
                            {
                                otherPerson.IsDead = true;
                                otherPerson.TimeDied = DateTime.Now;
                            }
                        }
                        msg += GetLocaleString("ArsonistWins");
                        game.Winner = "Arsonist";
                        SendWithQueue(msg, GetRandomImage(ArsonistWins));
                        break;
                    case ITeam.Cult:
                        msg += GetLocaleString("CultWins");
                        game.Winner = "Cult";
                        SendWithQueue(msg, GetRandomImage(CultWins)); //, GetRandomImage(Program.VillagersWin));
                        break;
                    case ITeam.SerialKiller:
                        if (Players.Count(x => !x.IsDead) > 1)
                        {
                            var alive = Players.Where(x => !x.IsDead);
                            var otherPerson = alive.FirstOrDefault(x => x.PlayerRole != IRole.SerialKiller);
                            var sk = alive.FirstOrDefault(x => x.PlayerRole == IRole.SerialKiller);
                            SendWithQueue(GetLocaleString("SerialKillerWinsOverpower", sk.GetName(), otherPerson.GetName()));
                            if (otherPerson != null)
                            {
                                KillPlayer(otherPerson, KillMthd.SerialKilled, killer: sk, isNight: false, hunterFinalShot: false);
                            }
                        }
                        msg += GetLocaleString("SerialKillerWins");
                        game.Winner = "SerialKiller";
                        SendWithQueue(msg, GetRandomImage(SerialKillerWins));
                        break;
                    case ITeam.Lovers:
                        msg += GetLocaleString("LoversWin");
                        game.Winner = "Lovers";
                        if (IsDateAnywhere(14, 2, 2021))
                            LoversWin = new List<string> { "CgACAgQAAxkBY4AFS2Am26QAAVRzfm3kG7wxS9Mq7PFpsAACnQIAAkAwtVI_dFDRMF2c_h4E" };
                        SendWithQueue(msg, GetRandomImage(LoversWin));
                        break;
                    case ITeam.SKHunter:
                        var skhunter = Players.Where(x => !x.IsDead);
                        var hunter = skhunter.FirstOrDefault(x => x.PlayerRole != IRole.SerialKiller);
                        var skh = skhunter.FirstOrDefault(x => x.PlayerRole == IRole.SerialKiller);
                        msg += GetLocaleString("NoWinner");
                        game.Winner = "NoOne";
                        AddAchievement(skh, AchievementsReworked.DoubleKill);
                        AddAchievement(hunter, AchievementsReworked.DoubleKill);
                        if (skh != null)
                        {
                            KillPlayer(skh, KillMthd.HunterShot, killer: hunter, isNight: false);
                            if (hunter != null)
                            {
                                KillPlayer(hunter, KillMthd.SerialKilled, killer: skh, isNight: false, hunterFinalShot: false);
                                SendWithQueue(GetLocaleString("SKHunterEnd", skh.GetName(), hunter.GetName()));
                            }
                        }
                        SendWithQueue(msg, GetRandomImage(NoWinner));
                        break;
                    default: //village
                        msg += GetLocaleString("VillageWins");
                        game.Winner = "Village";
                        SendWithQueue(msg, GetRandomImage(VillagersWin));
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
                    _timePlayed = game.TimeEnded.Value - game.TimeStarted.Value;
                    msg += "\n" + GetLocaleString("EndTime", _timePlayed.Value.ToString(@"hh\:mm\:ss"));
                }
                SendWithQueue(msg);
                var customwins = CustomWinMessages.Where(x => Players.Any(y => y.Id == x.Key && y.Won));
                if (customwins.Any())
                    SendWithQueue(customwins.ElementAt(Program.R.Next(customwins.Count())).Value);
                //Program.Bot.SendTextMessage(ChatId, "[Enjoy playing? Support the developers and get some swag!](https://teespring.com/stores/werewolf-for-telegram)", parseMode: ParseMode.Markdown, disableWebPagePreview: true);
                UpdateAchievements();
                UpdateGroupRanking();
                //if (ChatId == -1001094614730)
                //{
                //    foreach (var p in Players.Where(x => x.IsDead))
                //    {
                //        Program.Bot.RestrictChatMemberAsync(-1001094614730, p.Id, default(DateTime), true, true, true, true);
                //    }
                //}

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
                var possibleChoices = Players.Where(x => !x.IsDead && x.Id != player.Id).ToList();
                if (ShufflePlayerList)
                    possibleChoices.Shuffle();
                var choices = possibleChoices.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Lynch}|{x.Id}") }).ToList();
                SendMenu(choices, player, GetLocaleString("AskLynch"), QuestionType.Lynch);
                Thread.Sleep(100);
            }
        }

        private void SendMenu(List<InlineKeyboardButton[]> choices, IPlayer to, string text, QuestionType qtype)
        {
            choices = choices.ToList();
            var skip = choices.FirstOrDefault(x => x[0].Text == GetLocaleString("Skip"));

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
                    ValidAnswers = choices.Select(x => x[0].CallbackData).ToArray(),
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
                    if (ShufflePlayerList)
                        options.Shuffle();
                    var choices = options.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Detect}|{x.Id}") }).ToList();
                    choices.Add(new[] { InlineKeyboardButton.WithCallbackData(GetLocaleString("Skip"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Detect}|-1") });
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
                        InlineKeyboardButton.WithCallbackData(GetLocaleString("Reveal"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Mayor}|reveal")
                    }
                }.ToList();
                SendMenu(choices, mayor, GetLocaleString("AskMayor"), QuestionType.Mayor);
            }

            var pacifist = Players.FirstOrDefault(x => x.PlayerRole == IRole.Pacifist & !x.IsDead);
            if (pacifist != null && GameDay == 1)
            {
                var choices = new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(GetLocaleString("Peace"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Pacifist}|peace")
                    }
                }.ToList();
                SendMenu(choices, pacifist, GetLocaleString("AskPacifist"), QuestionType.Pacifist);
            }

            var sandman = Players.FirstOrDefault(x => x.PlayerRole == IRole.Sandman & !x.IsDead & !x.HasUsedAbility);
            if (sandman != null)
            {
                var choices = new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(GetLocaleString("Yes"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Sandman}|yes"), InlineKeyboardButton.WithCallbackData(GetLocaleString("No"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Sandman}|no")
                    }
                }.ToList();
                SendMenu(choices, sandman, GetLocaleString("AskSandman"), QuestionType.Sandman);
            }

            var blacksmith = Players.FirstOrDefault(x => x.PlayerRole == IRole.Blacksmith & !x.IsDead & !x.HasUsedAbility);

            if (blacksmith != null)
            {
                var choices = new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(GetLocaleString("Yes"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.SpreadSilver}|yes"), InlineKeyboardButton.WithCallbackData(GetLocaleString("No"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.SpreadSilver}|no")
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
                        if (ShufflePlayerList)
                            options.Shuffle();
                        var choices = options.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Shoot}|{x.Id}") }).ToList();
                        choices.Add(new[] { InlineKeyboardButton.WithCallbackData(GetLocaleString("Skip"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Shoot}|-1") });
                        SendMenu(choices, gunner, GetLocaleString("AskShoot", gunner.Bullet), QuestionType.Shoot);
                    }
                }
            }

            var spumpkin = Players.FirstOrDefault(x => x.PlayerRole == IRole.Spumpkin & !x.IsDead);

            if (spumpkin != null)
            {
                spumpkin.Choice = 0;
                var options = Players.Where(x => !x.IsDead && x.Id != spumpkin.Id).ToList();
                if (options.Any())
                {
                    if (ShufflePlayerList)
                        options.Shuffle();
                    var choices = options.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Shoot}|{x.Id}") }).ToList();
                    choices.Add(new[] { InlineKeyboardButton.WithCallbackData(GetLocaleString("Skip"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Shoot}|-1") });
                    SendMenu(choices, spumpkin, GetLocaleString("AskDetonate"), QuestionType.Shoot);
                }
            }

            var troublemaker = Players.FirstOrDefault(x => x.PlayerRole == IRole.Troublemaker & !x.IsDead & !x.HasUsedAbility);

            if (troublemaker != null)
            {
                troublemaker.Choice = 0;
                var choices = new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(GetLocaleString("Yes"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Trouble}|yes"), InlineKeyboardButton.WithCallbackData(GetLocaleString("No"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.Trouble}|no")
                    }
                }.ToList();
                SendMenu(choices, troublemaker, GetLocaleString("AskTroublemaker"), QuestionType.Trouble);
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
                    case IRole.Oracle:
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
                    case IRole.Lycan:
                        if (!_silverSpread)
                        {
                            targets = targetBase.Where(x => !WolfRoles.Contains(x.PlayerRole) && x.PlayerRole != IRole.SnowWolf).ToList();
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
                        targets = targetBase.Where(x => x.PlayerRole != IRole.Cultist).ToList();
                        msg = GetLocaleString("AskConvert");
                        var otherCults = targetBase.Where(x => x.PlayerRole == IRole.Cultist).ToList();
                        if (otherCults.Any())
                        {
                            var andStr = GetLocaleString("And");
                            msg += GetLocaleString("DiscussWith", otherCults.Select(x => x.GetName()).Aggregate((current, a) => current + andStr + a));
                        }
                        qtype = QuestionType.Convert;
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
                    case IRole.Thief:
                        if ((!ThiefFull && GameDay == 1) || ThiefFull)
                        {
                            targets = targetBase.ToList();
                            msg = GetLocaleString("AskThief");
                            qtype = QuestionType.Thief;
                        }
                        else player.Choice = -1;
                        break;
                    case IRole.Chemist:
                        if (player.HasUsedAbility)
                        {
                            targets = targetBase.ToList();
                            msg = GetLocaleString("AskChemist");
                            qtype = QuestionType.Chemistry;
                        }
                        else
                        {
                            player.HasUsedAbility = true;
                            player.Choice = -1;
                            Send(GetLocaleString("ChemistBrewing"), player.Id);
                        }
                        break;
                    case IRole.SnowWolf:
                        if (!_silverSpread)
                        {
                            targets = targetBase.Where(x => !WolfRoles.Contains(x.PlayerRole) && !x.Frozen).ToList(); // don't freeze one player twice in a row
                            msg = GetLocaleString("AskFreeze");
                            qtype = QuestionType.Freeze;
                        }
                        break;
                    case IRole.Arsonist:
                        targets = targetBase.Where(x => !x.Doused).ToList();
                        var alreadyDoused = targetBase.Where(x => !x.IsDead && x.Doused).ToList();
                        if (alreadyDoused.Any())
                        {
                            var andStr = GetLocaleString("And");
                            msg = GetLocaleString("AskArsonist") + "\n";
                            msg += GetLocaleString("AlreadyDousedList", alreadyDoused.Select(x => x.GetName()).Aggregate((current, a) => current + andStr + a));
                        }
                        else
                            msg = GetLocaleString("AskArsonistNoDoused");
                        qtype = QuestionType.Douse;
                        break;
                    case IRole.GraveDigger:
                        var diedSinceLastGrave = Players
                            .Where(x => x.IsDead && x.TimeDied > lastGrave && !x.DiedByFleeOrIdle).ToList();

                        secondLastGrave = lastGrave;
                        lastGrave = DateTime.Now;
                        player.DugGravesLastNight = diedSinceLastGrave.Count;
                        player.Choice = -1;
                        if (diedSinceLastGrave.Count < 1)
                        {
                            Send(GetLocaleString("DigNoGraves"), player.Id);
                        }
                        else
                        {
                            string playersDead = string.Join(GetLocaleString("And"), diedSinceLastGrave.Select(x => x.GetName()));
                            Send(GetLocaleString("DigGraves", playersDead), player.Id);
                        }
                        break;
                    default:
                        continue;
                }

                if (player.Drunk || string.IsNullOrEmpty(msg))
                {
                    player.Choice = -1;
                    continue;
                }

                if (ShufflePlayerList)
                    targets.Shuffle();

                var buttons = targets.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"vote|{Program.ClientId}|{Guid}|{(int)qtype}|{x.Id}") }).ToList();
                if (player.PlayerRole == IRole.Arsonist && Players.Any(x => !x.IsDead && x.Doused))
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetLocaleString("Spark"), $"vote|{Program.ClientId}|{Guid}|{(int)qtype}|-2") });
                if ((player.PlayerRole != IRole.WildChild && player.PlayerRole != IRole.Cupid && player.PlayerRole != IRole.Doppelgänger && player.PlayerRole != IRole.Thief) || (player.PlayerRole == IRole.Thief && ThiefFull))
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(GetLocaleString("Skip"), $"vote|{Program.ClientId}|{Guid}|{(int)qtype}|-1") });

                SendMenu(buttons, player, msg, qtype);
                Thread.Sleep(100);

            } // alive players foreach
            _silverSpread = false;

            foreach (var p in Players)
            {
                //reset drunk, frozen and burning status
                p.Drunk = false;
                p.Frozen = false;
                p.Burning = false;
            }
        }

        #endregion

        #region Helpers
        public void CleanupButtons()
        {
            foreach (var id in _joinButtons)
            {
                Program.Bot.DeleteMessageAsync(chatId: ChatId, messageId: id);
                Thread.Sleep(500);
            }
        }
        public void FleePlayer(long banid)
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
                    KillPlayer(p, KillMthd.Flee, killer: p, isNight: false, hunterFinalShot: false);
                    p.Fled = true;
                    if (DbGroup.HasFlag(GroupConfig.ShowRolesDeath))
                        SendWithQueue(GetLocaleString("PlayerRoleWas", p.GetName(), GetDescription(p.PlayerRole)));
                    CheckRoleChanges();

                    CheckForGameEnd();
                }
                else if (IsJoining)
                {
                    _playerListChanged = true;
                    Players.Remove(p);
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
                    Program.Bot.EditMessageReplyMarkupAsync(chatId: ChatId, messageId: _joinMsgId, replyMarkup: null);
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

        public void HunterFinalShot(IPlayer hunter, KillMthd method, bool delay = false)
        {
            CheckRoleChanges();
            if (delay)
            {
                hunter.FinalShotDelay = method;
                return;
            }

            //send a menu to the hunter, asking who he wants to kill as he is hung....
            var hunterChoices = new List<InlineKeyboardButton[]>();
            var possibleTargets = Players.Where(x => !x.IsDead).ToList();
            if (ShufflePlayerList)
                possibleTargets.Shuffle();
            hunterChoices.AddRange(possibleTargets.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.HunterKill}|{x.Id}") }));
            hunterChoices.Add(new[] { InlineKeyboardButton.WithCallbackData(GetLocaleString("Skip"), $"vote|{Program.ClientId}|{Guid}|{(int)QuestionType.HunterKill}|-1") });

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
                Program.Bot.EditMessageTextAsync(chatId: hunter.Id, messageId: hunter.CurrentQuestion.MessageId, text: GetLocaleString("TimesUp"));
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
                        SendWithQueue(GetLocaleString(method == KillMthd.Lynch ? "HunterKilledFinalLynched" : "HunterKilledFinalShot", hunter.GetName(), killed.GetName(), !DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? "" : $"{killed.GetName()} {GetLocaleString("Was")} {GetDescription(killed.PlayerRole)}"));
                        if (killed.PlayerRole == IRole.WiseElder)
                        {
                            SendWithQueue(GetLocaleString("HunterKilledWiseElder", hunter.GetName(), killed.GetName()));
                            Transform(hunter, IRole.Villager, TransformationMethod.KillElder);
                            AddAchievement(hunter, AchievementsReworked.DemotedByTheDeath);
                        }
                        if (killed.PlayerRole == IRole.Wolf || killed.PlayerRole == IRole.AlphaWolf || killed.PlayerRole == IRole.WolfCub || killed.PlayerRole == IRole.SerialKiller || killed.PlayerRole == IRole.Lycan || killed.PlayerRole == IRole.SnowWolf)
                            AddAchievement(hunter, AchievementsReworked.HeyManNiceShot);

                        CheckRoleChanges(); // In case the hunter shot their own doppelgänger, they should die as hunter too
                        if (killed.PlayerRole == IRole.Hunter)
                            AddAchievement(hunter, AchievementsReworked.Domino);
                        KillPlayer(killed, KillMthd.HunterShot, killer: hunter, isNight: false);

                        CheckRoleChanges(); // In case the hunter shot someone's role model / the seer / ...
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

        public enum GameTime
        {
            Day,
            Lynch,
            Night
        }

        private long ChooseRandomPlayerId(IPlayer exclude, bool all = true)
        {
            try
            {
                var possible = Players.Where(x => x.Id != exclude?.Id).ToList();
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
            Program.Bot.AnswerCallbackQueryAsync(callbackQueryId: query.Id, text: edit ? null : text, showAlert: showAlert);
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
            //Program.MessagesSent++;
            return Program.Bot.EditMessageTextAsync(chatId: id, messageId: msgId, text: text, replyMarkup: replyMarkup);
        }

        internal void LogException(AggregateException ae)
        {
            foreach (var e in ae.InnerExceptions)
                LogAllExceptions(e);
        }

        internal void LogAllExceptions(Exception e)
        {
            do
            {
                LogException(e);
                e = e.InnerException;
            } while (e != null);
            return;
        }

        internal void LogException(Exception e)
        {
            Send(Program.Version.FileVersion + $"\nGroup: {ChatId} ({ChatGroup})\nLanguage: {DbGroup?.Language ?? "null"}\n{Program.ClientId}\n{e.Message}\n{e.StackTrace}", Program.ErrorGroup);
        }

        private void KillPlayer(IPlayer p, KillMthd? killMethod, IPlayer killer = null, bool isNight = true, bool diedByVisitingVictim = false, bool diedByVisitingKiller = false, IRole? killedByRole = null, bool hunterFinalShot = true, List<IPlayer> dyingSimultaneously = null)
            => KillPlayer(p, killMethod, killers: new IPlayer[] { killer }, isNight: isNight, diedByVisitingVictim: diedByVisitingVictim, diedByVisitingKiller: diedByVisitingKiller, killedByRole: killedByRole ?? killer?.PlayerRole, hunterFinalShot: hunterFinalShot, dyingSimultaneously: dyingSimultaneously);

        private void KillPlayer(IPlayer p, KillMthd? killMethod, IEnumerable<IPlayer> killers = null, bool isNight = true, bool diedByVisitingVictim = false, bool diedByVisitingKiller = false, IRole? killedByRole = null, bool hunterFinalShot = true, List<IPlayer> dyingSimultaneously = null)
        {
            // if it was a death by love, don't handle it separately
            p.DiedLastNight = isNight && killMethod != KillMthd.LoverDied;
            p.TimeDied = DateTime.Now;
            if (killedByRole.HasValue) p.KilledByRole = killedByRole.Value;
            p.DiedByVisitingKiller = diedByVisitingKiller;
            p.DiedByVisitingVictim = diedByVisitingVictim;
            p.IsDead = true;
            if (killers != null && killMethod.HasValue) DBKill(killers, p, killMethod.Value);
            // if it was an idle kill or flee, all further consequences will be skipped
            if (killMethod == KillMthd.Idle || killMethod == KillMthd.Flee)
            {
                p.DiedByFleeOrIdle = true;
                return;
            }

            // Only kill the lover if they are not already dying at the very same time (that is, when both of them are burnt by arsonist).
            if (p.InLove && Players.Any(x => x.Id == p.LoverId && !x.IsDead && !(dyingSimultaneously?.Contains(x) ?? false)))
            {
                if (killMethod.HasValue && new[] { KillMthd.HunterShot, KillMthd.Shoot }.Contains(killMethod.Value) && killers.Count() == 1
                && !new[] { p, Players.First(x => x.Id == p.LoverId) }.Any(x => new[] { ITeam.Village, ITeam.Neutral, ITeam.Thief }.Contains(x.Team)))
                    AddAchievement(killers.First(), AchievementsReworked.DoubleShot);
                KillLover(p, sendNoMessage: isNight);
            }

            switch (p.PlayerRole)
            {
                case IRole.WolfCub:
                    WolfCubKilled = true;
                    break;
                case IRole.Hunter:
                    if (killMethod.HasValue && hunterFinalShot) HunterFinalShot(p, killMethod.Value, delay: isNight);
                    break;
            }
        }

        #endregion

        #region Database Helpers
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

                try
                {
                    //if (ChatId == Settings.VeteranChatId) //vets group
                    //{
                    //    //let's try this out, shall we?
                    //    var status = Program.Bot.GetChatMemberAsync(Settings.VeteranChatId, victim.Id).Result;
                    //    if (status.Status != ChatMemberStatus.Administrator && status.Status != ChatMemberStatus.Creator)
                    //    {
                    //        Program.Bot.RestrictChatMemberAsync(Settings.VeteranChatId, victim.Id, DateTime.Now.AddHours(1), false, false, false, false);
                    //    }
                    //}
                }
                catch
                {

                }
            }

            if (victim.LoverId == killer.Id && Time == GameTime.Night && method != KillMthd.LoverDied)
            {
                if (GameDay == 1) //killed lover on first night
                    AddAchievement(killer, AchievementsReworked.OhShi);
                else if (WolfRoles.Contains(killer.PlayerRole)) //wolf pack killed lover, not on first night
                    AddAchievement(killer, AchievementsReworked.ShouldveMentioned);
            }

        }

        private void KillLover(IPlayer victim, bool sendNoMessage = false)
        {
            var p = Players.FirstOrDefault(x => x.Id == victim.LoverId && !x.IsDead);
            if (p != null)
            {
                if (!sendNoMessage) SendWithQueue(GetLocaleString("LoverDied", victim.GetName(), p.GetName(), !DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? "" : $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}"));
                else p.LoverMsg = GetLocaleString("LoverDied", victim.GetName(), p.GetName(), !DbGroup.HasFlag(GroupConfig.ShowRolesDeath) ? "" : $"{p.GetName()} {GetLocaleString("Was")} {GetDescription(p.PlayerRole)}");
                KillPlayer(p, KillMthd.LoverDied, killer: victim, isNight: sendNoMessage);
            }
            CheckRoleChanges();
        }

        private bool _longHaulReached;

        private void CheckLongHaul()
        {
            if (Players == null) return;
            if ((DateTime.Now - _timeStarted).Hours >= 1 & !_longHaulReached)
            {
                foreach (var p in Players.Where(x => !x.IsDead && !x.Fled))
                {
                    AddAchievement(p, AchievementsReworked.LongHaul);
                }
                _longHaulReached = true;
            }
        }

        /// <summary>
        /// True if right now it's the specified date in any timezone on the world.
        /// </summary>
        public static bool IsDateAnywhere(int day, int month, int? year = null)
        {
            var dates = new[] { DateTime.UtcNow.AddHours(-11), DateTime.UtcNow, DateTime.UtcNow.AddHours(14) };
            return dates.Any(x => x.Day == day && x.Month == month && (!year.HasValue || x.Year == year.Value));
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
                var convention = Players.Count(x => x.PlayerRole == IRole.Cultist && !x.IsDead) >= 10;
                foreach (var player in Players.Where(x => !x.Fled)) //flee / afk? no achievements for you.
                {
                    var p = GetDBPlayer(player, db);
                    if (p != null)
                    {

                        // switching to new achv system, do not notify now

                        /*
                        var newFlags = newAch.GetUniqueFlags().ToList();
                        if (newAch == Achievements.None) continue;
                        var msg = "New Unlocks!".ToBold() + Environment.NewLine;
                        msg = newFlags.Aggregate(msg, (current, a) => current + $"{a.GetName().ToBold()}\n{a.GetDescription()}\n\n");
                        Send(msg, p.TelegramId);
                        */

                        //
                        //
                        // new achievements system
                        //
                        //

                        // saving new achvs
                        BitArray newAch2 = new BitArray(200);

                        // existing achvs
                        var ach2 = p.NewAchievements == null ? new BitArray(200) : new BitArray(p.NewAchievements);


                        //calculate achievements
                        //automatically get welcome to hell
                        if (!ach2.HasFlag(AchievementsReworked.WelcomeToHell))
                            newAch2.Set(AchievementsReworked.WelcomeToHell);
                        if (!ach2.HasFlag(AchievementsReworked.WelcomeToAsylum) && GameMode == GameMode.Chaos)
                            newAch2.Set(AchievementsReworked.WelcomeToAsylum);
                        if (!ach2.HasFlag(AchievementsReworked.AlzheimerPatient) && Language.Contains("Amnesia"))
                            newAch2.Set(AchievementsReworked.AlzheimerPatient);
                        //if (!ach2.HasFlag(AchievementsReworked.OHAIDER) && Players.Any(x => x.TeleUser.Id == Program.Para))
                        //    newAch2.Set(AchievementsReworked.OHAIDER);
                        if (!ach2.HasFlag(AchievementsReworked.SpyVsSpy) & !DbGroup.HasFlag(GroupConfig.ShowRolesDeath))
                            newAch2.Set(AchievementsReworked.SpyVsSpy);
                        if (!ach2.HasFlag(AchievementsReworked.NoIdeaWhat) & !DbGroup.HasFlag(GroupConfig.ShowRolesDeath) && Language.Contains("Amnesia"))
                            newAch2.Set(AchievementsReworked.NoIdeaWhat);
                        if (!ach2.HasFlag(AchievementsReworked.Enochlophobia) && Players.Count == 35)
                            newAch2.Set(AchievementsReworked.Enochlophobia);
                        if (!ach2.HasFlag(AchievementsReworked.Introvert) && Players.Count == 5)
                            newAch2.Set(AchievementsReworked.Introvert);
                        if (!ach2.HasFlag(AchievementsReworked.Naughty) && Language.Contains("NSFW"))
                            newAch2.Set(AchievementsReworked.Naughty);
                        if (!ach2.HasFlag(AchievementsReworked.Dedicated) && p.GamePlayers.Count >= 100)
                            newAch2.Set(AchievementsReworked.Dedicated);
                        if (!ach2.HasFlag(AchievementsReworked.Obsessed) && p.GamePlayers.Count >= 1000)
                            newAch2.Set(AchievementsReworked.Obsessed);
                        if (!ach2.HasFlag(AchievementsReworked.Veteran) && p.GamePlayers.Count >= 500)
                            newAch2.Set(AchievementsReworked.Veteran);
                        if (!ach2.HasFlag(AchievementsReworked.Masochist) && player.Won && player.PlayerRole == IRole.Tanner)
                            newAch2.Set(AchievementsReworked.Masochist);
                        if (!ach2.HasFlag(AchievementsReworked.Wobble) && !player.IsDead && player.PlayerRole == IRole.Drunk && Players.Count >= 10)
                            newAch2.Set(AchievementsReworked.Wobble);
                        if (!ach2.HasFlag(AchievementsReworked.Survivalist) && p.GamePlayers.Count(x => x.Survived) >= 100)
                            newAch2.Set(AchievementsReworked.Survivalist);
                        if (!ach2.HasFlag(AchievementsReworked.MasonBrother) && player.PlayerRole == IRole.Mason && !player.IsDead && Players.Count(x => x.PlayerRole == IRole.Mason && !x.IsDead) >= 2)
                            newAch2.Set(AchievementsReworked.MasonBrother);
                        if (!ach2.HasFlag(AchievementsReworked.ChangingSides) && player.ChangedRolesCount > 0 && player.Won)
                            newAch2.Set(AchievementsReworked.ChangingSides);
                        if (!ach2.HasFlag(AchievementsReworked.LoneWolf) && GameMode == GameMode.Chaos && Players.Count >= 10 && WolfRoles.Contains(player.PlayerRole) && Players.GetPlayersForRoles(WolfRoles, false).Count() == 1 && player.Won)
                            newAch2.Set(AchievementsReworked.LoneWolf);
                        if (!ach2.HasFlag(AchievementsReworked.Inconspicuous) && !player.HasBeenVoted & !player.IsDead)
                            newAch2.Set(AchievementsReworked.Inconspicuous);
                        if (!ach2.HasFlag(AchievementsReworked.Promiscuous) && !player.HasStayedHome & !player.HasRepeatedVisit && player.PlayersVisited.Count >= 5)
                            newAch2.Set(AchievementsReworked.Promiscuous);
                        if (!ach2.HasFlag(AchievementsReworked.DoubleShifter) && player.ChangedRolesCount - (player.ConvertedToCult ? 1 : 0) >= 2)
                            newAch2.Set(AchievementsReworked.DoubleShifter);
                        if (!ach2.HasFlag(AchievementsReworked.BrokenClock) && player.FoolCorrectSeeCount >= 2)
                            newAch2.Set(AchievementsReworked.BrokenClock);
                        if (!ach2.HasFlag(AchievementsReworked.SmartGunner) && player.PlayerRole == IRole.Gunner & player.BulletHitBaddies >= 2)
                            newAch2.Set(AchievementsReworked.SmartGunner);
                        if (!ach2.HasFlag(AchievementsReworked.CultCon) && player.PlayerRole == IRole.Cultist && !player.IsDead && convention)
                            newAch2.Set(AchievementsReworked.CultCon);
                        if (!ach2.HasFlag(AchievementsReworked.SerialSamaritan) && player.PlayerRole == IRole.SerialKiller && player.SerialKilledWolvesCount >= 3)
                            newAch2.Set(AchievementsReworked.SerialSamaritan);
                        if (!ach2.HasFlag(AchievementsReworked.CultistTracker) && player.PlayerRole == IRole.CultistHunter && player.CHHuntedCultCount >= 3)
                            newAch2.Set(AchievementsReworked.CultistTracker);
                        if (!ach2.HasFlag(AchievementsReworked.ImNotDrunk) && player.PlayerRole == IRole.ClumsyGuy && player.ClumsyCorrectLynchCount >= 3)
                            newAch2.Set(AchievementsReworked.ImNotDrunk);
                        if (!ach2.HasFlag(AchievementsReworked.WuffieCult) && player.PlayerRole == IRole.AlphaWolf && player.AlphaConvertCount >= 3)
                            newAch2.Set(AchievementsReworked.WuffieCult);
                        if (!ach2.HasFlag(AchievementsReworked.DidYouGuardYourself) && player.PlayerRole == IRole.GuardianAngel && player.GAGuardWolfCount >= 3)
                            newAch2.Set(AchievementsReworked.DidYouGuardYourself);
                        if (!ach2.HasFlag(AchievementsReworked.ThreeLittleWolves) && player.PlayerRole == IRole.Sorcerer && !player.IsDead && Players.GetPlayersForRoles(WolfRoles, true).Count() >= 3)
                            newAch2.Set(AchievementsReworked.ThreeLittleWolves);
                        if (!ach2.HasFlag(AchievementsReworked.President) && player.PlayerRole == IRole.Mayor && player.MayorLynchAfterRevealCount >= 3)
                            newAch2.Set(AchievementsReworked.President);
                        if (!ach2.HasFlag(AchievementsReworked.ItWasABusyNight) && player.BusyNight)
                            newAch2.Set(AchievementsReworked.ItWasABusyNight);
                        if (!ach2.HasFlag(AchievementsReworked.StrongestAlpha) && player.StrongestAlpha)
                            newAch2.Set(AchievementsReworked.StrongestAlpha);
                        if (!ach2.HasFlag(AchievementsReworked.AmIYourSeer) && player.FoolCorrectlySeenBH)
                            newAch2.Set(AchievementsReworked.AmIYourSeer);
                        if (!ach2.HasFlag(AchievementsReworked.Trustworthy) && player.Trustworthy && !player.IsDead && player.Won)
                            newAch2.Set(AchievementsReworked.Trustworthy);
                        if (!ach2.HasFlag(AchievementsReworked.CultLeader) && player.CultLeader && !player.IsDead && player.Won)
                            newAch2.Set(AchievementsReworked.CultLeader);
                        if (!ach2.HasFlag(AchievementsReworked.DeathVillage) && Players.Count(x => x.Won) == 0)
                            newAch2.Set(AchievementsReworked.DeathVillage);
                        if (!ach2.HasFlag(AchievementsReworked.PsychopathKiller) && Players.Count >= 35 && player.PlayerRole == IRole.SerialKiller && player.Won)
                            newAch2.Set(AchievementsReworked.PsychopathKiller);
                        if (!ach2.HasFlag(AchievementsReworked.ColdAsIce) && player.FrozeHarlot)
                            newAch2.Set(AchievementsReworked.ColdAsIce);

                        //now save
                        p.NewAchievements = ach2.Or(newAch2).ToByteArray();
                        db.SaveChanges();

                        //notify
                        var newFlags2 = newAch2.GetUniqueFlags().ToList();
                        if (newAch2.Cast<bool>().All(x => x == false)) continue;
                        var msg2 = "New Unlocks!".ToBold() + Environment.NewLine;
                        msg2 = newFlags2.Aggregate(msg2, (current, a) => current + $"{a.GetName().ToBold()}\n{a.GetDescription()}\n\n");
                        Send(msg2, p.TelegramId);
                    }
                }
            }
        }

        private void UpdateGroupRanking()
        {
            using (var db = new WWContext())
            {
                var refreshdate = db.RefreshDate.FirstOrDefault().Date;
                if (DateTime.Now.Date - refreshdate >= TimeSpan.FromDays(7))
                {
                    refreshdate = DateTime.Now.Date;
                    db.RefreshDate.FirstOrDefault().Date = refreshdate;
                    db.SaveChanges();
                }

                var curRanking = db.GroupRanking.FirstOrDefault(x => x.GroupId == DbGroup.Id && x.Language == Locale.Language);
                if (curRanking == null)
                {
                    curRanking = new GroupRanking { GroupId = DbGroup.Id, Language = Locale.Language, LastRefresh = refreshdate };
                    db.GroupRanking.Add(curRanking);
                    db.SaveChanges();
                }

                var allVarRanking = db.GroupRanking.FirstOrDefault(x => x.GroupId == DbGroup.Id && x.Language == Locale.Base + "BaseAllVariants");
                if (allVarRanking == null && !string.IsNullOrEmpty(Locale.Base)) // Locale.Base shouldn't be empty but better be careful...
                {
                    allVarRanking = new GroupRanking { GroupId = DbGroup.Id, Language = Locale.Base + "BaseAllVariants", LastRefresh = refreshdate };
                    db.GroupRanking.Add(allVarRanking);
                    db.SaveChanges();
                }

                var rankings = db.GroupRanking.Where(x => x.GroupId == DbGroup.Id && x.LastRefresh < refreshdate && x.GamesPlayed != 0); //games played should always be != 0, but you never know..

                foreach (var grpranking in rankings)
                {
                    var daysspan = (refreshdate - grpranking.LastRefresh).Days; //well really this should be 7
                    daysspan = daysspan == 0 ? 1 : daysspan;

                    var avgPlayersPerGame = (decimal)grpranking.PlayersCount / grpranking.GamesPlayed;
                    var avgGameLength = grpranking.MinutesPlayed / grpranking.GamesPlayed;
                    var avgMinutesPerDay = grpranking.MinutesPlayed / daysspan;

                    // PPGFactor: Quadratic. PPGFactor(PPG) = -(2/45)PPG^2 + (16/9)PPG - (70/9)
                    // This means that PPGFactor(5) = 0, PPGFactor(20) = 10 and PPGFactor(35) = 0
                    var PPGFactor = -((decimal)2 / 45) * avgPlayersPerGame * avgPlayersPerGame + ((decimal)16 / 9) * avgPlayersPerGame - ((decimal)70 / 9);

                    // GLFactor: Quadratic. GLFactor(GL) = -(1/90)GL^2 + (2/3)GL
                    // This means that GLFactor(0) = 0, GLFactor(10) = 5.55, GLFactor(30) = 10, GLFactor(50) = 5.55 and GLFactor(60) = 0
                    var GLFactor = -((decimal)1 / 90) * avgGameLength * avgGameLength + ((decimal)2 / 3) * avgGameLength;

                    // PlayerFactor: Sum of PPGFactor and GLFactor. This is between 0 and 20.
                    // Best value is if both PPGFactor and GLFactor are 10, meaning there are 20 average players per game
                    // and the average game length is 30 minutes.
                    var PlayerFactor = PPGFactor + GLFactor;

                    // TimeFactor: Linear. TimeFactor(MinutesPerDay) = MinutesPerDay / 54
                    // This means that TimeFactor(0) = 0, TimeFactor(120) = 2.222, TimeFactor(1080) = 20 and TimeFactor(1440) = 26.666
                    // Games running 24 hours a day is unrealistic, so 1080 minutes = 18 hours get a factor of 20.
                    // Generally: The more time of the day played, the better.
                    var TimeFactor = avgMinutesPerDay / 54;

                    // Malus: If they play really few with a lot players, or really much with few players, give a small malus
                    // The worst malus would be if the difference between PlayerFactor and TimeFactor was 20
                    // That would mean the malus is 20^2 / 20 = 20
                    var malus = (PlayerFactor - TimeFactor) * (PlayerFactor - TimeFactor) / 20;

                    var ranking = Math.Round(PlayerFactor + TimeFactor - malus, 10);

                    grpranking.Ranking = ranking;
                    grpranking.PlayersCount = 0;
                    grpranking.MinutesPlayed = 0;
                    grpranking.GamesPlayed = 0;
                    grpranking.LastRefresh = refreshdate;
                }

                if (_timePlayed.HasValue)
                {
                    curRanking.GamesPlayed++;
                    curRanking.PlayersCount += Players.Count();
                    curRanking.MinutesPlayed += Math.Round((decimal)_timePlayed.Value.TotalMinutes, 10);

                    if (allVarRanking != null)
                    {
                        allVarRanking.GamesPlayed++;
                        allVarRanking.PlayersCount += Players.Count();
                        allVarRanking.MinutesPlayed += Math.Round((decimal)_timePlayed.Value.TotalMinutes, 10);
                    }
                }

                db.SaveChanges();
            }
        }

        /*
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

                    // switching to new achv system, do not notify now
                    // Send($"Achievement Unlocked!\n{a.GetName().ToBold()}\n{a.GetDescription()}", player.Id);

                    // add to new achv system
                    AddAchievement(player, (AchievementsReworked)Enum.Parse(typeof(AchievementsReworked), a.ToString()));
                }
            }
        }*/

        private void AddAchievement(IPlayer player, AchievementsReworked a)
        {
            using (var db = new WWContext())
            {
                var p = GetDBPlayer(player, db);
                if (p != null)
                {
                    var ach = p.NewAchievements == null ? new BitArray(200) : new BitArray(p.NewAchievements);
                    if (ach.HasFlag(a)) return; //no point making another db call if they already have it
                    ach = ach.Set(a);
                    p.NewAchievements = ach.ToByteArray();
                    db.SaveChanges();

                    Send($"Achievement Unlocked!\n{a.GetName().ToBold()}\n{a.GetDescription()}", player.Id);
                }
            }
        }

        /*
        private void SwitchAchievementsSystem(IPlayer player)
        {
            using (var db = new WWContext())
            {
                var p = GetDBPlayer(player, db);
                var ach2 = p.NewAchievements == null ? new BitArray(200) : new BitArray(p.NewAchievements);

                // copy existing achievements to new achievements field
                var oldAchList = ((Achievements)(p.Achievements ?? 0)).GetUniqueFlags();
                foreach (var achv in oldAchList)
                {
                    var newAchv = (AchievementsReworked)Enum.Parse(typeof(AchievementsReworked), achv.ToString());
                    ach2.Set(newAchv);
                }
                p.NewAchievements = ach2.ToByteArray();
                player.NewAchievements = ach2;
                db.SaveChanges();
            }
        }*/

        #endregion
    }
}
