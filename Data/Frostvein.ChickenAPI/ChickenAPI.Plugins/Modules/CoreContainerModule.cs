using Autofac;
using Autofac.Core;
using System.Linq;

namespace ChickenAPI.Plugins.Modules
{
    public class CoreContainerModule : Module
    {
        private readonly IContainer _container;

        public CoreContainerModule(IContainer container) => _container = container;

        private void OnComponentPreparing(object sender, PreparingEventArgs e)
        {
            e.Parameters = e.Parameters.Union(
                new[]
                {
                    new ResolvedParameter(
                        (p, i) => !e.Context.IsRegistered(p.ParameterType),
                        (p, i) => _container.Resolve(p.ParameterType))
                });
        }

        /* protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
         {
             registration.Preparing += OnComponentPreparing;
         }*/
    }
}