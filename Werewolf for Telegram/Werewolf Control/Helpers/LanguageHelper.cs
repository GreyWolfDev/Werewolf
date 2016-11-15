using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Handler;
using File = System.IO.File;

namespace Werewolf_Control.Helpers
{
    public class LangFile
    {
        public string Name { get; set; }
        public string Base { get; set; }
        public string Variant { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public XDocument Doc { get; set; }

        public LangFile(string path)
        {
            Doc = XDocument.Load(path);
            Name = Doc.Descendants("language").First().Attribute("name").Value;
            Base = Doc.Descendants("language").First().Attribute("base").Value;
            Variant = Doc.Descendants("language").First().Attribute("variant").Value;
            FilePath = path;
            FileName = Path.GetFileNameWithoutExtension(path);
        }
    }
    public static class LanguageHelper
    {
        private static List<LangFile> _langFiles = new List<LangFile>();
        private static DateTime LastGet = DateTime.MinValue;
        internal static List<LangFile> GetAllLanguages()
        {
            if (LastGet < DateTime.Now.AddMinutes(60))
            {
                var files = Directory.GetFiles(Bot.LanguageDirectory, "*.xml");
                var temp = files.Select(file => new LangFile(file)).ToList();
                _langFiles = temp;
                LastGet = DateTime.Now;
            }
            return _langFiles;
        }
        public static void ValidateFiles(long id, int msgId)
        {
            var errors = new List<LanguageError>();
            //first, let's load up the English file, which is our master file
            var master = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));
            var masterStrings = master.Descendants("string");
            foreach (var file in Directory.GetFiles(Bot.LanguageDirectory).Where(x => !x.EndsWith("English.xml")))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var doc = XDocument.Load(file);
                //first check the language node
                var langNode = doc.Descendants("language").First();
                if (langNode.Attributes().All(x => x.Name != "base"))
                {
                    errors.Add(new LanguageError(fileName, "language node", "base attribute is missing", ErrorLevel.Error));
                }
                if (langNode.Attributes().All(x => x.Name != "variant"))
                {
                    errors.Add(new LanguageError(fileName, "language node", "variant attribute is missing", ErrorLevel.Error));
                }
                //now test the length
                if (langNode.Attributes().Any(x => x.Name == "base") &&
                    langNode.Attributes().Any(x => x.Name == "variant"))
                {
                    var test = $"setlang|-1001049529775|{langNode.Attribute("base").Value}|{langNode.Attribute("variant").Value}|v";
                    var count = Encoding.UTF8.GetByteCount(test);
                    if (count > 64)
                        errors.Add(new LanguageError(fileName, "language node", "base and variant are too long. (*38 utf8 byte max*)", ErrorLevel.Error));
                }
                //check CultConvertHunter duplication



                foreach (var str in masterStrings)
                {
                    var key = str.Attribute("key").Value;
                    var isgif = str.Attributes().Any(x => x.Name == "isgif");
                    //get the english string
                    //get the locale values
                    var masterString = GetLocaleString(key, master);
                    var values = doc.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key)?.Descendants("value");
                    if (values == null)
                    {
                        errors.Add(new LanguageError(fileName, key, $"Values missing"));
                        continue;
                    }
                    //check master string for {#} values
                    int vars = 0;
                    if (masterString.Contains("{0}"))
                        vars = 1;
                    if (masterString.Contains("{1}"))
                        vars = 2;
                    if (masterString.Contains("{2}"))
                        vars = 3;
                    if (masterString.Contains("{3}"))
                        vars = 4;
                    if (masterString.Contains("{4}"))
                        vars = 5;

                    foreach (var value in values)
                    {
                        for (int i = 0; i <= 5 - 1; i++)
                        {
                            if (!value.Value.Contains("{" + i + "}") && vars - 1 >= i)
                            {
                                //missing a value....
                                errors.Add(new LanguageError(fileName, key, "Missing {" + i + "}", ErrorLevel.Error));
                            }
                            else if (value.Value.Contains("{" + i + "}") && vars - 1 < i)
                            {
                                errors.Add(new LanguageError(fileName, key, "Extra {" + i + "}", ErrorLevel.Error));
                            }
                        }

                        if (isgif && value.Value.Length > 200)
                        {
                            errors.Add(new LanguageError(fileName, key, "GIF string length cannot exceed 200 characters", ErrorLevel.Error));
                        }
                        //foreach (var html in new[] { "<", ">", "\"", "&" })
                        //{
                        //    if (value.Value.Contains(html))
                        //        errors.Add(new LanguageError(fileName, key, "Value contains HTML markup: " + html, ErrorLevel.Error));
                        //}


                    }


                }
            }

            //now pack up the errors and send
            var result = "";
            foreach (var file in errors.Select(x => x.File).Distinct().ToList())
            {
                result += $"*{file}*\n";
                result +=
                    $"_Missing strings: {errors.Count(x => x.Level == ErrorLevel.MissingString && x.File == file)}_\n";
                if (errors.Any(x => x.File == file && x.Level == ErrorLevel.Error))
                    result = errors.Where(x => x.File == file && x.Level == ErrorLevel.Error).Aggregate(result, (current, fileError) => current + $"_{fileError.Level} - {fileError.Key}_\n{fileError.Message}\n\n");


            }
            Bot.Api.SendTextMessage(id, result, parseMode: ParseMode.Markdown);
            result =
                $"*Validation complete*\nErrors: {errors.Count(x => x.Level == ErrorLevel.Error)}\nMissing strings: {errors.Count(x => x.Level == ErrorLevel.MissingString)}";

            Bot.Api.EditMessageText(id, msgId, result, parseMode: ParseMode.Markdown);
        }

        public static void ValidateLanguageFile(long id, string filePath, int msgId)
        {
            var errors = new List<LanguageError>();
            //first, let's load up the English file, which is our master file
            var master = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));
            var masterStrings = master.Descendants("string");

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var doc = XDocument.Load(filePath);
            //first check the language node
            var langNode = doc.Descendants("language").First();
            if (langNode.Attributes().All(x => x.Name != "base"))
            {
                errors.Add(new LanguageError(fileName, "language node", "base attribute is missing", ErrorLevel.Error));
            }
            if (langNode.Attributes().All(x => x.Name != "variant"))
            {
                errors.Add(new LanguageError(fileName, "language node", "variant attribute is missing", ErrorLevel.Error));
            }
            //now test the length
            if (langNode.Attributes().Any(x => x.Name == "base") &&
                langNode.Attributes().Any(x => x.Name == "variant"))
            {
                var test = $"setlang|-1001049529775|{langNode.Attribute("base").Value}|{langNode.Attribute("variant").Value}|v";
                var count = Encoding.UTF8.GetByteCount(test);
                if (count > 64)
                    errors.Add(new LanguageError(fileName, "language node", "base and variant are too long. (*38 utf8 byte max*)", ErrorLevel.Error));
            }
            foreach (var str in masterStrings)
            {
                var key = str.Attribute("key").Value;
                var isgif = str.Attributes().Any(x => x.Name == "isgif");
                //get the english string
                //get the locale values
                var masterString = GetLocaleString(key, master);
                var values = doc.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key)?.Descendants("value");
                if (values == null)
                {
                    errors.Add(new LanguageError(fileName, key, $"Values missing"));
                    continue;
                }
                //check master string for {#} values
                int vars = 0;
                if (masterString.Contains("{0}"))
                    vars = 1;
                if (masterString.Contains("{1}"))
                    vars = 2;
                if (masterString.Contains("{2}"))
                    vars = 3;
                if (masterString.Contains("{3}"))
                    vars = 4;
                if (masterString.Contains("{4}"))
                    vars = 5;

                foreach (var value in values)
                {
                    for (int i = 0; i <= 5 - 1; i++)
                    {
                        if (!value.Value.Contains("{" + i + "}") && vars - 1 >= i)
                        {
                            //missing a value....
                            errors.Add(new LanguageError(fileName, key, "Missing {" + i + "}", ErrorLevel.Error));
                        }
                        else if (value.Value.Contains("{" + i + "}") && vars - 1 < i)
                        {
                            errors.Add(new LanguageError(fileName, key, "Extra {" + i + "}", ErrorLevel.Error));
                        }
                    }

                    if (isgif && value.Value.Length > 200)
                    {
                        errors.Add(new LanguageError(fileName, key, "GIF string length cannot exceed 200 characters", ErrorLevel.Error));
                    }

                    //foreach (var html in new[] { "<", ">", "\"", "&" })
                    //{
                    //    if (value.Value.Contains(html))
                    //        errors.Add(new LanguageError(fileName, key, "Value contains HTML markup: " + html, ErrorLevel.Error));
                    //}
                }
            }



            var result = $"*{fileName}*" + Environment.NewLine;
            if (errors.Any(x => x.Level == ErrorLevel.Error))
            {
                result += "_Errors:_\n";
                result = errors.Where(x => x.Level == ErrorLevel.Error).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n{fileError.Message}\n\n");
            }
            if (errors.Any(x => x.Level == ErrorLevel.MissingString))
            {
                result += "_Missing Values:_\n";
                result = errors.Where(x => x.Level == ErrorLevel.MissingString).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n");
            }
            result += "\n";
            //Program.Send(result, id);
            Thread.Sleep(500);
            result += $"*Validation complete*.\nErrors: {errors.Count(x => x.Level == ErrorLevel.Error)}\nMissing strings: {errors.Count(x => x.Level == ErrorLevel.MissingString)}";
            Bot.Api.EditMessageText(id, msgId, result, parseMode: ParseMode.Markdown);

        }

        private static string GetLocaleString(string key, XDocument file)
        {
            var strings = file.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key);
            var values = strings.Descendants("value");
            return values.First().Value;
        }

        internal static void UploadFile(string fileid, long id, string newFileCorrectName, int msgID)
        {
            var file = Bot.Api.GetFile(fileid).Result;
            var path = Directory.CreateDirectory(Bot.TempLanguageDirectory);
            //var fileName = file.FilePath.Substring(file.FilePath.LastIndexOf("/") + 1);
            var uri = $"https://api.telegram.org/file/bot{Bot.TelegramAPIKey}/{file.FilePath}";
            var newFilePath = Path.Combine(path.FullName, newFileCorrectName);
            using (var client = new WebClient())
            {
                client.DownloadFile(new Uri(uri), newFilePath);
            }


            //ok, we have the file.  Now we need to determine the language, scan it and the original file.
            var newFileErrors = new List<LanguageError>();
            var curFileErorrs = new List<LanguageError>();
            //first, let's load up the English file, which is our master file
            var master = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));
            var masterStrings = master.Descendants("string");

            var newFileName = Path.GetFileNameWithoutExtension(newFilePath);

            var newFile = new LangFile(newFilePath);
            //first check the language node

            //now test the length

            var lentest = $"setlang|-1001049529775|{newFile.Base}|{newFile.Variant}|v";
            var lencount = Encoding.UTF8.GetByteCount(lentest);
            if (lencount > 64)
                newFileErrors.Add(new LanguageError(newFileName, "language node", "base and variant are too long. (*38 utf8 byte max*)", ErrorLevel.Error));

            //check for CultConvertHunter duplication
            var dup = newFile.Doc.Descendants("string").Count(x => x.Attribute("key").Value == "CultConvertHunter");
            if (dup > 1)
            {
                newFileErrors.Add(new LanguageError(newFileName, "CultConvertHunter", "CultConvertHunter duplication.  First instance should be renamed to CultConvertCultHunter.  Please use /getlang to get the latest version (which has this fixed)", ErrorLevel.Error));
            }
            foreach (var str in masterStrings)
            {
                var key = str.Attribute("key").Value;
                var isgif = str.Attributes().Any(x => x.Name == "isgif");
                //get the english string
                //get the locale values
                var masterString = GetLocaleString(key, master);
                var values = newFile.Doc.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key)?.Descendants("value");
                if (values == null)
                {
                    newFileErrors.Add(new LanguageError(newFileName, key, $"Values missing"));
                    continue;
                }
                //check master string for {#} values
                int vars = 0;
                if (masterString.Contains("{0}"))
                    vars = 1;
                if (masterString.Contains("{1}"))
                    vars = 2;
                if (masterString.Contains("{2}"))
                    vars = 3;
                if (masterString.Contains("{3}"))
                    vars = 4;
                if (masterString.Contains("{4}"))
                    vars = 5;

                foreach (var value in values)
                {
                    for (int i = 0; i <= 5 - 1; i++)
                    {
                        if (!value.Value.Contains("{" + i + "}") && vars - 1 >= i)
                        {
                            //missing a value....
                            newFileErrors.Add(new LanguageError(newFileName, key, "Missing {" + i + "}", ErrorLevel.Error));
                        }
                        else if (value.Value.Contains("{" + i + "}") && vars - 1 < i)
                        {
                            newFileErrors.Add(new LanguageError(newFileName, key, "Extra {" + i + "}", ErrorLevel.Error));
                        }
                    }

                    if (isgif && value.Value.Length > 200)
                    {
                        newFileErrors.Add(new LanguageError(newFileName, key, "GIF string length cannot exceed 200 characters", ErrorLevel.Error));
                    }

                }
            }

            //need to get the current file

            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x));
            var lang = langs.FirstOrDefault(x => x.Name == newFile.Name);
            var curFileName = lang?.FileName;
            //check for matching filename, different name
            var error = langs.FirstOrDefault(x => (x.Name != newFile.Name || x.Base != newFile.Base || x.Variant != newFile.Variant) && x.FileName == newFile.FileName);
            if (error != null)
            {
                //problem....
                newFileErrors.Add(new LanguageError(curFileName, "File Name",
                    $"File with same filename ({error.FileName}) exists with a different language node!\nCancelled!", ErrorLevel.Error));
                
            }
            //check for a file with same base and variant
            error = langs.FirstOrDefault(x => x.Base == newFile.Base && x.Variant == newFile.Variant && newFile.Name != x.Name);
            if (error != null)
            {
                //problem....
                newFileErrors.Add(new LanguageError(curFileName, "Language Node",
                    $"File with different language name ({error.Name}) exists with the same base ({error.Base}) and variant ({error.Variant})!\nCancelled!", ErrorLevel.Error));

            }
            if (lang != null)
            {
                var test = $"setlang|-1001049529775|{lang.Base}|{lang.Variant}|v";
                var count = Encoding.UTF8.GetByteCount(test);
                if (count > 64)
                    curFileErorrs.Add(new LanguageError(curFileName, "language node", "base and variant are too long. (*38 utf8 byte max*)", ErrorLevel.Error));

                //validate current file name / base / variants match
                if (newFile.Base != lang.Base)
                {
                    newFileErrors.Add(new LanguageError(curFileName, "Language Node", $"Mismatched Base! {newFile.Base} - {lang.Base}", ErrorLevel.Error));
                }
                if (newFile.Variant != lang.Variant)
                {
                    newFileErrors.Add(new LanguageError(curFileName, "Language Node", $"Mismatched Variant! {newFile.Variant} - {lang.Variant}", ErrorLevel.Error));
                }
                
                foreach (var str in masterStrings)
                {
                    var key = str.Attribute("key").Value;
                    var isgif = str.Attributes().Any(x => x.Name == "isgif");
                    //get the english string
                    //get the locale values
                    var masterString = GetLocaleString(key, master);
                    var values = lang.Doc.Descendants("string")
                            .FirstOrDefault(x => x.Attribute("key").Value == key)?
                            .Descendants("value");
                    if (values == null)
                    {
                        curFileErorrs.Add(new LanguageError(curFileName, key, $"Values missing"));
                        continue;
                    }
                    //check master string for {#} values
                    int vars = 0;
                    if (masterString.Contains("{0}"))
                        vars = 1;
                    if (masterString.Contains("{1}"))
                        vars = 2;
                    if (masterString.Contains("{2}"))
                        vars = 3;
                    if (masterString.Contains("{3}"))
                        vars = 4;
                    if (masterString.Contains("{4}"))
                        vars = 5;

                    foreach (var value in values)
                    {
                        for (int i = 0; i <= 5 - 1; i++)
                        {
                            if (!value.Value.Contains("{" + i + "}") && vars - 1 >= i)
                            {
                                //missing a value....
                                curFileErorrs.Add(new LanguageError(curFileName, key, "Missing {" + i + "}", ErrorLevel.Error));
                            }
                            else if (value.Value.Contains("{" + i + "}") && vars - 1 < i)
                            {
                                curFileErorrs.Add(new LanguageError(curFileName, key, "Extra {" + i + "}", ErrorLevel.Error));
                            }
                        }

                        if (isgif && value.Value.Length > 200)
                        {
                            newFileErrors.Add(new LanguageError(newFileName, key, "GIF string length cannot exceed 200 characters", ErrorLevel.Error));
                        }
                    }
                }
            }
            var result = $"NEW FILE\n*{newFile.FileName}.xml - ({newFile.Name})*" + Environment.NewLine;
            if (newFileErrors.Any(x => x.Level == ErrorLevel.Error))
            {
                result += "_Errors:_\n";
                result = newFileErrors.Where(x => x.Level == ErrorLevel.Error).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n{fileError.Message}\n\n");
            }
            if (newFileErrors.Any(x => x.Level == ErrorLevel.MissingString))
            {
                result += "_Missing Values:_\n";
                result = newFileErrors.Where(x => x.Level == ErrorLevel.MissingString).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n");
            }
            if (newFileErrors.Count == 0)
            {
                result += "_No errors_\n";
            }
            if (lang != null)
            {
                result += "\n\n";
                result += $"CURRENT FILE\n*{curFileName}.xml - ({lang.Name})*\n";
                result +=
                    $"Errors: {curFileErorrs.Count(x => x.Level == ErrorLevel.Error)}\nMissing strings: {curFileErorrs.Count(x => x.Level == ErrorLevel.MissingString)}";
            }
            else
            {
                result += "\n\n*No current file, this is a new language*";
                result += $"\n_Base:_ {newFile.Base}";
                if (!langs.Any(x => x.Base == newFile.Base))
                    result += " *(NEW)*";
                result += $"\n_Variant:_ {newFile.Variant}";
            }
            Bot.Api.SendTextMessage(id, result, parseMode: ParseMode.Markdown);
            Thread.Sleep(500);
            if (newFileErrors.All(x => x.Level != ErrorLevel.Error))
            {
                //load up each file and get the names
                var buttons = new[]
                {
                    new InlineKeyboardButton($"New - ({newFileName})", $"upload|{id}|{newFileName}"),
                    new InlineKeyboardButton("Current", $"upload|{id}|current")
                };
                var menu = new InlineKeyboardMarkup(buttons.ToArray());
                Bot.Api.SendTextMessage(id, "Which file do you want to keep?", replyToMessageId: msgID,
                    replyMarkup: menu);
            }
            else
            {
                Bot.Api.SendTextMessage(id, "Errors present, cannot upload.", replyToMessageId: msgID);
            }
        }

        public static void UseNewLanguageFile(string fileName, long id, int msgId)
        {
            var msg = "Moving file to production..\n";
            msg += "Checking paths for duplicate language file...\n";
            Bot.Api.EditMessageText(id, msgId, msg);
            fileName += ".xml";
            var tempPath = Bot.TempLanguageDirectory;
            var langPath = Bot.LanguageDirectory;
            var newFilePath = Path.Combine(tempPath, fileName);
            var copyToPath = Path.Combine(langPath, fileName);
            
            //get the new files language
            var doc = XDocument.Load(newFilePath);

            var newFileLang = new
            {
                Name = doc.Descendants("language").First().Attribute("name").Value,
                Base = doc.Descendants("language").First().Attribute("base").Value,
                Variant = doc.Descendants("language").First().Attribute("variant").Value
            };


            //check for existing file
            var langs = Directory.GetFiles(langPath).Select(x => new LangFile(x)).ToList();
            var lang = langs.FirstOrDefault(x => x.Name == newFileLang.Name && x.FilePath != copyToPath);
            if (lang != null)
            {
                msg += $"Found duplicate language (name attribute) with filename {Path.GetFileNameWithoutExtension(lang.FilePath)}\n";
                copyToPath = lang.FilePath;
            }
            else
            {
                lang = langs.FirstOrDefault(x => x.Base == newFileLang.Base && x.Variant == newFileLang.Variant && x.Name != newFileLang.Name);
                if (lang != null)
                {
                    msg += $"Found duplicate language (matching base and variant) with filename {Path.GetFileNameWithoutExtension(lang.FilePath)}\n";
                    msg += "Aborting!";
                    Bot.Api.EditMessageText(id, msgId, msg);
                    return;
                }
            }



            System.IO.File.Copy(newFilePath, copyToPath, true);
            msg += "File copied to bot\n";
//#if RELEASE
//            msg += $"File copied to bot 1\n";
//#elif RELEASE2
//            msg += $"File copied to bot 2\n";
//#endif
            Bot.Api.EditMessageText(id, msgId, msg);
//#if RELEASE
//            copyToPath = copyToPath.Replace("Werewolf 3.0", "Werewolf 3.0 Clone");
//            System.IO.File.Copy(newFilePath, copyToPath, true);
//            msg += $"File copied to bot 2\n";
//            Bot.Api.EditMessageText(id, msgId, msg);
//#endif
            var gitPath = Path.Combine(@"C:\Werewolf Source\Werewolf\Werewolf for Telegram\Languages", Path.GetFileName(copyToPath));
            File.Copy(newFilePath, gitPath, true);
            System.IO.File.Delete(newFilePath);
            msg += $"File copied to git directory\n";

            Bot.Api.EditMessageText(id, msgId, msg);
            //msg += $"Committing changes to repo...\n";
            //try
            //{
            //    var p = new Process
            //    {
            //        StartInfo =
            //        {
            //            FileName = @"C:\Werewolf Source\Werewolf\Werewolf for Telegram\Languages\commit.bat",
            //            Arguments = $"\"Updating {fileName} from Telegram ***NO_CI***\"",
            //            WorkingDirectory = @"C:\Werewolf Source\Werewolf\Werewolf for Telegram\Languages",
            //            UseShellExecute = false,
            //            RedirectStandardOutput = true,
            //            RedirectStandardError = true,
            //            CreateNoWindow = true
            //        }
            //    };
            //    p.Start();
            //    var output = "";
            //    while (!p.StandardOutput.EndOfStream)
            //        output += p.StandardOutput.ReadLine() + Environment.NewLine;
            //    while (!p.StandardError.EndOfStream)
            //        output += p.StandardError.ReadLine() + Environment.NewLine;

            //    //validate the output
            //    if (output.Contains("failed"))
            //    {
            //        msg += $"*Failed to commit file.  See control output for information*";
            //        Console.WriteLine(output);
            //    }
            //    else if (output.Contains("nothing to commit"))
            //    {
            //        msg += $"*File not committed, matches existing file.*";
            //    }
            //    else
            //    {
            //        //try to grab the commit
            //        var regex = new Regex("(\\[master .*])");
            //        var match = regex.Match(output);
            //        var commit = "";
            //        if (match.Success)
            //        {
            //            commit = match.Value.Replace("[master ", "").Replace("]", "");
            //        }
            //        msg += $"File committed successfully. {(String.IsNullOrEmpty(commit) ? "" : $"[{commit}](https://github.com/parabola949/Werewolf/commit/{commit})")}";
            //    }
            //}
            //catch (Exception e)
            //{
            //    msg += e.Message;
            //}

            msg += "* Operation complete.*";

             Bot.Api.EditMessageText(id, msgId, msg, parseMode: ParseMode.Markdown);
        }

        public static void SendAllFiles(long id)
        {

            //need to zip up all the files
            var path = Path.Combine(Bot.RootDirectory, "languages.zip");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            ZipFile.CreateFromDirectory(Bot.LanguageDirectory, path);
            //now send the file
            var fs = new FileStream(path, FileMode.Open);
            Bot.Api.SendDocument(id, new FileToSend("languages.zip", fs));
        }

        public static void SendFile(long id, string choice)
        {
            var langOptions =
                                Directory.GetFiles(Bot.LanguageDirectory)
                                    .Select(
                                        x =>
                                            new
                                            {
                                                Name =
                                                    XDocument.Load(x)
                                                        .Descendants("language")
                                                        .First()
                                                        .Attribute("name")
                                                        .Value,
                                                FilePath = x,
                                                FileName = Path.GetFileName(x)
                                            });
            var option = langOptions.First(x => x.Name == choice);
            var fs = new FileStream(option.FilePath, FileMode.Open);
            Bot.Api.SendDocument(id, new FileToSend(option.FileName, fs));
        }
    }



    public class LanguageError
    {
        public string File { get; set; }
        public string Key { get; set; }
        public string Message { get; set; }
        public ErrorLevel Level { get; set; }

        public LanguageError(string file, string key, string message, ErrorLevel level = ErrorLevel.MissingString)
        {
            File = file;
            Key = key;
            Message = message;
            Level = level;
        }
    }

    public enum ErrorLevel
    {
        Info, MissingString, Error
    }
}
