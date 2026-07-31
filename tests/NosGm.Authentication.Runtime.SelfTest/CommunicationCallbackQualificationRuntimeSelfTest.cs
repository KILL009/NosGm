using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackQualificationRuntimeSelfTest
{
    private const string Identity =
        "World:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:1:Sumeria";
    private const string Generation1 =
        "33333333-2222-3333-4444-555555555551";
    private const string Generation2 =
        "33333333-2222-3333-4444-555555555552";
    private const string Generation3 =
        "33333333-2222-3333-4444-555555555553";
    private const string Generation4 =
        "33333333-2222-3333-4444-555555555554";

    [ModuleInitializer]
    public static void Run()
    {
        CommunicationCallbackQualificationRuntime runtime =
            CommunicationCallbackQualificationRuntime.Instance;
        DateTimeOffset start = new DateTimeOffset(
            2031,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);

        CaptureParity(runtime, Generation1, start, 7);
        CaptureParity(runtime, Generation2, start.AddMinutes(1), 8);
        CaptureParity(runtime, Generation3, start.AddMinutes(2), 9);

        CommunicationCallbackQualificationStatus qualified =
            runtime.GetStatus();
        AssertEqual(
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            qualified.TargetKind,
            "Qualification runtime is restricted to PenaltyRefresh");
        AssertEqual((long)3, qualified.AppendedEvidence,
            "Three terminal streams produce three retained evidence entries");
        AssertEqual(true, qualified.HasCompleteHistory,
            "Three terminal streams preserve a complete process history");
        AssertEqual(true, qualified.IsQualified,
            "Three terminal parity streams qualify the inactive cutover gate");
        AssertEqual(false, qualified.IsInvalidated,
            "Canonical terminal parity does not invalidate qualification");
        AssertEqual(Generation3,
            qualified.LastEvidence.RuntimeGenerationId,
            "Qualification status exposes the newest terminal generation");
        AssertEqual(3,
            runtime.GetPenaltyRefreshEvidenceSnapshot().Count,
            "Qualification runtime exposes a defensive evidence snapshot");

        CaptureMismatch(
            runtime,
            Generation4,
            start.AddMinutes(3));
        CommunicationCallbackQualificationStatus mismatch =
            runtime.GetStatus();
        AssertEqual(false, mismatch.IsQualified,
            "A newer terminal mismatch breaks the three-generation parity streak");
        AssertEqual(
            CommunicationCallbackParityVerdict.OrderMismatch,
            mismatch.LastEvidence.Verdict,
            "Qualification status preserves the terminal mismatch verdict");
        AssertEqual(false, mismatch.IsInvalidated,
            "A valid mismatch is evidence, not ledger corruption");
        AssertEqual(null, mismatch.LastException,
            "A valid mismatch does not create a capture exception");

        Console.WriteLine(
            "[PASS] Terminal PenaltyRefresh qualification runtime self-test");
    }

    private static void CaptureParity(
        CommunicationCallbackQualificationRuntime runtime,
        string generation,
        DateTimeOffset observedAt,
        int penaltyLogId)
    {
        string fingerprint =
            CommunicationCallbackSemanticFingerprint
                .ComputePenaltyRefresh(penaltyLogId);
        CommunicationCallbackReplayEvidence replay =
            CreateReplayEvidence(generation);
        CommunicationCallbackParityWindow typed =
            Window(
                CommunicationCallbackParitySource.TypedGrpc,
                generation,
                replay,
                21,
                fingerprint);
        CommunicationCallbackParityWindow scs =
            Window(
                CommunicationCallbackParitySource.LegacyScs,
                generation,
                replay,
                1,
                fingerprint);

        AssertEqual(true,
            runtime.TryCapturePenaltyRefresh(
                typed,
                scs,
                observedAt,
                out CommunicationCallbackKindParityEvidence evidence),
            "A terminal parity stream is appended to qualification history");
        AssertEqual(
            CommunicationCallbackParityVerdict.Parity,
            evidence.Verdict,
            "A terminal parity stream retains its positive verdict");
    }

    private static void CaptureMismatch(
        CommunicationCallbackQualificationRuntime runtime,
        string generation,
        DateTimeOffset observedAt)
    {
        CommunicationCallbackReplayEvidence replay =
            CreateReplayEvidence(generation);
        CommunicationCallbackParityWindow typed =
            Window(
                CommunicationCallbackParitySource.TypedGrpc,
                generation,
                replay,
                22,
                CommunicationCallbackSemanticFingerprint
                    .ComputePenaltyRefresh(10));
        CommunicationCallbackParityWindow scs =
            Window(
                CommunicationCallbackParitySource.LegacyScs,
                generation,
                replay,
                2,
                CommunicationCallbackSemanticFingerprint
                    .ComputePenaltyRefresh(11));

        AssertEqual(true,
            runtime.TryCapturePenaltyRefresh(
                typed,
                scs,
                observedAt,
                out CommunicationCallbackKindParityEvidence evidence),
            "A valid terminal mismatch is appended as fail-closed evidence");
        AssertEqual(
            CommunicationCallbackParityVerdict.OrderMismatch,
            evidence.Verdict,
            "A terminal fingerprint mismatch is classified explicitly");
    }

    private static CommunicationCallbackParityWindow Window(
        CommunicationCallbackParitySource source,
        string generation,
        CommunicationCallbackReplayEvidence replay,
        ulong ordinal,
        string fingerprint)
    {
        return new CommunicationCallbackParityWindow(
            source,
            Identity,
            generation,
            false,
            replay,
            1,
            0,
            new[]
            {
                new CommunicationCallbackParitySample(
                    generation,
                    ordinal,
                    WireV1.CommunicationCallbackKind.PenaltyRefresh,
                    fingerprint)
            });
    }

    private static CommunicationCallbackReplayEvidence CreateReplayEvidence(
        string generation)
    {
        var tracker = new CommunicationCallbackReplayTracker();
        tracker.BeginStream(generation, 0);
        return tracker.Complete(
            new WireV1.CommunicationCallbackEnvelope
            {
                Sequence = 20,
                ReplayComplete =
                    new WireV1.CommunicationCallbackReplayComplete
                    {
                        RuntimeGenerationId = generation,
                        ReplayThroughSequence = 20,
                        ResumeAfterSequence = 0,
                        ReplayedEvents = 0
                    }
            },
            DateTimeOffset.UtcNow);
    }

    private static void AssertEqual<T>(
        T expected,
        T actual,
        string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', received '{actual}'.");
        }

        Console.WriteLine("[PASS] " + name);
    }
}
