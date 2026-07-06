using System.Collections.Generic;

namespace Frostvein.GameObject.Helpers
{
    public class QuestHelper
    {
        #region Members

        private static QuestHelper _instance;

        #endregion

        #region Instantiation

        public QuestHelper()
        {
            LoadSkipQuests();
        }

        #endregion

        #region Methods

        public void LoadSkipQuests()
        {
            SkipQuests = new List<int>();
            SkipQuests.AddRange(new List<int> { 1676, 1677, 1698, 1714, 1715, 1719, 3014, 3019 });
        }

        #endregion

        #region Properties

        public static QuestHelper Instance => _instance ?? (_instance = new QuestHelper());

        public List<int> SkipQuests { get; set; }

        #endregion
    }
}