using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;

namespace Werewolf_Control.Helpers
{
    public static class GameBalancing
    {
        public static readonly Random R = new Random();
        public static readonly DisabledRole[] WolfRoles = { DisabledRole.Wolf, DisabledRole.AlphaWolf, DisabledRole.WolfCub, DisabledRole.Lycan };

        public static bool TryBalance(DisabledRole disabledRoles)
        {
            List<DisabledRole> rolesToAssign;
            for (int count = 5; count <= 35; count++)
            {
                var balanced = false;
                var attempts = 0;
                var nonVgRoles = new[] { DisabledRole.Cultist, DisabledRole.SerialKiller, DisabledRole.Tanner, DisabledRole.Wolf, DisabledRole.AlphaWolf, DisabledRole.Sorcerer, DisabledRole.WolfCub, DisabledRole.Lycan, DisabledRole.Thief, DisabledRole.SnowWolf, DisabledRole.Arsonist };

                do
                {
                    attempts++;
                    if (attempts >= 500)
                    {
                        return false;
                    }

                    //determine which roles should be assigned
                    rolesToAssign = GetRoleList(count, disabledRoles);
                    rolesToAssign.Shuffle();
                    rolesToAssign = rolesToAssign.Take(count).ToList();

                    //let's fix some roles that should or shouldn't be there...

                    //sorcerer or traitor or snowwolf, without wolves, are pointless. change one of them to wolf
                    if ((rolesToAssign.Contains(DisabledRole.Sorcerer) || rolesToAssign.Contains(DisabledRole.Traitor) || rolesToAssign.Contains(DisabledRole.SnowWolf)) &&
                        !rolesToAssign.Any(x => WolfRoles.Contains(x)))
                    {
                        var towolf = rolesToAssign.FindIndex(x => x == DisabledRole.Sorcerer || x == DisabledRole.Traitor || x == DisabledRole.SnowWolf); //if there are multiple, the random order of rolesToAssign will choose for us which one to substitute
                        rolesToAssign[towolf] = WolfRoles[R.Next(WolfRoles.Count())]; //choose randomly from WolfRoles
                    }

                    //cult without CH -> add CH
                    if (rolesToAssign.Contains(DisabledRole.Cultist) && !rolesToAssign.Contains(DisabledRole.CultistHunter))
                    {
                        //just pick a vg, and turn them to CH
                        var vg = rolesToAssign.FindIndex(x => !nonVgRoles.Contains(x));
                        rolesToAssign[vg] = DisabledRole.CultistHunter;
                    }

                    //appseer without seer -> seer
                    if (rolesToAssign.Contains(DisabledRole.ApprenticeSeer) && !rolesToAssign.Contains(DisabledRole.Seer))
                    {
                        //substitute with seer
                        var apps = rolesToAssign.IndexOf(DisabledRole.ApprenticeSeer);
                        rolesToAssign[apps] = DisabledRole.Seer;
                    }

                    //make sure that we have at least two teams
                    if (
                        rolesToAssign.Any(x => !nonVgRoles.Contains(x)) //make sure we have VGs
                        && rolesToAssign.Any(x => nonVgRoles.Contains(x) && x != DisabledRole.Sorcerer && x != DisabledRole.Tanner && x != DisabledRole.Thief) //make sure we have at least one enemy
                    )
                        balanced = true;
                    //else, redo role assignment. better to rely on randomness, than trying to fix it

                    //also make sure that baddie count is lower than village count
                    if (rolesToAssign.Count(x => nonVgRoles.Contains(x)) >= rolesToAssign.Count(x => !nonVgRoles.Contains(x))) balanced = false;

                    if (rolesToAssign.Contains(DisabledRole.SerialKiller) && rolesToAssign.Contains(DisabledRole.Arsonist))
                        balanced = false; // it needs to be able to create a game even if burningOverkill is off

                    //the roles to assign are good, now if it's not a chaos game we need to check if they're balanced
                    var villageStrength =
                        rolesToAssign.Where(x => !nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));
                    var enemyStrength =
                        rolesToAssign.Where(x => nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));

                    //check balance
                    var varianceAllowed = (count / 4) + 1;
                    balanced = balanced && (Math.Abs(villageStrength - enemyStrength) <= varianceAllowed);
                } while (!balanced);
            }

            return true;
        }

        public static List<DisabledRole> GetRoleList(int playerCount, DisabledRole disabledRoles)
        {
            var rolesToAssign = new List<DisabledRole>();
            //need to set the max wolves so game doesn't end immediately - 25% max wolf population
            //25% was too much, max it at 5 wolves.
            var possiblewolves = new List<DisabledRole>()
            { DisabledRole.Wolf, DisabledRole.AlphaWolf, DisabledRole.WolfCub, DisabledRole.Lycan }
                .Where(x => !disabledRoles.HasFlag(x)).ToList();

            var wolftoadd = possiblewolves[R.Next(possiblewolves.Count())];
            if (!disabledRoles.HasFlag(DisabledRole.SnowWolf)) possiblewolves.Add(DisabledRole.SnowWolf); // add snow wolf only after one other wolf has been chosen already
            for (int i = 0; i < Math.Min(Math.Max(playerCount / 5, 1), 5); i++)
            {
                rolesToAssign.Add(wolftoadd);
                if (wolftoadd != DisabledRole.Wolf)
                    possiblewolves.Remove(wolftoadd);
                wolftoadd = possiblewolves[R.Next(possiblewolves.Count())];
            }
            //add remaining roles to 'card pile'
            foreach (var role in RoleConfigHelper.GetRoles())
            {
                switch (role)
                {
                    case DisabledRole.Wolf:
                    case DisabledRole.Lycan:
                    case DisabledRole.WolfCub:
                    case DisabledRole.AlphaWolf:
                    case DisabledRole.SnowWolf:
                        break;
                    case DisabledRole.CultistHunter:
                    case DisabledRole.Cultist:
                        if (playerCount > 10)
                            rolesToAssign.Add(role);
                        break;
                    default:
                        rolesToAssign.Add(role);
                        break;
                }
            }

            //add a couple more masons
            rolesToAssign.Add(DisabledRole.Mason);
            rolesToAssign.Add(DisabledRole.Mason);
            //for smaller games, all roles will be available and chosen randomly.  For large games, it will be about the
            //same as it was before....


            if (rolesToAssign.Any(x => x == DisabledRole.CultistHunter))
            {
                rolesToAssign.Add(DisabledRole.Cultist);
                rolesToAssign.Add(DisabledRole.Cultist);
            }
            //now fill rest of the slots with villagers (for large games)
            for (int i = 0; i < playerCount / 4; i++)
                rolesToAssign.Add(DisabledRole.Villager);
            rolesToAssign = rolesToAssign.Where(x => !disabledRoles.HasFlag(x)).ToList();
            return rolesToAssign;
        }

        public static int GetStrength(this DisabledRole role, List<DisabledRole> allRoles)
        {
            DisabledRole[] WolfRoles = { DisabledRole.Wolf, DisabledRole.WolfCub, DisabledRole.AlphaWolf, DisabledRole.Lycan };
            DisabledRole[] nonConvertibleRoles = { DisabledRole.Seer, DisabledRole.GuardianAngel, DisabledRole.Detective, DisabledRole.Cursed, DisabledRole.Harlot, DisabledRole.Hunter, DisabledRole.Doppelgänger, DisabledRole.Wolf, DisabledRole.AlphaWolf, DisabledRole.WolfCub, DisabledRole.SerialKiller, DisabledRole.Lycan, DisabledRole.Thief, DisabledRole.SnowWolf };
            switch (role)
            {
                case DisabledRole.Villager:
                    return 1;
                case DisabledRole.Drunk:
                    return 3;
                case DisabledRole.Harlot:
                    return 6;
                case DisabledRole.Seer:
                    return 7 - allRoles.Count(x => x == DisabledRole.Lycan) - (allRoles.Count(x => x == DisabledRole.WolfMan) * 2);
                case DisabledRole.Traitor:
                    return 0;
                case DisabledRole.GuardianAngel:
                    return 7 + (allRoles.Contains(DisabledRole.Arsonist) ? 1 : 0);
                case DisabledRole.Detective:
                    return 6;
                case DisabledRole.Wolf:
                    return 10;
                case DisabledRole.Cursed:
                    return 1 - allRoles.Count(x => WolfRoles.Contains(x) || x == DisabledRole.SnowWolf) / 2; //vg, or worse
                case DisabledRole.Gunner:
                    return 6;
                case DisabledRole.Tanner:
                    return allRoles.Count / 2;
                case DisabledRole.Fool:
                    return 3;
                case DisabledRole.WildChild:
                    return 1;
                case DisabledRole.Beholder:
                    return 1 + (allRoles.Any(x => x == DisabledRole.Seer) ? 4 : 0) + (allRoles.Any(x => x == DisabledRole.Fool) ? 1 : 0); //only good if seer is present!
                case DisabledRole.ApprenticeSeer:
                    return 6;
                case DisabledRole.Cultist:
                    return 10 + allRoles.Count(x => !nonConvertibleRoles.Contains(x));
                case DisabledRole.CultistHunter:
                    return allRoles.Count(x => x == DisabledRole.Cultist) == 0 ? 1 : 7;
                case DisabledRole.Mason:
                    return allRoles.Count(x => x == DisabledRole.Mason) <= 1 ? 1 : allRoles.Count(x => x == DisabledRole.Mason) + 3; //strength in numbers
                case DisabledRole.Doppelgänger:
                    return 2;
                case DisabledRole.Cupid:
                    return 2;
                case DisabledRole.Hunter:
                    return 6;
                case DisabledRole.SerialKiller:
                    return 15;
                case DisabledRole.Sorcerer:
                    return 2;
                case DisabledRole.AlphaWolf:
                    return 12;
                case DisabledRole.WolfCub:
                    return new[] { DisabledRole.AlphaWolf, DisabledRole.Wolf, DisabledRole.Lycan, DisabledRole.SnowWolf, DisabledRole.WildChild,
                        DisabledRole.Doppelgänger, DisabledRole.Cursed, DisabledRole.Traitor }
                        .Any(x => allRoles.Contains(x)) ? 12 : 10; // only count as 12 if there can be another wolf
                case DisabledRole.Blacksmith:
                    return 5;
                case DisabledRole.ClumsyGuy:
                    return -1;
                case DisabledRole.Mayor:
                    return 4;
                case DisabledRole.Prince:
                    return 3;
                case DisabledRole.WolfMan:
                    return 1;
                case DisabledRole.Augur:
                    return 5;
                case DisabledRole.Pacifist:
                    return 3;
                case DisabledRole.WiseElder:
                    return 3;
                case DisabledRole.Oracle:
                    return 4;
                case DisabledRole.Sandman:
                    return 3;
                case DisabledRole.Lycan:
                    return 10;
                case DisabledRole.Thief:
                    return 4;
                case DisabledRole.Troublemaker:
                    return 5;
                case DisabledRole.Chemist:
                    return 0;
                case DisabledRole.SnowWolf:
                    return 15;
                case DisabledRole.GraveDigger:
                    return 8;
                case DisabledRole.Arsonist:
                    return 8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }

        }
    }
}
