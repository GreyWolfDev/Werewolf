using Microsoft.Win32;

namespace Werewolf_Control.Helpers
{
    public static class RegHelper
    {
        private static RegistryKey _key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Werewolf");
        public static string GetRegValue(string key)
        {
            return _key.GetValue(key, "").ToString();
        }
    }
}
