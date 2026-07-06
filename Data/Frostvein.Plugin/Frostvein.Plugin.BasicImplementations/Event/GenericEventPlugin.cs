using Autofac;
using ChickenAPI.Events;
using ChickenAPI.Plugins;

using Frostvein.Core;
using Frostvein.Core.Extensions;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Service;
using System;

namespace Plugins.BasicImplementations.Event
{
    public class GenericEventPlugin : IGamePlugin
    {
        private readonly IContainer _container;
        private readonly IEventPipeline _handlers;

        public GenericEventPlugin(IEventPipeline handlers, IContainer container)
        {
            _handlers = handlers;
            _container = container;
        }

        public string Name => nameof(GenericEventPlugin);

        public void OnDisable()
        {
        }

        public void OnEnable()
        {
            foreach (var handlerType in typeof(GenericEventPlugin).Assembly.GetTypesImplementingGenericClass(
                typeof(GenericEventHandlerBase<>)))
                try
                {
                    var handler = _container.Resolve(handlerType);
                    if (!(handler is IEventHandler postProcessor)) continue;

                    var type = handlerType.BaseType.GenericTypeArguments[0];

                    _handlers.RegisterPostProcessorAsync(postProcessor, type);
                }
                catch (Exception e)
                {
                    //LOGGERServerLog($"[EventPlugin] {e.ToString()}", LogType.ServerError);
                }
        }

        public void OnLoad()
        {
        }
    }
}