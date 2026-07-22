using Autofac;
using ChickenAPI.Plugins;

using NosGm.Core;
using NosGm.Core.Extensions;
using NosGm.GameObject._Guri;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Service;
using System;

namespace Plugins.BasicImplementations.Guri
{
    public class GuriPlugin : IGamePlugin
    {
        private readonly IContainer _container;
        private readonly IGuriHandlerContainer _handlers;

        public GuriPlugin(IGuriHandlerContainer handlers, IContainer container)
        {
            _handlers = handlers;
            _container = container;
        }

        public string Name => nameof(GuriPlugin);

        public void OnDisable()
        {
        }

        public void OnEnable()
        {
            foreach (var handlerType in typeof(GuriPlugin).Assembly.GetTypesImplementingInterface<IGuriHandler>())
                try
                {
                    var tmp = _container.Resolve(handlerType);
                    if (!(tmp is IGuriHandler real)) continue;

                    _handlers.Register(real).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    //LOGGERServerLog($"[GuriPlugin] {e.ToString()}", LogType.ServerError);
                }
        }

        public void OnLoad()
        {
        }
    }
}