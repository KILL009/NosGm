/*
 * This file is part of the Frostvein Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using Frostvein.Core.Diagnostics;
using Frostvein.Domain;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Frostvein.Core.Handling
{
    public class HandlerMethodReference
    {
        #region Instantiation

        public HandlerMethodReference(
            Action<object, object> handlerMethod,
            IPacketHandler parentHandler,
            PacketAttribute handlerMethodAttribute)
        {
            ParentHandler = parentHandler;
            HandlerMethodAttribute = handlerMethodAttribute;
            Identification = HandlerMethodAttribute.Header;
            PassNonParseablePacket = false;
            Authority = AuthorityType.User;
            string header = ResolveMetricHeader(Identification);
            HandlerMethod = Wrap(
                handlerMethod,
                ParentHandler,
                header,
                Authority,
                new HandlerPacketRateGuard(header));
        }

        public HandlerMethodReference(
            Action<object, object> handlerMethod,
            IPacketHandler parentHandler,
            Type packetBaseParameterType)
        {
            ParentHandler = parentHandler;
            PacketDefinitionParameterType = packetBaseParameterType;
            PacketHeaderAttribute headerAttribute =
                (PacketHeaderAttribute)Array.Find(
                    PacketDefinitionParameterType.GetCustomAttributes(true),
                    ca => ca.GetType().Equals(typeof(PacketHeaderAttribute)));
            Identification = headerAttribute?.Identification;
            PassNonParseablePacket = headerAttribute?.PassNonParseablePacket ?? false;
            Authority = headerAttribute?.Authority ?? AuthorityType.User;
            IsCharScreen = headerAttribute?.IsCharScreen ?? false;
            Amount = headerAttribute?.Amount ?? 1;
            string header = ResolveMetricHeader(Identification);
            HandlerMethod = Wrap(
                handlerMethod,
                ParentHandler,
                header,
                Authority,
                new HandlerPacketRateGuard(header));
        }

        #endregion

        #region Properties

        public AuthorityType[] Authorities { get; }

        public Action<object, object> HandlerMethod { get; }

        public PacketAttribute HandlerMethodAttribute { get; }

        /// <summary>
        /// String identification of the Packet by Header
        /// </summary>
        public string[] Identification { get; }

        public Type PacketDefinitionParameterType { get; }

        public IPacketHandler ParentHandler { get; }

        public bool PassNonParseablePacket { get; }

        public bool IsCharScreen { get; }

        public int Amount { get; }

        public AuthorityType Authority { get; set; }

        #endregion

        private static Action<object, object> Wrap(
            Action<object, object> handlerMethod,
            IPacketHandler parentHandler,
            string header,
            AuthorityType requiredAuthority,
            HandlerPacketRateGuard rateGuard)
        {
            if (handlerMethod == null)
            {
                throw new ArgumentNullException(nameof(handlerMethod));
            }

            if (rateGuard == null)
            {
                throw new ArgumentNullException(nameof(rateGuard));
            }

            return (handler, packet) =>
            {
                PacketRateDecision decision = rateGuard.Check();
                if (!decision.Allowed)
                {
                    if (decision.ShouldLog)
                    {
                        Logger.Warn(
                            $"Packet guard blocked handler {header} because {decision.Reason}. Disconnect: {decision.Disconnect}.");
                    }

                    if (decision.Disconnect)
                    {
                        TryDisconnect(parentHandler);
                    }
                    return;
                }

                long started = Stopwatch.GetTimestamp();
                bool succeeded = false;
                Exception failure = null;
                try
                {
                    handlerMethod(handler, packet);
                    succeeded = true;
                }
                catch (Exception exception)
                {
                    failure = exception;
                    throw;
                }
                finally
                {
                    ServerPerformanceMonitor.Instance.RecordHandler(
                        header,
                        Stopwatch.GetTimestamp() - started,
                        succeeded);

                    if (IsStaffCommand(header, requiredAuthority))
                    {
                        GmCommandAuditBridge.Publish(new GmCommandExecutionEvent
                        {
                            ParentHandler = parentHandler,
                            Packet = packet,
                            Header = header,
                            RequiredAuthority = requiredAuthority,
                            Outcome = succeeded
                                ? GmCommandAuditOutcome.Executed
                                : GmCommandAuditOutcome.Failed,
                            Exception = failure
                        });
                    }
                }
            };
        }

        private static bool IsStaffCommand(string header, AuthorityType requiredAuthority) =>
            requiredAuthority > AuthorityType.User ||
            (!string.IsNullOrWhiteSpace(header) && header.StartsWith("$", StringComparison.Ordinal));

        private static void TryDisconnect(IPacketHandler parentHandler)
        {
            try
            {
                object session = parentHandler?
                    .GetType()
                    .GetProperty("Session")?
                    .GetValue(parentHandler);
                session?
                    .GetType()
                    .GetMethod("Disconnect", Type.EmptyTypes)?
                    .Invoke(session, null);
            }
            catch
            {
                // Security enforcement must never destabilize the packet loop.
            }
        }

        private static string ResolveMetricHeader(string[] identification)
        {
            return identification?
                       .FirstOrDefault(header => !string.IsNullOrWhiteSpace(header))
                   ?? "<unidentified>";
        }
    }

    /// <summary>
    /// Dependency-neutral bridge from Core to the world-layer persistence service.
    /// A failing audit sink can never break command execution.
    /// </summary>
    public static class GmCommandAuditBridge
    {
        private static Action<GmCommandExecutionEvent> _sink;

        public static void Configure(Action<GmCommandExecutionEvent> sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            Interlocked.CompareExchange(ref _sink, sink, null);
        }

        public static void Publish(GmCommandExecutionEvent auditEvent)
        {
            if (auditEvent == null) return;
            try
            {
                Volatile.Read(ref _sink)?.Invoke(auditEvent);
            }
            catch (Exception exception)
            {
                Logger.Error("The GM command audit sink failed.", exception);
            }
        }
    }

    public sealed class GmCommandExecutionEvent
    {
        public object ParentHandler { get; set; }

        public object Packet { get; set; }

        public string Header { get; set; }

        public AuthorityType RequiredAuthority { get; set; }

        public GmCommandAuditOutcome Outcome { get; set; }

        public Exception Exception { get; set; }
    }
}
