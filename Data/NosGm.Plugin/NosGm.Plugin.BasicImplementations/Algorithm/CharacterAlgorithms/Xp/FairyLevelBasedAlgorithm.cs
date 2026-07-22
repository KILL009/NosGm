namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Xp
{
    public class FairyLevelBasedAlgorithm : ILevelBasedDataAlgorithm
    {
        public void Initialize()
        {
            Data = new long[256];
            for (var i = 0; i < Data.Length; i++) Data[i] = i < 40 ? i * i + 50 : i * i * 3 + 50;
        }

        public long[] Data { get; set; }
    }
}