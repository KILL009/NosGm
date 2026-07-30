using Grpc.Core;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Communication.Client;
using NosGm.Core;
using NosGm.Domain;
using NosGm.Master.Library.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Master.Server
{
    internal enum MasterCommunicationCallbackMirrorState
    {
        Created = 0,
        Running = 1,
        Stopping = 2,
        Stopped = 3,
        Faulted = 4
    }

    internal sealed class MasterCommunicationCallbackMirror : IDisposable
    {
        private sealed class MirrorItem
        {
            public string Operation { get; set; }

            public WireV1.PublishCommunicationCallbackRequest Template
            {
                get;
                set;
            }

            public DateTimeOffset EnqueuedAt { get; set; }
        }

        private static readonly Lazy<MasterCommunicationCallbackMirror>
            LazyInstance =
                new Lazy<MasterCommunicationCallbackMirror>(
                    () => new MasterCommunicationCallbackMirror());

        private const int InitialRetryDelayMilliseconds = 250;
        private const int MaximumRetryDelayMilliseconds = 5000;
        private readonly object _syncRoot = new object();
        private BlockingCollection<MirrorItem> _queue;
        private CancellationTokenSource _cancellation;
        private ICommunicationCallbackPublisher _publisher;
        private Task _worker;
        private CommunicationCallbackMirrorOptions _options;
        private MasterCommunicationCallbackMirrorState _state =
            MasterCommunicationCallbackMirrorState.Created;
        private Exception _lastException;
        private long _enqueued;
        private long _published;
        private long _dropped;
        private long _expired;
        private int _disposed;

        private MasterCommunicationCallbackMirror()
        {
        }

        public static MasterCommunicationCallbackMirror Instance =>
            LazyInstance.Value;

        public MasterCommunicationCallbackMirrorState State
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

        public long Enqueued => Interlocked.Read(ref _enqueued);

        public long Published => Interlocked.Read(ref _published);

        public long Dropped => Interlocked.Read(ref _dropped);

        public long Expired => Interlocked.Read(ref _expired);

        public bool Start()
        {
            ThrowIfDisposed();
            CommunicationCallbackMirrorOptions options =
                CommunicationCallbackMirrorOptions.Load();

            lock (_syncRoot)
            {
                if (_state == MasterCommunicationCallbackMirrorState.Running)
                {
                    return false;
                }
                if (_state == MasterCommunicationCallbackMirrorState.Stopping)
                {
                    throw new InvalidOperationException(
                        "The Master callback mirror is stopping.");
                }
                if (_state == MasterCommunicationCallbackMirrorState.Faulted)
                {
                    throw new InvalidOperationException(
                        "The Master callback mirror cannot restart after a terminal failure.",
                        _lastException);
                }

                _options = options;
                if (!options.Enabled)
                {
                    _state = MasterCommunicationCallbackMirrorState.Stopped;
                    Logger.Info(
                        "[CALLBACK_MIRROR_DISABLED] EnableWith=" +
                        CommunicationCallbackMirrorOptions.EnabledVariable +
                        "=true");
                    return false;
                }

                var publisher = new GrpcCommunicationCallbackPublisher(
                    MasterCommunicationGrpcIdentityOptions.Load());
                var queue = new BlockingCollection<MirrorItem>(
                    new ConcurrentQueue<MirrorItem>(),
                    options.QueueCapacity);
                var cancellation = new CancellationTokenSource();

                _publisher = publisher;
                _queue = queue;
                _cancellation = cancellation;
                _lastException = null;
                _state = MasterCommunicationCallbackMirrorState.Running;
                _worker = Task.Run(
                    () => RunWorkerAsync(
                        queue,
                        publisher,
                        cancellation.Token));
                Logger.Info(
                    "[CALLBACK_MIRROR_STARTED] QueueCapacity=" +
                    options.QueueCapacity +
                    " StopTimeoutMs=" +
                    options.StopTimeoutMilliseconds);
                return true;
            }
        }

        public bool TryCharacterPresence(
            string worldGroup,
            long characterId,
            bool connected)
        {
            return TryCreateAndEnqueue(
                connected ? "CharacterConnected" : "CharacterDisconnected",
                () => new WireV1.PublishCommunicationCallbackRequest
                {
                    EventId = NewEventId(),
                    TtlSeconds =
                        CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
                    Target = WorldGroupTarget(worldGroup),
                    CharacterPresence = new WireV1.CharacterPresenceCallback
                    {
                        CharacterId = characterId,
                        Connected = connected
                    }
                });
        }

        public bool TryKickSession(long? accountId, int? sessionId)
        {
            return TryCreateAndEnqueue(
                "KickSession",
                () =>
                {
                    var callback = new WireV1.KickSessionCallback();
                    if (accountId.HasValue)
                    {
                        callback.AccountId = accountId.Value;
                    }
                    if (sessionId.HasValue)
                    {
                        callback.SessionId = sessionId.Value;
                    }
                    return new WireV1.PublishCommunicationCallbackRequest
                    {
                        EventId = NewEventId(),
                        TtlSeconds = CommunicationCallbackContractLimits
                            .DefaultEventTtlSeconds,
                        Target = AllWorldsTarget(),
                        KickSession = callback
                    };
                });
        }

        public bool TryRestart(string worldGroup, int delaySeconds)
        {
            return TryLifecycle(
                "Restart",
                worldGroup,
                WireV1.CommunicationLifecycleAction.Restart,
                checked((uint)delaySeconds));
        }

        public bool TryShutdown(string worldGroup)
        {
            return TryLifecycle(
                "Shutdown",
                worldGroup,
                WireV1.CommunicationLifecycleAction.Shutdown,
                0);
        }

        public bool TryGlobalEvent(EventType eventType, byte value)
        {
            return TryCreateAndEnqueue(
                "RunGlobalEvent",
                () => new WireV1.PublishCommunicationCallbackRequest
                {
                    EventId = NewEventId(),
                    TtlSeconds =
                        CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
                    Target = AllWorldsTarget(),
                    GlobalEvent = new WireV1.GlobalEventCallback
                    {
                        EventType =
                            CommunicationGlobalEventMapper.ToWire(eventType),
                        Value = value
                    }
                });
        }

        public bool TryBazaarRefresh(string worldGroup, long bazaarItemId)
        {
            return TryCreateAndEnqueue(
                "UpdateBazaar",
                () => new WireV1.PublishCommunicationCallbackRequest
                {
                    EventId = NewEventId(),
                    TtlSeconds =
                        CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
                    Target = WorldGroupTarget(worldGroup),
                    BazaarRefresh = new WireV1.BazaarRefreshCallback
                    {
                        BazaarItemId = bazaarItemId
                    }
                });
        }

        public bool TryFamilyRefresh(
            string worldGroup,
            long familyId,
            bool changeFaction)
        {
            return TryCreateAndEnqueue(
                "UpdateFamily",
                () => new WireV1.PublishCommunicationCallbackRequest
                {
                    EventId = NewEventId(),
                    TtlSeconds =
                        CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
                    Target = WorldGroupTarget(worldGroup),
                    FamilyRefresh = new WireV1.FamilyRefreshCallback
                    {
                        FamilyId = familyId,
                        ChangeFaction = changeFaction
                    }
                });
        }

        public bool TryPenaltyRefresh(int penaltyLogId)
        {
            return TryCreateAndEnqueue(
                "UpdatePenaltyLog",
                () => new WireV1.PublishCommunicationCallbackRequest
                {
                    EventId = NewEventId(),
                    TtlSeconds =
                        CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
                    Target = new WireV1.CommunicationCallbackTarget
                    {
                        Kind = WireV1.CommunicationCallbackTargetKind.AllNodes
                    },
                    PenaltyRefresh = new WireV1.PenaltyRefreshCallback
                    {
                        PenaltyLogId = penaltyLogId
                    }
                });
        }

        public bool TryRelationRefresh(string worldGroup, long relationId)
        {
            return TryCreateAndEnqueue(
                "UpdateRelation",
                () => new WireV1.PublishCommunicationCallbackRequest
                {
                    EventId = NewEventId(),
                    TtlSeconds =
                        CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
                    Target = WorldGroupTarget(worldGroup),
                    RelationRefresh = new WireV1.RelationRefreshCallback
                    {
                        RelationId = relationId
                    }
                });
        }

        public bool TryStaticBonusRefresh(long characterId)
        {
            return TryCreateAndEnqueue(
                "UpdateStaticBonus",
                () => new WireV1.PublishCommunicationCallbackRequest
                {
                    EventId = NewEventId(),
                    TtlSeconds =
                        CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
                    Target = new WireV1.CommunicationCallbackTarget
                    {
                        Kind = WireV1
                            .CommunicationCallbackTargetKind.CharacterId,
                        CharacterId = characterId
                    },
                    StaticBonusRefresh = new WireV1.StaticBonusRefreshCallback
                    {
                        CharacterId = characterId
                    }
                });
        }

        public bool Stop()
        {
            BlockingCollection<MirrorItem> queue;
            CancellationTokenSource cancellation;
            ICommunicationCallbackPublisher publisher;
            Task worker;
            int timeoutMilliseconds;

            lock (_syncRoot)
            {
                if (_state == MasterCommunicationCallbackMirrorState.Created ||
                    _state == MasterCommunicationCallbackMirrorState.Stopped)
                {
                    return true;
                }
                if (_state == MasterCommunicationCallbackMirrorState.Stopping)
                {
                    return false;
                }

                queue = _queue;
                cancellation = _cancellation;
                publisher = _publisher;
                worker = _worker;
                timeoutMilliseconds = _options?.StopTimeoutMilliseconds ??
                    CommunicationCallbackMirrorOptions
                        .DefaultStopTimeoutMilliseconds;
                if (_state != MasterCommunicationCallbackMirrorState.Faulted)
                {
                    _state = MasterCommunicationCallbackMirrorState.Stopping;
                }
                queue?.CompleteAdding();
            }

            bool completed = worker == null ||
                WaitWithoutThrow(worker, timeoutMilliseconds);
            if (!completed)
            {
                cancellation?.Cancel();
                publisher?.Dispose();
                completed = WaitWithoutThrow(worker, 1000);
                Logger.Error(
                    "[CALLBACK_MIRROR_STOP_TIMEOUT] TimeoutMs=" +
                    timeoutMilliseconds +
                    " Pending=" +
                    (queue?.Count ?? 0));
            }
            else
            {
                publisher?.Dispose();
            }

            cancellation?.Dispose();
            queue?.Dispose();
            lock (_syncRoot)
            {
                _queue = null;
                _cancellation = null;
                _publisher = null;
                _worker = null;
                if (_state != MasterCommunicationCallbackMirrorState.Faulted)
                {
                    _state = MasterCommunicationCallbackMirrorState.Stopped;
                }
            }

            Logger.Info(
                "[CALLBACK_MIRROR_STOPPED] Completed=" + completed +
                " Enqueued=" + Enqueued +
                " Published=" + Published +
                " Dropped=" + Dropped +
                " Expired=" + Expired);
            return completed;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            Stop();
        }

        private bool TryLifecycle(
            string operation,
            string worldGroup,
            WireV1.CommunicationLifecycleAction action,
            uint delaySeconds)
        {
            return TryCreateAndEnqueue(
                operation,
                () => new WireV1.PublishCommunicationCallbackRequest
                {
                    EventId = NewEventId(),
                    TtlSeconds =
                        CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
                    Target = string.Equals(
                            worldGroup,
                            "*",
                            StringComparison.Ordinal)
                        ? AllWorldsTarget()
                        : WorldGroupTarget(worldGroup),
                    Lifecycle = new WireV1.LifecycleCallback
                    {
                        Action = action,
                        DelaySeconds = delaySeconds
                    }
                });
        }

        private bool TryCreateAndEnqueue(
            string operation,
            Func<WireV1.PublishCommunicationCallbackRequest> create)
        {
            BlockingCollection<MirrorItem> queue;
            lock (_syncRoot)
            {
                if (_state != MasterCommunicationCallbackMirrorState.Running ||
                    _queue == null)
                {
                    return false;
                }
                queue = _queue;
            }

            try
            {
                WireV1.PublishCommunicationCallbackRequest template = create();
                template.Context = null;
                bool added = queue.TryAdd(
                    new MirrorItem
                    {
                        Operation = operation,
                        Template = template,
                        EnqueuedAt = DateTimeOffset.UtcNow
                    });
                if (added)
                {
                    Interlocked.Increment(ref _enqueued);
                    return true;
                }
                RecordDrop(operation, "QUEUE_FULL");
                return false;
            }
            catch (InvalidOperationException)
            {
                RecordDrop(operation, "QUEUE_CLOSED");
                return false;
            }
            catch (Exception ex)
            {
                RecordDrop(operation, "BUILD_FAILED");
                Logger.Error(
                    "[CALLBACK_MIRROR_BUILD_FAILED] Operation=" + operation,
                    ex);
                return false;
            }
        }

        private async Task RunWorkerAsync(
            BlockingCollection<MirrorItem> queue,
            ICommunicationCallbackPublisher publisher,
            CancellationToken cancellationToken)
        {
            try
            {
                foreach (MirrorItem item in
                         queue.GetConsumingEnumerable(cancellationToken))
                {
                    await PublishWithRetryAsync(
                            item,
                            publisher,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Controlled bounded shutdown.
            }
            catch (Exception ex)
            {
                lock (_syncRoot)
                {
                    _lastException = ex;
                    _state = MasterCommunicationCallbackMirrorState.Faulted;
                }
                Logger.Error(
                    "[CALLBACK_MIRROR_FAULTED] SCS remains authoritative.",
                    ex);
            }
            finally
            {
                lock (_syncRoot)
                {
                    if (_state == MasterCommunicationCallbackMirrorState.Running)
                    {
                        _state = MasterCommunicationCallbackMirrorState.Stopped;
                    }
                }
            }
        }

        private async Task PublishWithRetryAsync(
            MirrorItem item,
            ICommunicationCallbackPublisher publisher,
            CancellationToken cancellationToken)
        {
            int retryDelay = InitialRetryDelayMilliseconds;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DateTimeOffset expiresAt = item.EnqueuedAt.AddSeconds(
                    item.Template.TtlSeconds);
                if (DateTimeOffset.UtcNow >= expiresAt)
                {
                    Interlocked.Increment(ref _expired);
                    Logger.Warn(
                        "[CALLBACK_MIRROR_EXPIRED] Operation=" +
                        item.Operation +
                        " EventId=" +
                        item.Template.EventId);
                    return;
                }

                try
                {
                    WireV1.PublishCommunicationCallbackResponse response =
                        await publisher.PublishAsync(
                                item.Template,
                                cancellationToken)
                            .ConfigureAwait(false);
                    if (response.Result ==
                        WireV1.CommunicationResultCode.Success)
                    {
                        Interlocked.Increment(ref _published);
                        return;
                    }
                    if (response.Result ==
                            WireV1.CommunicationResultCode.Unavailable ||
                        response.Result ==
                            WireV1.CommunicationResultCode.CapacityExceeded)
                    {
                        await DelayBeforeRetryAsync(
                                retryDelay,
                                expiresAt,
                                cancellationToken)
                            .ConfigureAwait(false);
                        retryDelay = Math.Min(
                            MaximumRetryDelayMilliseconds,
                            checked(retryDelay * 2));
                        continue;
                    }

                    throw new InvalidOperationException(
                        "Callback mirror publication " +
                        item.Operation +
                        " failed with " +
                        response.Result +
                        ".");
                }
                catch (RpcException ex)
                    when (IsTransient(ex, cancellationToken))
                {
                    await DelayBeforeRetryAsync(
                            retryDelay,
                            expiresAt,
                            cancellationToken)
                        .ConfigureAwait(false);
                    retryDelay = Math.Min(
                        MaximumRetryDelayMilliseconds,
                        checked(retryDelay * 2));
                }
            }
        }

        private static async Task DelayBeforeRetryAsync(
            int retryDelayMilliseconds,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            TimeSpan remaining = expiresAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }
            int delay = Math.Min(
                retryDelayMilliseconds,
                Math.Max(1, checked((int)Math.Min(
                    int.MaxValue,
                    remaining.TotalMilliseconds))));
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsTransient(
            RpcException exception,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            switch (exception.StatusCode)
            {
                case StatusCode.Cancelled:
                case StatusCode.Unknown:
                case StatusCode.DeadlineExceeded:
                case StatusCode.ResourceExhausted:
                case StatusCode.Aborted:
                case StatusCode.Internal:
                case StatusCode.Unavailable:
                    return true;
                default:
                    return false;
            }
        }

        private static bool WaitWithoutThrow(Task task, int timeoutMilliseconds)
        {
            try
            {
                return task.Wait(timeoutMilliseconds);
            }
            catch (AggregateException)
            {
                return true;
            }
        }

        private void RecordDrop(string operation, string reason)
        {
            long dropped = Interlocked.Increment(ref _dropped);
            if (dropped == 1 || (dropped & (dropped - 1)) == 0)
            {
                Logger.Warn(
                    "[CALLBACK_MIRROR_DROPPED] Operation=" + operation +
                    " Reason=" + reason +
                    " TotalDropped=" + dropped);
            }
        }

        private static WireV1.CommunicationCallbackTarget AllWorldsTarget()
        {
            return new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.AllWorlds
            };
        }

        private static WireV1.CommunicationCallbackTarget WorldGroupTarget(
            string worldGroup)
        {
            return new WireV1.CommunicationCallbackTarget
            {
                Kind = WireV1.CommunicationCallbackTargetKind.WorldGroup,
                WorldGroup = worldGroup ?? string.Empty
            };
        }

        private static string NewEventId()
        {
            return Guid.NewGuid().ToString("D");
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(
                    nameof(MasterCommunicationCallbackMirror));
            }
        }
    }
}
