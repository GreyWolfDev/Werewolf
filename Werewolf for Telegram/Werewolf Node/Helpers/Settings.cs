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
        internal static List<string> VillagerDieImages = new List<string> { "CgACAgQAAyEFAATkOdz5AAOYabc_UDD-r7X9Kh0KrSld_rLejKgAAlcBAAI-v7xQvgaVsAiEQrI6BA" }; //1
        internal static List<string> WolfWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAObabc_ac4pc4y4r09sJgJwmeCrdGwAAuIFAAKg-r1QSynIRe4BbFE6BA" };
        internal static List<string> WolvesWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAOGabc-wJvv4c1QLgacTNVEJr6a0BwAAuoCAAKAawxT5x69ivU7aa86BA" };
        internal static List<string> VillagersWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAOMabc-79IWuBl9Wx4XZ__Apo_Epg4AArgDAAJVF2QHt0KFiKsnzjs6BA" };
        internal static List<string> NoWinner = new List<string> { "CgACAgQAAyEFAATkOdz5AAOhabdBNP57fxhpoiet0etcFAna3fsAAnUFAAIBJqxTRFWeCDnXFAs6BA" };
        internal static List<string> StartGame = new List<string> { "CgACAgQAAyEFAATkOdz5AAIBYGm5uoLA1__WO7pVLsJflWbD17C_AAIxAwACho0lUxvlOBXV3ZWiOgQ", "CgACAgQAAyEFAATkOdz5AAIBXmm5unW8tuhz_CJnH3pHPNtsHURyAAK4AQACfgK0UDhuqxpKZx23OgQ", "CgACAgQAAyEFAATkOdz5AAOPabc_CGCveKeDLiSFT5OXz6SG9csAAuACAAJnDx1Tf7xojI-Ax186BA", "CgACAgQAAyEFAATkOdz5AAIBY2m5uqGhHo0gezoqTx-YmvX8b8idAALHBQACQSIVUZFNx0sR9OsUOgQ"};
        internal static List<string> StartChaosGame = new List<string> {"CgACAgQAAyEFAATkOdz5AAIBYGm5uoLA1__WO7pVLsJflWbD17C_AAIxAwACho0lUxvlOBXV3ZWiOgQ", "CgACAgQAAyEFAATkOdz5AAIBXmm5unW8tuhz_CJnH3pHPNtsHURyAAK4AQACfgK0UDhuqxpKZx23OgQ", "CgACAgQAAyEFAATkOdz5AAOPabc_CGCveKeDLiSFT5OXz6SG9csAAuACAAJnDx1Tf7xojI-Ax186BA", "CgACAgQAAyEFAATkOdz5AAIBY2m5uqGhHo0gezoqTx-YmvX8b8idAALHBQACQSIVUZFNx0sR9OsUOgQ" }; //2
        internal static List<string> TannerWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAOVabc_O8l5hKr3RtMD-ioon4EWf_AAAv4DAALYGmQH8ZtwNJR7umk6BA" };
        internal static List<string> CultWins = new List<string> { "CgACAgQAAyEFAATkOdz5AAOPabc_CGCveKeDLiSFT5OXz6SG9csAAuACAAJnDx1Tf7xojI-Ax186BA" };
        internal static List<string> SerialKillerWins = new List<string> { "CgACAgQAAyEFAATkOdz5AAOSabc_JU3TAks2tWBFhXBvK4Bn19EAAmoBAAIW7LRQlRcTQ7V4WQM6BA" };
        internal static List<string> LoversWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAODabc-mO2YN-S0Xc6wAAHR_8um_Na6AALvAgACZdgNUwogz0dG00ZDOgQ" };
#else
        internal static List<string> VillagerDieImages = new List<string> { "CgACAgQAAyEFAATkOdz5AAOYabc_UDD-r7X9Kh0KrSld_rLejKgAAlcBAAI-v7xQvgaVsAiEQrI6BA" };
        internal static List<string> WolfWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAObabc_ac4pc4y4r09sJgJwmeCrdGwAAuIFAAKg-r1QSynIRe4BbFE6BA" };
        internal static List<string> WolvesWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAOGabc-wJvv4c1QLgacTNVEJr6a0BwAAuoCAAKAawxT5x69ivU7aa86BA" };
        internal static List<string> VillagersWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAOMabc-79IWuBl9Wx4XZ__Apo_Epg4AArgDAAJVF2QHt0KFiKsnzjs6BA" };
        internal static List<string> NoWinner = new List<string> { "CgACAgQAAyEFAATkOdz5AAOhabdBNP57fxhpoiet0etcFAna3fsAAnUFAAIBJqxTRFWeCDnXFAs6BA" };
        internal static List<string> StartGame = new List<string> { "CgACAgQAAyEFAATkOdz5AAIBYGm5uoLA1__WO7pVLsJflWbD17C_AAIxAwACho0lUxvlOBXV3ZWiOgQ", "CgACAgQAAyEFAATkOdz5AAIBXmm5unW8tuhz_CJnH3pHPNtsHURyAAK4AQACfgK0UDhuqxpKZx23OgQ", "CgACAgQAAyEFAATkOdz5AAOPabc_CGCveKeDLiSFT5OXz6SG9csAAuACAAJnDx1Tf7xojI-Ax186BA", "CgACAgQAAyEFAATkOdz5AAIBY2m5uqGhHo0gezoqTx-YmvX8b8idAALHBQACQSIVUZFNx0sR9OsUOgQ"};
        internal static List<string> StartChaosGame = new List<string> { "CgACAgQAAyEFAATkOdz5AAIBYGm5uoLA1__WO7pVLsJflWbD17C_AAIxAwACho0lUxvlOBXV3ZWiOgQ", "CgACAgQAAyEFAATkOdz5AAIBXmm5unW8tuhz_CJnH3pHPNtsHURyAAK4AQACfgK0UDhuqxpKZx23OgQ", "CgACAgQAAyEFAATkOdz5AAOPabc_CGCveKeDLiSFT5OXz6SG9csAAuACAAJnDx1Tf7xojI-Ax186BA", "CgACAgQAAyEFAATkOdz5AAIBY2m5uqGhHo0gezoqTx-YmvX8b8idAALHBQACQSIVUZFNx0sR9OsUOgQ" };
        internal static List<string> TannerWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAOVabc_O8l5hKr3RtMD-ioon4EWf_AAAv4DAALYGmQH8ZtwNJR7umk6BA" };
        internal static List<string> CultWins = new List<string> { "CgACAgQAAyEFAATkOdz5AAOPabc_CGCveKeDLiSFT5OXz6SG9csAAuACAAJnDx1Tf7xojI-Ax186BA" };
        internal static List<string> SerialKillerWins = new List<string> { "CgACAgQAAyEFAATkOdz5AAOSabc_JU3TAks2tWBFhXBvK4Bn19EAAmoBAAIW7LRQlRcTQ7V4WQM6BA" };
        internal static List<string> LoversWin = new List<string> { "CgACAgQAAyEFAATkOdz5AAODabc-mO2YN-S0Xc6wAAHR_8um_Na6AALvAgACZdgNUwogz0dG00ZDOgQ" };
        internal static List<string> SKKilled = new List<string> { "CgACAgQAAxkBAAIu8WnwrnYs_8jYSvNRvOhMmqGEZAZ7AAJGCAAC0ytNUyWUBL0oCT0MOwQ" };
        public static List<string> ArsonistWins = new List<string> { "CgACAgQAAyEFAATkOdz5AAOeabc_gcqfnH36DkC1gk9m4cQWSgQAApQBAAIVoLVQ_3pxo5vu2Mw6BA" };
        public static List<string> BurnToDeath = new List<string> { "CgACAgQAAxkBAAFE6Blpt0F1OShQAtU997JtUr_lGJKkTAACqwADa589U-4JhzY72lIaOgQ" };

#endif
        /* beta
        public static List<string> VillagerDieImages = new List<string> { "BQADAwADggADdBexBxVNNy-rt--bAg", "BQADBAADWAMAAt4cZAfbY0WobzNPwAI", "BQADBAADKgMAAoMbZAc7Ldme4T3DKQI" };
        public static List<string> WolfWin = new List<string> { "BQADAwADgQADdBexBzrFBt-CBlhbAg", "BQADAwADgAADdBexB88vVl1RuLb3Ag" };
        public static List<string> WolvesWin = new List<string> { "BQADBAADcAMAAn8ZZAfjilsAAeijzEAC", "BQADBAADlwMAAtgaZAcKX7eF4AgXCAI" };
        public static List<string> VillagersWin = new List<string> { "BQADAwADgwADdBexB5XubJT7w_zDAg" };
        public static List<string> NoWinner = new List<string> { "BQADBAAD8QgAAqIeZAdO5PeO55YsOQI", "BQADBAADuAMAAlUXZAePrr-YU3PDJwI" };
        public static List<string> StartGame = new List<string> { "BQADBAADwg0AAu0XZAdw1sAIIH6xQQI", "BQADAwADhAADdBexByVGjSOQUSx_Ag" };
        public static List<string> StartChaosGame = new List<string> { "BQADBAAD7wYAAgcYZAfk95HeMjOEfgI", "BQADBAAD_wcAAiUYZAcehPF7vHGFXAI" };
        public static List<string> TannerWin = new List<string> { "BQADBAAD_gMAAtgaZAeTCzBjKyXi6wI", "BQADBAADQwgAAuQaZAcBXBZ1bAUmwQI" };
        public static List<string> CultWins = new List<string> { "BQADBAADWAMAAosYZAfwfixffVnZywI", "BQADBAADHwsAAgUYZAfm4J7Dr5HpJQI" };
        public static List<string> SerialKillerWins = new List<string> { "BQADBAADdQMAAsEcZAf2I8Sj2kPcNQI", "BQADBAADmgMAArgcZAebNN10T84w9AI", "BQADBAADKwMAAsQZZAflRhJNO_knQAI", "BQADBAADOAQAAqUXZAcKgmVLwfIHvAI" };
        public static List<string> LoversWin = new List<string> { "BQADBAADYAMAAkMdZAf0_rs89KCyDAI", "BQADBAAD8hUAAhYYZAcV2T0l7f-lJQI" };
        public static List<string> SKKilled = new List<string> { "CgADBAADNZ8AAu0XZAd3o6VVV4IvLQI", "CgADBAADfvgAAtUaZAfmDpy1f5hnRwI" };
        public static List<string> ArsonistWins = new List<string> { "CgADBAADuwADTz49UzDYA8zEWtN0Ag" };
        public static List<string> BurnToDeath = new List<string> { "CgADBAADqwADa589U9Z936jXmRz4Ag" };
#endif
    */

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
#if !DEBUG
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
            OracleConversionChance = 50,
            SandmanConversionChance = 60,
            WiseElderConversionChance = 30,
            ImamConversionChance = 80,
            ThiefStealChance = 50,
            ChemistSuccessChance = 50,
            GraveDiggerConversionChance = 30,
            AugurConversionChance = 40,
#else
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
            OracleConversionChance = 50,
            SandmanConversionChance = 60,
            WiseElderConversionChance = 30,
            ImamConversionChance = 80,
            ThiefStealChance = 100,
            ChemistSuccessChance = 50,
            GraveDiggerConversionChance = 30,
            AugurConversionChance = 40,
#endif

            GameJoinTime = 180,
            MaxJoinTime = 300;


#if DEBUG
        //public static long MainChatId = -134703013;
        public static long MainChatId = -1001049529775; //Beta group
#else
        public static long MainChatId = -1001030085238;
#endif
        public static long VeteranChatId = -1001322721489;
        public static string VeteranChatLink = "werewolfvets";

        public static bool RandomLynch = false;
    }
}