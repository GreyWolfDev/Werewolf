using System.Collections.Generic;

namespace Database
{
    public partial class AspNetUser
    {
        public IEnumerable<string> SelectedRoles { get; set; }
        public IEnumerable<AspNetRole> Roles { get; set; }
    }
}
