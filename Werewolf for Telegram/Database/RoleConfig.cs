using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    [Flags]
    public enum DisabledRole : long // NOTE: FALSE WILL MEAN ENABLED, TRUE WILL MEAN DISABLED!
    {
        None = 0,
        VALID = 1,

        [Role("👱", false)]
        Villager = 2,

        [Role("🍻")]
        Drunk = 4,

        [Role("💋")]
        Harlot = 8,

        [Role("👳")]
        Seer = 16,

        [Role("🖕")]
        Traitor = 32,

        [Role("👼")]
        GuardianAngel = 64,

        [Role("🕵")]
        Detective = 128,

        [Role("🐺", false)]
        Wolf = 256,

        [Role("😾")]
        Cursed = 512,

        [Role("🔫")]
        Gunner = 1024,

        [Role("👺")]
        Tanner = 2048,

        [Role("🃏")]
        Fool = 4096,

        [Role("👶")]
        WildChild = 8192,

        [Role("👁")]
        Beholder = 16384,

        [Role("🙇")]
        ApprenticeSeer = 32768,

        [Role("👤")]
        Cultist = 65536,

        [Role("💂")]
        CultistHunter = 131072,

        [Role("👷")]
        Mason = 262144,

        [Role("🎭")]
        Doppelgänger = 524288,

        [Role("🏹")]
        Cupid = 1048576,

        [Role("🎯")]
        Hunter = 2097152,

        [Role("🔪")]
        SerialKiller = 4194304,

        [Role("🔮")]
        Sorcerer = 8388608,

        [Role("⚡️")]
        AlphaWolf = 16777216,

        [Role("🐶")]
        WolfCub = 33554432,

        [Role("⚒")]
        Blacksmith = 67108864,

        [Role("🤕")]
        ClumsyGuy = 134217728,

        [Role("🎖")]
        Mayor = 268435456,

        [Role("👑")]
        Prince = 536870912,

        [Role("🐺🌝")]
        Lycan = 1073741824,

        [Role("☮️")]
        Pacifist = 2147483648,

        [Role("📚")]
        WiseElder = 4294967296,

        [Role("🌀")]
        Oracle = 8589934592,

        [Role("💤")]
        Sandman = 17179869184,

        [Role("👱🌚")]
        WolfMan = 34359738368,

        [Role("😈")]
        Thief = 68719476736,
        
        [Role("🤯")]
        Troublemaker = 137438953472,

        [Role("👨‍🔬")]
        Chemist = 274877906944,

        [Role("🐺☃️")]
        SnowWolf = 549755813888,

        [Role("☠️")]
        GraveDigger = 1099511627776,

        [Role("🦅")]
        Augur = 2199023255552,

        [Role("🔥")]
        Arsonist = 4398046511104,
    }

    public class RoleAttribute : Attribute
    {
        public string Emoji { get; }
        public bool CanBeDisabled { get; }

        public RoleAttribute(string emoji, bool canBeDisabled = true)
        {
            Emoji = emoji;
            CanBeDisabled = canBeDisabled;
        }
    }

    public static class RoleConfigHelper
    {
        public static List<DisabledRole> GetRoles()
            => Enum.GetValues(typeof(DisabledRole)).Cast<DisabledRole>()
                .Where(x => x != DisabledRole.None && x != DisabledRole.VALID).ToList();

        public static RoleAttribute GetRoleAttribute(this DisabledRole role)
        {
            var fieldInfo = role.GetType().GetField(role.ToString());

            var qA = fieldInfo.GetCustomAttributes(
                typeof(RoleAttribute), false) as RoleAttribute[];

            if (qA == null) return null;
            return (qA.Length > 0) ? qA[0] : null;
        }

        public static IEnumerable<DisabledRole> GetUniqueRoles(this DisabledRole roles)
        {
            foreach(var r in GetRoles())
            {
                if (roles.HasFlag(r))
                {
                    yield return r;
                }
            }
        }
    }
}
