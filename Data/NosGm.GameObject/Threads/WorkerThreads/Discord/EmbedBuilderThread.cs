using System;
using System.Collections.Generic;

namespace EmbedBuilderThread
{
    internal class EmbedBuilder
    {
        #region Members

        private Embed embed;

        #endregion

        #region Instantiation

        public EmbedBuilder()
        {
            embed = new Embed();
        }

        #endregion

        #region Properties

        public Author Author { get { return embed.author; } set { embed.author = value; } }

        public string Description { get { return embed.description; } set { embed.description = value; } }

        public IEnumerable<Field> Fields { get { return embed.fields; } }

        public Footer Footer { get { return embed.footer; } set { embed.footer = value; } }

        public Image Image { get { return embed.image; } set { embed.image = value; } }

        public Thumbnail Thumbnail { get { return embed.thumbnail; } set { embed.thumbnail = value; } }

        public string Title { get { return embed.title; } set { embed.title = value; } }

        public string Url { get { return embed.url; } set { embed.url = value; } }

        #endregion

        #region Methods

        public EmbedBuilder AddField(string name = null, string value = null, bool inline = false)
        {
            embed.fields.Add(new Field()
            {
                name = name,
                value = value,
                inline = inline
            });
            return this;
        }

        public Embed Build() => this.embed;

        public EmbedBuilder WithAuthor(string name = null, string url = null, string iconUrl = null)
        {
            embed.author = new Author()
            {
                name = name,
                url = url,
                icon_url = iconUrl
            };
            return this;
        }

        public EmbedBuilder WithColor(uint r, uint g, uint b)
        {
            embed.color = (r * 65536) + (g * 256) + b;
            return this;
        }

        public EmbedBuilder WithColor(UInt32 colorVal)
        {
            embed.color = colorVal;
            return this;
        }

        public EmbedBuilder WithDescription(string description = null)
        {
            embed.description = description;
            return this;
        }

        public EmbedBuilder WithFooter(string text, string iconUrl)
        {
            embed.footer = new Footer()
            {
                text = text,
                icon_url = iconUrl
            };
            return this;
        }

        public EmbedBuilder WithImage(string url)
        {
            embed.image = new Image { url = url };
            return this;
        }

        public EmbedBuilder WithThumbnail(string url)
        {
            embed.thumbnail = new Thumbnail { url = url };
            return this;
        }

        public EmbedBuilder WithTimestamp()
        {
            embed.timestamp = DateTime.Now;
            return this;
        }

        public EmbedBuilder WithTimestamp(DateTime dateTime)
        {
            embed.timestamp = dateTime;
            return this;
        }

        public EmbedBuilder WithTitle(string title = null)
        {
            embed.title = title;
            return this;
        }

        public EmbedBuilder WithUrl(string url = null)
        {
            embed.url = url;
            return this;
        }

        #endregion
    }
}
