using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleSharp.TL;

namespace Werewolf_Control.Models
{
    public class ChannelInfo
    {
        public TLChannel Channel { get; set; }
        public TeleSharp.TL.Messages.TLChatFull ChatFull { get; set; }
        public List<TLUser> Users { get; set; } = new List<TLUser>();

        private DateTime _dateCreated;
        public DateTime DateCreated
        {
            get { return _dateCreated; }
            set
            {
                _dateCreated = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Channel.date).ToLocalTime();
            }
        }
    }
}
