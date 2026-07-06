using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Extension
{
    public static class FamilyExtensions
    {
        #region Methods

        /// <summary> GENERATE FMI && FMP PACKETS SHALL BE AWAITED OR THREADED AFTER 2-3 seconds
        /// after incremeting progress to mission cause some missions incrementation are ran asynchronously
        ///
        /// IT HAVE TO BE RUN ONCE FOR ENTIRE FAMILY, NOT FOR EACH CHARACTER !!! (IMPORTANT) --
        /// check SessionsToFamilies(List<Character>) function </summary> <param name="f"></param>
        /// <param name="vnum"></param> Value - ammount of missions done at once (to prevent lags)
        /// <param name="value"></param>
        public static void AddMissionProgress(this Family f, short vnum, short value, byte incrementation = 0)
        {
            var missionData = FamilySystemHelper.GetMissValues(vnum);
            if (missionData == null) return;

            //Mission dont exist
            if (!f.FamilySkillMissions.Any(s => s.ItemVNum == vnum))
            {
                FamilySkillMissionDTO newMission = new FamilySkillMissionDTO()
                {
                    FamilyId = f.FamilyId,
                    ItemVNum = vnum,
                    CurrentValue = value,
                    Date = DateTime.Now, //DATE IS NEEDED ONLY IN NORMAL MISSIONS (DateEnd/LastUpdate)
                    TotalValue = value, //TOTAL VALUE IS NEEDED ONLY IN DAILY MISSIONS
                };

                f.FamilySkillMissions.Add(new FamilySkillMission(newMission));
                DAOFactory.FamilySkillMissionDAO.InsertOrUpdate(ref newMission);
                if (value >= missionData[2])
                {
                    f.GenerateReward(vnum);
                    Task.Run(() => f.AddMissionProgress((short)(vnum + 1), value, (byte)(incrementation + 1))); //CREATE NEW MISSION (if such exist)
                }
                ServerManager.Instance.FamilyRefresh(f.FamilyId);
            }

            //MISSION PROGRESS
            else
            {
                if (missionData[0].Equals(1)) //DailyMissions
                {
                    if (f.FamilySkillMissions.FirstOrDefault(s => s.ItemVNum == vnum) is FamilySkillMission mis)
                    {
                        if (mis.CurrentValue < missionData[2])
                        {
                            mis.CurrentValue += value;

                            if (mis.CurrentValue > missionData[2]) //MISSION COMPLETE
                            {
                                f.GenerateReward(vnum);
                                mis.CurrentValue = missionData[2];
                            }
                        }

                        mis.TotalValue += value;
                        f.SaveMission(mis);
                    }
                }
                else if (missionData[0].Equals(2)) //NormalMissions
                {
                    if (f.FamilySkillMissions.FirstOrDefault(s => s.ItemVNum == vnum) is FamilySkillMission mis && mis != null)
                    {
                        if (mis.CurrentValue < missionData[2])
                        {
                            mis.CurrentValue += value;
                            mis.TotalValue += value;

                            if (mis.CurrentValue >= missionData[2]) //QUEST COMPLETE
                            {
                                if (FamilySystemHelper.IsBase((short)(vnum - incrementation)) && incrementation < 4)
                                {
                                    f.GenerateReward(vnum);
                                    Task.Run(() => f.AddMissionProgress((short)(vnum + 1), (short)mis.TotalValue, (byte)(incrementation + 1))); //CREATE NEW MISSION (if such exist)
                                }
                                else if (incrementation == 4)
                                    f.GenerateReward(vnum);

                                mis.CurrentValue = missionData[2];
                            }
                            f.SaveMission(mis);
                        }
                        else
                        {
                            if (FamilySystemHelper.IsBase((short)(vnum - incrementation)) && incrementation < 4)
                            {
                                Task.Run(() => f.AddMissionProgress((short)(vnum + 1), value, (byte)(incrementation + 1)));
                            }
                        }
                    }
                }
            }
        }

        public static void AddStaticExtension(this Family f, short vnum)
        {
            lock (f.FamilySkillMissions)
            {
                FamilySkillMissionDTO newExtension = new FamilySkillMissionDTO()
                {
                    FamilyId = f.FamilyId,
                    ItemVNum = vnum,
                    CurrentValue = 1,  //FOR BUFFS (IF HAVE 1 - can be used 0 - cant be used)
                    Date = DateTime.Now,
                    TotalValue = 0,
                };

                f.FamilySkillMissions.Add(new FamilySkillMission(newExtension));
                DAOFactory.FamilySkillMissionDAO.InsertOrUpdate(ref newExtension);
            }

            if (vnum == 9696 || vnum == 9697)
            {
                f.MaxSize = (short)(vnum == 9696 ? 70 : 100);
                FamilyDTO fam = f;
                DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);
            }
        }

        /// <summary> USEFULL FOR INCREMENTING MISSIONS FROM RAID, WHERE YOU HAVE ONLY
        /// LIST<CLIENTSESSION> </summary> <param name="characters"></param> 
        /// <returns> List of families without duplicates from list of characters </returns>
        public static List<Family> SessionsToFamilies(List<ClientSession> sessions)
        {
            List<Family> list = new List<Family> { };
            foreach (var s in sessions)
            {
                if (s.Character?.Family == null) continue;

                if (!list.Any(f => f.FamilyId == s.Character.Family.FamilyId)) list.Add(s.Character.Family);
            }

            return list;
        }

        public static bool CheckBuff(this Family f, short vnum)
        {
            if (f.FamilySkillMissions.FirstOrDefault(s => s.ItemVNum == vnum) is FamilySkillMission fsm && fsm != null)
            {
                if (fsm.CurrentValue > 0)
                {
                    return true; //CAN BE USED !
                }
            }

            return false; //CANT
        }

        public static byte CheckFsmStatus(this Family f, short[] staticData, short vnum)
        {
            //staticData[1] - min family level
            if (f.FamilyLevel >= staticData[1])
            {
                if (staticData[0].Equals(0) || staticData[0].Equals(1) || staticData[0].Equals(2)) // FOR FMI PACKET
                {
                    if (f.FamilySkillMissions.FirstOrDefault(s => s.ItemVNum == vnum) is FamilySkillMission fsm && fsm != null)
                    {
                        //staticData[2] - Max Value for mission
                        if (fsm.CurrentValue >= staticData[2])
                        {
                            return 1; // Already Done
                        }
                        else
                        {
                            return 2; //IN PROGRESS
                        }
                    }
                    else if (!staticData[0].Equals(2) || FamilySystemHelper.IsBase(vnum))
                    {
                        return 2;
                    }
                }
            }
            return 0; //Too low level
        }

        internal static void GenerateReward(this Family f, short vnum)
        {
            var fl = FamilySystemHelper.GetMissValues(vnum);
            if (fl[3] != null)
            {
                f.InsertFamilyLog(fl[0] == 2 ? FamilyLogType.FamilyExtension : FamilyLogType.FamilyMission, itemVNum: (short)(9000 + fl[3]));
            }

            var ammount = FamilySystemHelper.XpReward(vnum);
            var extension = FamilySystemHelper.ExtensionReward(vnum);

            if (ammount > 0)
            {
                f.GenerateFamilyExp(ammount);
            }

            if (extension > 9600 && extension < 9800)
            {
                f.AddStaticExtension((short)extension);
            }
        }

        private static void GenerateFamilyExp(this Family f, int FXP)
        {
            FamilyDTO fam = f;
            fam.FamilyExperience += FXP;
            if (CharacterHelper.LoadFamilyXPData(fam.FamilyLevel) <= fam.FamilyExperience)
            {
                fam.FamilyExperience -= CharacterHelper.LoadFamilyXPData(fam.FamilyLevel);
                fam.FamilyLevel++;
                f.AddMissionProgress((short)(9616 + fam.FamilyLevel), 1);
                f.InsertFamilyLog(FamilyLogType.FamilyLevelUp, level: fam.FamilyLevel);
                f.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("FAMILY_UP"), 0));
            }

            DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);
            ServerManager.Instance.FamilyRefresh(fam.FamilyId);
        }

        /// <summary>
        /// Save eventual progress to database
        /// </summary>
        /// <param name="f"></param>
        /// <param name="fsm"></param>
        public static void SaveMission(this Family f, FamilySkillMission fsm)
        {
            try
            {
                FamilySkillMissionDTO dto = new FamilySkillMissionDTO
                {
                    FamilyId = f.FamilyId,
                    CurrentValue = fsm.CurrentValue,
                    TotalValue = fsm.TotalValue,
                    ItemVNum = fsm.ItemVNum,
                    Date = DateTime.Now,
                };

                DAOFactory.FamilySkillMissionDAO.InsertOrUpdate(ref dto);
                ServerManager.Instance.FamilyRefresh(f.FamilyId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
