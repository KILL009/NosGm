using Autofac;
using ChickenAPI.Events;
using ChickenAPI.Plugins;

namespace Plugins.BasicImplementations.Event
{
    public class GenericEventPluginCore : ICorePlugin
    {
        public string Name => nameof(GenericEventPluginCore);

        public void OnDisable()
        {
        }

        public void OnEnable()
        {
        }

        public void OnLoad(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(GenericEventPlugin).Assembly)
                .AsClosedTypesOf(typeof(GenericEventHandlerBase<>)).PropertiesAutowired();
        }
    }
}