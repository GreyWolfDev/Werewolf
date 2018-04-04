﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;


namespace Telegram.Bot
{
    public class Client
    {
        private const string BaseUrl = "https://api.telegram.org/bot";
        private const string BaseFileUrl = "https://api.telegram.org/file/bot";
        public string LogDirectory;
        private Status _status = Status.Normal;
        private string LogPath => Path.Combine(LogDirectory, "getUpdates.log");
        private readonly string _token;
        private bool _invalidToken;

        /// <summary>
        /// Timeout for uploading Files/Videos/Documents etc.
        /// </summary>
        public TimeSpan UploadTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Timeout for long-polling
        /// </summary>
        public TimeSpan PollingTimeout { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Indecates if receiving updates
        /// </summary>
        public bool IsReceiving { get; set; }

        /// <summary>
        /// The current message offset
        /// </summary>
        public int MessageOffset { get; set; }

        #region Events

        protected virtual void OnUpdatesReceived(UpdatesReceivedEventArgs e)
        {
            UpdatesReceived?.Invoke(this, e);
        }
        protected virtual void OnStatusChanged(StatusChangeEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        protected virtual void OnUpdateReceived(UpdateEventArgs e)
        {
            UpdateReceived?.Invoke(this, e);

            switch (e.Update.Type)
            {
                case UpdateType.MessageUpdate:
                    MessageReceived?.Invoke(this, e);
                    break;

                case UpdateType.InlineQueryUpdate:
                    InlineQueryReceived?.Invoke(this, e);
                    break;

                case UpdateType.ChosenInlineResultUpdate:
                    ChosenInlineResultReceived?.Invoke(this, e);
                    break;

                case UpdateType.CallbackQueryUpdate:
                    CallbackQueryReceived?.Invoke(this, e);
                    break;
            }
        }

        protected virtual void OnReceiveError(ReceiveErrorEventArgs e)
        {
            ReceiveError?.Invoke(this, e);
        }

        /// <summary>
        /// Fired when any updates are availible
        /// </summary>
        public event EventHandler<UpdateEventArgs> UpdateReceived;

        /// <summary>
        /// Fired when status has changed
        /// </summary>
        public event EventHandler<StatusChangeEventArgs> StatusChanged;

        /// <summary>
        /// Fired when updates have been received
        /// </summary>
        public event EventHandler<UpdatesReceivedEventArgs> UpdatesReceived;

        /// <summary>
        /// Fired when messages are availible
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageReceived;

        /// <summary>
        /// Fired when inline queries are availible
        /// </summary>
        public event EventHandler<InlineQueryEventArgs> InlineQueryReceived;

        /// <summary>
        /// Fired when chosen inline results are availible
        /// </summary>
        public event EventHandler<ChosenInlineResultEventArgs> ChosenInlineResultReceived;

        /// <summary>
        /// Fired when an callback query is received
        /// </summary>
        public event EventHandler<CallbackQueryEventArgs> CallbackQueryReceived;

        public event EventHandler<ReceiveErrorEventArgs> ReceiveError;

        #endregion

        /// <summary>
        /// Creat new Telegram Bot Api Client
        /// </summary>
        /// <param name="token">API token</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="token"/> format is invvalid</exception>
        public Client(string token, string logDirectory)
        {
            LogDirectory = logDirectory;
            if (!Regex.IsMatch(token, @"^\d*:[\w\d-_]{35}$"))
                throw new ArgumentException("Invalid token format", nameof(token));

            _token = token;
        }

        #region Support Methods - Public

        /// <summary>
        /// Test the API token
        /// </summary>
        /// <returns><c>true</c> if token is valid</returns>
        public async Task<bool> TestApi()
        {
            try
            {
                await GetMeAsync().ConfigureAwait(false);
            }
            catch (ApiRequestException e) when (e.ErrorCode == 401)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Start update receiving
        /// </summary>
        /// <exception cref="ApiRequestException"> Thrown if token is invalid</exception>
        public void StartReceiving() => StartReceiving(PollingTimeout);

        /// <summary>
        /// Start update receiving
        /// </summary>
        /// <exception cref="ApiRequestException"> Thrown if token is invalid</exception>
        public void StartReceiving(TimeSpan timeout)
        {
            if (_invalidToken)
                throw new ApiRequestException("Invalid token", 401);

            PollingTimeout = timeout;
            IsReceiving = true;

            Receive();
        }

#pragma warning disable AsyncFixer03 // Avoid fire & forget async void methods

        private CancellationTokenSource cts;

        private async void Receive()
        {
            cts = new CancellationTokenSource();
            var sw = new Stopwatch();

            while (IsReceiving)
            {
                var timeout = Convert.ToInt32(PollingTimeout.TotalSeconds);

                try
                {
                    sw.Reset();
                    sw.Start();
                    var updates = await GetUpdatesAsync(MessageOffset, timeout: timeout).ConfigureAwait(false);
                    sw.Stop();
                    OnUpdatesReceived(new UpdatesReceivedEventArgs(updates.Length));
                    //check updates.Length

                    if (updates.Length == 0)
                    {
                       SetStatus(Status.NotReceiving);
                    }
                    else if (updates.Length == 100)
                    {
                        SetStatus(Status.Recovering);
                    }
                    else SetStatus(Status.Normal);


                    try
                    {
                        using (var s = new StreamWriter(LogPath, true))
                        {
                            s.WriteLine($"{DateTime.Now} - {sw.Elapsed.ToString("g")} - {updates.Length}");
                            s.Flush();
                        }
                    }
                    finally
                    {
                        
                    }
                    foreach (var update in updates)
                    {
                        OnUpdateReceived(new UpdateEventArgs(update));
                        MessageOffset = update.Id + 1;
                    }

                }
                catch (ApiRequestException e)
                {
                    sw.Stop();
                    SetStatus(Status.Error);
                    try
                    {
                        using (var s = new StreamWriter(LogPath, true))
                        {
                            s.WriteLine($"{DateTime.Now} - {sw.Elapsed.ToString("g")} - {e.Message}");
                            s.Flush();
                        }
                    }
                    finally
                    {
                        OnReceiveError(e);
                    }
                }
                catch (TaskCanceledException e)
                {
                    sw.Stop();
                    SetStatus(Status.Error);
                    try
                    {
                        using (var s = new StreamWriter(LogPath, true))
                        {
                            s.WriteLine($"{DateTime.Now} - {sw.Elapsed.ToString("g")} - {e.Message}");
                            s.Flush();
                        }
                    }
                    finally
                    {
                        // do nothing
                    }
                }
                catch (Exception e)
                {
                    sw.Stop();
                    SetStatus(Status.Error);
                    //well.  bad things happened.  ignore it this time I guess? At least until I can figure out what the error actually is
                    while (e.InnerException != null)
                        e = e.InnerException;
                    try
                    {
                        var path = Path.GetDirectoryName(LogPath);
                        Directory.CreateDirectory(path);
                        using (var s = new StreamWriter(LogPath, true))
                        {
                            s.WriteLine($"{DateTime.Now} - {sw.Elapsed.ToString("g")} - {e.Message}");
                            s.Flush();
                        }
                    }
                    finally
                    {
                        // do nothing
                    }
                    
                }

            }
        }
#pragma warning restore AsyncFixer03 // Avoid fire & forget async void methods

        /// <summary>
        /// Stop update receiving
        /// </summary>
        public void StopReceiving()
        {
            IsReceiving = false;
        }

        internal void SetStatus(Status status)
        {
            if (status == Status.Error)
                OnUpdatesReceived(new UpdatesReceivedEventArgs(0));
            if (_status != status)
            {
                _status = status;
                OnStatusChanged(new StatusChangeEventArgs(status));
            }
        }

        #endregion

        #region API Methods - General

        /// <summary>
        /// A simple method for testing your bot's auth token.
        /// </summary>
        /// <returns>Returns basic information about the bot in form of <see cref="User"/> object</returns>
        public Task<User> GetMeAsync() => SendWebRequestAsync<User>("getMe");

        /// <summary>
        /// Use this method to receive incoming updates using long polling.
        /// </summary>
        /// <param name="offset">
        /// Identifier of the first update to be returned.
        /// </param>
        /// <param name="limit">
        /// Limits the number of updates to be retrieved. Values between 1—100 are accepted. Defaults to 100
        /// </param>
        /// <param name="timeout">
        /// Timeout in seconds for long polling. Defaults to 0, i.e. usual short polling
        /// </param>
        /// <remarks>
        /// 1. This method will not work if an outgoing webhook is set up.
        /// 2. In order to avoid getting duplicate updates, recalculate offset after each server response.
        /// 
        /// Must be greater by one than the highest among the identifiers of previously received updates. 
        /// By default, updates starting with the earliest unconfirmed update are returned. An update is considered confirmed as soon as GetUpdates is called with an offset higher than its Id.
        /// </remarks>
        /// <returns>An Array of <see cref="Update"/> is returned.</returns>
        public Task<Update[]> GetUpdatesAsync(int offset = 0, int limit = 100, int timeout = 0)
        {
            var parameters = new Dictionary<string, object>
            {
                {"offset", offset},
                {"limit", limit},
                {"timeout", timeout}
            };

            return SendWebRequestAsync<Update[]>("getUpdates", parameters);
        }

        /// <summary>
        /// Use this method to specify a url and receive incoming updates via an outgoing webhook. Whenever there is an update for the bot, we will
        /// send an HTTPS POST request to the specified url, containing a JSON-serialized Update. In case of an unsuccessful request, we will
        /// give up after a reasonable amount of attempts.
        /// </summary>
        /// <param name="url">Optional. HTTPS url to send updates to. Use an empty string to remove webhook integration</param>
        /// <param name="certificate">Upload your public key certificate so that the root certificate in use can be checked</param>
        /// <remarks>
        /// 1. You will not be able to receive updates using getUpdates for as long as an outgoing webhook is set up.
        /// 2. We currently do not support self-signed certificates.
        /// 3. For the moment, the only supported port for Webhooks is 443. We may support additional ports later.
        /// </remarks>
        public Task SetWebhookAsync(string url = "", FileToSend? certificate = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"url", url}
            };

            if (certificate != null)
                parameters.Add("certificate", certificate);

            return SendWebRequestAsync<bool>("setWebhook", parameters);
        }

        #endregion

        #region API Methods - SendMessages

        /// <summary>
        /// Use this method to send text messages. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="text">Text of the message to be sent</param>
        /// <param name="disableWebPagePreview">Optional. Disables link previews for links in this message</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <param name="parseMode">Optional. Change, if you want Telegram apps to show bold, italic, fixed-width text or inline URLs in your bot's message.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendTextMessageAsync(long chatId, string text, bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null,
            ParseMode parseMode = ParseMode.Default)
            =>
                SendTextMessageAsync(chatId.ToString(), text, disableWebPagePreview, disableNotification, replyToMessageId,
                    replyMarkup, parseMode);

        /// <summary>
        /// Use this method to send text messages. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">username of the target channel (in the format @channelusername)</param>
        /// <param name="text">Text of the message to be sent</param>
        /// <param name="disableWebPagePreview">Optional. Disables link previews for links in this message</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <param name="parseMode">Optional. Change, if you want Telegram apps to show bold, italic, fixed-width text or inline URLs in your bot's message.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendTextMessageAsync(string chatId, string text, bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null,
            ParseMode parseMode = ParseMode.Default)
        {
            var additionalParameters = new Dictionary<string, object>();

            if (disableWebPagePreview)
                additionalParameters.Add("disable_web_page_preview", true);

            if (parseMode != ParseMode.Default)
                additionalParameters.Add("parse_mode", parseMode.ToModeString());

            return SendMessageAsync(MessageType.TextMessage, chatId, text, disableNotification, replyToMessageId, replyMarkup,
                additionalParameters);
        }

        /// <summary>
        /// Use this method to forward messages of any kind. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="fromChatId">Unique identifier for the chat where the original message was sent</param>
        /// <param name="messageId">Unique message identifier</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> ForwardMessageAsync(long chatId, long fromChatId, int messageId,
            bool disableNotification = false)
            => ForwardMessageAsync(chatId.ToString(), fromChatId.ToString(), messageId, disableNotification);

        /// <summary>
        /// Use this method to forward messages of any kind. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="fromChatId">channel username in the format @channelusername</param>
        /// <param name="messageId">Unique message identifier</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> ForwardMessageAsync(long chatId, string fromChatId, int messageId,
            bool disableNotification = false)
            => ForwardMessageAsync(chatId.ToString(), fromChatId, messageId, disableNotification);

        /// <summary>
        /// Use this method to forward messages of any kind. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">username of the target channel (in the format @channelusername)</param>
        /// <param name="fromChatId">Unique identifier for the chat where the original message was sent</param>
        /// <param name="messageId">Unique message identifier</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> ForwardMessageAsync(string chatId, long fromChatId, int messageId,
            bool disableNotification = false)
            => ForwardMessageAsync(chatId, fromChatId.ToString(), messageId, disableNotification);

        /// <summary>
        /// Use this method to forward messages of any kind. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">username of the target channel (in the format @channelusername)</param>
        /// <param name="fromChatId">channel username in the format @channelusername</param>
        /// <param name="messageId">Unique message identifier</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> ForwardMessageAsync(string chatId, string fromChatId, int messageId,
            bool disableNotification = false)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId},
                {"from_chat_id", fromChatId},
                {"message_id", messageId},
            };

            if (disableNotification)
                parameters.Add("disable_notification", true);

            return SendWebRequestAsync<Message>("forwardMessage", parameters);
        }

        /// <summary>
        /// Use this method to send photos. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="photo">Photo to send. You can either pass a file_id as String to resend a photo that is already on the Telegram servers, or upload a new photo using multipart/form-data.</param>
        /// <param name="caption">Optional. Photo caption (may also be used when resending photos by file_id).</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendPhotoAsync(long chatId, FileToSend photo, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            => SendPhotoAsync(chatId.ToString(), photo, caption, disableNotification, replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send photos. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="photo">Photo to send. You can either pass a file_id as String to resend a photo that is already on the Telegram servers, or upload a new photo using multipart/form-data.</param>
        /// <param name="caption">Optional. Photo caption (may also be used when resending photos by file_id).</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendPhotoAsync(string chatId, FileToSend photo, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"caption", caption}
            };

            return SendMessageAsync(MessageType.PhotoMessage, chatId, photo, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method to send photos. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="photo">Photo to send. You can either pass a file_id as String to resend a photo that is already on the Telegram servers, or upload a new photo using multipart/form-data.</param>
        /// <param name="caption">Optional. Photo caption (may also be used when resending photos by file_id).</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendPhotoAsync(long chatId, string photo, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            => SendPhotoAsync(chatId.ToString(), photo, caption, disableNotification, replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send photos. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="photo">Photo to send. You can either pass a file_id as String to resend a photo that is already on the Telegram servers, or upload a new photo using multipart/form-data.</param>
        /// <param name="caption">Optional. Photo caption (may also be used when resending photos by file_id).</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendPhotoAsync(string chatId, string photo, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"caption", caption}
            };

            return SendMessageAsync(MessageType.PhotoMessage, chatId, photo, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method to send audio files, if you want Telegram clients to display them in the music player. Your audio must be in the .mp3 format. On success, the sent Message is returned. Bots can currently send audio files of up to 50 MB in size, this limit may be changed in the future.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="audio">Audio file to send. You can either pass a file_id as String to resend an audio that is already on the Telegram servers, or upload a new audio file using multipart/form-data.</param>
        /// <param name="duration">Duration of the audio in seconds</param>
        /// <param name="performer">Performer</param>
        /// <param name="title">Track name</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendAudioAsync(long chatId, FileToSend audio, int duration, string performer, string title,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendAudioAsync(chatId.ToString(), audio, duration, performer, title, disableNotification, replyToMessageId,
                    replyMarkup);

        /// <summary>
        /// Use this method to send audio files, if you want Telegram clients to display them in the music player. Your audio must be in the .mp3 format. On success, the sent Message is returned. Bots can currently send audio files of up to 50 MB in size, this limit may be changed in the future.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="audio">Audio file to send. You can either pass a file_id as String to resend an audio that is already on the Telegram servers, or upload a new audio file using multipart/form-data.</param>
        /// <param name="duration">Duration of the audio in seconds</param>
        /// <param name="performer">Performer</param>
        /// <param name="title">Track name</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendAudioAsync(string chatId, FileToSend audio, int duration, string performer, string title,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"duration", duration},
                {"performer", performer},
                {"title", title}
            };

            return SendMessageAsync(MessageType.AudioMessage, chatId, audio, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method to send audio files, if you want Telegram clients to display the file as a playable voice message. For this to work, your audio must be in an .ogg file encoded with OPUS (other formats may be sent as Document). On success, the sent Message is returned. Bots can send audio files of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="audio">Audio file to send. You can either pass a file_id as String to resend an audio that is already on the Telegram servers, or upload a new audio file using multipart/form-data.</param>
        /// <param name="duration">Duration of the audio in seconds</param>
        /// <param name="performer">Performer</param>
        /// <param name="title">Track name</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendAudioAsync(long chatId, string audio, int duration, string performer, string title,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendAudioAsync(chatId.ToString(), audio, duration, performer, title, disableNotification, replyToMessageId,
                    replyMarkup);

        /// <summary>
        /// Use this method to send audio files, if you want Telegram clients to display the file as a playable voice message. For this to work, your audio must be in an .ogg file encoded with OPUS (other formats may be sent as Document). On success, the sent Message is returned. Bots can send audio files of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="audio">Audio file to send. You can either pass a file_id as String to resend an audio that is already on the Telegram servers, or upload a new audio file using multipart/form-data.</param>
        /// <param name="duration">Duration of the audio in seconds</param>
        /// <param name="performer">Performer</param>
        /// <param name="title">Track name</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendAudioAsync(string chatId, string audio, int duration, string performer, string title,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"duration", duration},
                {"performer", performer},
                {"title", title}
            };

            return SendMessageAsync(MessageType.AudioMessage, chatId, audio, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method to send phone contacts.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="phoneNumber">Contact's phone number</param>
        /// <param name="firstName">Contact's first name</param>
        /// <param name="lastName">Contact's last name</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Additional interface options. A JSON-serialized object for an inline keyboard, custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendContactAsync(long chatId, string phoneNumber, string firstName, string lastName = null,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendContactAsync(chatId.ToString(), phoneNumber, firstName, lastName, disableNotification, replyToMessageId,
                    replyMarkup);

        /// <summary>
        /// Use this method to send phone contacts.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="phoneNumber">Contact's phone number</param>
        /// <param name="firstName">Contact's first name</param>
        /// <param name="lastName">Contact's last name</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Additional interface options. A JSON-serialized object for an inline keyboard, custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendContactAsync(string chatId, string phoneNumber, string firstName, string lastName = null,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"first_name", firstName}
            };

            if (!string.IsNullOrWhiteSpace(lastName))
                parameters.Add("last_name", lastName);

            return SendMessageAsync(MessageType.ContactMessage, chatId, phoneNumber, disableNotification, replyToMessageId,
                replyMarkup, parameters);
        }

        /// <summary>
        /// Use this method to send general files. On success, the sent Message is returned. Bots can send files of any type of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="document">File to send. You can either pass a file_id as String to resend a file that is already on the Telegram servers, or upload a new file using multipart/form-data.</param>
        /// <param name="caption">Document caption</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendDocumentAsync(long chatId, FileToSend document, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            => SendDocumentAsync(chatId.ToString(), document, caption, disableNotification, replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send general files. On success, the sent Message is returned. Bots can send files of any type of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="document">File to send. You can either pass a file_id as String to resend a file that is already on the Telegram servers, or upload a new file using multipart/form-data.</param>
        /// <param name="caption">Document caption</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendDocumentAsync(string chatId, FileToSend document, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"caption", caption}
            };

            return SendMessageAsync(MessageType.DocumentMessage, chatId, document, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }


        /// <summary>
        /// Use this method to send general files. On success, the sent Message is returned. Bots can send files of any type of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="document">File to send. You can either pass a file_id as String to resend a file that is already on the Telegram servers, or upload a new file using multipart/form-data.</param>
        /// <param name="caption">Document caption</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendDocumentAsync(long chatId, string document, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            => SendDocumentAsync(chatId.ToString(), document, caption, disableNotification, replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send general files. On success, the sent Message is returned. Bots can send files of any type of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="document">File to send. You can either pass a file_id as String to resend a file that is already on the Telegram servers, or upload a new file using multipart/form-data.</param>
        /// <param name="caption">Document caption</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendDocumentAsync(string chatId, string document, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"caption", caption}
            };

            return SendMessageAsync(MessageType.DocumentMessage, chatId, document, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method to send .webp stickers. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="sticker">Sticker to send. You can either pass a file_id as String to resend a sticker that is already on the Telegram servers, or upload a new sticker using multipart/form-data.</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendStickerAsync(long chatId, FileToSend sticker,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendMessageAsync(MessageType.StickerMessage, chatId.ToString(), sticker, disableNotification,
                    replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send .webp stickers. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="sticker">Sticker to send. You can either pass a file_id as String to resend a sticker that is already on the Telegram servers, or upload a new sticker using multipart/form-data.</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendStickerAsync(string chatId, FileToSend sticker,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendMessageAsync(MessageType.StickerMessage, chatId, sticker, disableNotification, replyToMessageId,
                    replyMarkup);

        /// <summary>
        /// Use this method to send .webp stickers. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="sticker">Sticker to send. You can either pass a file_id as String to resend a sticker that is already on the Telegram servers, or upload a new sticker using multipart/form-data.</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendStickerAsync(long chatId, string sticker,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendMessageAsync(MessageType.StickerMessage, chatId.ToString(), sticker, disableNotification,
                    replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send .webp stickers. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="sticker">Sticker to send. You can either pass a file_id as String to resend a sticker that is already on the Telegram servers, or upload a new sticker using multipart/form-data.</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendStickerAsync(string chatId, string sticker,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendMessageAsync(MessageType.StickerMessage, chatId, sticker, disableNotification, replyToMessageId,
                    replyMarkup);

        /// <summary>
        /// Use this method to send information about a venue.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="latitude">Latitude of the venue</param>
        /// <param name="longitude">Longitude of the venue</param>
        /// <param name="title">Name of the venue</param>
        /// <param name="address">Address of the venue</param>
        /// <param name="foursquareId">Foursquare identifier of the venue</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVenueAsync(long chatId, float latitude, float longitude, string title, string address,
            string foursquareId = null,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendVenueAsync(chatId.ToString(), latitude, longitude, title, address, foursquareId, disableNotification,
                    replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send information about a venue.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="latitude">Latitude of the venue</param>
        /// <param name="longitude">Longitude of the venue</param>
        /// <param name="title">Name of the venue</param>
        /// <param name="address">Address of the venue</param>
        /// <param name="foursquareId">Foursquare identifier of the venue</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVenueAsync(string chatId, float latitude, float longitude, string title, string address,
            string foursquareId = null,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"longitude", longitude},
                {"title", title},
                {"address", address}
            };

            if (!string.IsNullOrWhiteSpace(foursquareId))
                parameters.Add("foursquare_id", foursquareId);

            return SendMessageAsync(MessageType.VenueMessage, chatId, latitude, disableNotification, replyToMessageId,
                replyMarkup, parameters);
        }

        /// <summary>
        /// Use this method to send video files, Telegram clients support mp4 videos (other formats may be sent as Document). On success, the sent Message is returned. Bots can send video files of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="video">Video to send. You can either pass a file_id as String to resend a video that is already on the Telegram servers, or upload a new video file using multipart/form-data.</param>
        /// <param name="duration">Duration of sent video in seconds</param>
        /// <param name="caption">Video caption</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVideoAsync(long chatId, FileToSend video, int duration = 0, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendVideoAsync(chatId.ToString(), video, duration, caption, disableNotification, replyToMessageId,
                    replyMarkup);

        /// <summary>
        /// Use this method to send video files, Telegram clients support mp4 videos (other formats may be sent as Document). On success, the sent Message is returned. Bots can send video files of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="video">Video to send. You can either pass a file_id as String to resend a video that is already on the Telegram servers, or upload a new video file using multipart/form-data.</param>
        /// <param name="duration">Duration of sent video in seconds</param>
        /// <param name="caption">Video caption</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVideoAsync(string chatId, FileToSend video, int duration = 0, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"duration", duration},
                {"caption", caption}
            };

            return SendMessageAsync(MessageType.VideoMessage, chatId, video, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method to send video files, Telegram clients support mp4 videos (other formats may be sent as Document). On success, the sent Message is returned. Bots can send video files of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="video">Video to send. You can either pass a file_id as String to resend a video that is already on the Telegram servers, or upload a new video file using multipart/form-data.</param>
        /// <param name="duration">Duration of sent video in seconds</param>
        /// <param name="caption">Video caption</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVideoAsync(long chatId, string video, int duration = 0, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            =>
                SendVideoAsync(chatId.ToString(), video, duration, caption, disableNotification, replyToMessageId,
                    replyMarkup);

        /// <summary>
        /// Use this method to send video files, Telegram clients support mp4 videos (other formats may be sent as Document). On success, the sent Message is returned. Bots can send video files of up to 50 MB in size.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="video">Video to send. You can either pass a file_id as String to resend a video that is already on the Telegram servers, or upload a new video file using multipart/form-data.</param>
        /// <param name="duration">Duration of sent video in seconds</param>
        /// <param name="caption">Video caption</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVideoAsync(string chatId, string video, int duration = 0, string caption = "",
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"duration", duration},
                {"caption", caption}
            };

            return SendMessageAsync(MessageType.VideoMessage, chatId, video, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method to send audio files, if you want Telegram clients to display the file as a playable voice message. For this to work, your audio must be in an .ogg file encoded with OPUS (other formats may be sent as Audio or Document). On success, the sent Message is returned. Bots can currently send voice messages of up to 50 MB in size, this limit may be changed in the future.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="audio">Audio file to send. You can either pass a file_id as String to resend an audio that is already on the Telegram servers, or upload a new audio file using multipart/form-data.</param>
        /// <param name="duration">Duration of sent audio in seconds</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVoiceAsync(long chatId, FileToSend audio, int duration = 0,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            => SendVoiceAsync(chatId.ToString(), audio, duration, disableNotification, replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send audio files, if you want Telegram clients to display the file as a playable voice message. For this to work, your audio must be in an .ogg file encoded with OPUS (other formats may be sent as Audio or Document). On success, the sent Message is returned. Bots can currently send voice messages of up to 50 MB in size, this limit may be changed in the future.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="audio">Audio file to send. You can either pass a file_id as String to resend an audio that is already on the Telegram servers, or upload a new audio file using multipart/form-data.</param>
        /// <param name="duration">Duration of sent audio in seconds</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVoiceAsync(string chatId, FileToSend audio, int duration = 0,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"duration", duration}
            };

            return SendMessageAsync(MessageType.VoiceMessage, chatId, audio, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method to send audio files, if you want Telegram clients to display the file as a playable voice message. For this to work, your audio must be in an .ogg file encoded with OPUS (other formats may be sent as Audio or Document). On success, the sent Message is returned. Bots can currently send voice messages of up to 50 MB in size, this limit may be changed in the future.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="audio">Audio file to send. You can either pass a file_id as String to resend an audio that is already on the Telegram servers, or upload a new audio file using multipart/form-data.</param>
        /// <param name="duration">Duration of sent audio in seconds</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVoiceAsync(long chatId, string audio, int duration = 0,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            => SendVoiceAsync(chatId.ToString(), audio, duration, disableNotification, replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send audio files, if you want Telegram clients to display the file as a playable voice message. For this to work, your audio must be in an .ogg file encoded with OPUS (other formats may be sent as Audio or Document). On success, the sent Message is returned. Bots can currently send voice messages of up to 50 MB in size, this limit may be changed in the future.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="audio">Audio file to send. You can either pass a file_id as String to resend an audio that is already on the Telegram servers, or upload a new audio file using multipart/form-data.</param>
        /// <param name="duration">Duration of sent audio in seconds</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendVoiceAsync(string chatId, string audio, int duration = 0,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"duration", duration}
            };

            return SendMessageAsync(MessageType.VoiceMessage, chatId, audio, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method to send point on the map. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="latitude">Latitude of location</param>
        /// <param name="longitude">Longitude of location</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendLocationAsync(long chatId, float latitude, float longitude,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
            => SendLocationAsync(chatId.ToString(), latitude, longitude, disableNotification, replyToMessageId, replyMarkup);

        /// <summary>
        /// Use this method to send point on the map. On success, the sent Message is returned.
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="latitude">Latitude of location</param>
        /// <param name="longitude">Longitude of location</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent Message is returned.</returns>
        public Task<Message> SendLocationAsync(string chatId, float latitude, float longitude,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null)
        {
            var additionalParameters = new Dictionary<string, object>
            {
                {"longitude", longitude},
            };

            return SendMessageAsync(MessageType.LocationMessage, chatId, latitude, disableNotification, replyToMessageId,
                replyMarkup, additionalParameters);
        }

        /// <summary>
        /// Use this method when you need to tell the user that something is happening on the bot's side. The status is set for 5 seconds or less (when a message arrives from your bot, Telegram clients clear its typing status).
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="chatAction">Type of action to broadcast. Choose one, depending on what the user is about to receive.</param>
        /// <remarks>We only recommend using this method when a response from the bot will take a noticeable amount of time to arrive.</remarks>
        public Task SendChatActionAsync(long chatId, ChatAction chatAction)
            => SendChatActionAsync(chatId.ToString(), chatAction);

        /// <summary>
        /// Use this method when you need to tell the user that something is happening on the bot's side. The status is set for 5 seconds or less (when a message arrives from your bot, Telegram clients clear its typing status).
        /// </summary>
        /// <param name="chatId">Username of the target channel (in the format @channelusername)</param>
        /// <param name="chatAction">Type of action to broadcast. Choose one, depending on what the user is about to receive.</param>
        /// <remarks>We only recommend using this method when a response from the bot will take a noticeable amount of time to arrive.</remarks>
        public Task SendChatActionAsync(string chatId, ChatAction chatAction)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId},
                {"action", chatAction.ToActionString()}
            };

            return SendWebRequestAsync<bool>("sendChatAction", parameters);
        }

        #endregion

        #region API Methods - Administration

        /// <summary>
        /// Use this method to get a list of administrators in a chat.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <returns>On success, returns an Array of ChatMember objects that contains information about all chat administrators except other bots. If the chat is a group or a supergroup and no administrators were appointed, only the creator will be returned.</returns>
        public Task<ChatMember[]> GetChatAdministratorsAsync(long chatId) => GetChatAdministratorsAsync(chatId.ToString());

        /// <summary>
        /// Use this method to get a list of administrators in a chat.
        /// </summary>
        /// <param name="chatId">Username of the target supergroup or channel (in the format @channelusername)</param>
        /// <returns>On success, returns an Array of ChatMember objects that contains information about all chat administrators except other bots. If the chat is a group or a supergroup and no administrators were appointed, only the creator will be returned.</returns>
        public Task<ChatMember[]> GetChatAdministratorsAsync(string chatId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId }
            };

            return SendWebRequestAsync<ChatMember[]>("getChatAdministrators", parameters);
        }

        /// <summary>
        /// Use this method to get the number of members in a chat.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <returns>Returns Int on success.</returns>
        public Task<int> GetChatMembersCountAsync(long chatId) => GetChatMembersCountAsync(chatId.ToString());

        /// <summary>
        /// Use this method to get the number of members in a chat.
        /// </summary>
        /// <param name="chatId">Username of the target supergroup or channel (in the format @channelusername)</param>
        /// <returns>Returns Int on success.</returns>
        public Task<int> GetChatMembersCountAsync(string chatId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId }
            };

            return SendWebRequestAsync<int>("getChatMembersCount", parameters);
        }

        /// <summary>
        /// Use this method to get information about a member of a chat.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="userId">Unique identifier of the target user</param>
        /// <returns>Returns a ChatMember object on success.</returns>
        public Task<ChatMember> GetChatMemberAsync(long chatId, int userId) => GetChatMemberAsync(chatId.ToString(), userId);

        /// <summary>
        /// Use this method to get information about a member of a chat.
        /// </summary>
        /// <param name="chatId">Username of the target supergroup or channel (in the format @channelusername)</param>
        /// <param name="userId">Unique identifier of the target user</param>
        /// <returns>Returns a ChatMember object on success.</returns>
        public Task<ChatMember> GetChatMemberAsync(string chatId, int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId },
                {"user_id", userId}
            };

            return SendWebRequestAsync<ChatMember>("getChatMember", parameters);
        }

        /// <summary>
        /// Use this method to get up to date information about the chat (current name of the user for one-on-one conversations, current username of a user, group or channel, etc.).
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <returns>Returns a Chat object on success.</returns>
        public Task<Chat> GetChatAsync(long chatId) => GetChatAsync(chatId.ToString());

        /// <summary>
        /// Use this method to get up to date information about the chat (current name of the user for one-on-one conversations, current username of a user, group or channel, etc.).
        /// </summary>
        /// <param name="chatId">Username of the target supergroup or channel (in the format @channelusername)</param>
        /// <returns>Returns a Chat object on success.</returns>
        public Task<Chat> GetChatAsync(string chatId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId }
            };

            return SendWebRequestAsync<Chat>("getChat", parameters);
        }

        /// <summary>
        /// Use this method for your bot to leave a group, supergroup or channel.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <returns>Returns a Chat object on success.</returns>
        public Task<bool> LeaveChatAsync(long chatId) => LeaveChatAsync(chatId.ToString());

        /// <summary>
        /// Use this method for your bot to leave a group, supergroup or channel.
        /// </summary>
        /// <param name="chatId">Username of the target supergroup or channel (in the format @channelusername)</param>
        /// <returns>Returns a Chat object on success.</returns>
        public Task<bool> LeaveChatAsync(string chatId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId }
            };

            return SendWebRequestAsync<bool>("leaveChat", parameters);
        }

        /// <summary>
        /// Use this method to kick a user from a group or a supergroup. In the case of supergroups, the user will not be able to return to the group on their own using invite links, etc., unless unbanned first. The bot must be an administrator in the group for this to work.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target group</param>
        /// <param name="userId">Unique identifier of the target user</param>
        /// <returns>True on success.</returns>
        public Task<bool> KickChatMemberAsync(long chatId, int userId) => KickChatMemberAsync(chatId.ToString(), userId);

        /// <summary>
        /// Use this method to kick a user from a group or a supergroup. In the case of supergroups, the user will not be able to return to the group on their own using invite links, etc., unless unbanned first. The bot must be an administrator in the group for this to work.
        /// </summary>
        /// <param name="chatId">Username of the target supergroup (in the format @supergroupusername)</param>
        /// <param name="userId">Unique identifier of the target user</param>
        /// <returns>True on success.</returns>
        public Task<bool> KickChatMemberAsync(string chatId, int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId},
                {"user_id", userId}
            };

            return SendWebRequestAsync<bool>("kickChatMember", parameters);
        }

        /// <summary>
        /// Use this method to unban a previously kicked user in a supergroup. The user will not return to the group automatically, but will be able to join via link, etc. The bot must be an administrator in the group for this to work. 
        /// </summary>
        /// <param name="chatId">Unique identifier for the target group</param>
        /// <param name="userId">Unique identifier of the target user</param>
        /// <returns>True on success.</returns>
        public Task<bool> UnbanChatMemberAsync(long chatId, int userId) => UnbanChatMemberAsync(chatId.ToString(), userId);

        /// <summary>
        /// Use this method to unban a previously kicked user in a supergroup. The user will not return to the group automatically, but will be able to join via link, etc. The bot must be an administrator in the group for this to work. 
        /// </summary>
        /// <param name="chatId">Username of the target supergroup (in the format @supergroupusername)</param>
        /// <param name="userId">Unique identifier of the target user</param>
        /// <returns>True on success.</returns>
        public Task<bool> UnbanChatMemberAsync(string chatId, int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId},
                {"user_id", userId}
            };

            return SendWebRequestAsync<bool>("unbanChatMember", parameters);
        }

        public Task<bool> DeleteMessageAsync(long chatid, int msgid) => DeleteMessageAsync(chatid.ToString(), msgid);

        public Task<bool> DeleteMessageAsync(string chatid, int msgid)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatid },
                {"message_id", msgid }
            };

            return SendWebRequestAsync<bool>("deleteMessage", parameters);
        }

        #endregion

        #region API Methods - Download Content

        /// <summary>
        /// Use this method to get a list of profile pictures for a user. Returns a UserProfilePhotos object.
        /// </summary>
        /// <param name="userId">Unique identifier of the target user</param>
        /// <param name="offset">Optional. Sequential number of the first photo to be returned. By default, all photos are returned.</param>
        /// <param name="limit">Optional. Limits the number of photos to be retrieved. Values between 1—100 are accepted. Defaults to 100.</param>
        /// <returns></returns>
        public Task<UserProfilePhotos> GetUserProfilePhotosAsync(int userId, int? offset = null, int limit = 100)
        {
            var parameters = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"offset", offset},
                {"limit", limit}
            };

            return SendWebRequestAsync<UserProfilePhotos>("getUserProfilePhotos", parameters);
        }

        /// <summary>
        /// Use this method to get basic info about a file and prepare it for downloading. For the moment, bots can download files of up to 20MB in size.
        /// </summary>
        /// <param name="fileId">File identifier</param>
        /// <param name="destination">The destination stream</param>
        /// <returns>The File object. If destination is empty stream ist embedded in the File Object</returns>
        public async Task<Types.File> GetFileAsync(string fileId, Stream destination = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"file_id", fileId}
            };

            var fileInfo = await SendWebRequestAsync<Types.File>("getFile", parameters).ConfigureAwait(false);

            var fileUri = new Uri(BaseFileUrl + _token + "/" + fileInfo.FilePath);

            if (destination == null)
                destination = fileInfo.FileStream = new MemoryStream();

            using (var downloader = new HttpClient())
            {
                using (
                    var response =
                        await
                            downloader.GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false)
                    )
                {
                    await response.Content.CopyToAsync(destination).ConfigureAwait(false);
                    destination.Position = 0;
                }
            }

            return fileInfo;
        }

        #endregion

        #region API Methods - Inline

        /// <summary>
        /// Use this method to send answers to an inline query.
        /// </summary>
        /// <param name="inlineQueryId">Unique identifier for answered query</param>
        /// <param name="results">A array of results for the inline query</param>
        /// <param name="cacheTime">Optional. The maximum amount of time in seconds the result of the inline query may be cached on the server</param>
        /// <param name="isPersonal">Optional. Pass True, if results may be cached on the server side only for the user that sent the query. By default, results may be returned to any user who sends the same query</param>
        /// <param name="nextOffset">Optional. Pass the offset that a client should send in the next query with the same text to receive more results. Pass an empty string if there are no more results or if you don‘t support pagination. Offset length can’t exceed 64 bytes.</param>
        /// <param name="switchPmText">If passed, clients will display a button with specified text that switches the user to a private chat with the bot and sends the bot a start message with the parameter switch_pm_parameter</param>
        /// <param name="switchPmParameter">Parameter for the start message sent to the bot when user presses the switch button</param>
        /// <returns>On success, True is returned.</returns>
        public Task<bool> AnswerInlineQueryAsync(string inlineQueryId, InlineQueryResult[] results, int? cacheTime = null,
            bool isPersonal = false, string nextOffset = null, string switchPmText = null,
            string switchPmParameter = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"inline_query_id", inlineQueryId},
                {"results", results},
                {"is_personal", isPersonal}
            };

            if (cacheTime.HasValue)
                parameters.Add("cache_time", cacheTime);

            if (!string.IsNullOrWhiteSpace(nextOffset))
                parameters.Add("next_offset", nextOffset);

            if (!string.IsNullOrWhiteSpace(switchPmText))
                parameters.Add("switch_pm_text", switchPmText);

            if (!string.IsNullOrWhiteSpace(switchPmParameter))
                parameters.Add("switch_pm_parameter", switchPmParameter);

            return SendWebRequestAsync<bool>("answerInlineQuery", parameters);
        }

        /// <summary>
        /// Use this method to send answers to callback queries sent from inline keyboards. The answer will be displayed to the user as a notification at the top of the chat screen or as an alert.
        /// </summary>
        /// <param name="callbackQueryId">Unique identifier for the query to be answered</param>
        /// <param name="text">Text of the notification. If not specified, nothing will be shown to the user</param>
        /// <param name="showAlert">If true, an alert will be shown by the client instead of a notification at the top of the chat screen. Defaults to false.</param>
        /// <returns>On success, True is returned.</returns>
        public Task<bool> AnswerCallbackQueryAsync(string callbackQueryId, string text = null, bool showAlert = false)
        {
            var parameters = new Dictionary<string, object>
            {
                {"callback_query_id", callbackQueryId},
                {"show_alert", showAlert},
            };

            if (!string.IsNullOrEmpty(text))
                parameters.Add("text", text);

            return SendWebRequestAsync<bool>("answerCallbackQuery", parameters);
        }

        #endregion

        #region API Methods - Edit

        /// <summary>
        /// Use this method to edit text messages sent by the bot or via the bot (for inline bots).
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="messageId">Unique identifier of the sent message</param>
        /// <param name="text">New text of the message</param>
        /// <param name="parseMode">Send Markdown or HTML, if you want Telegram apps to show bold, italic, fixed-width text or inline URLs in your bot's message.</param>
        /// <param name="disableWebPagePreview">Disables link previews for links in this message</param>
        /// <param name="replyMarkup">A JSON-serialized object for an inline keyboard.</param>
        /// <returns>On success, the edited Message is returned.</returns>
        public Task<Message> EditMessageTextAsync(long chatId, int messageId, string text,
            ParseMode parseMode = ParseMode.Default, bool disableWebPagePreview = false, IReplyMarkup replyMarkup = null)
            => EditMessageTextAsync(chatId.ToString(), messageId, text, parseMode, disableWebPagePreview, replyMarkup);

        /// <summary>
        /// Use this method to edit text messages sent by the bot or via the bot (for inline bots).
        /// </summary>
        /// <param name="chatId">username of the target channel (in the format @channelusername)</param>
        /// <param name="messageId">Unique identifier of the sent message</param>
        /// <param name="text">New text of the message</param>
        /// <param name="parseMode">Send Markdown or HTML, if you want Telegram apps to show bold, italic, fixed-width text or inline URLs in your bot's message.</param>
        /// <param name="disableWebPagePreview">Disables link previews for links in this message</param>
        /// <param name="replyMarkup">A JSON-serialized object for an inline keyboard.</param>
        /// <returns>On success, the edited Message is returned.</returns>
        public Task<Message> EditMessageTextAsync(string chatId, int messageId, string text,
            ParseMode parseMode = ParseMode.Default, bool disableWebPagePreview = false, IReplyMarkup replyMarkup = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId},
                {"message_id", messageId},
                {"text", text},
                {"disable_web_page_preview", disableWebPagePreview},
                {"reply_markup", replyMarkup},
            };

            if (parseMode != ParseMode.Default)
                parameters.Add("parse_mode", parseMode.ToModeString());

            return SendWebRequestAsync<Message>("editMessageText", parameters);
        }

        /// <summary>
        /// Use this method to edit text messages sent by the bot or via the bot (for inline bots).
        /// </summary>
        /// <param name="inlineMessageId">Identifier of the inline message</param>
        /// <param name="text">New text of the message</param>
        /// <param name="parseMode">Send Markdown or HTML, if you want Telegram apps to show bold, italic, fixed-width text or inline URLs in your bot's message.</param>
        /// <param name="disableWebPagePreview">Disables link previews for links in this message</param>
        /// <param name="replyMarkup">A JSON-serialized object for an inline keyboard.</param>
        /// <returns>On success, the edited Message is returned.</returns>
        public Task<Message> EditInlineMessageTextAsync(string inlineMessageId, string text,
            ParseMode parseMode = ParseMode.Default, bool disableWebPagePreview = false, IReplyMarkup replyMarkup = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"inline_message_id", inlineMessageId},
                {"text", text},
                {"disable_web_page_preview", disableWebPagePreview},
                {"reply_markup", replyMarkup},
            };

            if (parseMode != ParseMode.Default)
                parameters.Add("parse_mode", parseMode.ToModeString());

            return SendWebRequestAsync<Message>("editMessageText", parameters);
        }

        /// <summary>
        /// Use this method to edit captions of messages sent by the bot or via the bot (for inline bots).
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="messageId">Unique identifier of the sent message</param>
        /// <param name="caption">New caption of the message</param>
        /// <param name="replyMarkup">A JSON-serialized object for an inline keyboard.</param>
        /// <returns>On success, the edited Message is returned.</returns>
        public Task<Message> EditMessageCaptionAsync(long chatId, int messageId, string caption,
            IReplyMarkup replyMarkup = null)
            => EditMessageCaptionAsync(chatId.ToString(), messageId, caption, replyMarkup);

        /// <summary>
        /// Use this method to edit captions of messages sent by the bot or via the bot (for inline bots).
        /// </summary>
        /// <param name="chatId">username of the target channel (in the format @channelusername)</param>
        /// <param name="messageId">Unique identifier of the sent message</param>
        /// <param name="caption">New caption of the message</param>
        /// <param name="replyMarkup">A JSON-serialized object for an inline keyboard.</param>
        /// <returns>On success, the edited Message is returned.</returns>
        public Task<Message> EditMessageCaptionAsync(string chatId, int messageId, string caption,
            IReplyMarkup replyMarkup = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId},
                {"message_id", messageId},
                {"caption", caption},
                {"reply_markup", replyMarkup},
            };

            return SendWebRequestAsync<Message>("editMessageCaption", parameters);
        }

        /// <summary>
        /// Use this method to edit captions of messages sent by the bot or via the bot (for inline bots).
        /// </summary>
        /// <param name="inlineMessageId">Unique identifier of the sent message</param>
        /// <param name="caption">New caption of the message</param>
        /// <param name="replyMarkup">A JSON-serialized object for an inline keyboard.</param>
        /// <returns>On success, the edited Message is returned.</returns>
        public Task<Message> EditInlineMessageCaptionAsync(string inlineMessageId, string caption,
            IReplyMarkup replyMarkup = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"inline_message_id", inlineMessageId},
                {"caption", caption},
                {"reply_markup", replyMarkup},
            };

            return SendWebRequestAsync<Message>("editMessageCaption", parameters);
        }

        /// <summary>
        /// Use this method to edit only the reply markup of messages sent by the bot or via the bot (for inline bots).
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat</param>
        /// <param name="messageId">Unique identifier of the sent message</param>
        /// <param name="replyMarkup">A JSON-serialized object for an inline keyboard.</param>
        /// <returns>On success, the edited Message is returned.</returns>
        public Task<Message> EditMessageReplyMarkup(long chatId, int messageId, IReplyMarkup replyMarkup = null)
            => EditMessageReplyMarkup(chatId.ToString(), messageId, replyMarkup);

        /// <summary>
        /// Use this method to edit only the reply markup of messages sent by the bot or via the bot (for inline bots).
        /// </summary>
        /// <param name="chatId">username of the target channel (in the format @channelusername)</param>
        /// <param name="messageId">Unique identifier of the sent message</param>
        /// <param name="replyMarkup">A JSON-serialized object for an inline keyboard.</param>
        /// <returns>On success, the edited Message is returned.</returns>
        public Task<Message> EditMessageReplyMarkup(string chatId, int messageId, IReplyMarkup replyMarkup = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"chat_id", chatId},
                {"message_id", messageId},
                {"reply_markup", replyMarkup},
            };

            return SendWebRequestAsync<Message>("editMessageReplyMarkup", parameters);
        }

        /// <summary>
        /// Use this method to edit only the reply markup of messages sent by the bot or via the bot (for inline bots).
        /// </summary>
        /// <param name="inlineMessageId">Unique identifier of the sent message</param>
        /// <param name="replyMarkup">A JSON-serialized object for an inline keyboard.</param>
        /// <returns>On success, the edited Message is returned.</returns>
        public Task<Message> EditInlineMessageReplyMarkupAsync(string inlineMessageId, IReplyMarkup replyMarkup = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"inline_message_id", inlineMessageId},
                {"reply_markup", replyMarkup},
            };

            return SendWebRequestAsync<Message>("editMessageReplyMarkup", parameters);
        }

        #endregion

        #region Payments

        /// <summary>
        /// Use this method to send invoices.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target private chat</param>
        /// <param name="title">Product name</param>
        /// <param name="description">Product description</param>
        /// <param name="payload">Bot-defined invoice payload, 1-128 bytes. This will not be displayed to the user, use for your internal processes.</param>
        /// <param name="providerToken">Payments provider token, obtained via Botfather</param>
        /// <param name="startParameter">Unique deep-linking parameter that can be used to generate this invoice when used as a start parameter</param>
        /// <param name="currency">Three-letter ISO 4217 currency code, see more on currencies</param>
        /// <param name="prices">Price breakdown, a list of components (e.g. product price, tax, discount, delivery cost, delivery tax, bonus, etc.)</param>
        /// <param name="photoUrl">URL of the product photo for the invoice. Can be a photo of the goods or a marketing image for a service.</param>
        /// <param name="photoSize">Photo size</param>
        /// <param name="photoWidth">Photo width</param>
        /// <param name="photoHeight">Photo height</param>
        /// <param name="needName">Pass True, if you require the user's full name to complete the order</param>
        /// <param name="needPhoneNumber">Pass True, if you require the user's phone number to complete the order</param>
        /// <param name="needEmail">Pass True, if you require the user's email to complete the order</param>
        /// <param name="needShippingAddress">Pass True, if you require the user's shipping address to complete the order</param>
        /// <param name="isFlexible">Pass True, if the final price depends on the shipping method</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <returns>On success, the sent <see cref="Message"/> is returned.</returns>
        /// <see href="https://core.telegram.org/bots/api#sendinvoice"/>
        public Task<Message> SendInvoiceAsync(ChatId chatId, string title, string description,
            string payload, string providerToken, string startParameter, string currency,
            LabeledPrice[] prices, string photoUrl = null, int photoSize = 0, int photoWidth = 0,
            int photoHeight = 0, bool needName = false, bool needPhoneNumber = false,
            bool needEmail = false, bool needShippingAddress = false, bool isFlexible = false,
            bool disableNotification = false, int replyToMessageId = 0, InlineKeyboardMarkup replyMarkup = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"description", description},
                {"payload", payload},
                {"provider_token", providerToken},
                {"start_parameter", startParameter},
                {"currency", currency},
                {"prices", prices},
            };

            if (photoUrl != null)
                parameters.Add("photo_url", photoUrl);

            if (photoSize != 0)
                parameters.Add("photo_size", photoSize);

            if (photoWidth != 0)
                parameters.Add("photo_width", photoWidth);

            if (photoHeight != 0)
                parameters.Add("photo_height", photoHeight);

            if (needName)
                parameters.Add("need_name", true);

            if (needPhoneNumber)
                parameters.Add("need_phone_number", true);

            if (needEmail)
                parameters.Add("need_email", true);

            if (needShippingAddress)
                parameters.Add("need_shipping_address", true);

            if (isFlexible)
                parameters.Add("is_flexible", true);

            return SendMessageAsync(MessageType.Invoice, chatId, title, disableNotification, replyToMessageId, replyMarkup, parameters);
        }

        /// <summary>
        /// If you sent an invoice requesting a shipping address and the parameter is_flexible was specified, the Bot API will send an Update with a shipping_query field to the bot. Use this method to reply to shipping queries.
        /// </summary>
        /// <param name="shippingQueryId">Unique identifier for the query to be answered</param>
        /// <param name="ok">Specify True if delivery to the specified address is possible and False if there are any problems</param>
        /// <param name="shippingOptions">Required if ok is True.</param>
        /// <param name="errorMessage">Required if ok is False. Error message in human readable form that explains why it is impossible to complete the order </param>
        /// <returns>On success, True is returned.</returns>
        /// <see href="https://core.telegram.org/bots/api#answershippingquery"/>
        public Task<bool> AnswerShippingQueryAsync(string shippingQueryId, bool ok,
            ShippingOption[] shippingOptions = null,
            string errorMessage = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"shipping_query_id", shippingQueryId},
                {"ok", ok}
            };

            if (shippingOptions != null)
                parameters.Add("shipping_options", shippingOptions);

            if (!ok)
                parameters.Add("error_message", errorMessage);

            return SendWebRequestAsync<bool>("answerShippingQuery", parameters);
        }

        /// <summary>
        /// Use this method to respond to such pre-checkout queries.
        /// </summary>
        /// <param name="preCheckoutQueryId">Unique identifier for the query to be answered</param>
        /// <param name="ok">Specify True if everything is alright</param>
        /// <param name="errorMessage">Required if ok is False. Error message in human readable form that explains the reason for failure to proceed with the checkout</param>
        /// <returns>On success, True is returned.</returns>
        /// <remarks>Note: The Bot API must receive an answer within 10 seconds after the pre-checkout query was sent.</remarks>
        /// <see href="https://core.telegram.org/bots/api#answerprecheckoutquery"/>
        public Task<bool> AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok,
            string errorMessage = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"pre_checkout_query_id", preCheckoutQueryId},
                {"ok", ok}
            };

            if (!ok)
                parameters.Add("error_message", errorMessage);

            return SendWebRequestAsync<bool>("answerPreCheckoutQuery", parameters);
        }

        #endregion Payments

        #region Support Methods - Private

        /// <summary>
        /// Use this method to send any messages. On success, the sent Message is returned.
        /// </summary>
        /// <param name="type">The <see cref="MessageType"/></param>
        /// <param name="chatId">Unique identifier for the target chat or username of the target channel (in the format @channelusername)</param>
        /// <param name="content">The content of the message. Could be a text, photo, audio, sticker, document, video or location</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <param name="additionalParameters">Optional. if additional Parameters could bei send i.e. "disable_web_page_preview" in for a TextMessage</param>
        /// <returns>On success, the sent Message is returned.</returns>
        private Task<Message> SendMessageAsync(MessageType type, string chatId, object content,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null,
            Dictionary<string, object> additionalParameters = null)
            => SendMessageAsync(type, chatId, content, false, replyToMessageId, replyMarkup, additionalParameters);

        /// <summary>
        /// Use this method to send any messages. On success, the sent Message is returned.
        /// </summary>
        /// <param name="type">The <see cref="MessageType"/></param>
        /// <param name="chatId">Unique identifier for the target chat or username of the target channel (in the format @channelusername)</param>
        /// <param name="content">The content of the message. Could be a text, photo, audio, sticker, document, video or location</param>
        /// <param name="disableNotification">Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.</param>
        /// <param name="replyToMessageId">Optional. If the message is a reply, ID of the original message</param>
        /// <param name="replyMarkup">Optional. Additional interface options. A JSON-serialized object for a custom reply keyboard, instructions to hide keyboard or to force a reply from the user.</param>
        /// <param name="additionalParameters">Optional. if additional Parameters could bei send i.e. "disable_web_page_preview" in for a TextMessage</param>
        /// <returns>On success, the sent Message is returned.</returns>
        private Task<Message> SendMessageAsync(MessageType type, string chatId, object content,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup replyMarkup = null,
            Dictionary<string, object> additionalParameters = null)
        {
            if (additionalParameters == null)
                additionalParameters = new Dictionary<string, object>();

            var typeInfo = type.ToKeyValue();

            additionalParameters.Add("chat_id", chatId);

            if (disableNotification)
                additionalParameters.Add("disable_notification", true);

            if (replyMarkup != null)
                additionalParameters.Add("reply_markup", replyMarkup);

            if (replyToMessageId != 0)
                additionalParameters.Add("reply_to_message_id", replyToMessageId);

            if (!string.IsNullOrEmpty(typeInfo.Value))
                additionalParameters.Add(typeInfo.Value, content);

            return SendWebRequestAsync<Message>(typeInfo.Key, additionalParameters);
        }

        private async Task<T> SendWebRequestAsync<T>(string method, Dictionary<string, object> parameters = null)
        {
            if (_invalidToken)
                throw new ApiRequestException("Invalid token", 401);

            var uri = new Uri(BaseUrl + _token + "/" + method);
            var error = "";
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(3);
                ApiResponse<T> responseObject = null;
                try
                {
                    HttpResponseMessage response;

                    if (parameters != null)
                    {
                        using (var form = new MultipartFormDataContent())
                        {
                            foreach (var parameter in parameters.Where(parameter => parameter.Value != null))
                            {
                                var content = ConvertParameterValue(parameter.Value);

                                //if (parameter.Key == "timeout" && (int) parameter.Value != 0)
                                //{
                                //    client.Timeout = TimeSpan.FromSeconds((int) parameter.Value + 1);
                                //}

                                if (parameter.Value is FileToSend)
                                {
                                    client.Timeout = UploadTimeout;
                                    form.Add(content, parameter.Key, ((FileToSend) parameter.Value).Filename);
                                }
                                else
                                    form.Add(content, parameter.Key);
                            }

                            response = await client.PostAsync(uri, form).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        response = await client.GetAsync(uri).ConfigureAwait(false);
                    }

#if NETSTANDARD1_3
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    responseObject = JsonConvert.DeserializeObject<ApiResponse<T>>(responseString);
#else
                    responseObject = await response.Content.ReadAsAsync<ApiResponse<T>>().ConfigureAwait(false);
#endif
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException e) when (e.Message.Contains("401"))
                {
                    _invalidToken = true;
                    throw new ApiRequestException("Invalid token", 401);
                }
                catch (HttpRequestException e)
                    when (e.Message.Contains("400") || e.Message.Contains("403") || e.Message.Contains("409"))
                {
                    error = e.Message;
                }


#if !NETSTANDARD1_3
                catch (UnsupportedMediaTypeException)
                {
                    throw new ApiRequestException("Invalid response received", 501);
                }
#endif
                catch (Exception e)
                {
                    //ignored
                    error = e.Message;
                }
                //TODO: catch more exceptions

                if (responseObject == null)
                    responseObject = new ApiResponse<T> { Ok = false, Message = "No response received: " + error };

                if (!responseObject.Ok)
                    throw new ApiRequestException(responseObject.Message, responseObject.Code);

                return responseObject.ResultObject;
            }
        }

        private static HttpContent ConvertParameterValue(object value)
        {
            var type = value.GetType();

            switch (type.Name)
            {
                case "String":
                case "Int32":
                    return new StringContent(value.ToString());
                case "Boolean":
                    return new StringContent((bool)value ? "true" : "false");
                case "FileToSend":
                    return new StreamContent(((FileToSend)value).Content);
                default:
                    var settings = new JsonSerializerSettings
                    {
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                    };

                    return new StringContent(JsonConvert.SerializeObject(value, settings));
            }
        }

        #endregion


    }
}