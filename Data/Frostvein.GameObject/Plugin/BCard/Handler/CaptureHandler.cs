using Game.Configuration.BCards;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class CaptureHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Capture;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;

            if (caster.Character?.Session is ClientSession senderSession)
            {
                if (target.MapMonster is MapMonster mapMonster)
                {
                    if (ServerManager.GetNpcMonster(mapMonster.MonsterVNum) is NpcMonster mateNpc)
                    {
                        if (mapMonster.Monster.Catch && (senderSession.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance || senderSession.Character.MapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance))
                        {
                            if (mapMonster.Monster.Level < senderSession.Character.Level)
                            {
                                if (mapMonster.CurrentHp < (mapMonster.Monster.MaxHP / 2))
                                {
                                    // Algo
                                    int capturerate = 100 - (int)((mapMonster.CurrentHp / (double)mapMonster.Monster.MaxHP + 1) * 100 / 3);
                                    if (ServerManager.RandomNumber() <= capturerate)
                                    {
                                        if (senderSession.Character.Quests.Any(q => q.Quest.QuestType == (int)QuestType.Capture1 && q.Quest.QuestObjectives.Any(d => d.Data == mapMonster.MonsterVNum && q.GetObjectives()[d.ObjectiveIndex - 1] < q.GetObjectiveByIndex(d.ObjectiveIndex).Objective)))
                                        {
                                            senderSession.Character.IncrementQuests(QuestType.Capture1, mapMonster.MonsterVNum);
                                            mapMonster.SetDeathStatement();
                                            senderSession.CurrentMapInstance?.Broadcast(StaticPacketHelper.Out(UserType.Monster, mapMonster.MapMonsterId));
                                        }
                                        else
                                        {
                                            senderSession.Character.IncrementQuests(QuestType.Capture2, mapMonster.MonsterVNum);
                                            Mate mate = new(senderSession.Character, mateNpc, (byte)mapMonster.Monster.Level, MateType.Pet);
                                            if (senderSession.Character.CanAddMate(mate))
                                            {
                                                mate.RefreshStats();
                                                senderSession.Character.AddPetWithSkill(mate);
                                                senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CATCH_SUCCESS"), 0));
                                                senderSession.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, senderSession.Character.CharacterId, 197));
                                                senderSession.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player, senderSession.Character.CharacterId, 3, mapMonster.MapMonsterId, -1, 0, 15, -1, -1, -1, true, (int)((float)mapMonster.CurrentHp / (float)mapMonster.MaxHp * 100), 0, -1, 0));
                                                mapMonster.SetDeathStatement();
                                                senderSession.CurrentMapInstance?.Broadcast(StaticPacketHelper.Out(UserType.Monster, mapMonster.MapMonsterId));
                                            }
                                            else { senderSession.SendPacket(senderSession.Character.GenerateSay(Language.Instance.GetMessageFromKey("PET_SLOT_FULL"), 10)); }
                                        }
                                    }
                                    else { senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CATCH_FAIL"), 0)); }
                                }
                                else { senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CURRENT_HP_TOO_HIGH"), 0)); }
                            }
                            else { senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LEVEL_LOWER_THAN_MONSTER"), 0)); }
                        }
                        else { senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MONSTER_CANT_BE_CAPTURED"), 0)); }
                    }
                }
                //senderSession.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player, senderSession.Character.CharacterId, 3, session.MapEntityId, -1, 0, 15, -1, -1, -1, true, (int)((float)session.Hp / (float)session.HpMax * 100), 0, -1, 0));
                senderSession.SendPacket(StaticPacketHelper.Cancel(2, target.MapEntityId));
            }
        }
    }
}
