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
        [Description("No group options have been configured")]
        None = 0,
        [Editable(true), Question, DefaultValue(true)]
        AllowTanner = 1,
        [Editable(true), Question(SettingQuestion.YesNo), DefaultValue(false)]
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
        public QuestionAttribute(SettingQuestion Question = SettingQuestion.AllowDisallow) { }
    }

    public static class GroupExtensions
    {
        public static string GetDescription(this GroupConfig value)
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
        public static string GetName(this GroupConfig value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var descriptionAttributes = fieldInfo.GetCustomAttributes(
                typeof(DisplayAttribute), false) as DisplayAttribute[];

            if (descriptionAttributes == null) return string.Empty;
            return (descriptionAttributes.Length > 0) ? descriptionAttributes[0].Name : value.ToString();
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
