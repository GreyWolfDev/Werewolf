using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class GameBalancing
    {
        static void Shuffle<T>(IList<T> list)
        {
            using (RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider())
            {
                int n = list.Count;
                while (n > 1)
                {
                    byte[] box = new byte[1];
                    do provider.GetBytes(box);
                    while (!(box[0] < n * (byte.MaxValue / n)));
                    int k = (box[0] % n);
                    n--;
                    T value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
            }
        }

        public static readonly Random R = new Random();
        public static readonly IRole[] WolfRoles = { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Lycan };

        public static List<IRole> Balance(IRole roleflags, int playerCount, bool chaos, bool burningOverkill, out List<IRole> possibleRoles, bool validationMode = false)
        {
            List<IRole> rolesToAssign;
            List<IRole> disabledRoles = roleflags.HasFlag(IRole.VALID) || validationMode
                ? roleflags.GetUniqueRoles().ToList()
                : new List<IRole>();

            var balanced = false;
            var attempts = 0;
            var nonVgRoles = new[] { IRole.Cultist, IRole.SerialKiller, IRole.Tanner, IRole.Wolf, IRole.AlphaWolf, IRole.Sorcerer, IRole.WolfCub, IRole.Lycan, IRole.Thief, IRole.SnowWolf, IRole.Arsonist };
            var revealedVgRoles = new[] { IRole.Blacksmith, IRole.Mayor, IRole.Pacifist, IRole.Gunner, IRole.Sandman, IRole.Troublemaker };
            

            do
            {
                attempts++;
                if (attempts >= 500)
                {
                    throw new IndexOutOfRangeException("Unable to create a balanced game.  Please try again.\nPlayer count: " + playerCount);
                }


                //determine which roles should be assigned
                rolesToAssign = GetRoleList(playerCount, disabledRoles);
                while (rolesToAssign.Count < playerCount) rolesToAssign.Add(IRole.Villager);

                possibleRoles = new List<IRole>(rolesToAssign);
                Shuffle(rolesToAssign);
                rolesToAssign = rolesToAssign.Take(playerCount).ToList();

                //let's fix some roles that should or shouldn't be there...

                //sorcerer or traitor or snowwolf, without wolves, are pointless. change one of them to wolf
                if ((rolesToAssign.Contains(IRole.Sorcerer) || rolesToAssign.Contains(IRole.Traitor) || rolesToAssign.Contains(IRole.SnowWolf)) &&
                    !rolesToAssign.Any(x => WolfRoles.Contains(x)))
                {
                    var towolf = rolesToAssign.FindIndex(x => x == IRole.Sorcerer || x == IRole.Traitor || x == IRole.SnowWolf); //if there are multiple, the random order of rolesToAssign will choose for us which one to substitute
                    var possibleWolves = WolfRoles.Where(x => !disabledRoles.Contains(x)).ToList();
                    rolesToAssign[towolf] = possibleWolves[R.Next(possibleWolves.Count)]; //choose randomly from WolfRoles
                }

                //cult without CH -> add CH (unless the group REALLY doesn't want it...)
                if (rolesToAssign.Contains(IRole.Cultist) && !rolesToAssign.Contains(IRole.CultistHunter)
                    && !disabledRoles.Contains(IRole.CultistHunter))
                {
                    //just pick a vg, and turn them to CH
                    var vg = rolesToAssign.FindIndex(x => !nonVgRoles.Contains(x));
                    rolesToAssign[vg] = IRole.CultistHunter;
                }

                //appseer without seer -> seer
                if (rolesToAssign.Contains(IRole.ApprenticeSeer) && !rolesToAssign.Contains(IRole.Seer))
                {
                    //substitute with seer
                    var apps = rolesToAssign.IndexOf(IRole.ApprenticeSeer);
                    rolesToAssign[apps] = IRole.Seer;
                }

                //make sure that we have at least two teams
                if (
                    rolesToAssign.Any(x => !nonVgRoles.Contains(x)) //make sure we have VGs
                    && rolesToAssign.Any(x => nonVgRoles.Contains(x) && x != IRole.Sorcerer && x != IRole.Tanner && x != IRole.Thief) //make sure we have at least one enemy
                )
                    balanced = true;
                //else, redo role assignment. better to rely on randomness, than trying to fix it

                //also make sure that baddie count is lower than village count
                if (rolesToAssign.Count(x => nonVgRoles.Contains(x)) >= rolesToAssign.Count(x => !nonVgRoles.Contains(x))) balanced = false;

                if (rolesToAssign.Contains(IRole.SerialKiller) && rolesToAssign.Contains(IRole.Arsonist))
                    balanced = balanced && burningOverkill;

                //the roles to assign are good, now if it's not a chaos game we need to check if they're balanced
                if (!chaos)
                {
                    var villageStrength =
                        rolesToAssign.Where(x => !nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));
                    var enemyStrength =
                        rolesToAssign.Where(x => nonVgRoles.Contains(x)).Sum(x => x.GetStrength(rolesToAssign));

                    //check balance
                    var varianceAllowed = (playerCount / 4) + 1;
                    balanced = balanced && (Math.Abs(villageStrength - enemyStrength) <= varianceAllowed);


                    // unbalanced if there's more than one revealed village role per 3 players
                    var revealedVgRoleCount = rolesToAssign.Count(x => revealedVgRoles.Contains(x));
                    if (revealedVgRoleCount * 3 > rolesToAssign.Count)
                        balanced = false;

                    // unbalanced if there's more than one role that can cause 2 lynches in between a baddie act per 4 players
                    var onlyWolfBaddies = !rolesToAssign.Any(x => new[] { IRole.Arsonist, IRole.SerialKiller, IRole.Cultist }.Contains(x));
                    var killStoppingRoleCount = rolesToAssign.Count(x => x == IRole.Troublemaker || x == IRole.Sandman || (onlyWolfBaddies && x == IRole.Blacksmith));
                    if (killStoppingRoleCount * 4 > rolesToAssign.Count)
                        balanced = false;
                }
            } while (!balanced);

            return rolesToAssign;
        }

        public static bool TryBalance(IRole disabledRoles, int maxPlayers)
        {
            for (int playerCount = 5; playerCount <= maxPlayers; playerCount++)
            {
                try
                {
                    Balance(disabledRoles, playerCount, false, true, out _, true);
                }
                catch (IndexOutOfRangeException)
                {
                    return false;
                }
            }

            return true;
        }

        public static List<IRole> GetRoleList(int playerCount, List<IRole> disabledRoles)
        {
            var rolesToAssign = new List<IRole>();
            //need to set the max wolves so game doesn't end immediately - 25% max wolf population
            //25% was too much, max it at 5 wolves.
            var possiblewolves = new List<IRole>()
            { IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.Lycan, IRole.SnowWolf }
                .Where(x => !disabledRoles.Contains(x)).ToList();

            var wolftoadd = possiblewolves[R.Next(possiblewolves.Count())];
            for (int i = 0; i < Math.Min(Math.Max(playerCount / 5, 1), 5); i++)
            {
                rolesToAssign.Add(wolftoadd);
                if (wolftoadd != IRole.Wolf)
                    possiblewolves.Remove(wolftoadd);
                wolftoadd = possiblewolves[R.Next(possiblewolves.Count())];
            }

            if (rolesToAssign.Count == 1 && rolesToAssign[0] == IRole.SnowWolf) // avoid lone snow wolves
                rolesToAssign[0] = IRole.Wolf;

            //add remaining roles to 'card pile'
            foreach (var role in RoleConfigHelper.GetRoles())
            {
                switch (role)
                {
                    case IRole.Wolf:
                    case IRole.Lycan:
                    case IRole.WolfCub:
                    case IRole.AlphaWolf:
                    case IRole.SnowWolf:
                        break;
                    case IRole.CultistHunter:
                    case IRole.Cultist:
                        if (playerCount > 10)
                            rolesToAssign.Add(role);
                        break;
                    case IRole.Spumpkin:
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

            if (rolesToAssign.Any(x => x == IRole.CultistHunter))
            {
                rolesToAssign.Add(IRole.Cultist);
                rolesToAssign.Add(IRole.Cultist);
            }
            //now fill rest of the slots with villagers (for large games)
            for (int i = 0; i < playerCount / 4; i++)
                rolesToAssign.Add(IRole.Villager);
            rolesToAssign = rolesToAssign.Where(x => !disabledRoles.Contains(x)).ToList();
            return rolesToAssign;
        }

        public static int GetStrength(this IRole role, List<IRole> allRoles)
        {
            IRole[] WolfRoles = { IRole.Wolf, IRole.WolfCub, IRole.AlphaWolf, IRole.Lycan };
            IRole[] nonConvertibleRoles = { IRole.Seer, IRole.GuardianAngel, IRole.Detective, IRole.Cursed, IRole.Harlot, IRole.Hunter, IRole.Doppelgänger, IRole.Wolf, IRole.AlphaWolf, IRole.WolfCub, IRole.SerialKiller, IRole.Lycan, IRole.Thief, IRole.SnowWolf };
            switch (role)
            {
                case IRole.Villager:
                    return 1;
                case IRole.Drunk:
                    return 3;
                case IRole.Harlot:
                    return 6;
                case IRole.Seer:
                    return 7 - allRoles.Count(x => x == IRole.Lycan) - (allRoles.Count(x => x == IRole.WolfMan) * 2);
                case IRole.Traitor:
                    return 0;
                case IRole.GuardianAngel:
                    return 7 + (allRoles.Contains(IRole.Arsonist) ? 1 : 0);
                case IRole.Detective:
                    return 6;
                case IRole.Wolf:
                    return 10;
                case IRole.Cursed:
                    return 1 - allRoles.Count(x => WolfRoles.Contains(x) || x == IRole.SnowWolf) / 2; //vg, or worse
                case IRole.Gunner:
                    return 6;
                case IRole.Tanner:
                    return allRoles.Count / 2;
                case IRole.Fool:
                    return 3;
                case IRole.WildChild:
                    return 1;
                case IRole.Beholder:
                    return 1 + (allRoles.Any(x => x == IRole.Seer) ? 4 : 0) + (allRoles.Any(x => x == IRole.Fool) ? 1 : 0); //only good if seer is present!
                case IRole.ApprenticeSeer:
                    return 6;
                case IRole.Cultist:
                    return 10 + allRoles.Count(x => !nonConvertibleRoles.Contains(x));
                case IRole.CultistHunter:
                    return allRoles.Count(x => x == IRole.Cultist) == 0 ? 1 : 7;
                case IRole.Mason:
                    return allRoles.Count(x => x == IRole.Mason) <= 1 ? 1 : allRoles.Count(x => x == IRole.Mason) + 3; //strength in numbers
                case IRole.Doppelgänger:
                    return 2;
                case IRole.Cupid:
                    return 2;
                case IRole.Hunter:
                    return 6;
                case IRole.SerialKiller:
                    return 15;
                case IRole.Sorcerer:
                    return 2;
                case IRole.AlphaWolf:
                    return 12;
                case IRole.WolfCub:
                    return new[] { IRole.AlphaWolf, IRole.Wolf, IRole.Lycan, IRole.SnowWolf, IRole.WildChild,
                        IRole.Doppelgänger, IRole.Cursed, IRole.Traitor }
                        .Any(x => allRoles.Contains(x)) ? 12 : 10; // only count as 12 if there can be another wolf
                case IRole.Blacksmith:
                    return 5;
                case IRole.ClumsyGuy:
                    return -1;
                case IRole.Mayor:
                    return 4;
                case IRole.Prince:
                    return 3;
                case IRole.WolfMan:
                    return 1;
                case IRole.Augur:
                    return 5;
                case IRole.Pacifist:
                    return 3;
                case IRole.WiseElder:
                    return 3;
                case IRole.Oracle:
                    return 4;
                case IRole.Sandman:
                    return 3;
                case IRole.Lycan:
                    return 10;
                case IRole.Thief:
                    return 0; // Testing 0 instead of 4 since thief
                              // doesn't really seem to "harm" the village team a lot
                case IRole.Troublemaker:
                    return 5;
                case IRole.Chemist:
                    return 0;
                case IRole.SnowWolf:
                    return 15;
                case IRole.GraveDigger:
                    return 5;
                case IRole.Arsonist:
                    return 8;
                case IRole.Spumpkin:
                    return 2;
                case IRole.ScapeGoat:
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
    }
}
