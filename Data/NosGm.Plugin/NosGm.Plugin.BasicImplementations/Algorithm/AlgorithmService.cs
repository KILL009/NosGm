using NosGm.Domain;
using NosGm.GameObject._Algorithm;
using Plugins.BasicImplementations.Algorithm.CharacterAlgorithms;
using Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Close;
using Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Damage;
using Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Distance;
using Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.HpMp;
using Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Magical;
using Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Xp;
using Plugins.BasicImplementations.Algorithm.FamilyAlgorithms;

namespace Plugins.BasicImplementations.Algorithm
{
    public class AlgorithmService : IAlgorithmService
    {
        private readonly ICharacterStatAlgorithm _closeDefenceAlgorithm;
        private readonly ICharacterStatAlgorithm _closeDodgeAlgorithm;
        private readonly ICharacterStatAlgorithm _criticalDist;
        private readonly ICharacterStatAlgorithm _criticalDistRate;
        private readonly ICharacterStatAlgorithm _criticalHit;
        private readonly ICharacterStatAlgorithm _criticalHitRate;
        private readonly ILevelBasedDataAlgorithm _fairyLevelBasedAlgorithm;
        private readonly ILevelBasedDataAlgorithm _familyLevelBasedAlgorithm;
        private readonly ILevelBasedDataAlgorithm _heroLevelBasedAlgorithm;
        private readonly ICharacterStatAlgorithm _hitRate;
        private readonly ICharacterStatAlgorithm _distanceHitRate;

        private readonly ICharacterStatAlgorithm _hpMax;
        private readonly ICharacterStatAlgorithm _hpRegen;
        private readonly ICharacterStatAlgorithm _hpRegenSitting;
        private readonly JobLevelBasedAlgorithm _jobLevelBasedAlgorithm;
        private readonly ILevelBasedDataAlgorithm _levelBasedAlgorithm;
        private readonly ICharacterStatAlgorithm _magicDefenceAlgorithm;
        private readonly ICharacterStatAlgorithm _magicDodgeAlgorithm;
        private readonly ICharacterStatAlgorithm _secondaryDamage;
        private readonly ICharacterStatAlgorithm _Damage;
        private readonly ICharacterStatAlgorithm _mpMax;
        private readonly ICharacterStatAlgorithm _mpRegen;
        private readonly ICharacterStatAlgorithm _mpRegenSitting;
        private readonly ICharacterStatAlgorithm _rangedDefenceAlgorithm;
        private readonly ICharacterStatAlgorithm _rangedDodgeAlgorithm;

        private readonly ICharacterStatAlgorithm _speedAlgorithm;
        private readonly ILevelBasedDataAlgorithm _spLevelBasedAlgorithm;

        public AlgorithmService()
        {
            _levelBasedAlgorithm = new LevelBasedAlgorithm();
            _jobLevelBasedAlgorithm = new JobLevelBasedAlgorithm();
            _spLevelBasedAlgorithm = new SpLevelBasedAlgorithm();
            _heroLevelBasedAlgorithm = new HeroLevelBasedAlgorithm();
            _fairyLevelBasedAlgorithm = new FairyLevelBasedAlgorithm();
            _familyLevelBasedAlgorithm = new FamilyLevelBasedAlgorithm();

            _speedAlgorithm = new SpeedAlgorithm();

            _closeDefenceAlgorithm = new CloseDefenceAlgorithm();
            _rangedDefenceAlgorithm = new RangedDefenceAlgorithm();
            _magicDefenceAlgorithm = new MagicDefenceAlgorithm();

            _closeDodgeAlgorithm = new CloseDodgeAlgorithm();
            _rangedDodgeAlgorithm = new RangedDodgeAlgorithm();
            _magicDodgeAlgorithm = new MagicDodgeAlgorithm();

            _Damage = new CharacterAlgorithms.Damage.DamageAlgorithm();
            _secondaryDamage = new SecondaryDamageAlgorithm();
            _hitRate = new HitRateAlgorithm();
            _distanceHitRate = new DistanceHitRateAlgorithm();
            _criticalHitRate = new CriticalHitRateAlgorithm();
            _criticalHit = new CriticalHitAlgorithm();
            _criticalDistRate = new CriticalDistRateAlgorithm();
            _criticalDist = new CriticalDistAlgorithm();

            _hpMax = new HpMax();
            _hpRegen = new HpRegen();
            _hpRegenSitting = new HpRegenSitting();

            _mpMax = new MpMax();
            _mpRegen = new MpRegen();
            _mpRegenSitting = new MpRegenSitting();

            _levelBasedAlgorithm.Initialize();
            _jobLevelBasedAlgorithm.Initialize();
            _spLevelBasedAlgorithm.Initialize();
            _heroLevelBasedAlgorithm.Initialize();
            _fairyLevelBasedAlgorithm.Initialize();
            _familyLevelBasedAlgorithm.Initialize();

            _closeDefenceAlgorithm.Initialize();
            _rangedDefenceAlgorithm.Initialize();
            _magicDefenceAlgorithm.Initialize();

            _closeDodgeAlgorithm.Initialize();
            _rangedDodgeAlgorithm.Initialize();
            _magicDodgeAlgorithm.Initialize();

            _Damage.Initialize();
            _secondaryDamage.Initialize();
            _hitRate.Initialize();
            _distanceHitRate.Initialize();
            _criticalHitRate.Initialize();
            _criticalHit.Initialize();
            _criticalDistRate.Initialize();
            _criticalDist.Initialize();

            _hpMax.Initialize();
            _hpRegen.Initialize();
            _hpRegenSitting.Initialize();
            _mpMax.Initialize();
            _mpRegen.Initialize();
            _mpRegenSitting.Initialize();
        }

        public int GetDistCritical(ClassType type, byte level) => _criticalDist.GetStat(type, level);

        public int GetDistCriticalRate(ClassType type, byte level) => _criticalDistRate.GetStat(type, level);

        public int GetHitCritical(ClassType type, byte level) => _criticalHit.GetStat(type, level);

        public int GetHitCriticalRate(ClassType type, byte level) => _criticalHitRate.GetStat(type, level);

        public int GetHitRate(ClassType type, byte level) => _hitRate.GetStat(type, level);

        public int GetDistanceHitRate(ClassType type, byte level) => _distanceHitRate.GetStat(type, level);

        public int GetMaxDistance(ClassType type, byte level) => _secondaryDamage.GetStat(type, level);

        public int GetMaxHit(ClassType type, byte level) => _Damage.GetStat(type, level);

        public int GetMinHit(ClassType type, byte level) => _Damage.GetStat(type, level);

        public long GetLevelXp(ClassType type, byte level) => _levelBasedAlgorithm.Data[level - 1 > 0 ? level - 1 : 0];

        public int GetJobLevelXp(ClassType type, byte level) => (int)(type == ClassType.Adventurer
                ? _jobLevelBasedAlgorithm.FirstJobXpData[
                        level - 1 > 0
                                ? level - 1 >= _jobLevelBasedAlgorithm.FirstJobXpData.Length
                                        ? _jobLevelBasedAlgorithm.FirstJobXpData.Length - 1
                                        : level - 1
                                : 0]
                : _jobLevelBasedAlgorithm.Data[
                        level - 1 > 0
                                ? level - 1 >= _jobLevelBasedAlgorithm.Data.Length
                                        ? _jobLevelBasedAlgorithm.Data.Length - 1
                                        : level - 1
                                : 0]);

        public int GetHeroLevelXp(ClassType type, byte level) => (int)_heroLevelBasedAlgorithm.Data[level - 1 > 0 ? level - 1 : 0];

        public int GetSpLevelXp(byte level) => (int)_spLevelBasedAlgorithm.Data[level - 1];

        public int GetFairyLevelXp(byte level) => (int)_fairyLevelBasedAlgorithm.Data[level - 1];

        public int GetFamilyLevelXp(byte level) => (int)_familyLevelBasedAlgorithm.Data[
                level > _familyLevelBasedAlgorithm.Data.Length
                        ? _familyLevelBasedAlgorithm.Data.Length - 1
                        : level - 1];

        public int GetSpeed(ClassType type, byte level) => _speedAlgorithm.GetStat(type, level);

        public int GetDefenceClose(ClassType type, byte level) => _closeDefenceAlgorithm.GetStat(type, level);

        public int GetDefenceRange(ClassType type, byte level) => _rangedDefenceAlgorithm.GetStat(type, level);

        public int GetDefenceMagic(ClassType type, byte level) => _magicDefenceAlgorithm.GetStat(type, level);

        public int GetDodgeClose(ClassType type, byte level) => _closeDodgeAlgorithm.GetStat(type, level);

        public int GetDodgeRanged(ClassType type, byte level) => _rangedDodgeAlgorithm.GetStat(type, level);

        public int GetDodgeMagic(ClassType type, byte level) => _magicDodgeAlgorithm.GetStat(type, level);

        public int GetMinimumAttackRange(ClassType type, byte level) => _secondaryDamage.GetStat(type, level);

        public int GetHpMax(ClassType type, byte level) => _hpMax.GetStat(type, level);

        public int GetMpMax(ClassType type, byte level) => _mpMax.GetStat(type, level);

        public int GetHpRegen(ClassType type, byte level) => _hpRegen.GetStat(type, level);

        public int GetHpRegenSitting(ClassType type, byte level) => _hpRegenSitting.GetStat(type, level);

        public int GetMpRegen(ClassType type, byte level)
        {
            return _mpRegen.GetStat(type, level);
        }

        public int GetMpRegenSitting(ClassType type, byte level)
        {
            return _mpRegenSitting.GetStat(type, level);
        }
    }
}