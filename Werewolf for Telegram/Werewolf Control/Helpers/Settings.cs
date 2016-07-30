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
#elif RELEASE
        public static int Port = 9050;  //9050-@werewolfbot 
#elif RELEASE2
        public static int Port = 9051;  //9051-@werewolfIIbot
#endif

        public static string TcpSecret => Environment.MachineName.GetHashCode().ToString();
        public static long PersianSupportChatId = -1001050225148;
        public static long MainChatId = -1001049529775; //Beta group
        public static long SupportChatId = -1001060486754; //@werewolfsupport
        public static long PrimaryChatId = -1001030085238; //@werewolfgame
        public static string DevChannelId = "@werewolfdev"; //@werewolfdev
#if RELEASE2
        internal static List<string> VillagerDieImages = new List<string> { "BQADAwAD2QEAAnQXsQeU2FMN-2D3GgI", "BQADAwADggADdBexB1_X0udQaRs7Ag", "BQADBAADWAMAAt4cZAcXTtE-UCQXxAI" }; //1
        internal static List<string> WolfWin = new List<string> { "BQADAwADgQADdBexB5kx2CsSNDp2Ag", "BQADAwADgAADdBexBx7XahnW5XBsAg" };
        internal static List<string> WolvesWin = new List<string> { "BQADBAADlwMAAtgaZAeaog1gGP_fkwI", "BQADBAADcAMAAn8ZZAfjUfLaMGoxzgI" };
        internal static List<string> VillagersWin = new List<string> { "BQADAwADgwADdBexB90OD5PHXLAuAg" };
        internal static List<string> NoWinner = new List<string> { "BQADBAAD8QgAAqIeZAeLBjBE4l0LSAI", "BQADBAADuAMAAlUXZAfHXDmd504z5AI" };
        internal static List<string> StartGame = new List<string> { "BQADAwADhAADdBexB7b36d3MSPzDAg", "BQADBAADwg0AAu0XZAcwCVhaZgAB_CsC" };
        internal static List<string> StartChaosGame = new List<string> { "BQADAwAD1wEAAnQXsQeswdJwV9BIyQI", "BQADAwAD2AEAAnQXsQeGw_-A8E7DLwI" }; //2
        internal static List<string> TannerWin = new List<string> { "BQADBAADQwgAAuQaZAcjXuF_tkE3JwI", "BQADBAAD_gMAAtgaZAf2YeVX6mXnUQI" };
        internal static List<string> CultWins = new List<string> { "BQADBAADWAMAAosYZAcuRvZYBpQmXwI", "BQADBAADHwsAAgUYZAcUTEIahD8XSQI" };
        internal static List<string> SerialKillerWins = new List<string> { "BQADBAADdQMAAsEcZAd7skaRqoWKzQI", "BQADBAADmgMAArgcZAdPyqayfRT6bQI", "BQADBAADOAQAAqUXZAeeuV5vjRd6QAI", "BQADBAADKwMAAsQZZAfwd2_EAeeOTgI" };
        internal static List<string> LoversWin = new List<string> { "BQADBAAD8hUAAhYYZAeHmbRRzioXXQI", "BQADBAADYAMAAkMdZAfR4qo8c95FGgI" };
#else
        internal static List<string> VillagerDieImages = new List<string> { "BQADAwADggADdBexB2aZTAMpXRGUAg", "BQADBAADKgMAAoMbZAfmSqOE1YY-9wI", "BQADBAADWAMAAt4cZAe6rGbV3KvLggI" };
        internal static List<string> WolfWin = new List<string> { "BQADAwADgAADdBexB015e-EU6O9CAg", "BQADAwADgQADdBexB9ksBD5NOWQvAg" };
        internal static List<string> WolvesWin = new List<string> { "BQADBAADlwMAAtgaZAfCK5MVLj27CwI", "BQADBAADcAMAAn8ZZAe0Xjey6zCmQwI" };
        internal static List<string> VillagersWin = new List<string> { "BQADAwADgwADdBexB2K0cDWari8QAg" };
        internal static List<string> NoWinner = new List<string> { "BQADBAAD8QgAAqIeZAeufUVx9eT3SgI", "BQADBAADuAMAAlUXZAfNGmq-KVN_0AI" };
        internal static List<string> StartGame = new List<string> { "BQADAwADhAADdBexB3lX1nJOQOdEAg", "BQADBAADwg0AAu0XZAcUNBLuEjnBhgI" };
        internal static List<string> StartChaosGame = new List<string> { "BQADBAAD7wYAAgcYZAei_MiVQcRUIAI", "BQADBAAD_wcAAiUYZAeV5N-hkeMW0QI" };
        internal static List<string> TannerWin = new List<string> { "BQADBAADQwgAAuQaZAcWZKq6Zm4NJAI", "BQADBAAD_gMAAtgaZAeJOVus4yf3RAI" };
        internal static List<string> CultWins = new List<string> { "BQADBAADWAMAAosYZAf5H-sYZ53nMwI", "BQADBAADHwsAAgUYZAeDkYYi5N3cIgI" };
        internal static List<string> SerialKillerWins = new List<string> { "BQADBAADKwMAAsQZZAf4t254zcOVdgI", "BQADBAADOAQAAqUXZAdnNEO6TaxtnQI", "BQADBAADdQMAAsEcZAfkYMOxn9xzBAI", "BQADBAADmgMAArgcZAfW46sJoTg9VQI" };
        internal static List<string> LoversWin = new List<string> { "BQADBAAD8hUAAhYYZAeSI-kDTlm6QAI", "BQADBAADYAMAAkMdZAesBzPWN8zN3QI" };
#endif

        /// <summary>
        /// How many games are allowed for any given node
        /// </summary>
        public static int MaxGamesPerNode = 60;

        /// <summary>
        /// How many games on each node before starting a new node (to be added later)
        /// </summary>
#if DEBUG
        public static int NewNodeThreshhold = 10;
#else
        public static int NewNodeThreshhold = 30;
#endif
        public static int ShutDownNodesAt = 15;

        public static int
#if DEBUG
            MinPlayers = 1,
#else
            MinPlayers = 3,
#endif
            MaxPlayers = 200,
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
