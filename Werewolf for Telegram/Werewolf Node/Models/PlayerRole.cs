namespace Werewolf_Node.Models
{
    /// <summary>
    /// Not in use yet, still working on this
    /// </summary>
    public abstract class PlayerRole
    {
        protected static bool HasNightAction { get; set; }
        protected static bool HasDayAction { get; set; }
        protected static ITeam Team { get; set; }
        protected static string Description { get; set; }
        protected static string RoleInfo { get; set; }
    }
}
