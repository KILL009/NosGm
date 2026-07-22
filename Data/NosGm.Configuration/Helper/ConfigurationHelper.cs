using System;
using System.IO;
using Newtonsoft.Json;
using NosGm.Configuration.Utilities;

namespace NosGm.Configuration.Helper
{
    public class ConfigurationHelper
    {
        #region Methods

        public static void CustomisationRegistration(bool delete = false)
        {
            const string configPath = "../Config/";
            if (delete)
            {
                var di = new DirectoryInfo(configPath);
                if (di != null)
                    foreach (var ff in di.GetDirectories())
                    {
                        if (ff == null) return;
                        Console.Write($"{ff.Name} Folder Deleted \n");
                        ff?.Delete(true);
                    }
            }

            DependencyContainer.Instance.Register(Load<JsonGameConfiguration>("../Config/Server/Global.json", true));
            DependencyContainer.Instance.Register(Load<JsonItemConfiguration>("../Config/Server/Item.json", true));
        }

        public static T Load<T>(string path) where T : class, new()
        {
            return Load<T>(path, false);
        }

        public static T Load<T>(string path, bool createIfNotExists) where T : class, new()
        {
            if (!File.Exists(path))
            {
                if (createIfNotExists)
                    Save(path, new T());
                else
                    throw new IOException(path);
            }

            var fileContent = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<T>(fileContent);
        }

        public static void Save<T>(string path, T value)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(Path.GetDirectoryName(path));

            var valueSerialized = JsonConvert.SerializeObject(value, Formatting.Indented);

            File.WriteAllText(path, valueSerialized);
        }

        #endregion
    }
}