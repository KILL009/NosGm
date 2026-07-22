using Autofac;
using ChickenAPI.Plugins;
using NosGm.Core;
using NosGm.GameObject._Algorithm;

namespace Plugins.BasicImplementations.Algorithm
{
    public class AlgorithmPluginCore : ICorePlugin
    {
        public string Name => nameof(AlgorithmPluginCore);

        public void OnDisable()
        {
        }

        public void OnEnable()
        {
        }

        public void OnLoad(ContainerBuilder builder)
        {
            builder.Register(s => new AlgorithmService()).As<IAlgorithmService>();
            builder.Register(s => new NpcMonsterAlgorithmService()).As<INpcMonsterAlgorithmService>();
            builder.Register(s => new DamageAlgorithm()).As<IDamageAlgorithm>();
        }
    }
}