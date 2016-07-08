using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using System.IO;
using System.Net;
using Database;

namespace Werewolf_Node.Helpers
{
    public static class ImageHelper
    {
        internal static string ImagePath = @"c:\inetpub\werewolf\Images\";
        public static async void GetUserImage(int userid)
        {
            using (var db = new WWContext())
            {
                var p = db.Players.FirstOrDefault(x => x.TelegramId == userid);
                if (p != null)
                {
                    var photos = await Program.Bot.GetUserProfilePhotos(userid, limit: 1);
                    if (photos.Photos.Length == 0) return;//nada
                    var sizes = photos.Photos[0];
                    var id = "";
                    var largest = 0;
                    foreach (var s in sizes)
                    {
                        if (s.FileSize > largest)
                            id = s.FileId;

                    }

                    if (String.IsNullOrEmpty(id))
                    {
                        return;
                    }


                    Telegram.Bot.Types.File file;
                    try
                    {
                        file = await Program.Bot.GetFile(id);

                        var photoPath = file.FilePath;
                        var fileName = photoPath.Substring(photoPath.LastIndexOf("/") + 1);
                        //check that the file name is different
                        if (p.ImageFile == fileName)
                            return; //same, no reason to download again

                        var uri = $"https://api.telegram.org/file/bot{Program.APIToken}/{photoPath}";

                        //write the new info to the database




                        //now download the photo to the path
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(new Uri(uri), ImagePath + fileName);
                        }
                        p.ImageFile = fileName;
                        db.SaveChanges();
                    }
                    catch
                    {
                        return;
                    }
                }
            }
        }
    }
}
