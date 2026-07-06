using Frostvein.Configuration;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace Frostvein.Core
{
    public class Language
    {
        #region Instantiation

        private Language()
        {
            try
            {
                _streamWriter = new StreamWriter("MissingLanguage.txt", true)
                {
                    AutoFlush = true
                };
            }
            catch
            {
            }

            _resourceCulture = new CultureInfo(ServerConfiguration.Language);

            if (Assembly.GetEntryAssembly() != null)
                _manager = new ResourceManager(
                    Assembly.GetEntryAssembly().GetName().Name + ".Resource.LocalizedResources",
                    Assembly.GetEntryAssembly());
        }

        #endregion

        #region Properties

        public static Language Instance => _instance ?? (_instance = new Language());

        #endregion

        #region Methods

        public string GetMessageFromKey(string key)
        {
            return _language.GetOrAdd(key, name =>
            {
                var value = _manager?.GetString(name, _resourceCulture);

                if (string.IsNullOrEmpty(value))
                {
                    _streamWriter?.WriteLine(name);
                    return $"{key} ";
                }

                return value;
            });
        }

        #endregion

        #region Members

        private static Language _instance;

        private readonly ResourceManager _manager;
        private readonly CultureInfo _resourceCulture;
        private readonly StreamWriter _streamWriter;

        private readonly ConcurrentDictionary<string, string> _language = new ConcurrentDictionary<string, string>();

        #endregion
    }
}