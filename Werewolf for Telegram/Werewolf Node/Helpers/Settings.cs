using System.Collections.Generic;

namespace Werewolf_Node
{
    static class Settings
    {
#if DEBUG
        public static string ServerIP = "127.0.0.1";
#else
        public static string ServerIP = "127.0.0.1";

#endif
#if DEBUG
        public static int Port = 9049;
#elif RELEASE
        public static int Port = 9050;
#elif RELEASE2
        public static int Port = 9051;
#elif BETA
        public static int Port = 9052;
#endif


#if RELEASE2
        internal static List<string> VillagerDieImages = new List<string> { };
        internal static List<string> WolfWin = new List<string> { };
        internal static List<string> WolvesWin = new List<string> { };
        internal static List<string> VillagersWin = new List<string> { };
        internal static List<string> NoWinner = new List<string> { };
        internal static List<string> StartGame = new List<string> { };
        internal static List<string> StartChaosGame = new List<string> { };
        internal static List<string> TannerWin = new List<string> { };
        internal static List<string> CultWins = new List<string> { };
        internal static List<string> SerialKillerWins = new List<string> { };
        internal static List<string> LoversWin = new List<string> { };
        internal static List<string> HunterKilledCultist = new List<string> { };
        internal static List<string> HunterKilledFinalShot = new List<string> { };
        internal static List<string> RoleInfoDrunk = new List<string> { };
        internal static List<string> BlacksmithSpreadSilver = new List<string> { };
        internal static List<string> HarlotVisitYou = new List<string> { };
#elif RELEASE
        public static List<string> VillagerDieImages = new List<string> { };
        public static List<string> WolfWin = new List<string> { };
        public static List<string> WolvesWin = new List<string> { };
        public static List<string> VillagersWin = new List<string> { };
        public static List<string> NoWinner = new List<string> { };
        public static List<string> StartGame = new List<string> { };
        public static List<string> StartChaosGame = new List<string> { };
        public static List<string> TannerWin = new List<string> { };
        public static List<string> CultWins = new List<string> { };
        public static List<string> SerialKillerWins = new List<string> { };
        public static List<string> LoversWin = new List<string> { };
        public static List<string> HunterKilledCultist = new List<string> { };
        public static List<string> HunterKilledFinalShot = new List<string> { };
        public static List<string> RoleInfoDrunk = new List<string> { };
        public static List<string> BlacksmithSpreadSilver = new List<string> { };
        public static List<string> HarlotVisitYou = new List<string> { };
#else
        public static List<string> VillagerDieImages = new List<string> { "BQADAwADggADdBexBxVNNy-rt--bAg", "BQADBAADWAMAAt4cZAfbY0WobzNPwAI", "BQADBAADKgMAAoMbZAc7Ldme4T3DKQI" };
        public static List<string> WolfWin = new List<string> { "BQADAwADgQADdBexBzrFBt-CBlhbAg", "BQADAwADgAADdBexB88vVl1RuLb3Ag" };
        public static List<string> WolvesWin = new List<string> { "BQADBAADcAMAAn8ZZAfjilsAAeijzEAC", "BQADBAADlwMAAtgaZAcKX7eF4AgXCAI" };
        public static List<string> VillagersWin = new List<string> { "BQADAQAD_wADcsVuDy8Un_hN5fq2Ag" };
        public static List<string> NoWinner = new List<string> { "BQADBAAD8QgAAqIeZAdO5PeO55YsOQI", "BQADBAADuAMAAlUXZAePrr-YU3PDJwI" };
        public static List<string> StartGame = new List<string> { "BQADBAADwg0AAu0XZAdw1sAIIH6xQQI", "BQADAwADhAADdBexByVGjSOQUSx_Ag" };
        public static List<string> StartChaosGame = new List<string> { "BQADBAAD7wYAAgcYZAfk95HeMjOEfgI", "BQADBAAD_wcAAiUYZAcehPF7vHGFXAI" };
        public static List<string> TannerWin = new List<string> { "BQADBAAD_gMAAtgaZAeTCzBjKyXi6wI", "BQADBAADQwgAAuQaZAcBXBZ1bAUmwQI" };
        public static List<string> CultWins = new List<string> { "BQADBAADWAMAAosYZAfwfixffVnZywI", "BQADBAADHwsAAgUYZAfm4J7Dr5HpJQI" };
        public static List<string> SerialKillerWins = new List<string> { "BQADBAADdQMAAsEcZAf2I8Sj2kPcNQI", "BQADBAADmgMAArgcZAebNN10T84w9AI", "BQADBAADKwMAAsQZZAflRhJNO_knQAI", "BQADBAADOAQAAqUXZAcKgmVLwfIHvAI" };
        public static List<string> LoversWin = new List<string> { "BQADBAADYAMAAkMdZAf0_rs89KCyDAI", "BQADBAAD8hUAAhYYZAcV2T0l7f-lJQI" };
        public static List<string> HunterKilledCultist = new List<string> { "BQADBAADNAMAAkcbZAfo06zarRzNXgI" };
        public static List<string> HunterKilledFinalShot = new List<string> { "BQADAQADuAADoS3DCeya0e_BqwABvgI" };
        public static List<string> RoleInfoDrunk = new List<string> { "BQADAQADwgADoS3DCblBCwQZU6nUAg", "BQADAQADvQADoS3DCQ2MVePhv534Ag" };
#endif

        public static int
#if DEBUG
            MinPlayers = 3,

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
            AlphaWolfConversionChance = 20,
            SorcererConversionChance = 40,
            BlacksmithConversionChance = 75,
            PsychicMageConversionChance = 80,
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
            AlphaWolfConversionChance = 100,
            SorcererConversionChance = 40,
            BlacksmithConversionChance = 75,
            PsychicMageConversionChance = 80,
#endif

            GameJoinTime = 180,
            MaxJoinTime = 300;



#if DEBUG
        //public static long MainChatId = -134703013;
        public static long MainChatId = -1001049529775; //Beta group
#else
        public static long MainChatId = -1001030085238;
#endif
        public static long VeteranChatId = -1001094614730;
        public static string VeteranChatLink = "werewolfvets";

        public static bool RandomLynch = false;
    }
}
