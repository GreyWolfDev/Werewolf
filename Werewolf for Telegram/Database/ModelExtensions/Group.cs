using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public partial class Group
    {
        public bool? ShowRolesDeath
        {
            get { return ShowRoles; }
            set { ShowRoles = value; }
        }

        public bool? AllowFlee
        {
            get { return DisableFlee == false; }
            set { DisableFlee = !value; }
        }
        //TODO: add properties which grab the flag enum
        public void UpdateFlags()
        {
            if (Flags == null)
            {
                Flags = 0;
            }
            if (!HasFlag(GroupConfig.Update)) //special flag indicating group needs to be updated.
                return;
            RemoveFlag(GroupConfig.Update);
            foreach (var flag in Enum.GetValues(typeof(GroupConfig)).Cast<GroupConfig>().Where(x => x.IsEditable()))
            {
                try
                {
                    if ((bool?)GetType().GetProperty(flag.ToString()).GetValue(this) == true)
                        AddFlag(flag);
                    //check if the setting wasn't set before.
                    if (GetType().GetProperty(flag.ToString()).GetValue(this) == null)
                    {
                        //check the default
                        if (flag.GetDefaultValue())
                            AddFlag(flag);
                    }
                    //GetType().GetProperty(flag.ToString()).SetValue(this, null);
                }
                catch (NullReferenceException)
                {
                    //property doesn't exist, ignore
                }
            }

            
            //RemoveFlag(GroupConfig.Update);
        }

        public bool HasFlag(GroupConfig flag)
        {
            if (Flags == null) Flags = 0;
            var f = (GroupConfig)Flags;
            return f.HasFlag(flag);
        }

        public void AddFlag(GroupConfig flag)
        {
            if (Flags == null) Flags = 0;
            var f = (GroupConfig)Flags;
            f = f | flag;
            Flags = (long)f;
        }

        public void RemoveFlag(GroupConfig flag)
        {
            if (Flags == null) Flags = 0;
            var f = (GroupConfig)Flags;
            f &= ~flag;
            Flags = (long)f;
        }
    }
}
