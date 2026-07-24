using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NosTale.ServiceManager.LogService.Configuration;
using NosTale.ServiceManager.LogService.LogModel;

namespace NosTale.ServiceManager.LogService
{
    public static class LogService
    {
        private static readonly Lazy<MongoClient> Client =
            new Lazy<MongoClient>(() => new MongoClient(LogConfiguration.ConnectionString));

        private static IMongoDatabase Database => Client.Value.GetDatabase(LogConfiguration.DatabaseName);

        public static async Task Generate(long characterId, string characterName, string information, LogType logType)
        {
            switch (logType)
            {
                case LogType.UpgradeEquipment:
                    var upgrade = new UppgradeLogServiceModel
                    {
                        CharacterId = characterId,
                        Name = characterName,
                        Information = information,
                        DateTime = DateTime.Now
                    };
                    await Database
                        .GetCollection<UppgradeLogServiceModel>(LogConfiguration.UpgradeEquipmentLogTable)
                        .InsertOneAsync(upgrade)
                        .ConfigureAwait(false);
                    break;

                case LogType.Exploit:
                    var exploit = new ExploitLogServiceModel
                    {
                        CharacterId = characterId,
                        Name = characterName,
                        Information = information,
                        DateTime = DateTime.Now
                    };
                    await Database
                        .GetCollection<ExploitLogServiceModel>(LogConfiguration.ExploitLogTable)
                        .InsertOneAsync(exploit)
                        .ConfigureAwait(false);
                    break;

                case LogType.UpgradeFairy:
                    var fairy = new UpgradeFairyLogServiceModel
                    {
                        CharacterId = characterId,
                        Name = characterName,
                        Message = information,
                        DateTime = DateTime.Now
                    };
                    await Database
                        .GetCollection<UpgradeFairyLogServiceModel>(LogConfiguration.UpgradeFairyLogTable)
                        .InsertOneAsync(fairy)
                        .ConfigureAwait(false);
                    break;
            }
        }

        public static async Task GenerateServerLog(string information, LogType logType)
        {
            if (logType != LogType.ServerError)
            {
                return;
            }

            var serverError = new ServerErrorLogServiceModel
            {
                Information = information,
                DateTime = DateTime.Now
            };

            await Database
                .GetCollection<ServerErrorLogServiceModel>(LogConfiguration.ServerErrorLogTable)
                .InsertOneAsync(serverError)
                .ConfigureAwait(false);
        }
    }
}
