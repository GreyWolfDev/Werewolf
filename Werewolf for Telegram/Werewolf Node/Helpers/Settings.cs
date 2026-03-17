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
        internal static List<string> VillagerDieImages = new List<string> { "CgACAgQAAxkBAAFE591ptz4Osg_A7RfeOLhOV8ecLK35ZQACIQMAAlVBBFOJqh9Y6nUayzoE" }; //1
        internal static List<string> WolfWin = new List<string> { "CgACAgQAAxkBAAFE599ptz6tElv3W62dnJ7findOgukVmwAC7wIAAmXYDVOEwT8WjE5hejoE" };
        internal static List<string> WolvesWin = new List<string> { "Fptz7FwiiIKoQ6TUueiZncv0ElkwAC6gIAAoBrDFOEtKQ4vZMQcToE" };
        internal static List<string> VillagersWin = new List<string> { "CgACAgMAAxkBAAFE5-Rptz7bZhtwpno2p0kufruJ_jtw8AACgwADdBexB8Y17zyjnh3BOgQ" };
        internal static List<string> NoWinner = new List<string> { "CgACAgQAAxkBAAFE5-hptz70A9uew21r6sjABaQT31BI6QACuAMAAlUXZAcRQqlrNnNGxDoE" };
        internal static List<string> StartGame = new List<string> { "CgACAgQAAxkBAAFE5-pptz8Nj078b0JLWUoto118UALvXgAC4AIAAmcPHVOG0F0ng1794zoE" };
        internal static List<string> StartChaosGame = new List<string> { "CgACAgQAAxkBAAFE5-5ptz8rxaEqygmSdcolExC-gr1BuQACagEAAhbstFB5wCF0m8k1FToE" }; //2
        internal static List<string> TannerWin = new List<string> { "CgACAgQAAxkBAAFE5_Bptz8-RhiKJF5xYDVxsFCgms3SowAC_gMAAtgaZAerbv_pp-46djoE" };
        internal static List<string> CultWins = new List<string> { "CgACAgQAAxkBAAFE5_Jptz9Ub9iZVoN2Yp-krwTKVR2kDAACVwEAAj6_vFCnSY97f8DTjToE" };
        internal static List<string> SerialKillerWins = new List<string> { "CgACAgQAAxkBAAFE5_hptz9vGORSYa1jujIX04Y6UoesfQAC4gUAAqD6vVDNDZft3J8q1joE" };
        internal static List<string> LoversWin = new List<string> { "CgACAgQAAxkBAAFE5_pptz-EhnFpvVl7HQrikH2wyGflxwAClAEAAhWgtVABzmmaduk7ZjoE" };
#else
        internal static List<string> VillagerDieImages = new List<string> { "CgACAgQAAxkBAAFE591ptz4Osg_A7RfeOLhOV8ecLK35ZQACIQMAAlVBBFOJqh9Y6nUayzoE" };
        internal static List<string> WolfWin = new List<string> { "CgACAgQAAxkBAAFE599ptz6tElv3W62dnJ7findOgukVmwAC7wIAAmXYDVOEwT8WjE5hejoE" };
        internal static List<string> WolvesWin = new List<string> { "Fptz7FwiiIKoQ6TUueiZncv0ElkwAC6gIAAoBrDFOEtKQ4vZMQcToE" };
        internal static List<string> VillagersWin = new List<string> { "CgACAgMAAxkBAAFE5-Rptz7bZhtwpno2p0kufruJ_jtw8AACgwADdBexB8Y17zyjnh3BOgQ" };
        internal static List<string> NoWinner = new List<string> { "CgACAgQAAxkBAAFE5-hptz70A9uew21r6sjABaQT31BI6QACuAMAAlUXZAcRQqlrNnNGxDoE" };
        internal static List<string> StartGame = new List<string> { "CgACAgQAAxkBAAFE5-pptz8Nj078b0JLWUoto118UALvXgAC4AIAAmcPHVOG0F0ng1794zoE" };
        internal static List<string> StartChaosGame = new List<string> { "CgACAgQAAxkBAAFE5-5ptz8rxaEqygmSdcolExC-gr1BuQACagEAAhbstFB5wCF0m8k1FToE" };
        internal static List<string> TannerWin = new List<string> { "CgACAgQAAxkBAAFE5_Bptz8-RhiKJF5xYDVxsFCgms3SowAC_gMAAtgaZAerbv_pp-46djoE" };
        internal static List<string> CultWins = new List<string> { "CgACAgQAAxkBAAFE5_Jptz9Ub9iZVoN2Yp-krwTKVR2kDAACVwEAAj6_vFCnSY97f8DTjToE" };
        internal static List<string> SerialKillerWins = new List<string> { "CgACAgQAAxkBAAFE5_hptz9vGORSYa1jujIX04Y6UoesfQAC4gUAAqD6vVDNDZft3J8q1joE" };
        internal static List<string> LoversWin = new List<string> { "CgACAgQAAxkBAAFE5_pptz-EhnFpvVl7HQrikH2wyGflxwAClAEAAhWgtVABzmmaduk7ZjoE" };
        internal static List<string> SKKilled = new List<string> { "CgACAgQAAxkBAAFE6BRpt0FDbn3UTqEt0F9KZ7yCcObcXwACdQUAAgEmrFM7HQh7_-BKGToE" };
        public static List<string> ArsonistWins = new List<string> { "CgACAgQAAxkBAAFE6BZpt0FkbYhBYw21Bok0OFaNFP4AATEAArsAA08-PVMOEp4jXHrAvjoE" };
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
