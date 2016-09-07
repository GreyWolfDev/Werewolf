using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotanIO.Api;

namespace Werewolf_Control.Helpers
{
    
    internal static class Analytics
    {
        private static readonly string _token = RegHelper.GetRegValue("BotanAPI");
        internal static BotanIO.Api.Botan Botan = new Botan(_token);
    }
}
