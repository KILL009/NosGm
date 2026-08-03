using NosGm.Packets.Packets.ServerPackets;
using NosGm.Domain;

namespace NosGm.GameObject.Helpers
{
    public static class StaticPacketHelper
    {
        #region Methods                       

        public static string Cancel(byte type = 0, long callerId = 0) => $"cancel {type} {callerId} -1";

        public static string CastOnTarget(UserType attackerType, long attackerId, UserType defenderType,
                                          long defenderId, short castAnimation, short castEffect, short skillVNum) => $"ct {(byte)attackerType} {attackerId} {(byte)defenderType} {defenderId} {castAnimation} {castEffect} {skillVNum}";

        public static string Die(UserType callerType, long callerId, UserType targetType, long targetId) => $"die {(byte)callerType} {callerId} {(byte)targetType} {targetId}";

        public static string GenerateEff(UserType effectType, long callerId, int effectId) => $"eff {(byte)effectType} {callerId} {effectId}";

        public static string GenerateEffT(UserType effectType, long callerId, long targetId, int effectId) => $"eff_t {(byte)effectType} {callerId} 1 {targetId} {effectId}";

        public static string GenerateGet(byte pickerType, long pickerId, long itemId) => $"get {pickerType} {pickerId} {itemId} 0";

        public static string In(UserType type, short callerVNum, long callerId, short mapX, short mapY, int direction,
                                int currentHp, int currentMp, short dialog, InRespawnType respawnType, bool isSitting, string Name,
                                bool invisible, byte unknown1, byte unknown2, byte unknown3, byte unknown4, byte unknown5, byte unknown6, byte unknown7, int icon)
        {
            switch (type)
            {
                case UserType.Npc:
                case UserType.Monster:
                    return
                        $"in {(byte)type} {callerVNum} {callerId} {mapX} {mapY} {direction} {currentHp} {currentMp} {dialog} 0 0 -1 {(byte)respawnType} {(isSitting ? 1 : 0)} -1 {Name} 0 -1 0 0 0 0 0 0 0 {(invisible ? 1 : 0)}";

                case UserType.Object:
                    return $"in 9 {callerVNum} {callerId} {mapX} {mapY} {direction} 0 0 -1";

                default:
                    return "";
            }
        }

        public static MovePacket Move(UserType type, long callerId, short positionX, short positionY, byte speed) => new MovePacket
        {
            MoveType = type,
            CallerId = callerId,
            PositionX = positionX,
            PositionY = positionY,
            Speed = speed
        };

        public static string Out(UserType type, long callerId) => $"out {(byte)type} {callerId}";

        public static string Say(byte type, long callerId, byte secondaryType, string message) => $"say {type} {callerId} {secondaryType} {message}";

        public static string SkillReset(int castId) => $"sr {castId}";

        public static string SkillResetWithCoolDown(int castId, int cooldown) => $"sr -10 {castId} {cooldown}";

        public static string SkillUsed(UserType type, long callerId, byte secondaryType, long targetId, short skillVNum,
                                       short cooldown, short attackAnimation, short skillEffect, short x, short y, bool isAlive, int health,
                                       int damage, int hitmode, byte skillType)
        {
            // The official client expects normal mate attacks to use the NPC basic
            // layout: skill 0, animation 11 and the monster BasicSkill as effect.
            // Sending BasicSkill as the packet skill made some mate models vanish
            // for the duration of the attack animation.
            if (MateCombatDiagnostics.TryConsumeBasicAttackPacket(
                    type,
                    callerId,
                    skillVNum,
                    skillEffect,
                    skillType))
            {
                skillEffect = skillVNum;
                skillVNum = 0;
                attackAnimation = 11;
            }

            return $"su {(byte)type} {callerId} {secondaryType} {targetId} {skillVNum} {cooldown} {attackAnimation} {skillEffect} {x} {y} {(isAlive ? 1 : 0)} {health} {damage} {hitmode} {skillType}";
        }

        public static string SkillUsed(UserType attackerType, long attackerId, UserType defenderType, long defenderId,
                                       short skillVNum, short cooldown, short attackAnimation, short skillEffect, short x, short y, bool isAlive,
                                       int hpPercent, int damage, int hitMode, byte skillType) => SkillUsed(attackerType, attackerId, (byte)defenderType, defenderId, skillVNum, cooldown,
                attackAnimation, skillEffect, x, y, isAlive, hpPercent, damage, hitMode, skillType);

        public static string GenerateRecovery(UserType userType, long callerId, int health) => $"rc {(byte)userType} {callerId} {health} 0";

        #endregion
    }
}