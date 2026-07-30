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
            lock (_syncRoot)
            {
                if (_state == CommunicationCallbackSubscriberHostState.Created)
                {
                    _state = CommunicationCallbackSubscriberHostState.Stopped;
                    DisposeRunner();
                    return true;
                }
                if (_state == CommunicationCallbackSubscriberHostState.Stopped ||
                    _state == CommunicationCallbackSubscriberHostState.Faulted)
                {
                    DisposeRunner();
                    return true;
                }

                _state = CommunicationCallbackSubscriberHostState.Stopping;
                runTask = _runTask;
                cancellation = _cancellation;
            }

            cancellation?.Cancel();
            bool completed = runTask == null ||
                             runTask.Wait(timeout);
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
            cancellation?.Dispose();
            DisposeRunner();
            return true;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                Stop(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException exception)
                when (exception.InnerExceptions.Count == 1 &&
                      exception.InnerException is OperationCanceledException)
            {
                DisposeRunner();
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
                _faultHandler?.Invoke(exception);
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
