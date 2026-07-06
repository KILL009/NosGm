



namespace Frostvein.GameObject.Service
{
    public static class PrimalQuestExtension
    {
        public static void GenerateCharacterQuest(ClientSession session, byte type)
        {
            if (session.Character.PrimalCharacterQuest == 0)
            {
                if (session.Character.PrimalQuestCount == 5)
                {
                    session.SendPacket("info You already completed 5 Primal Quests today");
                    return;
                }
                session.Character.PrimalQuestCount += 1;
                session.Character.PrimalCharacterQuest = type;
                session.SendPacket("info The Primal Quest has been accepted");
                //LOGGER($"[PrimalQuest] Name: {session.Character.Name} | Primal Quest Type: Character | Type: {type}");
            }
            else
            {
                session.SendPacket("info You already have an active Primal Character Quest");
            }
        }

        public static void GenerateRaidQuest(ClientSession session, byte type)
        {
            if (session.Character.PrimalRaidQuest == 0)
            {
                if (session.Character.PrimalQuestCount == 5)
                {
                    session.SendPacket("info You already completed 5 Primal Quests today");
                    return;
                }
                session.Character.PrimalQuestCount += 1;
                session.Character.PrimalRaidQuest = type;
                session.SendPacket("info The Primal Quest has been accepted");
                //LOGGER($"[PrimalQuest] Name: {session.Character.Name} | Primal Quest Type: Raid | Type: {type}");
            }
            else
            {
                session.SendPacket("info You already have an active Primal Character Quest");
            }
        }
    }
}