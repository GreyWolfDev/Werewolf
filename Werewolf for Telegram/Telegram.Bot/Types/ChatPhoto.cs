using Newtonsoft.Json;

namespace Telegram.Bot.Types
{
    /// <summary>
    /// Collection of fileIds of profile pictures of a chat.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ChatPhoto
    {
        /// <summary>
        /// File id of the big version of this <see cref="ChatPhoto"/>
        /// </summary>
        [JsonProperty(PropertyName = "big_file_id", Required = Required.Default)]
        public string BigFileId { get; set; }
        /// <summary>
        /// File id of the small version of this <see cref="ChatPhoto"/>
        /// </summary>
        [JsonProperty(PropertyName = "small_file_id", Required = Required.Default)]
        public string SmallFileId { get; set; }

        /// <summary>
        /// Unique Persistent ID for the small file that is the same over different bots
        /// </summary>
        [JsonProperty("small_file_unique_id", Required = Required.Default)]
        public string SmallFileUniqueId { get; set; }

        /// <summary>
        /// Unique Persistent ID for the big file that is the same over different bots
        /// </summary>
        [JsonProperty("big_file_unique_id", Required = Required.Default)]
        public string BigFileUniqueId { get; set; }
    }
}
