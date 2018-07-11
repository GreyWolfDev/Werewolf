using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Models.Xsolla
{
    public class XsollaData
    {
        public User User = new User();
        public Settings Settings = new Settings();
    }

    public class User
    {
        public UserId Id = new UserId();
        public UserName Name = new UserName();
    }

    public class UserId
    {
        public string Value { get; set; }
        public bool Hidden { get; set; } = false;
    }

    public class UserName
    {
        public string Value { get; set; }
        public bool Hidden { get; set; } = false;
    }

    public class Settings
    {
        public int ProjectId { get; set; }
        public string Currency { get; set; }
        public string Mode { get; set; }
        public Ui Ui = new Ui();
    }

    public class Ui
    {
        public string Theme { get; set; }
    }
}
