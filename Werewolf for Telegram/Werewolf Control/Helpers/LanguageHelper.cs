using System;
using System.Collections.Generic;
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
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Handler;
using File = System.IO.File;

namespace Werewolf_Control.Helpers
{
    public static class LanguageHelper
    {
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

            var doc = XDocument.Load(newFilePath);
            //first check the language node
            var langNode = doc.Descendants("language").First();
            if (langNode.Attributes().All(x => x.Name != "base"))
            {
                newFileErrors.Add(new LanguageError(newFileName, "language node", "base attribute is missing", ErrorLevel.Error));
            }
            if (langNode.Attributes().All(x => x.Name != "variant"))
            {
                newFileErrors.Add(new LanguageError(newFileName, "language node", "variant attribute is missing", ErrorLevel.Error));
            }

            //now test the length
            if (langNode.Attributes().Any(x => x.Name == "base") &&
                langNode.Attributes().Any(x => x.Name == "variant"))
            {
                var test = $"setlang|-1001049529775|{langNode.Attribute("base").Value}|{langNode.Attribute("variant").Value}|v";
                var count = Encoding.UTF8.GetByteCount(test);
                if (count > 64)
                    newFileErrors.Add(new LanguageError(newFileName, "language node", "base and variant are too long. (*38 utf8 byte max*)", ErrorLevel.Error));
            }
            var newFileLang = doc.Descendants("language")
                .First()
                .Attribute("name")
                .Value;
            //check for CultConvertHunter duplication
            var dup = doc.Descendants("string").Count(x => x.Attribute("key").Value == "CultConvertHunter");
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
                var values = doc.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key)?.Descendants("value");
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

            var langs = Directory.GetFiles(Bot.LanguageDirectory)
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
                                                    FilePath = x
                                                });
            var lang = langs.FirstOrDefault(x => x.Name == newFileLang);
            var curFileName = "";
            if (lang != null)
            {


                curFileName = Path.GetFileNameWithoutExtension(lang.FilePath);
                doc = XDocument.Load(lang.FilePath);
                langNode = doc.Descendants("language").First();
                if (langNode.Attributes().All(x => x.Name != "base"))
                {
                    curFileErorrs.Add(new LanguageError(newFileName, "language node", "base attribute is missing", ErrorLevel.Error));
                }
                if (langNode.Attributes().All(x => x.Name != "variant"))
                {
                    curFileErorrs.Add(new LanguageError(newFileName, "language node", "variant attribute is missing", ErrorLevel.Error));
                }
                if (langNode.Attributes().Any(x => x.Name == "base") &&
                langNode.Attributes().Any(x => x.Name == "variant"))
                {
                    var test = $"setlang|-1001049529775|{langNode.Attribute("base").Value}|{langNode.Attribute("variant").Value}|v";
                    var count = Encoding.UTF8.GetByteCount(test);
                    if (count > 64)
                        curFileErorrs.Add(new LanguageError(curFileName, "language node", "base and variant are too long. (*38 utf8 byte max*)", ErrorLevel.Error));
                }
                foreach (var str in masterStrings)
                {
                    var key = str.Attribute("key").Value;
                    var isgif = str.Attributes().Any(x => x.Name == "isgif");
                    //get the english string
                    //get the locale values
                    var masterString = GetLocaleString(key, master);
                    var values =
                        doc.Descendants("string")
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
            var result = $"NEW FILE\n*{newFileName} - ({newFileLang})*" + Environment.NewLine;
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
                result += $"CURRENT FILE\n*{curFileName} - ({lang.Name})*\n";
                result +=
                    $"Errors: {curFileErorrs.Count(x => x.Level == ErrorLevel.Error)}\nMissing strings: {curFileErorrs.Count(x => x.Level == ErrorLevel.MissingString)}";
            }
            else
            {
                result += "\n\n*No current file, this is a new language*";
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
            fileName += ".xml";
            var tempPath = Bot.TempLanguageDirectory;
            var langPath = Bot.LanguageDirectory;
            var newFilePath = Path.Combine(tempPath, fileName);
            var copyToPath = Path.Combine(langPath, fileName);
            //get the new files language
            var doc = XDocument.Load(newFilePath);

            var newFileLang = doc.Descendants("language")
                .First()
                .Attribute("name")
                .Value;

            //check for existing file
            var langs = Directory.GetFiles(langPath)
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
                                                    FilePath = x
                                                });
            var lang = langs.FirstOrDefault(x => x.Name == newFileLang);
            if (lang != null)
                copyToPath = lang.FilePath;
            System.IO.File.Copy(newFilePath, copyToPath, true);
            System.IO.File.Delete(newFilePath);

            Bot.Api.EditMessageText(id, msgId, "File moved to production folder.");
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
