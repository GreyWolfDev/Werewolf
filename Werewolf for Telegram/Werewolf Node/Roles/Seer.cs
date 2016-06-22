using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Werewolf_Node.Models;

namespace Werewolf_Node.Roles
{
    class Seer : NightRole
    {
        public Seer()
        {
            Priority = 5;       
            Team = ITeam.Village;
        }
    }
}
