using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Werewolf_Website.Models
{
    public class IPlayer
    {
        /// <summary>
        /// The players role
        /// </summary>
        public IRole PlayerRole { get; set; } = IRole.Villager;

        /// <summary>
        /// Choice of the player they want to use their ability on
        /// </summary>
        public int Choice { get; set; } = 0;

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
        /// For the gunner only
        /// </summary>
        public int Bullet { get; set; } = 2;

        /// <summary>
        /// Werewolf gets drunk after killing the drunk, so sits out one turn
        /// </summary>
        public bool Drunk { get; set; } = false;

        public bool Fled { get; set; } = false;
        public ITeam Team { get; set; } = ITeam.Village;
        public int DayCult { get; set; } = 0;
        public int RoleModel { get; set; } = 0;
        public bool DiedFromWolf { get; set; } = false;
        public bool DiedFromKiller { get; set; } = false;
        public bool DiedFromHunter { get; set; } = false;
        public bool DiedFromLove { get; set; } = false;
        public string Name { get; set; }
        public IRole OriginalRole { get; set; }
        public bool InLove { get; set; } = false;
        public int LoverId { get; set; } = 0;
        public int DBPlayerId { get; set; } = 0;
        public int DBGamePlayerId { get; set; } = 0;
        public DateTime TimeDied { get; set; } = DateTime.MaxValue;
        public int Id { get; set; }
    }



    public enum IRole
    {
        Villager, Drunk, Harlot, Seer, Traitor, GuardianAngel, Detective, Wolf, Cursed, Gunner, Tanner, Fool, WildChild, Beholder, ApprenticeSeer, Cultist, CultistHunter, Mason, Doppelgänger, Cupid, Hunter, SerialKiller
    }

    public enum ITeam
    {
        Village, Cult, Wolf, Tanner,
        Neutral, SerialKiller, Lovers,
        SKHunter,
        NoOne
    }

    public enum KillMthd
    {
        None, Lynch, Eat, Shoot, VisitWolf, VisitVictim, GuardWolf, Detected, Flee, Hunt, HunterShot, LoverDied, SerialKilled, HunterCult, GuardKiller, VisitKiller, Idle
    }

}
