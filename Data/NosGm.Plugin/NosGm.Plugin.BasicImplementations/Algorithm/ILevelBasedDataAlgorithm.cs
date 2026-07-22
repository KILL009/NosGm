namespace Plugins.BasicImplementations.Algorithm
{
    public interface ILevelBasedDataAlgorithm
    {
        long[] Data { get; set; }
        void Initialize();
    }
}