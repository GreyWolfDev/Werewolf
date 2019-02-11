using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Database
{
    [Flags]
    public enum GroupConfig : long
    {
        //Group settings are now as simple as adding an enum to this list.  No more database schema changes.
        None = 0,
        [Editable(true), Question("tanner"), DefaultValue(true), ConfigGroup(ConfigGroup.RoleConfig)]
        AllowTanner = 1,
        [Editable(true), Question("fool"), DefaultValue(true), ConfigGroup(ConfigGroup.RoleConfig)]
        AllowFool = 2,
        [Editable(true), Question("cult"), DefaultValue(true), ConfigGroup(ConfigGroup.RoleConfig)]
        AllowCult = 4,
        [Editable(true), Question("secretlynch", SettingQuestion.YesNo), DefaultValue(false), ConfigGroup(ConfigGroup.Mechanics)]
        EnableSecretLynch = 8,
        [Editable(true), Question("randommode", SettingQuestion.YesNo), DefaultValue(false), ConfigGroup(ConfigGroup.Mechanics)] //WAIT WHAT IS THIS? Shhhhhhhhh
        RandomMode = 16,
        [Editable(true), Question("extend"), DefaultValue(false), ConfigGroup(ConfigGroup.GroupSettings)]
        AllowExtend = 32,
        [Editable(true), Question("roles", SettingQuestion.ShowHide), DefaultValue(true), ConfigGroup(ConfigGroup.Mechanics)]
        ShowRolesDeath = 64,
        [Editable(true), Question("flee"), DefaultValue(true), ConfigGroup(ConfigGroup.GroupSettings)]
        AllowFlee = 128,
        [Editable(true), Question("showid", SettingQuestion.ShowHide), DefaultValue(false), ConfigGroup(ConfigGroup.GroupSettings)]
        ShowIDs = 256,
        [Editable(true), Question("allownsfw"), DefaultValue(false), ConfigGroup(ConfigGroup.GroupSettings)]
        AllowNSFW = 512,
        [Editable(true), Question("allowthief"), DefaultValue(true), ConfigGroup(ConfigGroup.RoleConfig)]
        AllowThief = 1024,
        [Editable(true), Question("thieffull", SettingQuestion.YesNo), DefaultValue(false), ConfigGroup(ConfigGroup.Mechanics)]
        ThiefFull = 2048,
        [Editable(true), Question("secretlynchshowvotes", SettingQuestion.ShowHide), DefaultValue(false), ConfigGroup(ConfigGroup.Mechanics)]
        SecretLynchShowVotes = 4096,
        [Editable(true), Question("secretlynchshowvoters", SettingQuestion.ShowHide), DefaultValue(false), ConfigGroup(ConfigGroup.Mechanics)]
        SecretLynchShowVoters = 8192,
        [Editable(false), Question("randomlangvariant"), DefaultValue(false)]
        RandomLangVariant = 16384,
        [Editable(false), Question("shuffleplayerlist", SettingQuestion.YesNo), DefaultValue(false), ConfigGroup("groupconfigbase")]
        ShufflePlayerList = 32768,


        //this is a flag that will be set on ALL groups indicating we need to update the settings
        Update = 4611686018427387904
    }

    public static class GroupDefaults
    {
        public static GroupConfig LoadDefaults()
        {
            GroupConfig result = GroupConfig.Update;
            foreach (GroupConfig flag in Enum.GetValues(typeof(GroupConfig)))
            {
                if (flag.GetDefaultValue())
                    result |= flag;
            }
            return result;
        }
    }

    public enum SettingQuestion
    {
        AllowDisallow,
        YesNo,
        ShowHide
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class QuestionAttribute : Attribute
    {
        /// <summary>
        /// The property field from Group model.
        /// NOTE: This will also be used for the translation file.  Property will be the key for the button option, add A or Q for the answer and question asked.
        /// </summary>
        public SettingQuestion Question { get; set; }
        /// <summary>
        /// Used internally as part of the callback button data
        /// </summary>
        public string ShortName { get; set; }
        public QuestionAttribute(string shortName, SettingQuestion question = SettingQuestion.AllowDisallow)
        {
            Question = question;
            ShortName = shortName;
        }
    }

    public enum ConfigGroup
    {
        Timers,
        RoleConfig,
        Mechanics,
        GroupSettings
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigGroupAttribute : Attribute
    {
        /// <summary>
        /// A list of hardcoded config options, key is the config group and value is an array of identifiers that will be used for language and callback data
        /// </summary>
        public static readonly Dictionary<ConfigGroup, string[]> hardcodedConfigOptions = new Dictionary<ConfigGroup, string[]>
        {
            { ConfigGroup.GroupSettings, new string[] { "Lang", "Mode", "MaxPlayer" } },
            { ConfigGroup.Mechanics, new string[] { "EndRoles" } },
            { ConfigGroup.Timers, new string[] { "MaxExtend", "DayTimer", "LynchTimer", "NightTimer" } }
        };

        /// <summary>
        /// The name of the config group to order the option by. Will also be used for the translation file. Property will be the key for the button option.
        /// </summary>
        public ConfigGroup ConfigGroup { get; set; }

        public ConfigGroupAttribute(ConfigGroup configGroup)
        {
            ConfigGroup = configGroup;
        }

        public static List<ConfigGroup> GetConfigGroups()
        {
            return Enum.GetValues(typeof(ConfigGroup)).Cast<ConfigGroup>().ToList();
        }

        public static ConfigGroup GetConfigGroup(string configOption)
        {
            foreach (var flag in Enum.GetValues(typeof(GroupConfig)).Cast<GroupConfig>())
            {
                if (flag.GetInfo()?.ShortName.ToLower() != configOption.ToLower()) continue;
                var fieldInfo = flag.GetType().GetField(flag.ToString());
                var cgA = fieldInfo.GetCustomAttributes(typeof(ConfigGroupAttribute), false) as ConfigGroupAttribute[];
                if (cgA == null || cgA.Length < 1) continue;
                return cgA[0].ConfigGroup;
            }
            if (hardcodedConfigOptions.Any(x => x.Value.Any(y => y.ToLower() == configOption.ToLower())))
            {
                return hardcodedConfigOptions.First(x => x.Value.Any(y => y.ToLower() == configOption.ToLower())).Key;
            }
            switch (configOption)
            {
                //add all the config options with messed up naming here manually
                case "night":
                    return ConfigGroup.Timers;
                case "lynch":
                    return ConfigGroup.Timers;
                default:
                    throw new ArgumentException("Did not find a config group for the option: " + configOption);
            }
        }
    }

    public static class GroupExtensions
    {
        public static bool IsEditable(this GroupConfig value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var dA = fi.GetCustomAttribute(typeof(EditableAttribute)) as EditableAttribute;
            return dA?.AllowEdit ?? false;
        }
        public static QuestionAttribute GetInfo(this GroupConfig value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var qA = fieldInfo.GetCustomAttributes(
                typeof(QuestionAttribute), false) as QuestionAttribute[];

            if (qA == null) return null;
            return (qA.Length > 0) ? qA[0] : null;
        }

        public static bool GetDefaultValue(this GroupConfig value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var dA = fi.GetCustomAttribute(typeof(DefaultValueAttribute)) as DefaultValueAttribute;
            if (dA?.Value == null) return false;
            return ((bool)dA.Value);
        }



        public static IEnumerable<GroupConfig> GetUniqueSettings(this Enum flags)
        {
            ulong flag = 1;
            foreach (var value in Enum.GetValues(flags.GetType()).Cast<GroupConfig>())
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
