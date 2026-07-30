using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Communication.Client
{
    public enum CommunicationCallbackSubscriberHostState
    {
        Created = 0,
        Starting = 1,
        Running = 2,
        Stopping = 3,
        Stopped = 4,
        Faulted = 5
    }

    public sealed class CommunicationCallbackSubscriberHost : IDisposable
    {
        private readonly Action<Exception> _faultHandler;
        private readonly ICommunicationCallbackSubscriberRunner _runner;
        private readonly object _syncRoot = new object();
        private CancellationTokenSource _cancellation;
        private Exception _lastException;
        private Task _runTask;
        private CommunicationCallbackSubscriberHostState _state =
            CommunicationCallbackSubscriberHostState.Created;
        private int _disposed;
        private int _runnerDisposed;

        public CommunicationCallbackSubscriberHost(
            ICommunicationCallbackSubscriberRunner runner,
            Action<Exception> faultHandler = null)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _faultHandler = faultHandler;
        }

        public CommunicationCallbackSubscriberHostState State
        {
            get
            {
                lock (_syncRoot)
                {
                    return _state;
                }
            }
        }

        public Exception LastException
        {
            get
            {
                lock (_syncRoot)
                {
                    return _lastException;
                }
            }
        }

        public Task Completion
        {
            get
            {
                lock (_syncRoot)
                {
                    return _runTask ?? Task.CompletedTask;
                }
            }
        }

        public void Start()
        {
            ThrowIfDisposed();
            lock (_syncRoot)
            {
                if (_state != CommunicationCallbackSubscriberHostState.Created)
                {
                    throw new InvalidOperationException(
                        "The communication callback subscriber host can be started only once.");
                }

                _state = CommunicationCallbackSubscriberHostState.Starting;
                _cancellation = new CancellationTokenSource();
                _runTask = Task.Run(
                    () => RunCoreAsync(_cancellation.Token));
            }
        }

        public bool Stop(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            Task runTask;
            CancellationTokenSource cancellation;
            bool alreadyInactive;
            lock (_syncRoot)
            {
                alreadyInactive =
                    _state == CommunicationCallbackSubscriberHostState.Created ||
                    _state == CommunicationCallbackSubscriberHostState.Stopped ||
                    _state == CommunicationCallbackSubscriberHostState.Faulted;
                if (_state == CommunicationCallbackSubscriberHostState.Created)
                {
                    _state = CommunicationCallbackSubscriberHostState.Stopped;
                }
                else if (!alreadyInactive)
                {
                    _state = CommunicationCallbackSubscriberHostState.Stopping;
                }

                runTask = _runTask;
                cancellation = _cancellation;
            }

            if (alreadyInactive)
            {
                DisposeCancellation(cancellation);
                DisposeRunner();
                return true;
            }

            cancellation?.Cancel();
            bool completed = runTask == null || runTask.Wait(timeout);
            if (!completed)
            {
                return false;
            }

            lock (_syncRoot)
            {
                if (_state != CommunicationCallbackSubscriberHostState.Faulted)
                {
                    _state = CommunicationCallbackSubscriberHostState.Stopped;
                }
            }
            DisposeCancellation(cancellation);
            DisposeRunner();
            return true;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            bool stopped = false;
            try
            {
                stopped = Stop(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException exception)
                when (exception.InnerExceptions.Count == 1 &&
                      exception.InnerException is OperationCanceledException)
            {
                stopped = true;
            }
            finally
            {
                if (!stopped)
                {
                    var timeout = new TimeoutException(
                        "The communication callback subscriber did not stop within five seconds.");
                    lock (_syncRoot)
                    {
                        _lastException = timeout;
                        _state =
                            CommunicationCallbackSubscriberHostState.Faulted;
                    }
                    NotifyFault(timeout);
                }

                // A runner that ignored cancellation still owns network and
                // certificate resources. Dispose it without aborting its thread.
                DisposeRunner();
                if (stopped)
                {
                    DisposeCancellation(_cancellation);
                }
            }
        }

        private async Task RunCoreAsync(CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                if (_state == CommunicationCallbackSubscriberHostState.Starting)
                {
                    _state = CommunicationCallbackSubscriberHostState.Running;
                }
            }

            try
            {
                await _runner.RunAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw new InvalidOperationException(
                        "The communication callback subscriber stopped unexpectedly.");
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Expected controlled shutdown.
            }
            catch (Exception exception)
            {
                lock (_syncRoot)
                {
                    _lastException = exception;
                    _state = CommunicationCallbackSubscriberHostState.Faulted;
                }
                NotifyFault(exception);
                return;
            }
            finally
            {
                lock (_syncRoot)
                {
                    if (_state != CommunicationCallbackSubscriberHostState.Faulted)
                    {
                        _state = CommunicationCallbackSubscriberHostState.Stopped;
                    }
                }
            }
        }

        private void NotifyFault(Exception exception)
        {
            if (_faultHandler == null)
            {
                return;
            }

            try
            {
                _faultHandler(exception);
            }
            catch
            {
                // Observability code must never replace the subscriber failure.
            }
        }

        private static void DisposeCancellation(
            CancellationTokenSource cancellation)
        {
            cancellation?.Dispose();
        }

        private void DisposeRunner()
        {
            if (Interlocked.Exchange(ref _runnerDisposed, 1) == 0)
            {
                _runner.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(CommunicationCallbackSubscriberHost));
            }
        }
    }
}
