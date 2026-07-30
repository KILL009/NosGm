using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackActivationSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        VerifyActivationOptions();
        VerifyShadowHandler();
    }

    private static void VerifyActivationOptions()
    {
        CommunicationCallbackActivationOptions disabled =
            CommunicationCallbackActivationOptions.Load(_ => null);
        AssertEqual(
            CommunicationCallbackActivationMode.Disabled,
            disabled.Mode,
            "Production callback subscriber is disabled by default");
        AssertEqual(
            CommunicationCallbackActivationOptions
                .DefaultStopTimeoutMilliseconds,
            disabled.StopTimeoutMilliseconds,
            "Callback lifecycle uses a bounded default stop timeout");

        var shadowValues = new Dictionary<string, string>
        {
            [CommunicationCallbackActivationOptions.EnabledVariable] = "true",
            [CommunicationCallbackActivationOptions.StopTimeoutVariable] =
                "7000"
        };
        CommunicationCallbackActivationOptions shadow =
            CommunicationCallbackActivationOptions.Load(
                name => shadowValues.TryGetValue(name, out string value)
                    ? value
                    : null);
        AssertEqual(
            CommunicationCallbackActivationMode.Shadow,
            shadow.Mode,
            "Explicit callback activation starts only shadow observation");
        AssertEqual(
            7000,
            shadow.StopTimeoutMilliseconds,
            "Callback shadow shutdown accepts a bounded explicit timeout");

        AssertThrows<InvalidOperationException>(
            () => CommunicationCallbackActivationOptions.Load(
                name => name == CommunicationCallbackActivationOptions
                    .EnabledVariable
                    ? " true"
                    : null),
            "Callback activation rejects ambiguous whitespace");
        AssertThrows<InvalidOperationException>(
            () => CommunicationCallbackActivationOptions.Load(
                name => name == CommunicationCallbackActivationOptions
                    .EnabledVariable
                    ? "yes"
                    : null),
            "Callback activation accepts only true or false");
        AssertThrows<InvalidOperationException>(
            () => CommunicationCallbackActivationOptions.Load(
                name => name == CommunicationCallbackActivationOptions
                    .ApplyVariable
                    ? "true"
                    : null),
            "Callback application cannot bypass subscriber activation");
        AssertThrows<InvalidOperationException>(
            () => CommunicationCallbackActivationOptions.Load(
                name =>
                {
                    if (name == CommunicationCallbackActivationOptions
                        .EnabledVariable)
                    {
                        return "true";
                    }
                    if (name == CommunicationCallbackActivationOptions
                        .ApplyVariable)
                    {
                        return "true";
                    }
                    return null;
                }),
            "Production callback effects remain blocked before atomic cutover");
        AssertThrows<InvalidOperationException>(
            () => CommunicationCallbackActivationOptions.Load(
                name => name == CommunicationCallbackActivationOptions
                    .StopTimeoutVariable
                    ? "999"
                    : null),
            "Callback lifecycle rejects an unsafe stop timeout");
    }

    private static void VerifyShadowHandler()
    {
        const string generation =
            "11111111-2222-3333-4444-555555555555";
        var handler = new CommunicationCallbackShadowEnvelopeHandler();
        handler.BeginStream(generation, 0);
        var envelope = new WireV1.CommunicationCallbackEnvelope
        {
            EventId = Guid.NewGuid().ToString("D"),
            Sequence = 42,
            IssuedAtUnixTimeMs = 1_900_000_000_000,
            ExpiresAtUnixTimeMs = 1_900_000_030_000,
            Target = new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.AllNodes
            },
            PenaltyRefresh = new WireV1.PenaltyRefreshCallback
            {
                PenaltyLogId = 7
            }
        };
        handler.ApplyAsync(envelope, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        AssertEqual(
            (long)1,
            handler.ObservedCallbacks,
            "Shadow callback handler records one validated envelope");
        AssertEqual(
            (ulong)42,
            handler.LastObservedSequence,
            "Shadow callback handler records the observed sequence");
        AssertEqual(
            1,
            handler.GetObservationSnapshot().Count,
            "Shadow callback handler retains one bounded observation");
        AssertEqual(
            generation,
            handler.GetObservationSnapshot()[0].RuntimeGenerationId,
            "Shadow observation is bound to the active runtime generation");
        handler.EndStream();
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
}
