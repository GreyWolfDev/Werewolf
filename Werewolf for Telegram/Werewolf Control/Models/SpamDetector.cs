using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Models
{
    class SpamDetector
    {
        public bool NotifiedAdmin { get; set; } = false;
        public int Warns { get; set; } = 0;
        public HashSet<UserMessage> Messages { get; set; } = new HashSet<UserMessage>();
    }
}
