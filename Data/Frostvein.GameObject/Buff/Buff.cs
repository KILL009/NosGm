/*
 * This file is part of the Frostvein Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using Frostvein.GameObject.Battle;
using Frostvein.GameObject.Networking;
using System;

namespace Frostvein.GameObject
{
    public enum BuffCasterType
    {
        Normal,
        Pet,
        Partner
    }
    public class Buff
    {
        #region Members

        public int Level;

        public bool IsPermaBuff { get; set; }

        #endregion Members

        #region Instantiation

        public Buff(short id, int level, bool isPermaBuff = false, BuffCasterType caster = BuffCasterType.Normal)
        {
            Card = ServerManager.GetCard(id);
            Level = level;
            IsPermaBuff = isPermaBuff;
            BuffCasterType = caster;
        }

        public Buff(short id, int level, BuffCasterType caster = BuffCasterType.Normal)
        {
            Card = ServerManager.GetCard(id);
            Level = level;
            IsPermaBuff = false;
            BuffCasterType = caster;
        }

        public Buff(short id, int level)
        {
            Card = ServerManager.GetCard(id);
            Level = level;
            IsPermaBuff = false;
            BuffCasterType = BuffCasterType.Normal;
        }

        #endregion Instantiation

        #region Properties

        public BuffCasterType BuffCasterType { get; set; }

        public Card Card { get; set; }

        public int RemainingTime { get; set; }

        public DateTime Start { get; set; }

        public bool StaticBuff { get; set; }

        public IDisposable StaticVisualEffect { get; set; }

        public BattleEntity Sender { get; set; }

        public short? SkillVNum { get; set; }

        #endregion Properties
    }
}