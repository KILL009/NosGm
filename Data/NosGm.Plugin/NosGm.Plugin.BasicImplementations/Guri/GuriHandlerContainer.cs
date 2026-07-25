using NosGm.Core;
using NosGm.Core.Diagnostics;
using NosGm.GameObject._Event;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri
{
    public class BaseGuriHandler : IGuriHandlerContainer
    {
        private const int MaximumRememberedMissingTypes = 256;

        private static readonly ConcurrentDictionary<long, byte> MissingHandlerTypes =
            new ConcurrentDictionary<long, byte>();

        protected readonly Dictionary<long, IGuriHandler> HandlersByDialogId;

        public BaseGuriHandler()
        {
            HandlersByDialogId = new Dictionary<long, IGuriHandler>();
        }

        public Task Register(IGuriHandler handler)
        {
            if (handler != null && !HandlersByDialogId.ContainsKey(handler.GuriEffectId))
            {
                HandlersByDialogId.Add(handler.GuriEffectId, handler);
            }

            return Task.CompletedTask;
        }

        public Task Unregister(long guriEffectId)
        {
            HandlersByDialogId.Remove(guriEffectId);
            return Task.CompletedTask;
        }

        public void Handle(EventEntity player, GuriEvent args)
        {
            if (args == null)
            {
                return;
            }

            if (!HandlersByDialogId.TryGetValue(args.Type, out IGuriHandler handler))
            {
                GuriPerformanceMonitor.RecordMissingHandler(args.Type);

                // A client can send arbitrary guri values. Logging every unknown value
                // synchronously to console and XML creates avoidable disk and CPU load.
                // Keep one diagnostic line per bounded set of distinct types instead.
                if (MissingHandlerTypes.Count < MaximumRememberedMissingTypes &&
                    MissingHandlerTypes.TryAdd(args.Type, 0))
                {
                    Logger.Log.Debug($"[HANDLER_NOT_FOUND] GURI_EFFECT : {args.Type}");
                }
                return;
            }

            handler.Execute(player?.Character?.Session, args);
        }
    }
}
