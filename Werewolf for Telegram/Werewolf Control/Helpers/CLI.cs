using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Channels;
using TeleSharp.TL.Messages;
using TLSharp.Core;
using Werewolf_Control.Models;

namespace Werewolf_Control.Helpers
{
    public static class CLI
    {
        private static TelegramClient client;
        internal static string AuthCode = null;
        public static Task Initialize()
        {
            client = new TelegramClient(Int32.Parse(RegHelper.GetRegValue("appid")), RegHelper.GetRegValue("apihash"));
            return client.ConnectAsync();
        }

        private static async Task<bool> AuthUser()
        {
            if (client == null || !client.IsUserAuthorized())
            {
                if (client == null)
                    await Initialize();

                if (client.IsUserAuthorized()) return true;
                var phone = RegHelper.GetRegValue("paraphone");
                var hash = await client.SendCodeRequestAsync(phone);
                await Bot.Send($"Registering bot with phone {phone}, hash code {hash}", UpdateHelper.Devs[0]);
                await Bot.Send("Please reply to this message with your Telegram authorization code", UpdateHelper.Devs[0]);
                while (AuthCode == null)
                {
                    await Task.Delay(500);
                }
                try
                {
                    var user = await client.MakeAuthAsync(phone, hash, AuthCode);
                    await Bot.Send($"Signed in as {user.first_name}", UpdateHelper.Devs[0]);
                }
                catch(Exception e)
                {
                    await Bot.Send(e.Message, UpdateHelper.Devs[0]);
                    AuthCode = null;
                    return false;
                }
            }
            return true;
        }

        public static async Task<ChannelInfo> GetChatInfo(string groupName)
        {
            if (! await AuthUser()) return null;
            var result = new ChannelInfo();
            var dialogs = (TLDialogs)await client.GetUserDialogsAsync();
            var main = dialogs.chats.lists.Where(c => c.GetType() == typeof(TLChannel))
                        .Cast<TLChannel>()
                        .FirstOrDefault(c => c.title == ("WereWuff - The Game"));
            var req = new TLRequestGetFullChannel()
            {
                channel = new TLInputChannel() { access_hash = main.access_hash.Value, channel_id = main.id }
            };

            var res = await client.SendRequestAsync<TeleSharp.TL.Messages.TLChatFull>(req);

            //we have to do this in slices
            var offset = 0;
            result.Channel = main;
            result.ChatFull = res;
            while (offset < (res.full_chat as TLChannelFull).participants_count)
            {
                var pReq = new TLRequestGetParticipants()
                {
                    channel = new TLInputChannel() { access_hash = main.access_hash.Value, channel_id = main.id },
                    filter = new TLChannelParticipantsRecent() { },
                    limit = 200,
                    offset = offset
                };
                var pRes = await client.SendRequestAsync<TLChannelParticipants>(pReq);
                result.Users.AddRange(pRes.users.lists.Cast<TLUser>());
                offset += 200;
                await Task.Delay(500);
            }

            return result;
        }
    }
}
