using Autofac;
using ChickenAPI.Plugins;
using NosGm.Core.Extensions;
using NosGm.GameObject._Guri;

namespace Plugins.BasicImplementations.Guri
{
    public class GuriPluginCore : ICorePlugin
    {
        public string Name => nameof(GuriPluginCore);

        public void OnDisable()
        {
        }

        public void OnEnable()
        {
        }

        public void OnLoad(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(GuriPlugin).Assembly)
                .Where(s => s.ImplementsInterface<IGuriHandler>());

            builder.Register(_ => new BaseGuriHandler())
                .As<IGuriHandlerContainer>().SingleInstance();
        }
    }
}