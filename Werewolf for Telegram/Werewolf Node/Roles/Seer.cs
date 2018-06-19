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
