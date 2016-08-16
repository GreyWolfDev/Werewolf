using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    [Flags]
    public enum Achievements : long
    {
        None = 0,
        WelcomeToHell = 1,
        WelcomeToAsylum = 2,
        AlzheimerPatient = 4,
        OHAIDER = 8,
        SpyVsSpy = 16,
        Explorer = 32,
        Linguist = 64,
        NoIdeaWhat = 128,
        Enochlophobia = 256,
        Introvert = 512,
        Naughty = 1024,
        Dedicated = 2048,
        Obsessed = 4096,
        HereJohnny = 8192,
        GotYourBack = 16384,
        Masochist = 32768,
        Wobble = 65536,
        Inconspicuous = 131072,
        Survivalist = 262144,
        BlackSheep = 524288,
        Promiscuous = 1048576,
        MasonBrother = 2097152,
        DoubleShifter = 4194304,
        HeyManNiceShot = 8388608,
        DontStayHome = 16777216,
        DoubleVision = 33554432,
        DoubleKill = 67108864,
        ShouldHaveKnown = 134217728,
        LackOfTrust = 268435456,
        BloodyNight = 536870912,
        ChangingSides = 1073741824,
        ForbiddenLove = 2147483648,
        Developer = 4294967296,
        FirstStone = 8589934592,
        SmartGunner = 17179869184,
        Streetwise = 34359738368,
        OnlineDating = 68719476736,
        BrokenClock = 137438953472,
        SoClose = 274877906944,
        CultCon = 549755813888,
        SelfLoving = 1099511627776,
        ShouldveMentioned = 2199023255552,
        TannerOverkill = 4398046511104,
        HelpfulKiller = 8796093022208,
        CultFodder = 17592186044416,
        LoneWolf = 35184372088832,
        PackHunter = 70368744177664,
        GunnerSaves = 140737488355328
    }
}
