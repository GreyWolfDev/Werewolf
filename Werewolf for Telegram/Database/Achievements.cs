using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    [Flags]
    public enum Achievements : long
    {
        [Display(Name="None"), Description("You haven't played a game yet!")]
        None = 0,
        [Display(Name="Welcome to Hell"), Description("Play a game")]
        WelcomeToHell = 1,
        [Display(Name="Welcome to the Asylum"), Description("Play a chaos game")]
        WelcomeToAsylum = 2,
        [Display(Name= "Alzheimer's Patient"), Description("Play a game with an amnesia language pack")]
        AlzheimerPatient = 4,
        [Display(Name="O HAI DER!"), Description("Play a game with Para's secret account (not @para949)")]
        OHAIDER = 8,
        [Display(Name="Spy vs Spy"), Description("Play a game in secret mode (no role reveal)")]
        SpyVsSpy = 16,
        [Display(Name="Explorer"), Description("Play at least 2 games each in 10 different groups")]
        Explorer = 32,
        [Display(Name="Linguist"), Description("Play at least 2 games each in 10 different language packs")]
        Linguist = 64,
        [Display(Name = "I Have No Idea What I'm Doing"), Description("Play a game in secret amnesia mode")]
        NoIdeaWhat = 128,
        [Display(Name= "Enochlophobia"), Description("Play a 35 player game")]
        Enochlophobia = 256,
        [Display(Name = "Introvert"), Description("Play a 5 player game")]
        Introvert = 512,
        [Display(Name = "Naughty!"), Description("Play a game using any NSFW language pack")]
        Naughty = 1024,
        [Display(Name = "Dedicated"), Description("Play 100 games")]
        Dedicated = 2048,
        [Display(Name="Obsessed"), Description("Play 1000 games")]
        Obsessed = 4096,
        [Display(Name="Here's Johnny!"), Description("Get 50 kills as the serial killer")]
        HereJohnny = 8192,
        [Display(Name="I've Got Your Back"), Description("Save 50 people as the Guardian Angel")]
        GotYourBack = 16384,
        [Display(Name="Masochist"), Description("Win a game as the Tanner")]
        Masochist = 32768,
        [Display(Name="Wobble Wobble"), Description("Survive a game as the drunk (at least 10 players)")]
        Wobble = 65536,
        [Display(Name= "Inconspicuous"), Description("In a game of 20 or more people, do not get a single lynch vote against you (and survive)")]
        Inconspicuous = 131072,
        [Display(Name="Survivalist"), Description("Survive 100 games")]
        Survivalist = 262144,
        [Display(Name="Black Sheep"), Description("Get lynched first 3 games in a row")]
        BlackSheep = 524288,
        [Display(Name= "Promiscuous"), Description("As the harlot, survive a 5+ night game without staying home or visiting the same person more than once")]
        Promiscuous = 1048576,
        [Display(Name="Mason Brother"), Description("Be one of at least two surviving masons in a game")]
        MasonBrother = 2097152,
        [Display(Name="Double Shifter"), Description("Change roles twice in one game (cult conversion does not count)")]
        DoubleShifter = 4194304,
        [Display(Name="Hey Man, Nice Shot"), Description("As the hunter, use your dying shot to kill a wolf or serial killer")]
        HeyManNiceShot = 8388608,
        [Display(Name= "That's Why You Don't Stay Home"), Description("As a wolf or cultist, kill or convert a harlot that stayed home")]
        DontStayHome = 16777216,
        [Display(Name="Double Vision"), Description("Be one of two seers at the same time")]
        DoubleVision = 33554432,
        [Display(Name="Double Kill"), Description("Be part of the Serial Killer / Hunter ending")]
        DoubleKill = 67108864,
        [Display(Name= "Should Have Known"), Description("As the Seer, reveal the Beholder")]
        ShouldHaveKnown = 134217728,
        [Display(Name="I See a Lack of Trust"), Description("As the Seer, get lynched on the first day")]
        LackOfTrust = 268435456,
        [Display(Name="Sunday Bloody Sunday"), Description("Be one of at least 4 victims to die in a single night")]
        BloodyNight = 536870912,
        [Display(Name= "Change Sides Works"), Description("Change roles in a game, and win")]
        ChangingSides = 1073741824,
        [Display(Name="Forbidden Love"), Description("Win as a wolf / villager couple (villager, not village team)")]
        ForbiddenLove = 2147483648,
        [Display(Name="Developer"), Description("Have a pull request merged into the repo")]
        Developer = 4294967296,
        [Display(Name="The First Stone"), Description("Be the first to cast a lynch vote 5 times in a single game")]
        FirstStone = 8589934592,
        [Display(Name= "Smart Gunner"), Description("As the Gunner, both of your bullets hit a wolf, serial killer, or cultist")]
        SmartGunner = 17179869184,
        [Display(Name="Streetwise"), Description("Find a different wolf, serial killer, or cultist 4 nights in a row as the detective")]
        Streetwise = 34359738368,
        [Display(Name="Speed Dating"), Description("Have the bot select you as a lover (cupid failed to choose)")]
        OnlineDating = 68719476736,
        [Display(Name= "Even a Stopped Clock is Right Twice a Day"), Description("As the Fool, have at least two of your visions be correct by the end of the game")]
        BrokenClock = 137438953472,
        [Display(Name = "So Close!"), Description("As the Tanner, be tied for the most lynch votes")]
        SoClose = 274877906944,
        [Display(Name="Cultist Convention"), Description("Be one of 10 or more cultists alive at the end of a game")]
        CultCon = 549755813888,
        [Display(Name="Self Loving"), Description("As cupid, pick yourself as one of the lovers")]
        SelfLoving = 1099511627776,
        [Display(Name= "Should've Said Something"), Description("As a wolf, your pack eats your lover (first night does not count)")]
        ShouldveMentioned = 2199023255552,
        [Display(Name="Tanner Overkill"), Description("As the Tanner, have everyone (but yourself) vote to lynch you")]
        TannerOverkill = 4398046511104,
        [Display(Name="Serial Samaritan"), Description("As the Serial Killer, kill at least 3 wolves in single game")]
        SerialSamaritan = 8796093022208,
        [Display(Name="Cultist Fodder"), Description("Be the cultist that is sent to attempt to convert the Cult Hunter")]
        CultFodder = 17592186044416,
        [Display(Name="Lone Wolf"), Description("In a chaos game of 10 or more people, be the only wolf - and win")]
        LoneWolf = 35184372088832,
        [Display(Name="Pack Hunter"), Description("Be one of 7 living wolves at one time")]
        PackHunter = 70368744177664,
        [Display(Name= "Saved by the Bull(et)"), Description("As a villager, the wolves match the number of villagers, but the game does not end because the gunner has a bullet")]
        GunnerSaves = 140737488355328,
        [Display(Name="In for the Long Haul"), Description("Survive for at least an hour in a single game")]
        LongHaul = 281474976710656,
        [Display(Name="OH SHI-"), Description("Kill your lover on the first night")]
        OhShi = 562949953421312,
        [Display(Name="Veteran"), Description("Play 500 games.  You can now join @werewolfvets")]
        Veteran = 1125899906842624,
        [Display(Name = "No Sorcery!"), Description("As a wolf, kill your sorcerer")]
        NoSorcery = 2251799813685248,
        [Display(Name = "Cultest Tracker"), Description("As the cultist hunter, kill at least 3 cultists in one game")]
        CultistTracker = 4503599627370496,
        [Display(Name = "I'M NOT DRUN-- *BURPPP*"), Description("As the clumsy guy, have at least 3 correct lynches by the end of the game")]
        ImNotDrunk = 9007199254740992,
        [Display(Name = "Wuffie-Cult"), Description("As the alpha wolf, successfully convert at least 3 victims into wolves")]
        WuffieCult = 18014398509481984,
        [Display(Name = "Did you guard yourself?"), Description("As the guardian angel, survive after 3 tries guarding an unattacked wolf")]
        DidYouGuardYourself = 36028797018963968,
        [Display(Name = "Spoiled Rich Brat"), Description("As the prince, still gets lynched even after revealing your identity")]
        SpoiledRichBrat = 72057594037927936,
        [Display(Name = "Three Little Wolves and a Big Bad Pig"), Description("As the sorcerer, survive a game with three or more alive wolves")]
        ThreeLittleWolves = 144115188075855872,
        [Display(Name = "President"), Description("As the mayor, successfully cast 3 lynch votes after revealing")]
        President = 288230376151711744,
        [Display(Name = "I Helped!"), Description("As a wolf cub, the alive pack has 2 successful eat attempts after you die")]
        IHelped = 576460752303423488,
        [Display(Name = "It Aas a Busy Night!"), Description("During the same night, got visited by 3 or more different visiting roles")]
        ItWasABusyNight = 1152921504606846976
    } // MAX VALUE: 9223372036854775807
      //            

    public static class Extensions
    {
        public static string GetDescription(this Achievements value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }
        public static string GetName(this Achievements value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var descriptionAttributes = fieldInfo.GetCustomAttributes(
                typeof(DisplayAttribute), false) as DisplayAttribute[];

            if (descriptionAttributes == null) return string.Empty;
            return (descriptionAttributes.Length > 0) ? descriptionAttributes[0].Name : value.ToString();
        }

        public static IEnumerable<Achievements> GetUniqueFlags(this Enum flags)
        {
            ulong flag = 1;
            foreach (var value in Enum.GetValues(flags.GetType()).Cast<Achievements>())
            {
                ulong bits = Convert.ToUInt64(value);
                while (flag < bits)
                {
                    flag <<= 1;
                }

                if (flag == bits && flags.HasFlag(value))
                {
                    yield return value;
                }
            }
        }
    }
}
