using System.Runtime.CompilerServices;
using NosGm.Communication.Client;
using WireV1 = global::NosGm.Cluster.Wire.V1;

internal static class CommunicationCallbackOperatorCutoverCoordinatorSelfTest
{
    private const string Identity =
        "World:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:1:Sumeria";
    private const string RequestId =
        "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb";
    private const string Generation1 =
        "11111111-2222-3333-4444-555555555551";
    private const string Generation2 =
        "11111111-2222-3333-4444-555555555552";
    private const string Generation3 =
        "11111111-2222-3333-4444-555555555553";
    private const string Generation4 =
        "11111111-2222-3333-4444-555555555554";
    private const string Generation5 =
        "11111111-2222-3333-4444-555555555555";

    [ModuleInitializer]
    public static void Run()
    {
        VerifyOperatorOptions();
        VerifyNoRequestLeavesScsAuthoritative();
        VerifyExplicitArmAndNewGenerationActivation();
        VerifyOperatorRollbackAndConfigurationMutation();

        Console.WriteLine(
            "[PASS] Operator PenaltyRefresh cutover handshake self-test");
    }

    private static void VerifyOperatorOptions()
    {
        CommunicationCallbackOperatorCutoverOptions disabled =
            Options(string.Empty, false);
        AssertEqual(false, disabled.HasPenaltyRefreshArmRequest,
            "Operator cutover is disabled by default");
        AssertEqual(false, disabled.PenaltyRefreshRollbackRequested,
            "Operator rollback is disabled by default");

        CommunicationCallbackOperatorCutoverOptions armed =
            Options(RequestId, false);
        AssertEqual(true, armed.HasPenaltyRefreshArmRequest,
            "A canonical request ID creates an explicit arm request");
        AssertEqual(RequestId, armed.PenaltyRefreshArmRequestId,
            "The exact operator request ID is preserved");

        AssertThrows<InvalidOperationException>(
            () => Options(RequestId.ToUpperInvariant(), false),
            "Uppercase request IDs cannot bypass canonical operator input");
        AssertThrows<InvalidOperationException>(
            () => Options(RequestId, true),
            "Arm and rollback requests cannot be issued together");
        AssertThrows<InvalidOperationException>(
            () => CommunicationCallbackOperatorCutoverOptions.Load(
                name => name ==
                    CommunicationCallbackOperatorCutoverOptions
                        .PenaltyRefreshRollbackVariable
                    ? "yes"
                    : string.Empty),
            "Operator rollback accepts only strict booleans");
    }

    private static void VerifyNoRequestLeavesScsAuthoritative()
    {
        var coordinator =
            new CommunicationCallbackOperatorCutoverCoordinator();
        coordinator.Configure(Identity, Options(string.Empty, false));
        CommunicationCallbackKindParityEvidenceLedger ledger =
            QualifiedLedger();

        AssertEqual(false, coordinator.ObserveQualification(ledger),
            "Qualification alone cannot arm without an operator request");
        AssertEqual(false, coordinator.ObserveRuntimeGeneration(Generation4),
            "A new generation cannot activate an unrequested cutover");
        AssertEqual(
            CommunicationCallbackCutoverState.ScsAuthoritative,
            coordinator.GetStatus().State,
            "SCS remains authoritative when the request ID is absent");
        AssertEqual(
            true,
            coordinator.ShouldApply(
                CommunicationCallbackParitySource.LegacyScs,
                WireV1.CommunicationCallbackKind.PenaltyRefresh),
            "SCS applies PenaltyRefresh while cutover is unrequested");
        AssertEqual(
            false,
            coordinator.ShouldApply(
                CommunicationCallbackParitySource.TypedGrpc,
                WireV1.CommunicationCallbackKind.PenaltyRefresh),
            "Typed gRPC cannot apply PenaltyRefresh while cutover is unrequested");
    }

    private static void VerifyExplicitArmAndNewGenerationActivation()
    {
        var coordinator =
            new CommunicationCallbackOperatorCutoverCoordinator();
        coordinator.Configure(Identity, Options(RequestId, false));
        CommunicationCallbackKindParityEvidenceLedger ledger =
            QualifiedLedger();

        AssertEqual(true, coordinator.ObserveQualification(ledger),
            "Three parity generations plus an explicit request arm cutover");
        AssertEqual(
            CommunicationCallbackCutoverState.Armed,
            coordinator.GetStatus().State,
            "Operator qualification arms without applying typed effects");
        AssertEqual(false, coordinator.ObserveRuntimeGeneration(Generation3),
            "A qualification generation cannot activate callback authority");
        AssertEqual(
            CommunicationCallbackCutoverState.Armed,
            coordinator.GetStatus().State,
            "Rejected generation reuse leaves the request armed");

        AssertEqual(true, coordinator.ObserveRuntimeGeneration(Generation4),
            "The first new runtime generation completes activation handshake");
        CommunicationCallbackOperatorCutoverStatus active =
            coordinator.GetStatus();
        AssertEqual(
            CommunicationCallbackCutoverState.TypedGrpcAuthoritative,
            active.State,
            "The modeled PenaltyRefresh authority becomes typed gRPC");
        AssertEqual(RequestId, active.ArmRequestId,
            "Active authority retains the exact operator request ID");
        AssertEqual(Identity, active.QualifiedIdentity,
            "Active authority remains bound to the qualified process identity");
        AssertEqual(Generation4, active.ActiveGeneration,
            "Activation is scoped to the new runtime generation");
        AssertEqual(
            true,
            coordinator.ShouldApply(
                CommunicationCallbackParitySource.TypedGrpc,
                WireV1.CommunicationCallbackKind.PenaltyRefresh),
            "Activated PenaltyRefresh selects typed gRPC in the authority model");
        AssertEqual(
            false,
            coordinator.ShouldApply(
                CommunicationCallbackParitySource.LegacyScs,
                WireV1.CommunicationCallbackKind.PenaltyRefresh),
            "Activated PenaltyRefresh excludes SCS in the authority model");
        AssertEqual(
            true,
            coordinator.ShouldApply(
                CommunicationCallbackParitySource.LegacyScs,
                WireV1.CommunicationCallbackKind.BazaarRefresh),
            "Every unselected callback kind remains on SCS");

        AssertEqual(false, coordinator.ObserveRuntimeGeneration(Generation4),
            "Repeating the active generation is idempotent");
        AssertEqual(false, coordinator.ObserveRuntimeGeneration(Generation5),
            "A later unapproved generation fails closed");
        CommunicationCallbackOperatorCutoverStatus rolledBack =
            coordinator.GetStatus();
        AssertEqual(
            CommunicationCallbackCutoverState.RolledBack,
            rolledBack.State,
            "Generation drift makes rollback terminal for the process");
        AssertEqual(true, rolledBack.IsBlocked,
            "Generation drift blocks reactivation in the same process");
        AssertEqual(
            true,
            coordinator.ShouldApply(
                CommunicationCallbackParitySource.LegacyScs,
                WireV1.CommunicationCallbackKind.PenaltyRefresh),
            "Rollback immediately restores PenaltyRefresh authority to SCS");
    }

    private static void VerifyOperatorRollbackAndConfigurationMutation()
    {
        var rollbackCoordinator =
            new CommunicationCallbackOperatorCutoverCoordinator();
        rollbackCoordinator.Configure(
            Identity,
            Options(string.Empty, true));
        AssertEqual(false,
            rollbackCoordinator.ObserveQualification(QualifiedLedger()),
            "An operator rollback request blocks qualification arming");
        AssertEqual(true, rollbackCoordinator.GetStatus().IsBlocked,
            "A rollback request blocks the process before activation");

        var armedCoordinator =
            new CommunicationCallbackOperatorCutoverCoordinator();
        armedCoordinator.Configure(Identity, Options(RequestId, false));
        armedCoordinator.ObserveQualification(QualifiedLedger());
        AssertEqual(true,
            armedCoordinator.RequestRollback(
                new InvalidOperationException("operator test rollback")),
            "An explicit operator rollback closes an armed request");
        AssertEqual(
            CommunicationCallbackCutoverState.RolledBack,
            armedCoordinator.GetStatus().State,
            "Operator rollback is terminal for the process");

        var mutationCoordinator =
            new CommunicationCallbackOperatorCutoverCoordinator();
        mutationCoordinator.Configure(
            Identity,
            Options(string.Empty, false));
        AssertThrows<InvalidOperationException>(
            () => mutationCoordinator.Configure(
                Identity,
                Options(RequestId, false)),
            "Operator configuration cannot change inside one process");
        AssertEqual(true, mutationCoordinator.GetStatus().IsBlocked,
            "Configuration mutation permanently blocks cutover");
    }

    private static CommunicationCallbackKindParityEvidenceLedger
        QualifiedLedger()
    {
        DateTimeOffset start = new DateTimeOffset(
            2030,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
        var ledger = new CommunicationCallbackKindParityEvidenceLedger(
            WireV1.CommunicationCallbackKind.PenaltyRefresh);
        ledger.TryAppend(Evidence(Generation1, start));
        ledger.TryAppend(Evidence(Generation2, start.AddMinutes(1)));
        ledger.TryAppend(Evidence(Generation3, start.AddMinutes(2)));
        return ledger;
    }

    private static CommunicationCallbackKindParityEvidence Evidence(
        string generation,
        DateTimeOffset observedAt)
    {
        return new CommunicationCallbackKindParityEvidence(
            Identity,
            WireV1.CommunicationCallbackKind.PenaltyRefresh,
            generation,
            CommunicationCallbackParityVerdict.Parity,
            1,
            1,
            observedAt);
    }

    private static CommunicationCallbackOperatorCutoverOptions Options(
        string armRequestId,
        bool rollbackRequested)
    {
        return CommunicationCallbackOperatorCutoverOptions.Load(
            name =>
            {
                if (name ==
                    CommunicationCallbackOperatorCutoverOptions
                        .PenaltyRefreshArmRequestVariable)
                {
                    return armRequestId;
                }
                if (name ==
                    CommunicationCallbackOperatorCutoverOptions
                        .PenaltyRefreshRollbackVariable)
                {
                    return rollbackRequested ? "true" : "false";
                }
                return string.Empty;
            });
    }

    private static void AssertThrows<TException>(
        Action action,
        string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Console.WriteLine("[PASS] " + name);
            return;
        }

        throw new InvalidOperationException(
            name + ": expected " + typeof(TException).Name + ".");
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
