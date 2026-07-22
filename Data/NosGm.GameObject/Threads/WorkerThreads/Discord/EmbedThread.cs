using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EmbedBuilderThread
{
    [JsonObject]
    public class Author
    {
        #region Members

        public string icon_url;
        public string name;
        public string url;

        #endregion
    }

    [JsonObject]
    public class Embed
    {
        #region Members

        public Author author;
        public UInt32 color;
        public string description;
        public List<Field> fields = new List<Field>();
        public Footer footer;
        public Image image;
        public Thumbnail thumbnail;
        public DateTime timestamp;
        public string title;
        public string url;

        #endregion
    }

    [JsonObject]
    public class Field
    {
        #region Members

        public bool inline;
        public string name;
        public string value;

        #endregion
    }

    [JsonObject]
    public class Footer
    {
        #region Members

        public string icon_url;
        public string text;

        #endregion
    }

    [JsonObject]
    public class Image
    {
        #region Members

        public string url;

        #endregion
    }

    [JsonObject]
    public class Thumbnail
    {
        #region Members

        public string url;

        #endregion
    }
}
