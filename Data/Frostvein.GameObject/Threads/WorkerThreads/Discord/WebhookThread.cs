using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EmbedBuilderThread;

namespace Frostvein.GameObject.WebhookThread
{
    internal class WebhookThread
    {
        #region Members

        private static readonly HttpClient _client = new HttpClient();

        private string _avatarUrl;
        private string _username;
        private string _webhookURL;

        #endregion

        #region Instantiation

        public WebhookThread(string url)
        {
            this._webhookURL = url;
        }

        public WebhookThread(string url, string username)
        {
            this._webhookURL = url; this._username = username;
        }

        public WebhookThread(string url, string username, string avatarURL)
        {
            this._webhookURL = url; this._username = username; this._avatarUrl = avatarURL;
        }

        #endregion

        #region Methods

        public async Task<HttpResponseMessage> SendMessageAsync(string username = null, string avatarUrl = null, string content = null, bool isTTS = false, IEnumerable<Embed> embeds = null)
        {
            var msg = new Message(username ?? this._username, avatarUrl ?? this._avatarUrl, content, isTTS, embeds);
            return await _client.PostAsync(this._webhookURL, new StringContent(JsonConvert.SerializeObject(msg), Encoding.UTF8, "application/json"));
        }

        public async Task<HttpResponseMessage> SendMessageAsync(string username = null, string avatarUrl = null, string content = null, bool isTTS = false, Embed embed = null)
        {
            var msg = new Message(username ?? this._username, avatarUrl ?? this._avatarUrl, content, isTTS, embed);
            return await _client.PostAsync(this._webhookURL, new StringContent(JsonConvert.SerializeObject(msg), Encoding.UTF8, "application/json"));
        }

        #endregion

        #region Classes

        [JsonObject]
        internal class Message
        {
            #region Members

            [JsonProperty("avatar_url")]
            public string AvatarUrl;

            [JsonProperty("content")]
            public string Content;

            [JsonProperty("embeds")]
            public List<Embed> Embeds;

            [JsonProperty("tts")]
            public bool isTTS;

            [JsonProperty("username")]
            public string Username;

            #endregion

            #region Instantiation

            public Message(string username, string avatarUrl, string content, bool isTTS, IEnumerable<Embed> embeds)
            {
                this.Username = username;
                this.AvatarUrl = avatarUrl;
                this.Content = content;
                this.isTTS = isTTS;
                Embeds = new List<Embed>(embeds);
            }

            public Message(string username, string avatarUrl, string content, bool isTTS, Embed embed)
            {
                this.Username = username;
                this.AvatarUrl = avatarUrl;
                this.Content = content;
                this.isTTS = isTTS;
                Embeds = new List<Embed>();
                Embeds.Add(embed);
            }

            #endregion
        }

        #endregion
    }
}
