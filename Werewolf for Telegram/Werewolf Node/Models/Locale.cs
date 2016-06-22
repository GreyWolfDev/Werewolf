using System.Xml.Linq;

namespace Werewolf_Node.Models
{
    class Locale
    {
        public string Language { get; set; }
        public XDocument File { get; set; }
    }
}
