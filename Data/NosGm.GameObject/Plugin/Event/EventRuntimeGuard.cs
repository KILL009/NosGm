using System;
using NosGm.Core;

namespace NosGm.GameObject.Plugin.Event
{
    public static class EventRuntimeGuard
    {
        public static void Run(string operation, Action action)
        {
            if (action == null)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception exception)
            {
                Logger.Error(
                    exception,
                    $"[EVENT_RUNTIME] Operation={operation} Result=Failed");
            }
        }

        public static Action<T> Protect<T>(string operation, Action<T> action)
        {
            return value => Run(operation, () => action(value));
        }

        public static Action<Exception> ObserveFailure(string operation)
        {
            return exception => Logger.Error(
                exception,
                $"[EVENT_RUNTIME] Operation={operation} Result=ObservableFailed");
        }
    }
}
