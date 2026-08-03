using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Battle;
using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;

namespace NosGm.GameObject.Helpers
{
    /// <summary>
    /// Keeps the pet combat probe visible in Release builds and marks the next
    /// normal pet attack packet so it can be encoded with the client-safe NPC
    /// animation layout.
    /// </summary>
    public static class MateCombatDiagnostics
    {
        private static readonly ConcurrentDictionary<long, DateTime> PendingBasicAttackPackets =
            new ConcurrentDictionary<long, DateTime>();

        private static readonly TimeSpan BasicAttackPacketLifetime = TimeSpan.FromSeconds(5);

        public static long BeginBasicAttack(Mate mate, BattleEntity target, string source)
        {
            if (mate == null)
            {
                return 0;
            }

            PendingBasicAttackPackets[mate.MateTransportId] =
                DateTime.UtcNow.Add(BasicAttackPacketLifetime);

            Logger.Info(
                $"[MATE_COMBAT] Source={source} Action=Basic Mate={mate.MateTransportId} " +
                $"Npc={mate.NpcMonsterVNum} BasicSkill={mate.Monster?.BasicSkill ?? 0} " +
                $"BasicCooldown={mate.Monster?.BasicCooldown ?? 0} " +
                $"TargetType={target?.UserType} Target={target?.MapEntityId}");

            return mate.Experience;
        }

        public static bool TryConsumeBasicAttackPacket(
            UserType attackerType,
            long attackerId,
            short skillVNum,
            short skillEffect,
            byte skillType)
        {
            if (attackerType != UserType.Npc || skillVNum <= 0 || skillEffect != 0 || skillType != 0)
            {
                return false;
            }

            if (!PendingBasicAttackPackets.TryRemove(attackerId, out DateTime expiresAt))
            {
                return false;
            }

            if (expiresAt < DateTime.UtcNow)
            {
                return false;
            }

            Logger.Info(
                $"[MATE_COMBAT_PACKET] Mate={attackerId} OriginalSkill={skillVNum} " +
                "NormalizedSkill=0 Animation=11 Effect=OriginalSkill");
            return true;
        }

        public static void ObserveExperienceAfterAttack(
            Mate mate,
            BattleEntity target,
            long experienceBefore)
        {
            if (mate == null)
            {
                return;
            }

            Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                try
                {
                    long experienceAfter = mate.Experience;
                    bool targetDied = target?.MapMonster != null &&
                                      (!target.MapMonster.IsAlive || target.MapMonster.CurrentHp <= 0);

                    if (!targetDied && experienceAfter == experienceBefore)
                    {
                        return;
                    }

                    long required = 0;
                    int levelIndex = mate.Level - 1;
                    if (levelIndex >= 0 && levelIndex < MateHelper.Instance.XpData.Length)
                    {
                        required = (long)MateHelper.Instance.XpData[levelIndex];
                    }

                    double percent = required > 0
                        ? Math.Min(100D, experienceAfter * 100D / required)
                        : 0D;

                    Logger.Info(
                        $"[MATE_XP] Owner={mate.CharacterId} Mate={mate.MateTransportId} " +
                        $"MateLevel={mate.Level} Target={target?.MapEntityId} TargetDied={targetDied} " +
                        $"Before={experienceBefore} After={experienceAfter} " +
                        $"Delta={experienceAfter - experienceBefore} Required={required} " +
                        $"Percent={percent:F2}");
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"[MATE_XP_DIAGNOSTIC_FAILED] Mate={mate.MateTransportId}",
                        ex);
                }
            });
        }
    }
}
