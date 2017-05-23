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
        None = 0,
        [Editable(true), Question("tanner"), DefaultValue(true)]
        AllowTanner = 1,
        [Editable(true), Question("secretlynch", SettingQuestion.YesNo), DefaultValue(false)]
        EnableSecretLynch = 2,
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

    public static class GroupExtensions
    {
        public static bool IsEditable(this GroupConfig value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var dA = fi.GetCustomAttribute(typeof(EditableAttribute)) as EditableAttribute;
            return dA.AllowEdit;
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
