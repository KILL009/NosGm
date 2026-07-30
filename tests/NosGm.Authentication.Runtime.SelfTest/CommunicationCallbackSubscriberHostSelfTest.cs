using System.Runtime.CompilerServices;
using NosGm.Communication.Client;

internal static class CommunicationCallbackSubscriberHostSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        VerifyControlledLifecycle();
        VerifyFaultVisibility();
        VerifyUnexpectedCompletion();
    }

    private static void VerifyControlledLifecycle()
    {
        var runner = new BlockingRunner();
        using var host = new CommunicationCallbackSubscriberHost(runner);
        host.Start();
        if (!runner.Started.Task.Wait(TimeSpan.FromSeconds(2)))
        {
            throw new InvalidOperationException(
                "The callback lifecycle test runner did not start in time.");
        }
        AssertEqual(
            CommunicationCallbackSubscriberHostState.Running,
            host.State,
            "Callback lifecycle host enters the running state");
        AssertThrows<InvalidOperationException>(
            host.Start,
            "Callback lifecycle host cannot be started twice");
        AssertEqual(
            true,
            host.Stop(TimeSpan.FromSeconds(2)),
            "Callback lifecycle host stops within its bounded deadline");
        AssertEqual(
            CommunicationCallbackSubscriberHostState.Stopped,
            host.State,
            "Callback lifecycle host records controlled shutdown");
        AssertEqual(
            true,
            runner.Disposed,
            "Callback lifecycle host disposes its subscriber runner");
    }

    private static void VerifyFaultVisibility()
    {
        var failure = new InvalidOperationException(
            "intentional callback subscriber failure");
        var runner = new FaultingRunner(failure);
        Exception observed = null;
        using var host = new CommunicationCallbackSubscriberHost(
            runner,
            exception => observed = exception);
        host.Start();
        host.Completion.GetAwaiter().GetResult();
        AssertEqual(
            CommunicationCallbackSubscriberHostState.Faulted,
            host.State,
            "Callback lifecycle host exposes terminal subscriber faults");
        AssertEqual(
            failure,
            host.LastException,
            "Callback lifecycle host retains the terminal exception");
        AssertEqual(
            failure,
            observed,
            "Callback lifecycle host invokes the explicit fault observer");
    }

    private static void VerifyUnexpectedCompletion()
    {
        var runner = new CompletingRunner();
        using var host = new CommunicationCallbackSubscriberHost(runner);
        host.Start();
        host.Completion.GetAwaiter().GetResult();
        AssertEqual(
            CommunicationCallbackSubscriberHostState.Faulted,
            host.State,
            "Unexpected callback subscriber completion is unhealthy");
        AssertEqual(
            true,
            host.LastException is InvalidOperationException,
            "Unexpected callback completion retains a terminal failure");
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', received '{actual}'.");
        }
        Console.WriteLine($"[PASS] {name}");
    }

    private static void AssertThrows<TException>(Action action, string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Console.WriteLine($"[PASS] {name}");
            return;
        }
        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}.");
    }

    private sealed class BlockingRunner
        : ICommunicationCallbackSubscriberRunner
    {
        public TaskCompletionSource<bool> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Disposed { get; private set; }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            Started.TrySetResult(true);
            cancellationToken.WaitHandle.WaitOne();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class FaultingRunner
        : ICommunicationCallbackSubscriberRunner
    {
        private readonly Exception _failure;

        public FaultingRunner(Exception failure)
        {
            _failure = failure;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromException(_failure);
        }

        public void Dispose()
        {
        }
    }

    private sealed class CompletingRunner
        : ICommunicationCallbackSubscriberRunner
    {
        public Task RunAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
