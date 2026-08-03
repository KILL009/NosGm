using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class MinilandRefresh
    {
        private const int DailyMinilandPoints = 2000;
        private const int ReputationPerVisit = 2;
        private const string VisitLogType = "MINILAND";
        private const string RefreshLogType = "MINILAND_REFRESH";

        public static void GenerateMinilandEvent()
        {
            Task.Run(RefreshAsync);
        }

        private static async Task RefreshAsync()
        {
            int onlineCharacters = 0;
            int offlineCharacters = 0;
            int reputationAwards = 0;
            DateTime visitCutoff = DateTime.Now.AddDays(-1);

            try
            {
                await SaveEvent.SaveAll().ConfigureAwait(false);

                foreach (CharacterDTO character in DAOFactory.CharacterDAO.LoadAll())
                {
                    if (character == null)
                    {
                        continue;
                    }

                    ClientSession session = ServerManager.Instance
                        .GetSessionByCharacterId(character.CharacterId);
                    bool isLocal = session?.Character != null;

                    // Every channel owns its local sessions. Channel 1 additionally
                    // handles offline characters, while characters connected to a
                    // different channel are deliberately left to that channel.
                    if (!isLocal)
                    {
                        if (ServerManager.Instance.ChannelId != 1 ||
                            CommunicationServiceClient.Instance.IsCharacterConnected(
                                ServerManager.Instance.ServerGroup,
                                character.CharacterId))
                        {
                            continue;
                        }
                    }

                    bool alreadyRewarded = DAOFactory.GeneralLogDAO
                        .LoadByLogType(RefreshLogType, character.CharacterId, true)
                        .Any();
                    int visitCount = 0;

                    if (!alreadyRewarded)
                    {
                        visitCount = DAOFactory.GeneralLogDAO
                            .LoadByLogType(VisitLogType, character.CharacterId)
                            .Count(log => log.Timestamp >= visitCutoff);
                    }

                    if (isLocal)
                    {
                        session.Character.MinilandPoint = DailyMinilandPoints;
                        onlineCharacters++;

                        if (!alreadyRewarded && visitCount > 0)
                        {
                            session.Character.GetReputation(ReputationPerVisit * visitCount);
                            reputationAwards += ReputationPerVisit * visitCount;
                        }
                    }
                    else
                    {
                        character.MinilandPoint = DailyMinilandPoints;
                        if (!alreadyRewarded && visitCount > 0)
                        {
                            character.Reputation += ReputationPerVisit * visitCount;
                            reputationAwards += ReputationPerVisit * visitCount;
                        }

                        await DAOFactory.CharacterDAO
                            .InsertOrUpdate(character)
                            .ConfigureAwait(false);
                        offlineCharacters++;
                    }

                    if (!alreadyRewarded)
                    {
                        DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
                        {
                            AccountId = character.AccountId,
                            CharacterId = character.CharacterId,
                            LogData = $"Visits={visitCount};Reputation={ReputationPerVisit * visitCount}",
                            LogType = RefreshLogType,
                            Timestamp = DateTime.Now
                        });
                    }
                }

                Logger.Info(
                    $"[MINILAND_REFRESH] Result=Completed Online={onlineCharacters} " +
                    $"Offline={offlineCharacters} ReputationAwarded={reputationAwards}");
            }
            catch (Exception exception)
            {
                Logger.Error("[MINILAND_REFRESH] Result=Failed", exception);
            }
            finally
            {
                GameEventHandler.CompleteEvent(EventType.MINILANDREFRESHEVENT);
            }
        }
    }
}
