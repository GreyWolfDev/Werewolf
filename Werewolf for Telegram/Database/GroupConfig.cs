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
        [Editable(true), Question("tanner"), DefaultValue(true), ConfigGroup("roleconfig")]
        AllowTanner = 1,
        [Editable(true), Question("fool"), DefaultValue(true), ConfigGroup("roleconfig")]
        AllowFool = 2,
        [Editable(true), Question("cult"), DefaultValue(true), ConfigGroup("roleconfig")]
        AllowCult = 4,
        [Editable(true), Question("secretlynch", SettingQuestion.YesNo), DefaultValue(false), ConfigGroup("mechanics")]
        EnableSecretLynch = 8,
        [Editable(true), Question("randommode", SettingQuestion.YesNo), DefaultValue(false), ConfigGroup("mechanics")] //WAIT WHAT IS THIS? Shhhhhhhhh
        RandomMode = 16,
        [Editable(true), Question("extend"), DefaultValue(false), ConfigGroup("permissions")]
        AllowExtend = 32,
        [Editable(true), Question("roles", SettingQuestion.ShowHide), DefaultValue(true), ConfigGroup("mechanics")]
        ShowRolesDeath = 64,
        [Editable(true), Question("flee"), DefaultValue(true), ConfigGroup("permissions")]
        AllowFlee = 128,
        [Editable(true), Question("showid", SettingQuestion.ShowHide), DefaultValue(false), ConfigGroup("groupconfigbase")]
        ShowIDs = 256,
        [Editable(true), Question("allownsfw"), DefaultValue(false), ConfigGroup("permissions")]
        AllowNSFW = 512,
        [Editable(true), Question("allowthief"), DefaultValue(true), ConfigGroup("roleconfig")]
        AllowThief = 1024,
        [Editable(true), Question("thieffull", SettingQuestion.YesNo), DefaultValue(false), ConfigGroup("mechanics")]
        ThiefFull = 2048,
        [Editable(true), Question("secretlynchshowvotes", SettingQuestion.ShowHide), DefaultValue(false), ConfigGroup("mechanics")]
        SecretLynchShowVotes = 4096,
        [Editable(true), Question("secretlynchshowvoters", SettingQuestion.ShowHide), DefaultValue(false), ConfigGroup("mechanics")]
        SecretLynchShowVoters = 8192,
        [Editable(false), Question("randomlangvariant"), DefaultValue(false)]
        RandomLangVariant = 16384,
        

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

    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigGroupAttribute : Attribute
    {
        /// <summary>
        /// The name of the config group to order the option by. Will also be used for the translation file. Property will be the key for the button option.
        /// </summary>
        public string ConfigGroup { get; set; }

        public ConfigGroupAttribute(string configGroup)
        {
            ConfigGroup = configGroup;
        }

        public static List<string> GetConfigGroups()
        {
            var strings = new List<string>();
            
            // add hardcoded config groups here, if there is no GroupConfig attribute of the same config group

            // search for all config groups
            foreach (var flag in Enum.GetValues(typeof(GroupConfig)).Cast<GroupConfig>())
            {
                var fieldInfo = flag.GetType().GetField(flag.ToString());
                var cgA = fieldInfo.GetCustomAttributes(typeof(ConfigGroupAttribute), false) as ConfigGroupAttribute[];
                if (cgA == null || cgA.Length < 1) continue;
                if (!strings.Contains(cgA[0].ConfigGroup)) strings.Add(cgA[0].ConfigGroup);
            }

            return strings;
        }

        public static string GetConfigGroup(string configOption)
        {
            foreach (var flag in Enum.GetValues(typeof(GroupConfig)).Cast<GroupConfig>())
            {
                if (flag.GetInfo()?.ShortName != configOption) continue;
                var fieldInfo = flag.GetType().GetField(flag.ToString());
                var cgA = fieldInfo.GetCustomAttributes(typeof(ConfigGroupAttribute), false) as ConfigGroupAttribute[];
                if (cgA == null || cgA.Length < 1) return null;
                return cgA[0].ConfigGroup;
            }
            return null;
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
