using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using System.IO;
using System.Xml.Linq;

namespace Werewolf_Control.Helpers
{
    internal static class PublicGroups
    {
#if RELEASE
        private static List<v_PublicGroups> _list;
        private static List<string> _langs;
        private static DateTime _lastGet = DateTime.MinValue;
        internal static List<v_PublicGroups> GetAll()
        {
            if (_lastGet < DateTime.Now.AddMinutes(-20))
            {
                //only refresh the list cache once every 20 minutes
                using (var db = new WWContext())
                    _list = db.v_PublicGroups.ToList();
                _lastGet = DateTime.Now;
            }
            return _list;
        }

        internal static List<string> GetBaseLanguages()
        {
            if (_lastGet < DateTime.Now.AddMinutes(-20)) //only refresh the list cache once every 20 minutes
            {
                var langs = new List<string>();
                foreach (var lang in LanguageHelper.GetAllLanguages())
                {
                    if (GetAll().Any(x => x.Language == lang.FileName))
                    {
                        //load the language to get the base
                        if (!langs.Contains(lang.Base))
                            langs.Add(lang.Base);
                    }
                }
                _langs = langs;
                _lastGet = DateTime.Now;
            }
            return _langs;
        }

        internal static IEnumerable<v_PublicGroups> ForLanguage(string baseLang)
        {
            var langs = LanguageHelper.GetAllLanguages().Where(x => x.Base == baseLang);
            foreach (var g in GetAll())
            {
                if (langs.Any(x => x.FileName == g.Language))
                    yield return g;
            }
        }
        
#else
        private static List<v_GroupRanking> _list;
        private static List<string> _langs;
        private static DateTime _lastGet = DateTime.MinValue;
        internal static List<v_GroupRanking> GetAll()
        {
            if (_lastGet < DateTime.Now.AddMinutes(-20))
            {
                //only refresh the list cache once every 20 minutes
                using (var db = new WWContext())
                    _list = db.v_GroupRanking.ToList();
                _lastGet = DateTime.Now;
            }
            return _list;
        }

        internal static List<string> GetBaseLanguages()
        {
            if (_lastGet < DateTime.Now.AddMinutes(-20)) //only refresh the list cache once every 20 minutes
            {
                var langs = new List<string>();
                foreach (var lang in LanguageHelper.GetAllLanguages())
                {
                    if (GetAll().Any(x => x.Language == lang.FileName))
                    {
                        //load the language to get the base
                        if (!langs.Contains(lang.Base))
                            langs.Add(lang.Base);
                    }
                }
                _langs = langs;
                _lastGet = DateTime.Now;
            }
            return _langs;
        }

        internal static IEnumerable<v_GroupRanking> ForLanguage(string baseLang)
        {
            var langs = LanguageHelper.GetAllLanguages().Where(x => x.Base == baseLang);
            foreach (var g in GetAll())
            {
                if (langs.Any(x => x.FileName == g.Language))
                    yield return g;
            }
        }
#endif
    }
}
