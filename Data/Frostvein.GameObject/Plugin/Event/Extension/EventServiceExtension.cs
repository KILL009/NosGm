using System.Reactive.Linq;
using System.Threading;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Reactive;

namespace Frostvein.GameObject.Plugin.Event
{
    public static class EventServiceExtension
    {
        public static IObservable<Unit> CreateRepeatingObservable(int minutes, Action action, CancellationToken cancellationToken)
        {
            return Observable.Create<Unit>(observer =>
            {
                var interval = Observable.Interval(TimeSpan.FromMinutes(minutes))
                    .Subscribe(async _ =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.Run(action);
                        }
                    });

                return new CompositeDisposable
            {
                interval,
                Disposable.Create(() => cancellationToken.ThrowIfCancellationRequested())
            };
            });
        }

        public static IObservable<Unit> CreateRepeatingObservableSeconds(int seconds, Action action, CancellationToken cancellationToken)
        {
            return Observable.Create<Unit>(observer =>
            {
                var interval = Observable.Interval(TimeSpan.FromSeconds(seconds))
                    .Subscribe(async _ =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.Run(action);
                        }
                    });

                return new CompositeDisposable
            {
                interval,
                Disposable.Create(() => cancellationToken.ThrowIfCancellationRequested())
            };
            });
        }

        public static IObservable<Unit> CreateRepeatingObservableMilliseconds(int milliseconds, Action action, CancellationToken cancellationToken)
        {
            return Observable.Create<Unit>(observer =>
            {
                var interval = Observable.Interval(TimeSpan.FromMilliseconds(milliseconds))
                    .Subscribe(async _ =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.Run(action);
                        }
                    });

                return new CompositeDisposable
            {
                interval,
                Disposable.Create(() => cancellationToken.ThrowIfCancellationRequested())
            };
            });
        }


    }
}
