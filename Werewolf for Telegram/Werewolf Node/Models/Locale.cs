using System.Xml.Linq;

namespace Werewolf_Node.Models
{
    public class Locale
    {
        public string Language { get; set; }
        public XDocument File { get; set; }
    }
}
