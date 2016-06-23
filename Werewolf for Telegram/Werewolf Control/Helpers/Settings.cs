using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Helpers
{
    internal static class Settings
    {
#if DEBUG
        public static int Port = 9051;
#else
        public static int Port = 9050;
#endif

        public static string TcpSecret => Environment.MachineName.GetHashCode().ToString();

        public static long MainChatId = -1001049529775; //Beta group
        public static long SupportChatId = -1001060486754; //@werewolfsupport
        public static long PrimaryChatId = -1001030085238; //@werewolfgame
        public static string DevChannelId = "@werewolfdev"; //@werewolfdev
        internal static List<string> VillagerDieImages = new List<string> { "BQADAwADggADdBexB2aZTAMpXRGUAg", "BQADBAADKgMAAoMbZAfmSqOE1YY-9wI", "BQADBAADWAMAAt4cZAe6rGbV3KvLggI" };
        internal static List<string> WolfWin = new List<string> { "BQADAwADgAADdBexB015e-EU6O9CAg", "BQADAwADgQADdBexB9ksBD5NOWQvAg" };
        internal static List<string> WolvesWin = new List<string> { "BQADBAADlwMAAtgaZAfCK5MVLj27CwI", "BQADBAADcAMAAn8ZZAe0Xjey6zCmQwI" };
        internal static List<string> VillagersWin = new List<string> { "BQADAwADgwADdBexB2K0cDWari8QAg", "BQADBAAD8QgAAqIeZAeufUVx9eT3SgI" };
        internal static List<string> StartGame = new List<string> { "BQADAwADhAADdBexB3lX1nJOQOdEAg", "BQADBAADwg0AAu0XZAcUNBLuEjnBhgI" };
        internal static List<string> StartChaosGame = new List<string> { "BQADBAAD7wYAAgcYZAei_MiVQcRUIAI", "BQADBAAD_wcAAiUYZAeV5N-hkeMW0QI" };
        internal static List<string> TannerWin = new List<string> { "BQADBAADQwgAAuQaZAcWZKq6Zm4NJAI", "BQADBAAD_gMAAtgaZAeJOVus4yf3RAI" };
        internal static List<string> CultWins = new List<string> { "BQADBAADWAMAAosYZAf5H-sYZ53nMwI", "BQADBAADHwsAAgUYZAeDkYYi5N3cIgI" };

        /// <summary>
        /// How many games are allowed for any given node
        /// </summary>
        public static int MaxGamesPerNode = 60;

        /// <summary>
        /// How many games on each node before starting a new node (to be added later)
        /// </summary>
#if DEBUG
        public static int NewNodeThreshhold = 1;
#else
        public static int NewNodeThreshhold = 30;
#endif
        public static int ShutDownNodesAt = 15;

        public static int
#if DEBUG
            MinPlayers = 1,
#else
            MinPlayers = 5,
#endif
            MaxPlayers = 35,
            TimeDay = 60,
            TimeNight = 90,
            TimeLynch = 90,
#if DEBUG
            PlayerCountSeerCursed = 6,
            PlayerCountHarlot = 7,
            PlayerCountBeholderChance = 8,
            PlayerCountSecondWolf = 9,
            PlayerCountGunner = 9,
            PlayerCountTraitor = 10,
            PlayerCountGuardianAngel = 11,
            PlayerCountDetective = 12,
            PlayerCountApprenticeSeer = 13,
            PlayerCountCultist = 15,
            PlayerCountThirdWolf = 16,
            PlayerCountWildChild = 17,
            PlayerCountFoolChance = 18,
            PlayerCountMasons = 21,
            PlayerCountSecondCultist = 22,
            MaxGames = 80,
            TannerChance = 40,
            FoolChance = 20,
            BeholderChance = 50,
            SeerConversionChance = 40,
            GuardianAngelConversionChance = 60,
            DetectiveConversionChance = 70,
            CursedConversionChance = 60,
            HarlotConversionChance = 70,
            HarlotDiscoverCultChance = 50,
            ChanceDetectiveCaught = 40,

#else
            PlayerCountSeerCursed = 6,
            PlayerCountHarlot = 7,
            PlayerCountBeholderChance = 8,
            PlayerCountSecondWolf = 9,
            PlayerCountGunner = 9,
            PlayerCountTraitor = 10,
            PlayerCountGuardianAngel = 11,
            PlayerCountDetective = 12,
            PlayerCountApprenticeSeer = 13,
            PlayerCountCultist = 15,
            PlayerCountThirdWolf = 16,
            PlayerCountWildChild = 17,
            PlayerCountFoolChance = 18,
            PlayerCountMasons = 21,
            PlayerCountSecondCultist = 22,
            MaxGames = 80,
            TannerChance = 40,
            FoolChance = 20,
            BeholderChance = 50,
            SeerConversionChance = 40,
            GuardianAngelConversionChance = 60,
            DetectiveConversionChance = 70,
            CursedConversionChance = 60,
            HarlotConversionChance = 70,
            HarlotDiscoverCultChance = 50,
            ChanceDetectiveCaught = 40,
#endif

            GameJoinTime = 180;
    }
}
