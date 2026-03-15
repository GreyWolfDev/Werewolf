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
            try
            {
                Doc = XDocument.Load(path);
            }
            catch (System.Xml.XmlException)
            {
                try
                {
                    // Fallback to reading file and replacing invalid characters before parsing
                    string content = File.ReadAllText(path);
                    content = content.Replace(" < ", " &lt; "); // replace unescaped less-than
                    content = content.Replace(" > ", " &gt; "); // replace unescaped greater-than
                    Doc = XDocument.Parse(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load language file {path}: {ex.Message}");
                    Doc = null;
                }
            }
            Name = Doc?.Descendants("language").FirstOrDefault()?.Attribute("name")?.Value;
            Base = Doc?.Descendants("language").FirstOrDefault()?.Attribute("base")?.Value;
            Variant = Doc?.Descendants("language").FirstOrDefault()?.Attribute("variant")?.Value;
            FilePath = path;
            FileName = Path.GetFileNameWithoutExtension(path);
            LatestUpdate = File.GetLastWriteTimeUtc(path);
        }
    }
}
