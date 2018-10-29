namespace Werewolf_Node.Models
{
    public class QuestionAsked
    {
        public QuestionType QType { get; set; }
        public string[] ValidAnswers { get; set; }
        public int MessageId { get; set; }
    }

    public enum QuestionType
    {
        Lynch,
        Kill,
        Visit,
        See,
        Shoot,
        Guard,
        Detect,
        Convert,
        RoleModel,
        Hunt,
        HunterKill,
        SerialKill,
        Lover1,
        Lover2,
        Mayor,
        SpreadSilver,
        Kill2,
        Sandman,
        Pacifist,
        Thief
    }
}
