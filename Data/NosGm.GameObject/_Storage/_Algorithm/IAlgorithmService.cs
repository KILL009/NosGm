using NosGm.Domain;

namespace NosGm.GameObject._Algorithm
{
    public interface IAlgorithmService
    {
        #region Xp

        /// <summary>
        ///     This method will search through algorithm service and return the LevelXp stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        long GetLevelXp(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the JobLevelXp stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetJobLevelXp(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the HeroLevelXp stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetHeroLevelXp(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the SpLevelXp stat based on <see cref="SpType" /> and
        ///     level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetSpLevelXp(byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the Fairy Level Xp stat based on the fairy's level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetFairyLevelXp(byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the Fairy Level Xp stat based on the fairy's level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetFamilyLevelXp(byte level);

        #endregion Xp

        #region Stats

        /// <summary>
        ///     This method will search through algorithm service and return the Speed stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetSpeed(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the Close Defence stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetDefenceClose(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the Ranged Defence stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetDefenceRange(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the Magic Defence stat
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetDefenceMagic(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the Dodge close stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetDodgeClose(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the Dodge Ranged stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetDodgeRanged(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the Dodge Magic stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        ///     /!\/!\ Even if the base game logic tells magic attack does not miss, you can customise this :)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetDodgeMagic(ClassType type, byte level);

        /// <summary>
        ///     This method will search through algorithm service and return the minimum attack range stat based on
        ///     <see cref="ClassType" /> and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetMinimumAttackRange(ClassType type, byte level);

        int GetDistCritical(ClassType type, byte level);

        int GetHitCritical(ClassType type, byte level);

        int GetHitCriticalRate(ClassType type, byte level);

        int GetHitRate(ClassType type, byte level);

        int GetDistanceHitRate(ClassType type, byte level);

        int GetDistCriticalRate(ClassType type, byte level);

        int GetMaxDistance(ClassType type, byte level);

        int GetMaxHit(ClassType type, byte level);

        int GetMinHit(ClassType type, byte level);

        #endregion Stats

        #region HpMp

        /// <summary>
        ///     Returns the maximum of Hp of a character, based on class and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetHpMax(ClassType type, byte level);

        /// <summary>
        ///     Returns the maximum of Hp of a character, based on class and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetMpMax(ClassType type, byte level);

        #region HpMpRegen

        /// <summary>
        ///     Returns the HpRegen when player is standing, based on class and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetHpRegen(ClassType type, byte level);

        /// <summary>
        ///     Returns the HpRegen when player is sitting, based on class and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetHpRegenSitting(ClassType type, byte level);

        /// <summary>
        ///     Returns the MpRegen when player is standing, based on class and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetMpRegen(ClassType type, byte level);

        /// <summary>
        ///     Returns the HpRegen when player is sitting, based on class and level
        ///     /!\ Should return the highest value under level if level is out of range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        int GetMpRegenSitting(ClassType type, byte level);

        #endregion HpMpRegen

        #endregion HpMp
    }
}