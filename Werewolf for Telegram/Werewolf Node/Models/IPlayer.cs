using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Telegram.Bot.Types;
using Shared;

namespace Werewolf_Node.Models
{
    public class IPlayer
    {
        /// <summary>
        /// Reference to the telegram user
        /// </summary>
        public User TeleUser { get; set; }
        /// <summary>
        /// The players role
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public IRole PlayerRole { get; set; } = IRole.Villager;

        /// <summary>
        /// Whether or not the player has used their ability this round
        /// </summary>
        public bool HasUsedAbility { get; set; } = false;

        /// <summary>
        /// Choice of the player they want to use their ability on
        /// </summary>
        public long Choice { get; set; } = 0;

        public long Choice2 { get; set; } = 0;

        /// <summary>
        /// Whooops! you died...
        /// </summary>
        public bool IsDead { get; set; } = false;

        /// <summary>
        /// If this reaches 2, they are automatically executed
        /// </summary>
        public int NonVote { get; set; } = 0;

        /// <summary>
        /// Indicates this person died overnight
        /// </summary>
        public bool DiedLastNight { get; set; }

        /// <summary>
        /// How many votes against them they have (lynching)
        /// </summary>
        public int Votes { get; set; } = 0;

        /// <summary>
        /// Who lynched who? (For secret lynching)
        /// </summary>
        public Dictionary<IPlayer, int> VotedBy = new Dictionary<IPlayer, int>();

        /// <summary>
        /// For the gunner only
        /// </summary>
        public int Bullet { get; set; } = 2;

        /// <summary>
        /// Werewolf gets drunk after killing the drunk, so sits out one turn
        /// </summary>
        public bool Drunk { get; set; } = false;

        /// <summary>
        /// Indicates whether user has PM'd the bot.  this is required by telegram.
        /// </summary>
        public bool HasPM { get; set; } = false;

        public bool Fled { get; set; } = false;
        [JsonConverter(typeof(StringEnumConverter))]
        public ITeam Team { get; set; } = ITeam.Village;
        //public bool HasNightAction { get; set; } = false;
        //public bool HasDayAction { get; set; } = false;
        public int DayCult { get; set; } = 0;
        public long RoleModel { get; set; } = 0;
        [JsonConverter(typeof(StringEnumConverter))]
        public IRole KilledByRole { get; set; }
        public bool DiedByVisitingKiller { get; set; } = false;
        public bool DiedByVisitingVictim { get; set; } = false;
        public bool DiedByFleeOrIdle { get; set; } = false;
        public bool WasSavedLastNight { get; set; } = false;
        public bool ChemistFailed { get; set; } = false;
        public bool Frozen { get; set; } = false;
        public int DugGravesLastNight { get; set; } = 0;
        public int StumbledGrave { get; set; } = 0;
        public int MessageId { get; set; }
        public string Name { get; set; }
        public bool InLove { get; set; } = false;
        public long LoverId { get; set; } = 0;
        public bool Doused { get; set; } = false;
        public bool Burning { get; set; } = false;
        public int DBPlayerId { get; set; } = 0;
        public int DBGamePlayerId { get; set; } = 0;
        public DateTime TimeDied { get; set; } = DateTime.MaxValue;
        /// <summary>
        /// Currently only used for augur
        /// </summary>
        public List<IRole> SawRoles { get; set; } = new List<IRole>();

        public string Language { get; set; } = "English";
        public bool Won { get; set; } = false;

        public long Id;
        [JsonConverter(typeof(StringEnumConverter))]
        public QuestionAsked CurrentQuestion { get; set; }


        #region Achievement Related Stuff
        public BitArray NewAchievements { get; set; }

        public bool HasBeenVoted { get; set; } = false;
        public bool HasStayedHome { get; set; } = false;
        public bool HasRepeatedVisit { get; set; } = false;
        public HashSet<long> PlayersVisited { get; set; } = new HashSet<long>();
        public int ChangedRolesCount { get; set; } = 0;
        public int FirstToLynchCount { get; set; } = 0;
        public int BulletHitBaddies { get; set; } = 0;
        public int FoundBadRolesRowCount { get; set; } = 0;
        public int FoolCorrectSeeCount { get; set; } = 0;
        public int SerialKilledWolvesCount { get; set; } = 0;
        public bool PackHunter { get; set; } = false;
        public bool LastShotWasSKWolf { get; set; } = false;
        public bool DoubleKillEnding { get; set; } = false;
        public bool Bitten { get; set; }
        public List<long> CorrectSnooped { get; set; } = new List<long>();
        public bool SpeedDating { get; set; } = false;
        public int FirstStone { get; set; } = 0;
        public int CHHuntedCultCount { get; set; } = 0;
        public int ClumsyCorrectLynchCount { get; set; } = 0;
        public int AlphaConvertCount { get; set; } = 0;
        public int GACleanedDousedCount { get; set; } = 0;
        public int GAGuardWolfCount { get; set; } = 0;
        public int MayorLynchAfterRevealCount { get; set; } = 0;
        public int BeingVisitedSameNightCount { get; set; } = 0;
        public int ChemistVisitSurviveCount { get; set; } = 0;
        public bool BusyNight { get; set; } = false;
        public bool StrongestAlpha { get; set; } = false;
        public bool FoolCorrectlySeenBH { get; set; } = false;
        public bool Trustworthy { get; set; } = false;
        public bool CultLeader { get; set; } = false;
        public bool ConvertedToCult { get; set; } = false;
        public bool FrozeHarlot { get; set; } = false;

        public int DonationLevel { get; set; } = 0;
        public bool Founder { get; set; } = false;
        public CustomGifData GifPack { get; set; } = null;
        public string LoverMsg { get; set; } = null;
        public KillMthd? FinalShotDelay { get; set; } = null;

        #endregion

    }

    public enum ITeam
    {
        Village, Cult, Wolf, Tanner,
        Neutral, SerialKiller, Lovers, Arsonist,
        SKHunter,
        NoOne, Thief
    }

    public enum KillMthd
    {
        None, Lynch, Eat, Shoot, VisitWolf, VisitVictim, GuardWolf, Detected, Flee, Hunt, HunterShot, LoverDied, SerialKilled, HunterCult, GuardKiller, VisitKiller, Idle, Suicide, StealKiller, Chemistry, FallGrave,
        Spotted, Burn, VisitBurning
    }
}
