using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Node.Helpers
{
    public static class SpecialStrings
    {
        public static Dictionary<string, string> Strings = new Dictionary<string, string>
        {
            { "Detonation", "{0}'s pumpkin detonated! {1} was in the way and they were impaled by pumpkin bits! {1} is dead. {2}" },
            { "DetonatedWiseElder", "{0}'s pumpkin detonated at an unfortunate time! The all-knowing {1} is dead!" },
            { "AskDetonate", "The pumpkin is growing too big and might detonate! In a panic, who do you run to?" },
            { "AboutSpumpkin", "The Spumpkin 🎃 is a pumpkin grower known for his plump pumpkins, which can grow large enough to explode! When detonated, the Spumpkin and another player of their choice die from the impact. The Spumpkin wins with the rest of the villagers." },
            { "RoleInfoSpumpkin", "You are the Spumpkin! Known in the village to grow the biggest, ripest, most delicious pumpkins, you carry your prized possession wherever you go. One pumpkin grows too big, and might explode at any moment! In your panic, you may run to someone else just as it explodes, killing both of you." },
            { "Spumpkin", "the Spumpkin 🎃" },
            { "SpumpkinFailDetonate", "You carried the large pumpkin towards {0} and... seems like this pumpkin is not big enough for it to detonate... Go home and keep plowing!" },
            { "BlackDeathWinner", "Congratulations, {0}, YOU survived the {1} alone! Happy April Fool's!" },
            { "BlackDeathKilledAll", "Everybody in the village was killed by the {0} and only corpses are lying around! Happy April Fool's!" },
            { "BlackDeathLovers", "Congratulations, {0} and {1}, you are so madly in love that you even managed to survive the {2} together! Happy April Fool's!" },
            { "AboutAurora", "Aurora 🌌 can remain invisible; no one knows her role. She helps the villagers, and when she touches a wolf, it dies immediately. However, if she touches a good player, she sacrifices herself to let them live." },
            { "RoleInfoAurora", "You are Aurora 🌌. You help the village by trying to touch the bad guys! If you touch a bad player, they die and you survive! However, if you accidentally touch a good player, you will sacrifice yourself to let them live. If you die, you can bring one good dead player back to life." },
            { "Aurora", "Aurora 🌌" },
            { "AskAurora", "Who do you want to touch tonight?" },
            { "AuroraTouchedBad", "Aurora 🌌 touched {0} last night, and they died! {1}" },
            { "AuroraTouchedGood", "Aurora 🌌 touched someone who was not a bad guy, and died! {0}" },
            { "AskAuroraRevive", "You have died! Who do you want to bring back to life? You have 30 seconds to choose!" },
            { "AuroraRevivedPlayer", "The power of Aurora 🌌 has revived {1}! They are back in the game!" },
        };
    }
}
