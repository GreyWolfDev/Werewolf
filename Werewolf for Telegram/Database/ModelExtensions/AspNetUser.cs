using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public partial class AspNetUser
    {
        public IEnumerable<string> SelectedRoles { get; set; }
        public IEnumerable<AspNetRole> Roles { get; set; }
    }
}
