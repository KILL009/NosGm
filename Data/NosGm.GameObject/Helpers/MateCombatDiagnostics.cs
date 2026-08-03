using NosGm.Configuration;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Battle;
using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;

namespace NosGm.GameObject.Helpers
{
    /// <summary>
    /// Keeps pet combat probes visible in Release builds, marks normal pet attack
    /// packets for the legacy client layout and verifies pet experience after kills.
    /// </summary>
    public static class MateCombatDiagnostics
    {
        private static readonly ConcurrentDictionary<long, DateTime> PendingBasicAttackPackets =
            new ConcurrentDictionary<long, DateTime>();

        private static readonly ConcurrentDictionary<long, byte> AwardedPetKills =
            new ConcurrentDictionary<long, byte>();

        private static readonly TimeSpan BasicAttackPacketLifetime = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan KillDeduplicationLifetime = TimeSpan.FromSeconds(8);

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
                "NormalizedSkill=0 Animation=11 Effect=OriginalSkill X=0 Y=0");
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

            Observable.Timer(TimeSpan.FromMilliseconds(1500)).Subscribe(_ =>
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

                    string source = "CharacterKillReward";
                    int fallbackXp = 0;
                    if (targetDied && experienceAfter == experienceBefore)
                    {
                        fallbackXp = TryAwardIndependentPetKillExperience(mate, target);
                        experienceAfter = mate.Experience;
                        source = fallbackXp > 0 ? "PetLevelReward" : "NoReward";
                    }

                    long required = GetRequiredExperience(mate);
                    double percent = required > 0
                        ? Math.Min(100D, experienceAfter * 100D / required)
                        : 0D;

                    Logger.Info(
                        $"[MATE_XP] Source={source} Owner={mate.CharacterId} " +
                        $"Mate={mate.MateTransportId} MateLevel={mate.Level} " +
                        $"Target={target?.MapEntityId} TargetDied={targetDied} " +
                        $"Before={experienceBefore} After={experienceAfter} " +
                        $"Delta={experienceAfter - experienceBefore} Awarded={fallbackXp} " +
                        $"Required={required} Percent={percent:F2}");
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"[MATE_XP_DIAGNOSTIC_FAILED] Mate={mate.MateTransportId}",
                        ex);
                }
            });
        }

        private static int TryAwardIndependentPetKillExperience(Mate mate, BattleEntity target)
        {
            if (mate?.Owner?.Session == null ||
                !mate.IsTeamMember ||
                !mate.IsAlive ||
                mate.Level >= mate.Owner.Level ||
                target?.MapMonster?.Monster == null)
            {
                return 0;
            }

            long targetId = target.MapEntityId;
            long killKey = ((long)mate.MateTransportId << 32) ^ (uint)targetId;
            if (!AwardedPetKills.TryAdd(killKey, 0))
            {
                return 0;
            }

            Observable.Timer(KillDeduplicationLifetime).Subscribe(_ =>
            {
                AwardedPetKills.TryRemove(killKey, out _);
            });

            NpcMonster monster = target.MapMonster.Monster;
            int rawXp = Math.Max(0, monster.XP);
            byte monsterLevel = (byte)Math.Max(1, Math.Min(byte.MaxValue, monster.Level));
            int levelDifference = mate.Level - monsterLevel;
            double rate = GameConfiguration.XPRate + (mate.Owner.MapInstance?.XpRate ?? 0);
            double baseXp = levelDifference < 5 ? rawXp : rawXp / 3D * 2D;
            double calculatedXp = baseXp *
                                  CharacterHelper.ExperiencePenalty(mate.Level, monsterLevel) *
                                  rate;

            if (mate.Level <= 5 && levelDifference < -4)
            {
                calculatedXp *= 1.5D;
            }

            int petXp = rawXp > 0
                ? Math.Max(1, (int)calculatedXp)
                : 0;

            if (petXp <= 0)
            {
                Logger.Info(
                    $"[MATE_XP_REWARD_SKIPPED] Mate={mate.MateTransportId} " +
                    $"Target={targetId} Monster={monster.NpcMonsterVNum} " +
                    $"MonsterLevel={monsterLevel} RawXp={rawXp} Rate={rate:F2}");
                return 0;
            }

            long before = mate.Experience;
            mate.GenerateXp(petXp);
            mate.Owner.Session.SendPacket(mate.GenerateScPacket());

            Logger.Info(
                $"[MATE_XP_REWARD] Mate={mate.MateTransportId} Target={targetId} " +
                $"Monster={monster.NpcMonsterVNum} MonsterLevel={monsterLevel} " +
                $"RawXp={rawXp} Rate={rate:F2} Award={petXp} " +
                $"Before={before} After={mate.Experience}");

            return petXp;
        }

        private static long GetRequiredExperience(Mate mate)
        {
            int levelIndex = mate.Level - 1;
            if (levelIndex < 0 || levelIndex >= MateHelper.Instance.XpData.Length)
            {
                return 0;
            }

            return (long)MateHelper.Instance.XpData[levelIndex];
        }
    }
}
