using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Telegram.Bot.Types;

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
        public int Choice { get; set; } = 0;

        public int Choice2 { get; set; } = 0;

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
        public bool HasNightAction { get; set; } = false;
        public bool HasDayAction { get; set; } = false;
        public int DayCult { get; set; } = 0;
        public int RoleModel { get; set; } = 0;
        [JsonConverter(typeof(StringEnumConverter))]
        public IRole KilledByRole { get; set; }
        public bool DiedByVisitingKiller { get; set; } = false;
        public bool DiedByVisitingVictim { get; set; } = false;
        public bool WasSavedLastNight { get; set; } = false;
        public int MessageId { get; set; }
        public string Name { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public IRole OriginalRole { get; set; }
        public bool InLove { get; set; } = false;
        public int LoverId { get; set; } = 0;
        public int DBPlayerId { get; set; } = 0;
        public int DBGamePlayerId { get; set; } = 0;
        public DateTime TimeDied { get; set; } = DateTime.MaxValue;

        public string Language { get; set; } = "English";
        public bool Won { get; set; } = false;

        public int Id;
        [JsonConverter(typeof(StringEnumConverter))]
        public QuestionAsked CurrentQuestion { get; set; }


        #region Achievement Related Stuff

        public bool HasBeenVoted { get; set; } = false;
        public bool HasStayedHome { get; set; } = false;
        public bool HasRepeatedVisit { get; set; } = false;
        public HashSet<int> PlayersVisited { get; set; } = new HashSet<int>();
        public int ChangedRolesCount { get; set; } = 0;
        public int FirstToLynchCount { get; set; } = 0;
        public bool BulletHitVillager { get; set; } = false;
        public int FoundBadRolesRowCount { get; set; } = 0;
        public int FoolCorrectSeeCount { get; set; } = 0;
        public int SerialKilledWolvesCount { get; set; } = 0;
        public bool PackHunter { get; set; } = false;
        public bool LastShotWasSKWolf { get; set; } = false;
        public bool DoubleKillEnding { get; set; } = false;
        public bool Bitten { get; set; }
        public List<int> CorrectSnooped { get; set; } = new List<int>();
        public bool SpeedDating { get; set; } = false;
        public int FirstStone { get; set; } = 0;
        public int CHHuntedCultCount { get; set; } = 0;
        public int ClumsyCorrectLynchCount { get; set; } = 0;
        public int AlphaConvertCount { get; set; } = 0;
        public int GAGuardWolfCount { get; set; } = 0;
        public int MayorLynchAfterRevealCount { get; set; } = 0;
        public int BeingVisitedSameNightCount { get; set; } = 0;
        public bool BusyNight { get; set; } = false;
        public int DonationLevel { get; set; } = 0;
        public bool Founder { get; set; } = false;
        public CustomGifData GifPack { get; set; } = null;

        #endregion

    }


    public enum IRole
    {
        Villager, Drunk, Harlot, Seer, Traitor, GuardianAngel, Detective, Wolf, Cursed, Gunner, Tanner, Fool, WildChild, Beholder, ApprenticeSeer, Cultist, CultistHunter, Mason, Doppelgänger, Cupid, Hunter, SerialKiller,
        //new roles
        Sorcerer, AlphaWolf, WolfCub, Blacksmith, ClumsyGuy, Mayor, Prince,
        //more new roles, from april fools.....
        Lycan, Pacifist, WiseElder, Oracle, Sandman, WolfMan, Thief
    }

    public enum ITeam
    {
        Village, Cult, Wolf, Tanner,
        Neutral, SerialKiller, Lovers,
        SKHunter,
        NoOne, Thief
    }

    public enum KillMthd
    {
        None, Lynch, Eat, Shoot, VisitWolf, VisitVictim, GuardWolf, Detected, Flee, Hunt, HunterShot, LoverDied, SerialKilled, HunterCult, GuardKiller, VisitKiller, Idle, Suicide
    }
}
