using Newtonsoft.Json;
using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.DiscordHelperThread
{

    [JsonObject]
    internal interface IEmbedDimension
    {
        #region Properties

        [JsonProperty("height")]
        int Height { get; set; }

        [JsonProperty("width")]
        int Width { get; set; }

        #endregion
    }

    [JsonObject]
    internal interface IEmbedIconProxyUrl
    {
        #region Properties

        [JsonProperty("proxy_icon_url")]
        string ProxyIconUrl { get; set; }

        #endregion
    }

    [JsonObject]
    internal interface IEmbedIconUrl
    {
        #region Properties

        [JsonProperty("icon_url")]
        string IconUrl { get; set; }

        #endregion
    }

    [JsonObject]
    internal interface IEmbedProxyUrl
    {
        #region Properties

        [JsonProperty("proxy_url")]
        string ProxyUrl { get; set; }

        #endregion
    }

    [JsonObject]
    internal interface IEmbedUrl
    {
        #region Properties

        [JsonProperty("url")]
        string Url { get; set; }

        #endregion
    }

    public static class DiscordHelperThread
    {
        private static readonly HttpClient _httpClient;
        private static readonly string _webhookUrlBazar;
        private static readonly string _webhookUrlEventRaid;
        private static string _webhookUrlEventRaidEnd;

        static DiscordHelperThread()
        {
            _httpClient = new HttpClient();
        }
    }

    [JsonObject]
    internal class Embed : IEmbedUrl
    {
        #region Properties

        [JsonProperty("author")]
        public EmbedAuthor Author { get; set; }

        [JsonProperty("color")]
        public int Color { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("fields")]
        public List<EmbedField> Fields { get; set; } = new List<EmbedField>();

        [JsonProperty("footer")]
        public EmbedFooter Footer { get; set; }

        [JsonProperty("image")]
        public EmbedImage Image { get; set; }

        [JsonProperty("provider")]
        public EmbedProvider Provider { get; set; }

        [JsonProperty("thumbnail")]
        public EmbedThumbnail Thumbnail { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset? TimeStamp { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "rich";

        public string Url { get; set; }

        [JsonProperty("video")]
        public EmbedVideo Video { get; set; }

        #endregion
    }

    [JsonObject]
    internal class EmbedAuthor : EmbedUrl, IEmbedIconUrl, IEmbedIconProxyUrl
    {
        #region Properties

        public string IconUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public string ProxyIconUrl { get; set; }

        #endregion
    }

    [JsonObject]
    internal class EmbedField
    {
        #region Properties

        [JsonProperty("inline")]
        public bool Inline { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        #endregion
    }

    [JsonObject]
    internal class EmbedFooter : IEmbedIconUrl, IEmbedIconProxyUrl
    {
        #region Properties

        public string IconUrl { get; set; }

        public string ProxyIconUrl { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        #endregion
    }

    [JsonObject]
    internal class EmbedImage : EmbedProxyUrl, IEmbedDimension
    {
        #region Properties

        public int Height { get; set; }

        public int Width { get; set; }

        #endregion
    }

    [JsonObject]
    internal class EmbedProvider : EmbedUrl
    {
        #region Properties

        [JsonProperty("name")]
        public string Name { get; set; }

        #endregion
    }

    [JsonObject]
    internal abstract class EmbedProxyUrl : EmbedUrl, IEmbedProxyUrl
    {
        #region Properties

        public string ProxyUrl { get; set; }

        #endregion
    }

    [JsonObject]
    internal class EmbedThumbnail : EmbedProxyUrl, IEmbedDimension
    {
        #region Properties

        public int Height { get; set; }

        public int Width { get; set; }

        #endregion
    }

    [JsonObject]
    internal abstract class EmbedUrl : IEmbedUrl
    {
        #region Properties

        public string Url { get; set; }

        #endregion
    }

    [JsonObject]
    internal class EmbedVideo : EmbedUrl, IEmbedDimension
    {
        #region Properties

        public int Height { get; set; }

        public int Width { get; set; }

        #endregion
    }

    [JsonObject]
    internal class WebhookObject
    {
        #region Properties

        [JsonProperty("avatar_url")] public string AvatarUrl { get; set; }

        [JsonProperty("content")] public string Content { get; set; }

        [JsonProperty("embeds")] public List<Embed> Embeds { get; set; } = new List<Embed>();

        [JsonProperty("tts")] public bool IsTTS { get; set; }

        [JsonProperty("username")] public string Username { get; set; }

        #endregion
    }
}
