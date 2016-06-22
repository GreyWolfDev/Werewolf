using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Node.Models
{
    class DayRole : PlayerRole
    {
        /// <summary>
        /// Indicates the order they should go in at night
        /// </summary>
        public int Priority { get; set; }

        public DayRole()
        {
            HasDayAction = true;
        }
    }
}
