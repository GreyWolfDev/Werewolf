using Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Helpers
{
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class UnbalancedRole
    {
        public IEnumerable<string> RoleCombination { get; set; }
        public string Role { get; set; }
        public double ExpectedPercentage { get; set; }
        public double ActualPercentage { get; set; }
    }

    public class Balancing
    {
        public static void ReadBalance(ICollection<UnbalancedRole> output)
        {
            List<IEnumerable<string>> roleCombinations = new List<IEnumerable<string>>();
            using (var db = new WWContext())
            {
                //collect all role combinations from past games
                foreach (var game in db.Games)
                {
                    var combination = game.GamePlayers.Select(x => x.Role);
                    if (!roleCombinations.Any(x => IsSame(x, combination)))
                    {
                        roleCombinations.Add(combination);
                    }
                }

                foreach (var combination in roleCombinations)
                {
                    //check for win count of each role and count of games with that combination
                    long games = 0;
                    Dictionary<string, long> wins = new Dictionary<string, long>();
                    //fill every role of the combination into the dictionary
                    foreach (var role in combination)
                    {
                        if (!wins.ContainsKey(role)) wins.Add(role, 0);
                    }
                    foreach (var game in db.Games.Where(x => IsSame(combination, x.GamePlayers.Select(y => y.Role))))
                    {
                        games++;
                        foreach (var player in game.GamePlayers.Where(x => x.Won))
                            wins[player.Role] = wins[player.Role] + 1;
                    }

                    //calculate win percentage, should be about the same for every role in a fair game
                    Dictionary<string, double> percentages = new Dictionary<string, double>();
                    foreach (var kvp in wins)
                    {
                        var perc = (double)kvp.Value / games;
                        percentages[kvp.Key] = perc;
                    }

                    //check whether one of them has an unfairly higher amount of wins
                    var expectedPerc = 1d / wins.Count;
                    var allowedVariation = expectedPerc * 0.05;
                    foreach (var perc in percentages.Where(x => Math.Abs(x.Value - expectedPerc) > allowedVariation))
                    {
                        output.Add(new UnbalancedRole { RoleCombination = combination, Role = perc.Key, ExpectedPercentage = expectedPerc, ActualPercentage = perc.Value });
                    }
                }
            }
        }

        private static bool IsSame(IEnumerable<string> l1, IEnumerable<string> l2)
        {
            return l2.All(x => l1.Contains(x)) && l1.All(x => l2.Contains(x));
        }
    }
}
