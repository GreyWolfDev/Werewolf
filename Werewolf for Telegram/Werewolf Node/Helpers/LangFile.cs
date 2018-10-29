using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Werewolf_Node.Helpers
{
    public class LangFile
    {
        public string Name { get; set; }
        public string Base { get; set; }
        public string Variant { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public XDocument Doc { get; set; }
        public DateTime LatestUpdate { get; }

        public LangFile(string path)
        {
            Doc = XDocument.Load(path);
            Name = Doc.Descendants("language").First().Attribute("name")?.Value;
            Base = Doc.Descendants("language").First().Attribute("base")?.Value;
            Variant = Doc.Descendants("language").First().Attribute("variant")?.Value;
            FilePath = path;
            FileName = Path.GetFileNameWithoutExtension(path);
            LatestUpdate = File.GetLastWriteTimeUtc(path);
        }
    }
}
