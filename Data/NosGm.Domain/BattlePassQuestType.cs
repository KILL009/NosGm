using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Domain
{
    public enum BpQuestType : byte
    {
        ComplimentPlayer = 1,
        CompleteRaid = 2,
        CompleteDailyQuest = 3,
        KillMonster = 4,
        CompleteBA = 5,
        CompleteIC = 6,
        CompleteTalentArena = 7,
        PlayMinigames = 8,
        CatchFish = 9,
        CompleteTimeSpaceOrCT = 10,
        KillCursedMonster = 11,
        CompleteMasterArena = 12,
        CookMeal = 13,
        CompleteMinigameRaid = 14,
        CompleteCaligor = 15,
        Upgrade = 16,
        Craft = 17,
        Act4PlayerKill = 18,
        GainReputation = 19,
        StayLogged = 20,
        KillMapBoss = 21,
        ReachCTLevel = 22,
        LoginRow = 23,
        SpendGold = 24,
        CompleteTimeSpace = 25,
        CompleteHidenTimeSpace = 26,
        CompleteLevelsOnCT = 27,
        GainsPointOnCombatArena = 28
    }
}
