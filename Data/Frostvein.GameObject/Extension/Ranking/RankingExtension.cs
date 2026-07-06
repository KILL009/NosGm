using Frostvein.Data;
using Frostvein.GameObject.Networking;

namespace Frostvein.GameObject.Service
{
    public static class RankingExtension
    {
        public static void GenerateComplimentRanking(ClientSession session)
        {
            /*
            string clinit = "clinit";
            foreach (CharacterDTO character in ServerManager.Instance.TopDuel)
            {
                clinit += $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Compliment}|{character.Name}";
            }
            session.SendPacket(clinit);
            */
        }

        public static void GenerateDuelRanking(ClientSession session)
        {
            string clinit = "clinit";
            foreach (CharacterDTO character in ServerManager.Instance.TopDuel)
            {
                clinit += $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.DuelWon}|{character.Name}";
            }
            session.SendPacket(clinit);
        }

        public static void GenerateReputationRanking(ClientSession session)
        {
            string flinit = "flinit";
            foreach (CharacterDTO character in ServerManager.Instance.TopReputation)
            {
                flinit +=
                    $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Reputation}|{character.Name}";
            }
            session.SendPacket(flinit);
        }

        public static void GenerateMonsterRanking(ClientSession session)
        {
            string kdlinit = "kdlinit";
            foreach (CharacterDTO character in ServerManager.Instance.TopMonster)
            {
                kdlinit +=
                    $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.MonsterCount}|{character.Name}";
            }
            session.SendPacket(kdlinit);
        }
    }
}