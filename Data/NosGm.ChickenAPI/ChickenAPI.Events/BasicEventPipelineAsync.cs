using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChickenAPI.Events
{
    public class BasicEventPipelineAsync : IEventPipeline
    {
        private readonly Dictionary<Type, List<IEventHandler>> _postprocessorsDictionary =
            new Dictionary<Type, List<IEventHandler>>();

        private readonly Dictionary<Type, List<IEventFilter>> _preprocessorsDictionary =
            new Dictionary<Type, List<IEventFilter>>();

        public async Task Notify<TNotification>(TNotification notification,
            CancellationToken cancellationToken = default) where TNotification : IEventNotification
        {
            if (!_postprocessorsDictionary.TryGetValue(typeof(TNotification), out var processors)) return;

            if (!await CanSendEvent(notification, typeof(TNotification), cancellationToken).ConfigureAwait(false)) return;

            foreach (var postProcessor in processors)
                try
                {
                    if (postProcessor is IAsyncEventHandler asyncPostProcessor)
                    {
                        await asyncPostProcessor.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        postProcessor.Handle(notification, cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    //Logger.Log.Error("Notify()", e);
                }
        }

        public Task RegisterPreprocessorAsync<T>(IEventFilter filter) where T : IEventNotification
        {
            return RegisterPreprocessorAsync(filter, typeof(T));
        }

        public Task RegisterPreprocessorAsync(IEventFilter filter, Type type)
        {
            //if (!type.ImplementsInterface(typeof(IEventNotification)))
                //throw new ArgumentException($"{type} should implement {typeof(IEventNotification)}");

            if (!_preprocessorsDictionary.TryGetValue(type, out var handlers))
            {
                handlers = new List<IEventFilter>();
                _preprocessorsDictionary[type] = handlers;
            }

            handlers.Add(filter);
            return Task.CompletedTask;
        }

        public Task UnregisterPreprocessorAsync<T>(IEventFilter filter) where T : IEventNotification
        {
            return UnregisterPreprocessorAsync(filter, typeof(T));
        }

        public Task UnregisterPreprocessorAsync(IEventFilter filter, Type type)
        {
            return Task.CompletedTask;
        }

        public Task RegisterPostProcessorAsync(IEventHandler handler, Type type)
        {
            if (!_postprocessorsDictionary.TryGetValue(type, out var handlers))
            {
                handlers = new List<IEventHandler>();
                _postprocessorsDictionary[type] = handlers;
            }

            handlers.Add(handler);
            return Task.CompletedTask;
        }

        public Task RegisterPostProcessorAsync<T>(IEventHandler handler) where T : IEventNotification
        {
            return RegisterPostProcessorAsync(handler, typeof(T));
        }

        public Task UnregisterPostprocessorAsync<T>(IEventHandler preprocessor) where T : IEventNotification
        {
            return UnregisterPostprocessorAsync(preprocessor, typeof(T));
        }

        public Task UnregisterPostprocessorAsync(IEventHandler handler, Type type)
        {
            return Task.CompletedTask;
        }


        private async Task<bool> CanSendEvent(IEventNotification e, Type type, CancellationToken cancellationToken)
        {
            if (!_preprocessorsDictionary.TryGetValue(type, out var filters)) return true;

            foreach (var filter in filters)
                // filter is not passed correctly
                if (await filter.Handle(e, cancellationToken).ConfigureAwait(false) == false)
                    return false;

            return true;
        }
    }
}
