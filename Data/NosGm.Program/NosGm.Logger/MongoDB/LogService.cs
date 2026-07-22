using NosGm.Configuration;
using NosGm.Domain;
using NosGm.LogServer.MongoDB.LogServiceModel;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.LogServer.MongoDB
{
    public static class LogService
    {
        public static async Task Generate(string information, LogType logType)
        {
            var client = new MongoClient(LogConfiguration.ConnectionString);
            var database = client.GetDatabase(LogConfiguration.DatabaseName);

            var loadCollection = database.GetCollection<LoadServiceModel>(LogConfiguration.LoadServiceModel);
            var errorCollection = database.GetCollection<ErrorServiceModel>(LogConfiguration.ErrorServiceModel);

            switch (logType)
            {
                case LogType.LOAD:
                    var load = new LoadServiceModel
                    {
                        Information = information,
                        DateTime = DateTime.Now
                    };
                    await Task.Run(() => loadCollection.InsertOne(load));
                    break;

                case LogType.ERROR:
                    var error = new ErrorServiceModel
                    {
                        Information = information,
                        DateTime = DateTime.Now
                    };
                    await Task.Run(() => errorCollection.InsertOne(error));
                    break;
            }
        }
    }
}
