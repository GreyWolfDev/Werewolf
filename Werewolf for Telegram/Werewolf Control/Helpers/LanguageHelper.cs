using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
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
    public static class LanguageHelper
    {
        private static List<LangFile> _langFiles = new List<LangFile>();
        private static DateTime LastGet = DateTime.MinValue;
        internal static List<LangFile> GetAllLanguages()
        {
            if (LastGet < DateTime.UtcNow.AddMinutes(60))
            {
                var files = Directory.GetFiles(Bot.LanguageDirectory, "*.xml");
                var temp = files.Select(file => new LangFile(file)).ToList();
                _langFiles = temp;
                LastGet = DateTime.UtcNow;
            }
            return _langFiles;
        }

        public static void ValidateFiles(long id, int msgId, string choice = null)
        {
            var errors = new List<LanguageError>();

            //first, let's load up the English file, which is our master file
            var master = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));

            foreach (var langfile in Directory.GetFiles(Bot.LanguageDirectory).Where(x => !x.EndsWith("English.xml")).Select(x => new LangFile(x)))
                if (langfile.Base == choice || choice == null)
                {
                    //first check the language node
                    CheckLanguageNode(langfile, errors);

                    //test the length
                    TestLength(langfile, errors);

                    //get the file errors
                    GetFileErrors(langfile, errors, master);
                }

            //now pack up the errors and send
            var result = "";
            foreach (var file in errors.Select(x => x.File).Distinct().ToList())
            {
                var langfile = new LangFile(Path.Combine(Bot.LanguageDirectory, $"{file}.xml"));
                result += $"*{langfile.FileName}.xml* (Last updated: {langfile.LatestUpdate.ToString("MMM dd")})\n";
                if (errors.Any(x => x.Level == ErrorLevel.Ads))
                {
                    result += "*Ads detected:";
                    result = errors.Where(x => x.File == langfile.FileName && x.Level == ErrorLevel.Ads).Aggregate(result, (current, fileError) => current + fileError.Key + ", ").TrimEnd(',', ' ') + "\n";
                    continue;
                }
                if (errors.Any(x => x.Level == ErrorLevel.DuplicatedString))
                {
                    result += "_Duplicated Strings: _";
                    result = errors.Where(x => x.File == langfile.FileName && x.Level == ErrorLevel.DuplicatedString).Aggregate(result, (current, fileError) => current + fileError.Key + ", ").TrimEnd(',', ' ') + "\n";
                }
                if (errors.Any(x => x.Level == ErrorLevel.JoinLink))
                {
                    result += "_Join commands: _";
                    result = errors.Where(x => x.File == langfile.FileName && x.Level == ErrorLevel.JoinLink).Aggregate(result, (current, fileError) => current + fileError.Key + ", ").TrimEnd(',', ' ') + "\n";
                }
                result += $"_Missing strings:_ {errors.Count(x => x.Level == ErrorLevel.MissingString && x.File == langfile.FileName)}\n";
                if (errors.Any(x => x.File == langfile.FileName && x.Level == ErrorLevel.Error))
                    result = errors.Where(x => x.File == langfile.FileName && x.Level == ErrorLevel.Error).Aggregate(result, (current, fileError) => current + $"_{fileError.Level} - {fileError.Key}_\n{fileError.Message}\n");
                result += "\n";
                
            }
            Bot.Api.SendTextMessageAsync(id, result, parseMode: ParseMode.Markdown);
            var sortedfiles = Directory.GetFiles(Bot.LanguageDirectory).Select(x => new LangFile(x)).Where(x => x.Base == (choice ?? x.Base)).OrderBy(x => x.LatestUpdate);
            result = $"*Validation complete*\nErrors: {errors.Count(x => x.Level == ErrorLevel.Error)}\nMissing strings: {errors.Count(x => x.Level == ErrorLevel.MissingString)}";
            result += $"\nMost recently updated file: {sortedfiles.Last().FileName}.xml ({sortedfiles.Last().LatestUpdate.ToString("MMM dd")})\nLeast recently updated file: {sortedfiles.First().FileName}.xml ({sortedfiles.First().LatestUpdate.ToString("MMM dd")})";

            Bot.Api.EditMessageTextAsync(id, msgId, result, parseMode: ParseMode.Markdown);
        }

        public static void ValidateLanguageFile(long id, string filePath, int msgId)
        {
            var errors = new List<LanguageError>();
            var langfile = new LangFile(filePath);
            
            //first, let's load up the English file, which is our master file
            var master = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));

            //first check the language node
            CheckLanguageNode(langfile, errors);

            //now test the length
            TestLength(langfile, errors);

            //get the errors
            GetFileErrors(langfile, errors, master);
            
            //send the result
            var result = $"*{langfile.FileName}.xml* (Last updated: {langfile.LatestUpdate.ToString("MMM dd")})" + Environment.NewLine;
            if (errors.Any(x => x.Level == ErrorLevel.Ads))
            {
                result += "*ADS DETECTED*\n";
                result = errors.Where(x => x.Level == ErrorLevel.Error).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n");
            }
            else
            {
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
                if (errors.Any(x => x.Level == ErrorLevel.DuplicatedString))
                {
                    result += "\n_Duplicated Strings:_\n";
                    result = errors.Where(x => x.Level == ErrorLevel.DuplicatedString).Aggregate(result, (current, fileError) => current + fileError.Key + ", ").TrimEnd(',', ' ') + "\n";
                }
                if (errors.Any(x => x.Level == ErrorLevel.JoinLink))
                {
                    result += "\n_Join commands:_\n";
                    result = errors.Where(x => x.Level == ErrorLevel.JoinLink).Aggregate(result, (current, fileError) => current + fileError.Key + ", ").TrimEnd(',', ' ') + "\n";
                }
                if (errors.Any(x => x.Level == ErrorLevel.FatalError))
                {
                    result += "\n*Fatal errors:*\n";
                    result = errors.Where(x => x.Level == ErrorLevel.FatalError).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n{fileError.Message}\n");
                }
            }
            result += "\n";
            //Program.Send(result, id);
            Thread.Sleep(500);
            result += $"*Validation complete*.\nErrors: {errors.Count(x => x.Level == ErrorLevel.Error)}\nMissing strings: {errors.Count(x => x.Level == ErrorLevel.MissingString)}";
            Bot.Api.EditMessageTextAsync(id, msgId, result, parseMode: ParseMode.Markdown);

        }

        internal static void UploadFile(string fileid, long id, string newFileCorrectName, int msgID)
        {
            var file = Bot.Api.GetFileAsync(fileid).Result;
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
            //first, let's load up the English file, which is our master file
            var langs = Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x));
            var master = XDocument.Load(Path.Combine(Bot.LanguageDirectory, "English.xml"));
            var newFile = new LangFile(newFilePath);

            //make sure it has a complete langnode
            CheckLanguageNode(newFile, newFileErrors);

            //test the length
            TestLength(newFile, newFileErrors);

            //check uniqueness
            var error = langs.FirstOrDefault(x =>
                    (x.FileName.ToLower() == newFile.FileName.ToLower() && x.Name != newFile.Name) //check for matching filename and mismatching name
                    || (x.Name == newFile.Name && (x.Base != newFile.Base || x.Variant != newFile.Variant)) //check for same name and mismatching base-variant
                    || (x.Base == newFile.Base && x.Variant == newFile.Variant && x.FileName != newFile.FileName) //check for same base-variant and mismatching filename
                    //if we want to have the possibility to rename the file, change previous line with FileName -> Name
            );
            if (error != null)
            {
                //problem....
                newFileErrors.Add(new LanguageError(newFile.FileName, "*Language Node*",
                    $"ERROR: The following file partially matches the same language node. Please check the file name, and the language name, base and variant. Aborting.\n\n*{error.FileName}.xml*\n_Name:_{error.Name}\n_Base:_{error.Base}\n_Variant:_{error.Variant}", ErrorLevel.FatalError));
            }

            //get the errors in it
            GetFileErrors(newFile, newFileErrors, master);
            
            //need to get the current file
            var curFile = langs.FirstOrDefault(x => x.Name == newFile.Name);
            var curFileErrors = new List<LanguageError>();

            if (curFile != null)
            {
                //test the length
                TestLength(curFile, curFileErrors);

                ////validate current file name / base / variants match
                //if (newFile.Base != lang.Base)
                //{
                //    newFileErrors.Add(new LanguageError(curFileName, "Language Node", $"Mismatched Base! {newFile.Base} - {lang.Base}", ErrorLevel.Error));
                //}
                //if (newFile.Variant != lang.Variant)
                //{
                //    newFileErrors.Add(new LanguageError(curFileName, "Language Node", $"Mismatched Variant! {newFile.Variant} - {lang.Variant}", ErrorLevel.Error));
                //}

                //get the errors in it
                GetFileErrors(curFile, curFileErrors, master);
            }

            //send the validation result
            Bot.Api.SendTextMessageAsync(id, OutputResult(newFile, newFileErrors, curFile, curFileErrors), parseMode: ParseMode.Markdown);
            Thread.Sleep(500);


            if (newFileErrors.All(x => x.Level != ErrorLevel.FatalError && x.Level != ErrorLevel.Ads))
            {
                //load up each file and get the names
                var buttons = new[]
                {
                    new InlineKeyboardCallbackButton($"New", $"upload|{id}|{newFile.FileName}"),
                    new InlineKeyboardCallbackButton($"Old", $"upload|{id}|current")
                };
                var menu = new InlineKeyboardMarkup(buttons.ToArray());
                Bot.Api.SendTextMessageAsync(id, "Which file do you want to keep?", replyToMessageId: msgID,
                    replyMarkup: menu);
            }
            else
            {
                Bot.Api.SendTextMessageAsync(id, "Fatal errors present, cannot upload.", replyToMessageId: msgID);
            }
        }

        

        public static void UseNewLanguageFile(string fileName, long id, int msgId)
        {
            var msg = "Moving file to production..\n";
            msg += "Checking paths for duplicate language file...\n";
            Bot.Api.EditMessageTextAsync(id, msgId, msg);
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
                    Bot.Api.EditMessageTextAsync(id, msgId, msg);
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
            //Bot.Api.EditMessageTextAsync(id, msgId, msg);
//#if RELEASE
//            copyToPath = copyToPath.Replace("Werewolf 3.0", "Werewolf 3.0 Clone");
//            System.IO.File.Copy(newFilePath, copyToPath, true);
//            msg += $"File copied to bot 2\n";
//            Bot.Api.EditMessageTextAsync(id, msgId, msg);
//#endif
            var gitPath = Path.Combine(@"C:\Werewolf Source\Werewolf\Werewolf for Telegram\Languages", Path.GetFileName(copyToPath));
            File.Copy(newFilePath, gitPath, true);
            System.IO.File.Delete(newFilePath);
            msg += $"File copied to git directory\n";
            if (newFilePath.EndsWith("English.xml"))
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        FileName = @"C:\Werewolf Source\Werewolf\Werewolf for Telegram\Languages\commit.bat",
                        Arguments = $"\"Syncing langfiles from Telegram (English.xml update)\"",
                        WorkingDirectory = @"C:\Werewolf Source\Werewolf\Werewolf for Telegram\Languages",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                p.Start();
                msg += "Started the committing process. Reading output from git...";
                Bot.Edit(id, msgId, msg);

                var output = "";
                while (!p.StandardOutput.EndOfStream)
                    output += p.StandardOutput.ReadLine() + Environment.NewLine;
                while (!p.StandardError.EndOfStream)
                    output += p.StandardError.ReadLine() + Environment.NewLine;

                msg += "\nValidating the output...";
                Bot.Edit(id, msgId, msg);

                //validate the output
                if (output.Contains("failed"))
                {
                    msg += "\n<b>Failed</b> to commit files. See control output for information";
                    Console.WriteLine(output);
                }
                else if (output.Contains("nothing to commit"))
                {
                    msg += "\nNothing to commit.";
                }
                else
                {
                    //try to grab the commit
                    var regex = new Regex("(\\[master .*])");
                    var match = regex.Match(output);
                    var commit = "";
                    if (match.Success)
                    {
                        commit = match.Value.Replace("[master ", "").Replace("]", "");
                    }
                    msg += $"\n<b>Files committed successfully.</b> {(String.IsNullOrEmpty(commit) ? "" : $"<a href=\"https://github.com/GreyWolfDev/Werewolf/commit/" + commit + $"\">{commit}</a>")}";
                }
            }
            msg += "\n<b>Operation complete.</b>";

            Bot.Api.EditMessageTextAsync(id, msgId, msg, parseMode: ParseMode.Html);
        }

        public static void SendAllFiles(long id)
        {

            //need to zip up all the files
            var path = Path.Combine(Bot.RootDirectory, "languages.zip");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            //create our zip file
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var langs = Directory.GetFiles(Bot.LanguageDirectory);
                foreach (var lang in langs)
                    zip.CreateEntryFromFile(lang, lang, CompressionLevel.Optimal); //add the langs to the zipfile
            }
            
            //now send the file
            var fs = new FileStream(path, FileMode.Open);
            Bot.Api.SendDocumentAsync(id, new FileToSend("languages.zip", fs));
        }

        public static void SendFile(long id, string choice)
        {
            var langOptions = Directory.GetFiles(Bot.LanguageDirectory).Select(x => new LangFile(x));
            var option = langOptions.First(x => x.Name == choice);
            var fs = new FileStream(option.FilePath, FileMode.Open);
            Bot.Api.SendDocumentAsync(id, new FileToSend(option.FileName + ".xml", fs));
        }
        
        internal static void SendBase(string choice, long id)
        {
            try
            {
                var zipname = new Regex("[^a-zA-Z0-9]").Replace(choice, "_"); //get rid of non-alphanumeric characters which can cause trouble
                var path = Path.Combine(Bot.LanguageDirectory, $"BaseZips\\{zipname}.zip"); //where the zipfile will be stored
                if (File.Exists(path))
                    File.Delete(path);

                //create our zip file
                using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    var langs = Directory.GetFiles(Bot.LanguageDirectory).Select(x => new LangFile(x)).Where(x => x.Base == choice); //get the base
                    foreach (var lang in langs)
                        zip.CreateEntryFromFile(Path.Combine(Bot.LanguageDirectory, $"{lang.FileName}.xml"), $"{lang.FileName}.xml", CompressionLevel.Optimal); //add the langs to the zipfile
                }
                //now send the zip file
                var fs = new FileStream(path, FileMode.Open);
                Bot.Api.SendDocumentAsync(id, new FileToSend($"{zipname}.zip", fs));
                
            }
            catch (Exception e)
            {
                Bot.Api.SendTextMessageAsync(id, e.Message);
            }
        }

        #region Helpers

        private static string GetLocaleString(string key, XDocument file)
        {
            var strings = file.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key);
            var values = strings.Descendants("value");
            return values.First().Value;
        }

        private static void CheckLanguageNode(LangFile langfile, List<LanguageError> errors)
        {
            
            if (String.IsNullOrWhiteSpace(langfile.Name))
                errors.Add(new LanguageError(langfile.FileName, "*Language Node*", "Language name is missing", ErrorLevel.FatalError));
            if (String.IsNullOrWhiteSpace(langfile.Base))
                errors.Add(new LanguageError(langfile.FileName, "*Language Node*", "Base is missing", ErrorLevel.FatalError));
            if (String.IsNullOrWhiteSpace(langfile.Variant))
                errors.Add(new LanguageError(langfile.FileName, "*Language Node*", "Variant is missing", ErrorLevel.FatalError));
        }

        private static string OutputResult(LangFile newFile, List<LanguageError> newFileErrors, LangFile curFile, List<LanguageError> curFileErrors)
        {
            var result = $"NEW FILE\n*{newFile.FileName}.xml - ({newFile.Name ?? ""})*" + Environment.NewLine;
            if (newFileErrors.Any(x => x.Level == ErrorLevel.Ads))
            {
                result += "*ADS DETECTED*\n";
                result = newFileErrors.Where(x => x.Level == ErrorLevel.Error).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n");
            }
            else
            {
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
                if (newFileErrors.Any(x => x.Level == ErrorLevel.DuplicatedString))
                {
                    result += "\n_Warning:_\n";
                    result = newFileErrors.Where(x => x.Level == ErrorLevel.DuplicatedString).Aggregate(result, (current, fileError) => current + $"{fileError.Message}\n");
                    result += "The second instance of the string won't be used, unless you move one of the two values inside the other. Check the latest English file to see how this is fixed.\n\n";
                }
                if (newFileErrors.Any(x => x.Level == ErrorLevel.JoinLink))
                {
                    result += "\n_Join commands detected:_\n";
                    result = newFileErrors.Where(x => x.Level == ErrorLevel.JoinLink).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n");
                    result += "These strings won't be used, and will fall back to English, until you remove `/join` from them.\n\n";
                }
                if (newFileErrors.Any(x => x.Level == ErrorLevel.FatalError))
                {
                    result += "\n*Fatal errors:*\n";
                    result = newFileErrors.Where(x => x.Level == ErrorLevel.FatalError).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n{fileError.Message}\n\n");
                }
                if (newFileErrors.Count == 0)
                {
                    result += "_No errors_\n";
                }
            }
            if (curFile != null)
            {
                result += "\n\n";
                result += $"OLD FILE (Last updated: {curFile.LatestUpdate.ToString("MMM dd")})\n*{curFile.FileName}.xml - ({curFile.Name})*\n";
                result +=
                    $"Errors: {curFileErrors.Count(x => x.Level == ErrorLevel.Error)}\nMissing strings: {curFileErrors.Count(x => x.Level == ErrorLevel.MissingString)}";
            }
            else
            {
                result += "\n\n*No old file, this is a new language*";
                result += "\nPlease double check the filename, and the language name, base and variant, as you won't be able to change them.";
                result += $"\n_Name:_ {newFile.Name ?? ""}";
                result += $"\n_Base:_ {newFile.Base ?? ""}";
                if (!Directory.GetFiles(Bot.LanguageDirectory, "*.xml").Select(x => new LangFile(x)).Any(x => x.Base == newFile.Base))
                    result += " *(NEW)*";
                result += $"\n_Variant:_ {newFile.Variant ?? ""}";
            }

            return result;
        }

        private static void TestLength(LangFile file, List<LanguageError> fileErrors)
        {
            var test = $"setlang|-1001049529775|{file.Base ?? ""}|{file.Variant ?? ""}|v";
            var count = Encoding.UTF8.GetByteCount(test);
            if (count > 64)
                fileErrors.Add(new LanguageError(file.FileName, "*Language Node*", "Base and variant are too long. (*38 utf8 byte max*)", ErrorLevel.FatalError));
        }

        private static void GetFileErrors(LangFile file, List<LanguageError> fileErrors, XDocument master)
        {
            var masterStrings = master.Descendants("string");

            //check for CultConvertSerialKiller & CupidChosen duplication
            var dup = file.Doc.Descendants("string").Count(x => x.Attribute("key").Value == "CultConvertSerialKiller");
            if (dup > 1)
                fileErrors.Add(new LanguageError(file.FileName, "CultConvertSerialKiller", "CultConvertSerialKiller duplication", ErrorLevel.DuplicatedString));
            dup = file.Doc.Descendants("string").Count(x => x.Attribute("key").Value == "CupidChosen");
            if (dup > 1)
                fileErrors.Add(new LanguageError(file.FileName, "CupidChosen", "CupidChosen duplication", ErrorLevel.DuplicatedString));

            foreach (var str in masterStrings)
            {
                var key = str.Attribute("key").Value;
                var isgif = str.Attributes().Any(x => x.Name == "isgif");
                var deprecated = str.Attributes().Any(x => x.Name == "deprecated");
                //get the english string
                //get the locale values
                var masterString = GetLocaleString(key, master);
                var values = file.Doc.Descendants("string")
                        .FirstOrDefault(x => x.Attribute("key").Value == key)?
                        .Descendants("value");
                if (values == null)
                {
                    if (!deprecated)
                        fileErrors.Add(new LanguageError(file.FileName, key, $"Values missing"));
                    continue;
                }
                //check master string for {#} values
                int vars = 0;
                for (int i = 0; i < 5; i++)
                    if (masterString.Contains("{" + i + "}"))
                        vars = i + 1;

                foreach (var value in values)
                {
                    for (int i = 0; i <= 5 - 1; i++)
                    {
                        if (!value.Value.Contains("{" + i + "}") && vars - 1 >= i)
                        {
                            //missing a value....
                            fileErrors.Add(new LanguageError(file.FileName, key, "Missing {" + i + "}", ErrorLevel.Error));
                        }
                        else if (value.Value.Contains("{" + i + "}") && vars - 1 < i)
                        {
                            fileErrors.Add(new LanguageError(file.FileName, key, "Extra {" + i + "}", ErrorLevel.Error));
                        }
                    }

                    if (isgif && value.Value.Length > 200)
                    {
                        fileErrors.Add(new LanguageError(file.FileName, key, "GIF string length cannot exceed 200 characters", ErrorLevel.FatalError));
                    }

                    if (value.Value.ToLower().Contains("/join") && !fileErrors.Any(x => x.File == file.FileName && x.Key == key && x.Level == ErrorLevel.JoinLink))
                        fileErrors.Add(new LanguageError(file.FileName, key, "", ErrorLevel.JoinLink));

                    if (new Regex(@"((https?:\/\/)?t(elegram)?\.me\/joinchat\/([a-zA-Z0-9_\-]+))|( @(?!werewolfbot)(?!werewolfbetabot)(?!werewolfdev)(?!werewolfsupport)(?!para949)(?!greywolfdev)\w)", RegexOptions.IgnoreCase).IsMatch(value.Value))
                    {
                        fileErrors.Add(new LanguageError(file.FileName, key, "No usernames or links in the langfile!", ErrorLevel.Ads));
                    }
                }
            }
        }


        #endregion
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
        DuplicatedString, MissingString, Error, FatalError, JoinLink,
        Ads
    }
}
