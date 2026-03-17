using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Helpers
{
    internal static class Settings
    {

        //By the way, these admin ports will have IP whitelisting.  Don't even bother trying to connect to them :P
        //The regular ports are not even open on the firewall
#if DEBUG
        public static int Port = 9049;
        public static int AdminPort = 9059;
#elif RELEASE
        public static int Port = 9050;  //9050-@werewolfbot 
        public static int AdminPort = 9060;
#elif RELEASE2
        public static int Port = 9051;  //9051-@werewolfIIbot
        public static int AdminPort = 9063; //9061 not responding
#elif BETA
        public static int Port = 9052;
        public static int AdminPort = 9062;
#endif



        public static string TcpSecret => Environment.MachineName.GetHashCode().ToString();
        public static readonly long PersianSupportChatId = -1001398432551;
        public static readonly long MainChatId = -1001049529775; //Beta group
        public static readonly long SupportChatId = -1001060486754; //@werewolfsupport
        public static readonly long PrimaryChatId = -1001268085464; //@werewolfgame
        public static readonly string DevChannelId = "@greywolfdev"; //@greywolfdev
        public static readonly long VeteranChatId = -1001322721489;
        public static readonly string VeteranChatLink = "werewolfvets";
        public static readonly string VeteranChatUrl = $"https://t.me/{VeteranChatLink}";
        public static readonly long TranslationChatId = -1001074012132;
        public static readonly long AdminChatId = -1001094155678;
        public static readonly long ErrorGroup = -1001098399855;
        public static readonly long BetaReportingChatId = -1001235990177;

        public static readonly string GifStoragePath = @"C:/inetpub/gifs";

#if RELEASE2
        public static List<string> VillagerDieImages = new List<string> { "CgACAgQAAxkBAAFE591ptz4Osg_A7RfeOLhOV8ecLK35ZQACIQMAAlVBBFOJqh9Y6nUayzoE" }; //1
        public static List<string> WolfWin = new List<string> { "CgACAgQAAxkBAAFE599ptz6tElv3W62dnJ7findOgukVmwAC7wIAAmXYDVOEwT8WjE5hejoE" };
        public static List<string> WolvesWin = new List<string> { "Fptz7FwiiIKoQ6TUueiZncv0ElkwAC6gIAAoBrDFOEtKQ4vZMQcToE" };
        public static List<string> VillagersWin = new List<string> { "CgACAgMAAxkBAAFE5-Rptz7bZhtwpno2p0kufruJ_jtw8AACgwADdBexB8Y17zyjnh3BOgQ" };
        public static List<string> NoWinner = new List<string> { "CgACAgQAAxkBAAFE5-hptz70A9uew21r6sjABaQT31BI6QACuAMAAlUXZAcRQqlrNnNGxDoE" };
        public static List<string> StartGame = new List<string> { "CgACAgQAAxkBAAFE5-pptz8Nj078b0JLWUoto118UALvXgAC4AIAAmcPHVOG0F0ng1794zoE" };
        public static List<string> StartChaosGame = new List<string> { "CgACAgQAAxkBAAFE5-5ptz8rxaEqygmSdcolExC-gr1BuQACagEAAhbstFB5wCF0m8k1FToE" }; //2
        public static List<string> TannerWin = new List<string> { "CgACAgQAAxkBAAFE5_Bptz8-RhiKJF5xYDVxsFCgms3SowAC_gMAAtgaZAerbv_pp-46djoE" };
        public static List<string> CultWins = new List<string> { "CgACAgQAAxkBAAFE5_Jptz9Ub9iZVoN2Yp-krwTKVR2kDAACVwEAAj6_vFCnSY97f8DTjToE" };
        public static List<string> SerialKillerWins = new List<string> { "CgACAgQAAxkBAAFE5_hptz9vGORSYa1jujIX04Y6UoesfQAC4gUAAqD6vVDNDZft3J8q1joE" };
        public static List<string> LoversWin = new List<string> { "CgACAgQAAxkBAAFE5_pptz-EhnFpvVl7HQrikH2wyGflxwAClAEAAhWgtVABzmmaduk7ZjoE" };
#elif RELEASE
        public static List<string> VillagerDieImages = new List<string> { "CgACAgQAAxkBAAFE591ptz4Osg_A7RfeOLhOV8ecLK35ZQACIQMAAlVBBFOJqh9Y6nUayzoE" };
        public static List<string> WolfWin = new List<string> { "CgACAgQAAxkBAAFE599ptz6tElv3W62dnJ7findOgukVmwAC7wIAAmXYDVOEwT8WjE5hejoE" };
        public static List<string> WolvesWin = new List<string> { "Fptz7FwiiIKoQ6TUueiZncv0ElkwAC6gIAAoBrDFOEtKQ4vZMQcToE" };
        public static List<string> VillagersWin = new List<string> { "CgACAgMAAxkBAAFE5-Rptz7bZhtwpno2p0kufruJ_jtw8AACgwADdBexB8Y17zyjnh3BOgQ" };
        public static List<string> NoWinner = new List<string> { "CgACAgQAAxkBAAFE5-hptz70A9uew21r6sjABaQT31BI6QACuAMAAlUXZAcRQqlrNnNGxDoE" };
        public static List<string> StartGame = new List<string> { "CgACAgQAAxkBAAFE5-pptz8Nj078b0JLWUoto118UALvXgAC4AIAAmcPHVOG0F0ng1794zoE" };
        public static List<string> StartChaosGame = new List<string> { "CgACAgQAAxkBAAFE5-5ptz8rxaEqygmSdcolExC-gr1BuQACagEAAhbstFB5wCF0m8k1FToE" };
        public static List<string> TannerWin = new List<string> { "CgACAgQAAxkBAAFE5_Bptz8-RhiKJF5xYDVxsFCgms3SowAC_gMAAtgaZAerbv_pp-46djoE" };
        public static List<string> CultWins = new List<string> { "CgACAgQAAxkBAAFE5_Jptz9Ub9iZVoN2Yp-krwTKVR2kDAACVwEAAj6_vFCnSY97f8DTjToE" };
        public static List<string> SerialKillerWins = new List<string> { "CgACAgQAAxkBAAFE5_hptz9vGORSYa1jujIX04Y6UoesfQAC4gUAAqD6vVDNDZft3J8q1joE" };
        public static List<string> LoversWin = new List<string> { "CgACAgQAAxkBAAFE5_pptz-EhnFpvVl7HQrikH2wyGflxwAClAEAAhWgtVABzmmaduk7ZjoE" };
        public static List<string> SKKilled = new List<string> { "CgACAgQAAxkBAAFE6BRpt0FDbn3UTqEt0F9KZ7yCcObcXwACdQUAAgEmrFM7HQh7_-BKGToE" };
        public static List<string> ArsonistWins = new List<string> { "CgACAgQAAxkBAAFE6BZpt0FkbYhBYw21Bok0OFaNFP4AATEAArsAA08-PVMOEp4jXHrAvjoE" };
        public static List<string> BurnToDeath = new List<string> { "CgACAgQAAxkBAAFE6Blpt0F1OShQAtU997JtUr_lGJKkTAACqwADa589U-4JhzY72lIaOgQ" };
#else
        public static List<string> VillagerDieImages = new List<string> { "CgACAgQAAxkBAAFE591ptz4Osg_A7RfeOLhOV8ecLK35ZQACIQMAAlVBBFOJqh9Y6nUayzoE" };
        public static List<string> WolfWin = new List<string> { "CgACAgQAAxkBAAFE599ptz6tElv3W62dnJ7findOgukVmwAC7wIAAmXYDVOEwT8WjE5hejoE" };
        public static List<string> WolvesWin = new List<string> { "Fptz7FwiiIKoQ6TUueiZncv0ElkwAC6gIAAoBrDFOEtKQ4vZMQcToE" };
        public static List<string> VillagersWin = new List<string> { "CgACAgMAAxkBAAFE5-Rptz7bZhtwpno2p0kufruJ_jtw8AACgwADdBexB8Y17zyjnh3BOgQ" };
        public static List<string> NoWinner = new List<string> { "CgACAgQAAxkBAAFE5-hptz70A9uew21r6sjABaQT31BI6QACuAMAAlUXZAcRQqlrNnNGxDoE" };
        public static List<string> StartGame = new List<string> { "CgACAgQAAxkBAAFE5-pptz8Nj078b0JLWUoto118UALvXgAC4AIAAmcPHVOG0F0ng1794zoE" };
        public static List<string> StartChaosGame = new List<string> { "CgACAgQAAxkBAAFE5-5ptz8rxaEqygmSdcolExC-gr1BuQACagEAAhbstFB5wCF0m8k1FToE" };
        public static List<string> TannerWin = new List<string> { "CgACAgQAAxkBAAFE5_Bptz8-RhiKJF5xYDVxsFCgms3SowAC_gMAAtgaZAerbv_pp-46djoE" };
        public static List<string> CultWins = new List<string> { "CgACAgQAAxkBAAFE5_Jptz9Ub9iZVoN2Yp-krwTKVR2kDAACVwEAAj6_vFCnSY97f8DTjToE" };
        public static List<string> SerialKillerWins = new List<string> { "CgACAgQAAxkBAAFE5_hptz9vGORSYa1jujIX04Y6UoesfQAC4gUAAqD6vVDNDZft3J8q1joE" };
        public static List<string> LoversWin = new List<string> { "CgACAgQAAxkBAAFE5_pptz-EhnFpvVl7HQrikH2wyGflxwAClAEAAhWgtVABzmmaduk7ZjoE" };
        public static List<string> SKKilled = new List<string> { "CgACAgQAAxkBAAFE6BRpt0FDbn3UTqEt0F9KZ7yCcObcXwACdQUAAgEmrFM7HQh7_-BKGToE" };
        public static List<string> ArsonistWins = new List<string> { "CgACAgQAAxkBAAFE6BZpt0FkbYhBYw21Bok0OFaNFP4AATEAArsAA08-PVMOEp4jXHrAvjoE" };
        public static List<string> BurnToDeath = new List<string> { "CgACAgQAAxkBAAFE6Blpt0F1OShQAtU997JtUr_lGJKkTAACqwADa589U-4JhzY72lIaOgQ" };
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
            PlayerCountZombie = 15,
            PlayerCountThirdWolf = 16,
            PlayerCountWildChild = 17,
            PlayerCountFoolChance = 18,
            PlayerCountMasons = 21,
            PlayerCountSecondZombie = 22,
            MaxGames = 80,
            TannerChance = 40,
            FoolChance = 20,
            BeholderChance = 50,
            SeerConversionChance = 40,
            GuardianAngelConversionChance = 60,
            DetectiveConversionChance = 70,
            CursedConversionChance = 60,
            HarlotConversionChance = 70,
            SorcererConversionChance = 40,
            BlacksmithConversionChance = 75,
            HarlotDiscoverCultChance = 50,
            ChanceDetectiveCaught = 40,
            ChemistSuccessChance = 50,

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
            PlayerCountZombie = 15,
            PlayerCountThirdWolf = 16,
            PlayerCountWildChild = 17,
            PlayerCountFoolChance = 18,
            PlayerCountMasons = 21,
            PlayerCountSecondZombie = 22,
            MaxGames = 80,
            TannerChance = 40,
            FoolChance = 20,
            BeholderChance = 50,
            SeerConversionChance = 40,
            GuardianAngelConversionChance = 60,
            DetectiveConversionChance = 70,
            CursedConversionChance = 60,
            HarlotConversionChance = 70,
            SorcererConversionChance = 40,
            BlacksmithConversionChance = 75,
            HarlotDiscoverCultChance = 50,
            ChanceDetectiveCaught = 40,
            ChemistSuccessChance = 50,
#endif

            GameJoinTime = 180,
            MaxExtend = 60;
    }
}
