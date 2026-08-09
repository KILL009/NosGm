using Grpc.Core;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Communication.Client;
using NosGm.Core;
using System;
using System.Threading;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Master.Server
{
    internal sealed class MasterPenaltyRefreshGrpcAuthority
    {
        private static readonly Lazy<MasterPenaltyRefreshGrpcAuthority>
            LazyInstance =
                new Lazy<MasterPenaltyRefreshGrpcAuthority>(
                    () => new MasterPenaltyRefreshGrpcAuthority());

        private const int MaximumAttempts = 3;
        private const int InitialRetryDelayMilliseconds = 100;

        private MasterPenaltyRefreshGrpcAuthority()
        {
        }

        public static MasterPenaltyRefreshGrpcAuthority Instance =>
            LazyInstance.Value;

        public ulong Publish(int penaltyLogId)
        {
            if (penaltyLogId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(penaltyLogId),
                    "PenaltyRefresh requires a positive penalty log ID.");
            }

            AuthenticationGrpcClientOptions options =
                MasterCommunicationGrpcIdentityOptions.Load();
            string eventId = Guid.NewGuid().ToString("D");
            var template = new WireV1.PublishCommunicationCallbackRequest
            {
                EventId = eventId,
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
            };

            using (var publisher =
                   new GrpcCommunicationCallbackPublisher(options))
            using (var timeout =
                   new CancellationTokenSource(
                       TimeSpan.FromMilliseconds(
                           options.DeadlineMilliseconds)))
            {
                int retryDelayMilliseconds =
                    InitialRetryDelayMilliseconds;
                Exception lastException = null;

                for (int attempt = 1; attempt <= MaximumAttempts; attempt++)
                {
                    timeout.Token.ThrowIfCancellationRequested();
                    try
                    {
                        WireV1.PublishCommunicationCallbackResponse response =
                            publisher.PublishAsync(
                                    template,
                                    timeout.Token)
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();
                        if (response.Result ==
                                WireV1.CommunicationResultCode.Success &&
                            response.AcceptedSequence > 0)
                        {
                            Logger.Info(
                                "[CALLBACK_PENALTY_GRPC_ACCEPTED] PenaltyLogId=" +
                                penaltyLogId +
                                " Sequence=" +
                                response.AcceptedSequence +
                                " MatchedSubscribers=" +
                                response.MatchedSubscribers);
                            return response.AcceptedSequence;
                        }

                        if (response.Result !=
                                WireV1.CommunicationResultCode.Unavailable &&
                            response.Result !=
                                WireV1.CommunicationResultCode.CapacityExceeded)
                        {
                            throw new InvalidOperationException(
                                "Authoritative PenaltyRefresh publication failed with " +
                                response.Result + ".");
                        }

                        lastException = new InvalidOperationException(
                            "Authoritative PenaltyRefresh publication returned " +
                            response.Result + ".");
                    }
                    catch (RpcException exception)
                        when (exception.StatusCode == StatusCode.Unavailable ||
                              exception.StatusCode == StatusCode.DeadlineExceeded)
                    {
                        lastException = exception;
                    }

                    if (attempt == MaximumAttempts)
                    {
                        break;
                    }

                    if (timeout.Token.WaitHandle.WaitOne(
                            retryDelayMilliseconds))
                    {
                        timeout.Token.ThrowIfCancellationRequested();
                    }
                    retryDelayMilliseconds = checked(
                        retryDelayMilliseconds * 2);
                }

                throw new InvalidOperationException(
                    "Authoritative PenaltyRefresh gRPC publication failed closed; no SCS callback was attempted.",
                    lastException);
            }
        }
    }
}
