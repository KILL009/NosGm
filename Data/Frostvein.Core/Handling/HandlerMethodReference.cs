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
            HandlerMethod = Wrap(handlerMethod, ResolveMetricHeader(Identification));
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
            HandlerMethod = Wrap(handlerMethod, ResolveMetricHeader(Identification));
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

        private static Action<object, object> Wrap(Action<object, object> handlerMethod, string header)
        {
            if (handlerMethod == null)
            {
                throw new ArgumentNullException(nameof(handlerMethod));
            }

            return (handler, packet) =>
            {
                long started = Stopwatch.GetTimestamp();
                bool succeeded = false;
                try
                {
                    handlerMethod(handler, packet);
                    succeeded = true;
                }
                finally
                {
                    ServerPerformanceMonitor.Instance.RecordHandler(
                        header,
                        Stopwatch.GetTimestamp() - started,
                        succeeded);
                }
            };
        }

        private static string ResolveMetricHeader(string[] identification)
        {
            return identification?
                       .FirstOrDefault(header => !string.IsNullOrWhiteSpace(header))
                   ?? "<unidentified>";
        }
    }
}
