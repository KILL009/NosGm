using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NosTale.ServiceManager.LogService.Configuration;
using NosTale.ServiceManager.LogService.LogModel;

namespace NosTale.ServiceManager.LogService
{
    public static class LogService
    {
        public async static Task Generate(long characterId, string characterName, string information, LogType logType)
        {
            var client = new MongoClient(LogConfiguration.ConnectionString);
            var database = client.GetDatabase(LogConfiguration.DatabaseName);

            #region Collections
            var upgradeCollection = database.GetCollection<UppgradeLogServiceModel>(LogConfiguration.UpgradeEquipmentLogTable);
            var exploitCollection = database.GetCollection<ExploitLogServiceModel>(LogConfiguration.ExploitLogTable);
            var fairyCollection = database.GetCollection<UpgradeFairyLogServiceModel>(LogConfiguration.UpgradeFairyLogTable);
            #endregion

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
                    upgradeCollection.InsertOne(upgrade);
                    break;

                case LogType.Exploit:
                    var exploit = new ExploitLogServiceModel
                    {
                        CharacterId = characterId,
                        Name = characterName,
                        Information = information,
                        DateTime = DateTime.Now
                    };
                    exploitCollection.InsertOne(exploit);
                    break;

                case LogType.UpgradeFairy:
                    var fairy = new UpgradeFairyLogServiceModel
                    {
                        CharacterId = characterId,
                        Name = characterName,
                        Message = information,
                        DateTime = DateTime.Now
                    };
                    fairyCollection.InsertOne(fairy);
                    break;
            }
        }

        public static async Task GenerateServerLog(string information, LogType logType)
        {
            var client = new MongoClient(LogConfiguration.ConnectionString);
            var database = client.GetDatabase(LogConfiguration.DatabaseName);

            #region Collections
            var serverErrorCollection = database.GetCollection<ServerErrorLogServiceModel>(LogConfiguration.ServerErrorLogTable);
            #endregion

            switch (logType)
            {
                case LogType.ServerError:
                    var serverError = new ServerErrorLogServiceModel
                    {
                        Information = information,
                        DateTime = DateTime.Now
                    };
                    serverErrorCollection.InsertOne(serverError);
                    break;
            }
        }
    }
}