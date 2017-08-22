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
        //private static List<v_PublicGroups> _list;
        //private static List<string> _langs;
        //private static DateTime _lastGet = DateTime.MinValue;
        //internal static List<v_PublicGroups> GetAll()
        //{
        //    if (_lastGet < DateTime.UtcNow.AddMinutes(-20))
        //    {
        //        //only refresh the list cache once every 20 minutes
        //        using (var db = new WWContext())
        //            _list = db.v_PublicGroups.ToList();
        //        _lastGet = DateTime.UtcNow;
        //    }
        //    return _list;
        //}

        //internal static List<string> GetBaseLanguages()
        //{
        //    if (_lastGet < DateTime.UtcNow.AddMinutes(-20)) //only refresh the list cache once every 20 minutes
        //    {
        //        var langs = new List<string>();
        //        foreach (var lang in LanguageHelper.GetAllLanguages())
        //        {
        //            if (GetAll().Any(x => x.Language == lang.FileName))
        //            {
        //                //load the language to get the base
        //                if (!langs.Contains(lang.Base))
        //                    langs.Add(lang.Base);
        //            }
        //        }
        //        _langs = langs;
        //        _lastGet = DateTime.UtcNow;
        //    }
        //    return _langs;
        //}

        //internal static IEnumerable<v_PublicGroups> ForLanguage(string baseLang)
        //{
        //    var langs = LanguageHelper.GetAllLanguages().Where(x => x.Base == baseLang);
        //    foreach (var g in GetAll())
        //    {
        //        if (langs.Any(x => x.FileName == g.Language))
        //            yield return g;
        //    }
        //}
        
        private static List<v_GroupRanking> _list;
        private static List<string> _langs;
        private static DateTime _lastGetAll = DateTime.MinValue, _lastGetBase = DateTime.MinValue;

        private static Dictionary<string,List<string>> _variants = new Dictionary<string, List<string>>();
        private static Dictionary<string, DateTime> _lastGetVariant = new Dictionary<string, DateTime>();
        internal static List<v_GroupRanking> GetAll()
        {
            if (_lastGetAll < DateTime.UtcNow.AddMinutes(-20))
            {
                //only refresh the list cache once every 20 minutes
                using (var db = new WWContext())
                    _list = db.v_GroupRanking.ToList();
                _lastGetAll = DateTime.UtcNow;
            }
            return _list;
        }

        internal static List<string> GetBaseLanguages()
        {
            if (_lastGetBase < DateTime.UtcNow.AddMinutes(-20)) //only refresh the list cache once every 20 minutes
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
                _lastGetBase = DateTime.UtcNow;
            }
            return _langs;
        }

        internal static List<string> GetVariants(string baseLang)
        {
            if (!_lastGetVariant.ContainsKey(baseLang) || _lastGetVariant[baseLang] < DateTime.UtcNow.AddMinutes(-20)) //only refresh the list cache once every 20 minutes
            {
                var langs = new List<string>();
                foreach (var lang in LanguageHelper.GetAllLanguages().Where(x => x.Base == baseLang))
                {
                    if (GetAll().Any(x => x.Language == lang.FileName))
                    {
                        //load the language to get the variant
                        if (!langs.Contains(lang.Variant))
                            langs.Add(lang.Variant);
                    }
                }
                _variants[baseLang] = langs;
                _lastGetVariant[baseLang] = DateTime.UtcNow;
            }
            return _variants[baseLang];
        }
        
        internal static IEnumerable<v_GroupRanking> ForLanguage(string baseLang, string variant)
        {
            if (variant == "all")
            {
                var langs = LanguageHelper.GetAllLanguages().Where(x => x.Base == baseLang).Select(x => x.FileName);
                foreach (var g in GetAll().Where(x => langs.Contains(x.Language)).GroupBy(x => x.GroupId).Select(x => x.OrderByDescending(y => y.Ranking).First()))
                    yield return g;
            }
            else
            {
                var lang = LanguageHelper.GetAllLanguages().FirstOrDefault(x => x.Base == baseLang && x.Variant == variant);
                foreach (var g in GetAll())
                {
                    if (lang.FileName == g.Language)
                        yield return g;
                }
            }
        }

    }
}
