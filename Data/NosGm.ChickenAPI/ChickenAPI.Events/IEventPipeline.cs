using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChickenAPI.Events
{
    public interface IEventPipeline
    {
        /// <summary>
        ///     Asynchronously send a notification to handlers of type T
        /// </summary>
        /// <param name="notification">Notification object</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task that represents the publish operation.</returns>
        Task Notify<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IEventNotification;


        /// <summary>
        ///     Asynchronously registers a preprocessor in the pipeline for events of type <see cref="T" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task RegisterPreprocessorAsync<T>(IEventFilter filter) where T : IEventNotification;

        /// <summary>
        ///     Asynchronously registers a filter in filters in piepline for event of the given type
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="type"></param>
        Task RegisterPreprocessorAsync(IEventFilter filter, Type type);

        /// <summary>
        ///     Asynchronously unregisters the preprocessor for handled type from the pipeline
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task UnregisterPreprocessorAsync<T>(IEventFilter filter) where T : IEventNotification;

        /// <summary>
        ///     Asynchronously unregisters the preprocessor from the pipeline
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Task UnregisterPreprocessorAsync(IEventFilter filter, Type type);


        /// <summary>
        ///     Asynchronously registers a PostProcessor (aka Handler) in the pipeline for events of type <see cref="T" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="type">Type of event handled by the post processor</param>
        /// <returns></returns>
        Task RegisterPostProcessorAsync(IEventHandler handler, Type type);

        /// <summary>
        ///     Asynchronously registers a PostProcessor (aka Handler) in the pipeline for events of type <see cref="T" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task RegisterPostProcessorAsync<T>(IEventHandler handler) where T : IEventNotification;

        /// <summary>
        ///     Asynchronously unregisters the postprocessor for handled type from the pipeline
        /// </summary>
        /// <param name="preprocessor"></param>
        /// <returns></returns>
        Task UnregisterPostprocessorAsync<T>(IEventHandler preprocessor) where T : IEventNotification;

        /// <summary>
        ///     Asynchronously unregisters the postprocessor from the pipeline
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task UnregisterPostprocessorAsync(IEventHandler handler, Type type);
    }
}