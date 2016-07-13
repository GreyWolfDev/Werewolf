using System.Collections.Generic;

namespace Werewolf_Node
{
    static class Settings
    {
#if DEBUG
        public static string ServerIP = "127.0.0.1";
        public static int Port = 9051;
#else
        public static string ServerIP = "127.0.0.1";
        public static int Port = 9050;
#endif



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

        public static int
#if DEBUG
            MinPlayers = 5,
#else
            MinPlayers = 5,
#endif
            MaxPlayers = 35,
            TimeDay = 60,
            TimeNight = 90,
            TimeLynch = 90,
#if !DEBUG
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
            PlayerCountDoppelGanger = 22,
            PlayerCountCupid = 23,
            PlayerCountHunter = 24,
            PlayerCountSerialKiller = 25,
            PlayerCountSecondCultist = 26,
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
            HunterConversionChance = 50,
            HunterKillCultChance = 50,
            HunterKillWolfChanceBase = 30,
            SerialKillerConversionChance = 20,

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
            PlayerCountDoppelGanger = 22,
            PlayerCountCupid = 23,
            PlayerCountHunter = 24,
            PlayerCountSerialKiller = 25,
            PlayerCountSecondCultist = 26,
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
            HunterConversionChance = 50,
            HunterKillCultChance = 50,
            HunterKillWolfChanceBase = 30,
            SerialKillerConversionChance = 20,
#endif

            GameJoinTime = 180;



#if DEBUG
        //public static long MainChatId = -134703013;
        public static long MainChatId = -1001049529775; //Beta group
#else
        public static long MainChatId = -1001030085238;
#endif

        public static bool RandomLynch = false;
    }
}
