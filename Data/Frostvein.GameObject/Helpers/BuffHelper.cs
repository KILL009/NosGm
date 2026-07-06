using System.Collections.Generic;

namespace Frostvein.GameObject.Helpers
{
    public class BuffHelper
    {
        #region Members

        private static BuffHelper _instance;

        #endregion Members

        #region Instantiation

        public BuffHelper()
        {
            Syncope = new List<int> { 7, 196 };
            SyncopeGlobal = new List<int> { 7, 66, 100, 195, 196, 197, 198, 662 };
            BleedingMinor = new List<int> { 1, 189 };
            BleedingMinorBleeding = new List<int> { 1, 189, 21, 190 };
            BleedingGlobal = new List<int> { 1, 21, 42, 82, 189, 190, 191, 192 };
            ReducedParalysis = new List<int> { 59 };
            Freeze = new List<int> { 27, 200 };
            Blinding = new List<int> { 37 };
            Slowness = new List<int> { 99, 464 };
            Shock = new List<int> { 70 };
            PoisonParalysis = new List<int> { 22, 54, 224, 225, 226, 227 };
            BleedingStuff = new List<int> { 1, 21, 189, 190 };
            Syncope = new List<int> { 7, 100, 195, 196 };
            SyncopeStuff = new List<int> { 7, 100, 195, 196, 206, 214 };
        }

        #endregion Instantiation

        #region Properties

        public static BuffHelper Instance => _instance = (_instance ?? new BuffHelper());

        public List<int> BleedingGlobal { get; set; }

        public List<int> BleedingMinor { get; set; }

        public List<int> BleedingMinorBleeding { get; set; }

        public List<int> BleedingStuff { get; set; }

        public List<int> Blinding { get; set; }

        public List<int> Freeze { get; set; }

        public List<int> PoisonParalysis { get; set; }

        public List<int> ReducedParalysis { get; set; }

        public List<int> Shock { get; set; }

        public List<int> Slowness { get; set; }

        public List<int> Syncope { get; set; }

        public List<int> SyncopeGlobal { get; set; }

        public List<int> SyncopeStuff { get; set; }

        #endregion Properties
    }
}