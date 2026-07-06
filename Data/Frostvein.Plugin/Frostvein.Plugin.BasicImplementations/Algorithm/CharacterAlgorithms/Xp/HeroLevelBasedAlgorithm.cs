namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Xp
{
    public class HeroLevelBasedAlgorithm : ILevelBasedDataAlgorithm
    {
        public void Initialize()
        {
            var index = 1;
            var increment = 118980;
            var increment2 = 9120;
            var increment3 = 360;

            Data = new long[256];
            Data[0] = 949560;
            for (var lvl = 1; lvl < Data.Length; lvl++)
            {
                Data[lvl] = Data[lvl - 1] + increment;
                increment2 += increment3;
                increment = increment + increment2;
                index++;
                if (index % 10 != 0) continue;

                if (index / 10 < 3)
                    increment3 -= index / 10 * 30;
                else
                    increment3 -= 30;
            }
        }

        public long[] Data { get; set; }
    }
}