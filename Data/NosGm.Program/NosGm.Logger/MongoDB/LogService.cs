using NosGm.Configuration;
using NosGm.Domain;
using NosGm.LogServer.MongoDB.LogServiceModel;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NosGm.LogServer.MongoDB
{
    public static class LogService
    {
        private static readonly Lazy<MongoClient> Client =
            new Lazy<MongoClient>(() => new MongoClient(LogConfiguration.ConnectionString));

        private static IMongoDatabase Database => Client.Value.GetDatabase(LogConfiguration.DatabaseName);

        public static async Task Generate(string information, LogType logType)
        {
            LogPipelineOperation operation = LogPipelineMonitor.CurrentOperation;
            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            try
            {
                switch (logType)
                {
                    case LogType.LOAD:
                        var load = new LoadServiceModel
                        {
                            Information = information,
                            DateTime = DateTime.Now
                        };
                        await Database
                            .GetCollection<LoadServiceModel>(LogConfiguration.LoadServiceModel)
                            .InsertOneAsync(load)
                            .ConfigureAwait(false);
                        success = true;
                        break;

                    case LogType.ERROR:
                        var error = new ErrorServiceModel
                        {
                            Information = information,
                            DateTime = DateTime.Now
                        };
                        await Database
                            .GetCollection<ErrorServiceModel>(LogConfiguration.ErrorServiceModel)
                            .InsertOneAsync(error)
                            .ConfigureAwait(false);
                        success = true;
                        break;

                    default:
                        success = true;
                        break;
                }
            }
            finally
            {
                stopwatch.Stop();
                LogPipelineMonitor.RecordMongoWrite(operation, stopwatch.ElapsedTicks, success);
            }
        }
    }
}
