using System;
using System.Reactive.Linq;

namespace NosGm.Event
{
    public static class EventSubscriber
    {
        public static IDisposable SafeSubscribe(this IObservable<long> obs, Action<long> callback)
        {
            IDisposable observable = null;

            try
            {
                observable = obs.Subscribe(x =>
                {
                    try
                    {
                        callback(x);
                    }
                    catch
                    {
                        observable?.Dispose();
                    }
                });

                return observable;
            }
            catch (Exception e)
            {
                observable?.Dispose();
                return null;
            }
        }
    }

}
