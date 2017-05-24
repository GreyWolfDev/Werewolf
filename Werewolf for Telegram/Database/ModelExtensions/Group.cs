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
            get => ShowRoles;
            set => ShowRoles = value;
        }
        //TODO: add properties which grab the flag enum

        //TODO: add method to update 'Flags' with the groups settings from before
        //TODO: null out the previous settings, forcing Flags to be used
    }
}
