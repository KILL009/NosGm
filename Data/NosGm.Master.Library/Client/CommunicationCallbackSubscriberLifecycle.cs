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
                return true;
            }

            CommunicationCallbackReplayEvidence replayEvidence =
                subscriber?.ReplayEvidence;
            bool stopped = false;
            try
            {
                stopped = host.Stop(
                    TimeSpan.FromMilliseconds(timeoutMilliseconds));
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
                if (!activation.IsEnabled)
                {
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
                    new CommunicationCallbackShadowEnvelopeHandler();
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
                    host.Dispose();
                    throw;
                }

                Logger.Info(
                    "[CALLBACK_SHADOW_STARTED] Identity=" + identity +
                    " CallerInstance=" + subscriberOptions.CallerInstanceId +
                    " WireMode=" + subscriberOptions.WireMode +
                    " Endpoint=" + subscriberOptions.Address +
                    " ObservationCapacity=" +
                    shadowHandler.ObservationCapacity);
                return true;
            }
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
