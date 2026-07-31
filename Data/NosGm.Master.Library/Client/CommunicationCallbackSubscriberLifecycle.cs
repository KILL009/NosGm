using NosGm.Cluster.Contracts.V1;
using NosGm.Communication.Client;
using NosGm.Core;
using System;
using System.Collections.Generic;

namespace NosGm.Master.Library.Client
{
    public sealed class CommunicationCallbackSubscriberLifecycle : IDisposable
    {
        private static readonly Lazy<CommunicationCallbackSubscriberLifecycle>
            LazyInstance =
                new Lazy<CommunicationCallbackSubscriberLifecycle>(
                    () => new CommunicationCallbackSubscriberLifecycle());

        private readonly object _syncRoot = new object();
        private CommunicationCallbackActivationMode _activationMode =
            CommunicationCallbackActivationMode.Disabled;
        private CommunicationCallbackSubscriberHost _host;
        private GrpcCommunicationCallbackSubscriber _subscriber;
        private CommunicationCallbackShadowEnvelopeHandler _shadowHandler;
        private CommunicationCallbackParityReport _lastParityReport;
        private string _identity = string.Empty;
        private int _stopTimeoutMilliseconds =
            CommunicationCallbackActivationOptions
                .DefaultStopTimeoutMilliseconds;
        private bool _disposed;
        private bool _processExitRegistered;

        private CommunicationCallbackSubscriberLifecycle()
        {
        }

        public static CommunicationCallbackSubscriberLifecycle Instance =>
            LazyInstance.Value;

        public CommunicationCallbackActivationMode ActivationMode
        {
            get
            {
                lock (_syncRoot)
                {
                    return _activationMode;
                }
            }
        }

        public ulong AppliedSequence
        {
            get
            {
                lock (_syncRoot)
                {
                    return _subscriber?.AppliedSequence ?? 0;
                }
            }
        }

        public bool IsReplayComplete
        {
            get
            {
                lock (_syncRoot)
                {
                    return _subscriber?.IsReplayComplete ?? false;
                }
            }
        }

        public CommunicationCallbackReplayEvidence ReplayEvidence
        {
            get
            {
                lock (_syncRoot)
                {
                    return _subscriber?.ReplayEvidence;
                }
            }
        }

        public string RuntimeGenerationId
        {
            get
            {
                lock (_syncRoot)
                {
                    return _subscriber?.RuntimeGenerationId ?? string.Empty;
                }
            }
        }

        public CommunicationCallbackSubscriberHostState State
        {
            get
            {
                lock (_syncRoot)
                {
                    return _host?.State ??
                           CommunicationCallbackSubscriberHostState.Stopped;
                }
            }
        }

        public long ObservedCallbacks
        {
            get
            {
                lock (_syncRoot)
                {
                    return _shadowHandler?.ObservedCallbacks ?? 0;
                }
            }
        }

        public ulong LastObservedSequence
        {
            get
            {
                lock (_syncRoot)
                {
                    return _shadowHandler?.LastObservedSequence ?? 0;
                }
            }
        }

        public int ObservationCapacity
        {
            get
            {
                lock (_syncRoot)
                {
                    return _shadowHandler?.ObservationCapacity ?? 0;
                }
            }
        }

        public long EvictedObservations
        {
            get
            {
                lock (_syncRoot)
                {
                    return _shadowHandler?.EvictedObservations ?? 0;
                }
            }
        }

        public IReadOnlyList<CommunicationCallbackShadowObservation>
            GetObservationSnapshot()
        {
            lock (_syncRoot)
            {
                return _shadowHandler?.GetObservationSnapshot() ??
                       Array.Empty<CommunicationCallbackShadowObservation>();
            }
        }

        public bool IsScsObservationWindowActive =>
            CommunicationCallbackScsObservationLedger.Instance
                .IsWindowActive;

        public bool IsScsReplayComplete =>
            CommunicationCallbackScsObservationLedger.Instance
                .IsReplayComplete;

        public CommunicationCallbackReplayEvidence ScsReplayEvidence =>
            CommunicationCallbackScsObservationLedger.Instance
                .ReplayEvidence;

        public int ScsObservationCapacity =>
            CommunicationCallbackScsObservationLedger.Instance
                .ObservationCapacity;

        public long ScsObservedCallbacks =>
            CommunicationCallbackScsObservationLedger.Instance
                .ObservedCallbacks;

        public long ScsEvictedObservations =>
            CommunicationCallbackScsObservationLedger.Instance
                .EvictedObservations;

        public IReadOnlyList<CommunicationCallbackScsObservation>
            GetScsObservationSnapshot()
        {
            return CommunicationCallbackScsObservationLedger.Instance
                .GetObservationSnapshot();
        }

        public CommunicationCallbackParityReport ParityReport
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_shadowHandler == null)
                    {
                        return _lastParityReport;
                    }

                    return CreateParityReport(
                        _identity,
                        _subscriber?.RuntimeGenerationId ?? string.Empty,
                        _subscriber?.ReplayEvidence,
                        _shadowHandler);
                }
            }
        }

        public Exception LastException
        {
            get
            {
                lock (_syncRoot)
                {
                    return _host?.LastException;
                }
            }
        }

        public bool StartLogin()
        {
            return Start(
                ClusterNodeRole.Login,
                Guid.Empty,
                0,
                string.Empty);
        }

        public bool StartWorld(
            Guid worldId,
            int channelId,
            string worldGroup)
        {
            return Start(
                ClusterNodeRole.World,
                worldId,
                channelId,
                worldGroup);
        }

        public bool Stop()
        {
            CommunicationCallbackSubscriberHost host;
            GrpcCommunicationCallbackSubscriber subscriber;
            CommunicationCallbackShadowEnvelopeHandler shadowHandler;
            int timeoutMilliseconds;
            string identity;
            lock (_syncRoot)
            {
                host = _host;
                subscriber = _subscriber;
                shadowHandler = _shadowHandler;
                timeoutMilliseconds = _stopTimeoutMilliseconds;
                identity = _identity;
                _host = null;
                _subscriber = null;
                _shadowHandler = null;
                _identity = string.Empty;
            }

            if (host == null)
            {
                EndScsObservationWindow();
                return true;
            }

            CommunicationCallbackReplayEvidence replayEvidence =
                subscriber?.ReplayEvidence;
            string runtimeGenerationId =
                subscriber?.RuntimeGenerationId ??
                replayEvidence?.RuntimeGenerationId ??
                string.Empty;
            bool stopped = false;
            try
            {
                stopped = host.Stop(
                    TimeSpan.FromMilliseconds(timeoutMilliseconds));
                CommunicationCallbackParityReport parityReport =
                    CreateParityReport(
                        identity,
                        runtimeGenerationId,
                        replayEvidence,
                        shadowHandler);
                lock (_syncRoot)
                {
                    _lastParityReport = parityReport;
                }
                LogParityReport(parityReport);
                if (!stopped)
                {
                    Logger.Error(
                        "[CALLBACK_SHADOW_STOP_TIMEOUT] Identity=" + identity +
                        " TimeoutMs=" + timeoutMilliseconds);
                }
                else
                {
                    Logger.Info(
                        "[CALLBACK_SHADOW_STOPPED] Identity=" + identity +
                        " Observed=" +
                        (shadowHandler?.ObservedCallbacks ?? 0) +
                        " RetainedObservations=" +
                        (shadowHandler?.GetObservationSnapshot().Count ?? 0) +
                        " EvictedObservations=" +
                        (shadowHandler?.EvictedObservations ?? 0) +
                        " ScsObserved=" + ScsObservedCallbacks +
                        " ScsRetained=" +
                        GetScsObservationSnapshot().Count +
                        " ScsEvicted=" + ScsEvictedObservations +
                        " ScsReplayComplete=" + IsScsReplayComplete +
                        " LastSequence=" +
                        (shadowHandler?.LastObservedSequence ?? 0) +
                        " ReplayComplete=" +
                        (replayEvidence != null) +
                        " ReplayThrough=" +
                        (replayEvidence?.ReplayThroughSequence ?? 0) +
                        " Replayed=" +
                        (replayEvidence?.ReplayedEvents ?? 0));
                }
                return stopped;
            }
            finally
            {
                EndScsObservationWindow();
                host.Dispose();
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }
            Stop();
        }

        private bool Start(
            ClusterNodeRole role,
            Guid worldId,
            int channelId,
            string worldGroup)
        {
            CommunicationCallbackActivationOptions activation =
                CommunicationCallbackActivationOptions.Load();
            string identity = CreateIdentity(
                role,
                worldId,
                channelId,
                worldGroup);

            lock (_syncRoot)
            {
                ThrowIfDisposed();
                if (_host != null)
                {
                    if (string.Equals(
                            _identity,
                            identity,
                            StringComparison.Ordinal))
                    {
                        return false;
                    }
                    throw new InvalidOperationException(
                        "The callback subscriber lifecycle already owns another process identity.");
                }

                _activationMode = activation.Mode;
                _stopTimeoutMilliseconds =
                    activation.StopTimeoutMilliseconds;
                _lastParityReport = null;
                if (!activation.IsEnabled)
                {
                    EndScsObservationWindow();
                    Logger.Info(
                        "[CALLBACK_SHADOW_DISABLED] Identity=" + identity +
                        " EnableWith=" +
                        CommunicationCallbackActivationOptions.EnabledVariable +
                        "=true");
                    return false;
                }

                CommunicationCallbackSubscriberOptions subscriberOptions =
                    CommunicationCallbackSubscriberOptions.Load(
                        role,
                        worldId,
                        channelId,
                        worldGroup);
                var cursorStore =
                    new FileCommunicationCallbackCursorStore(
                        subscriberOptions.CursorPath);
                var shadowHandler =
                    new CommunicationCallbackShadowEnvelopeHandler(
                        streamBegan: (runtimeGenerationId, resumeAfterSequence) =>
                            BeginScsObservationWindow(
                                identity,
                                runtimeGenerationId,
                                resumeAfterSequence),
                        replayCompleted: CompleteScsObservationReplay,
                        streamEnded: EndScsObservationWindow);
                var subscriber =
                    new GrpcCommunicationCallbackSubscriber(
                        subscriberOptions,
                        cursorStore,
                        shadowHandler);
                var host = new CommunicationCallbackSubscriberHost(
                    subscriber,
                    exception => OnFault(identity, exception));

                _identity = identity;
                _subscriber = subscriber;
                _shadowHandler = shadowHandler;
                _host = host;
                try
                {
                    RegisterProcessExitOnce();
                    host.Start();
                }
                catch
                {
                    _host = null;
                    _subscriber = null;
                    _shadowHandler = null;
                    _identity = string.Empty;
                    EndScsObservationWindow();
                    host.Dispose();
                    throw;
                }

                Logger.Info(
                    "[CALLBACK_SHADOW_STARTED] Identity=" + identity +
                    " CallerInstance=" + subscriberOptions.CallerInstanceId +
                    " WireMode=" + subscriberOptions.WireMode +
                    " Endpoint=" + subscriberOptions.Address +
                    " ObservationCapacity=" +
                    shadowHandler.ObservationCapacity +
                    " ScsObservationCapacity=" +
                    ScsObservationCapacity);
                return true;
            }
        }

        private static void BeginScsObservationWindow(
            string identity,
            string runtimeGenerationId,
            ulong resumeAfterSequence)
        {
            try
            {
                CommunicationCallbackScsObservationLedger.Instance
                    .BeginWindow(
                        identity,
                        runtimeGenerationId,
                        resumeAfterSequence);
                Logger.Info(
                    "[CALLBACK_SCS_OBSERVATION_WARMUP] Identity=" +
                    identity +
                    " Generation=" + runtimeGenerationId +
                    " ResumeAfter=" + resumeAfterSequence);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    "[CALLBACK_SCS_OBSERVATION_START_FAILED] Identity=" +
                    identity,
                    exception);
            }
        }

        private static void CompleteScsObservationReplay(
            CommunicationCallbackReplayEvidence evidence)
        {
            try
            {
                CommunicationCallbackScsObservationLedger.Instance
                    .CompleteReplay(evidence);
                Logger.Info(
                    "[CALLBACK_SCS_OBSERVATION_LIVE] Generation=" +
                    evidence.RuntimeGenerationId +
                    " ReplayThrough=" +
                    evidence.ReplayThroughSequence);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    "[CALLBACK_SCS_OBSERVATION_REPLAY_FAILED]",
                    exception);
            }
        }

        private static void EndScsObservationWindow()
        {
            try
            {
                CommunicationCallbackScsObservationLedger.Instance
                    .EndWindow();
            }
            catch (Exception exception)
            {
                Logger.Error(
                    "[CALLBACK_SCS_OBSERVATION_STOP_FAILED]",
                    exception);
            }
        }

        private static CommunicationCallbackParityReport CreateParityReport(
            string identity,
            string runtimeGenerationId,
            CommunicationCallbackReplayEvidence typedReplayEvidence,
            CommunicationCallbackShadowEnvelopeHandler shadowHandler)
        {
            try
            {
                CommunicationCallbackScsObservationLedger scsLedger =
                    CommunicationCallbackScsObservationLedger.Instance;
                CommunicationCallbackReplayEvidence scsReplayEvidence =
                    scsLedger.ReplayEvidence;
                string generation = !string.IsNullOrEmpty(runtimeGenerationId)
                    ? runtimeGenerationId
                    : typedReplayEvidence?.RuntimeGenerationId ??
                      scsReplayEvidence?.RuntimeGenerationId ??
                      string.Empty;
                if ((shadowHandler?.IsStreamActive ?? false) ||
                    scsLedger.IsWindowActive)
                {
                    return CommunicationCallbackParityReport.InProgress(
                        identity,
                        generation);
                }

                CommunicationCallbackParityWindow typedWindow =
                    CommunicationCallbackParityEvidenceAdapter
                        .CreateTypedWindow(
                            identity,
                            generation,
                            false,
                            typedReplayEvidence,
                            shadowHandler?.ObservedCallbacks ?? 0,
                            shadowHandler?.EvictedObservations ?? 0,
                            shadowHandler?.GetObservationSnapshot() ??
                                Array.Empty<
                                    CommunicationCallbackShadowObservation>());
                CommunicationCallbackParityWindow scsWindow =
                    CommunicationCallbackParityEvidenceAdapter
                        .CreateScsWindow(
                            identity,
                            generation,
                            false,
                            scsReplayEvidence,
                            scsLedger.ObservedCallbacks,
                            scsLedger.EvictedObservations,
                            scsLedger.GetObservationSnapshot());
                return CommunicationCallbackParityComparator.Compare(
                    typedWindow,
                    scsWindow);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    "[CALLBACK_PARITY_INVALID_EVIDENCE] Identity=" +
                    identity,
                    exception);
                return CommunicationCallbackParityReport.InvalidEvidence(
                    identity);
            }
        }

        private static void LogParityReport(
            CommunicationCallbackParityReport report)
        {
            if (report == null)
            {
                return;
            }

            Logger.Info(
                "[CALLBACK_PARITY_REPORT] Identity=" +
                report.ProcessIdentity +
                " Verdict=" + report.Verdict +
                " Generation=" + report.RuntimeGenerationId +
                " TypedLive=" + report.TypedLiveCount +
                " ScsLive=" + report.ScsLiveCount +
                " TypedEvicted=" + report.TypedEvictions +
                " ScsEvicted=" + report.ScsEvictions +
                " FirstMismatch=" +
                (report.FirstMismatchIndex?.ToString() ?? "none") +
                " TypedSequence=" + report.TypedSequence +
                " ScsOrdinal=" + report.ScsOrdinal +
                " SCS remains authoritative");
        }

        private void RegisterProcessExitOnce()
        {
            if (_processExitRegistered)
            {
                return;
            }

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            _processExitRegistered = true;
        }

        private void OnProcessExit(object sender, EventArgs eventArgs)
        {
            try
            {
                Stop();
            }
            catch (Exception exception)
            {
                Logger.Error(
                    "[CALLBACK_SHADOW_PROCESS_EXIT_FAILURE]",
                    exception);
            }
        }

        private static string CreateIdentity(
            ClusterNodeRole role,
            Guid worldId,
            int channelId,
            string worldGroup)
        {
            return role == ClusterNodeRole.Login
                ? "Login"
                : "World:" + worldId.ToString("D") +
                  ":" + channelId +
                  ":" + (worldGroup ?? string.Empty);
        }

        private static void OnFault(string identity, Exception exception)
        {
            Logger.Error(
                "[CALLBACK_SHADOW_FAULTED] Identity=" + identity,
                exception);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CommunicationCallbackSubscriberLifecycle));
            }
        }
    }
}
