using Game.Configuration;
using Game.Configuration.BCards;
using NosGm.Core;
using NosGm.Core.Diagnostics;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Plugin.Event;
using NosGm.Packets.Packets.ClientPackets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class GuriPacketHandler : IPacketHandler
    {
        #region Instantiation

        public GuriPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Guri(GuriPacket guriPacket)
        {
            if (guriPacket == null)
            {
                return;
            }

            long started = Stopwatch.GetTimestamp();
            bool succeeded = false;
            try
            {
                HandleGuri(guriPacket);
                succeeded = true;
            }
            finally
            {
                GuriPerformanceMonitor.Record(
                    guriPacket.Type,
                    Stopwatch.GetTimestamp() - started,
                    succeeded);
            }
        }

        public void GuriPerformance(GuriPerformancePacket packet)
        {
            if (Session?.Character == null)
            {
                return;
            }

            string mode = packet?.Mode?.Trim().ToLowerInvariant() ?? "total";
            if (mode == "reset")
            {
                GuriPerformanceMonitor.Reset();
                SendDiagnostic("Guri performance counters were reset.", 11);
                return;
            }

            HandlerSort sort = ParseSort(mode);
            var metrics = GuriPerformanceMonitor.GetTop(12, sort);

            SendDiagnostic($"========== Guri types by {sort} ==========", 11);
            if (metrics.Count == 0)
            {
                SendDiagnostic("No guri samples have been collected yet.");
                SendDiagnostic(GuriPerformancePacket.ReturnHelp(), 11);
                return;
            }

            int position = 1;
            foreach (GuriPerformanceSnapshot metric in metrics)
            {
                string typeName = ResolveTypeName(metric.Type);
                SendDiagnostic(
                    $"{position++}. Type {metric.Type} ({typeName}) | Calls {metric.Count:N0} | " +
                    $"Avg {metric.AverageMilliseconds:N3} ms | Max {metric.MaximumMilliseconds:N3} ms | " +
                    $"Total {metric.TotalMilliseconds:N1} ms | Missing {metric.MissingHandlers:N0} | Err {metric.Errors:N0}");
            }

            SendDiagnostic(GuriPerformancePacket.ReturnHelp(), 11);
        }

        public void BCardPerformance(BCardPerformancePacket packet)
        {
            if (Session?.Character == null)
            {
                return;
            }

            string mode = packet?.Mode?.Trim().ToLowerInvariant() ?? "summary";
            if (mode == "reset")
            {
                BCardPipelineMonitor.Reset();
                SendDiagnostic("BCard runtime counters were reset. Observed missing signatures remain available.", 11);
                return;
            }

            BCardPipelineSnapshot metrics = BCardPipelineMonitor.Capture();
            IReadOnlyDictionary<BCardType.CardType, string> registered = PluginFacility.RegisteredBCardHandlers;
            IReadOnlyCollection<string> missing = PluginFacility.MissingBCardSignatures;
            IReadOnlyDictionary<string, string> samples = PluginFacility.MissingBCardSampleDetails;

            SendDiagnostic("========== BCard Pipeline ==========", 11);
            SendDiagnostic(
                $"Registry {(PluginFacility.IsInitialized ? "READY" : "STOPPED")} | Registered {registered.Count:N0} | Missing signatures {missing.Count:N0}",
                PluginFacility.IsInitialized ? (byte)10 : (byte)11);
            SendDiagnostic(
                $"Executed {metrics.Executed:N0} | Passive calculation skips {metrics.PassiveSkipped:N0} | Missing calls {metrics.Missing:N0}");
            SendDiagnostic(
                $"Unique missing since counter reset {metrics.MissingUnique:N0} | Pre-init attempts {metrics.PreInitializationAttempts:N0} | Handler failures {metrics.HandlerFailures:N0}");

            if (mode == "missing")
            {
                ShowMissingBCardSignatures(missing, samples);
            }
            else if (mode == "handlers" || mode == "registered")
            {
                ShowRegisteredBCardHandlers(registered);
            }

            string health;
            if (!PluginFacility.IsInitialized)
            {
                health = "REGISTRY STOPPED";
            }
            else if (metrics.HandlerFailures > 0)
            {
                health = "HANDLER ERRORS";
            }
            else if (missing.Count > 0)
            {
                health = $"MISSING {missing.Count}";
            }
            else
            {
                health = "OK";
            }

            SendDiagnostic($"BCard health: {health}", health == "OK" ? (byte)10 : (byte)12);
            SendDiagnostic(BCardPerformancePacket.ReturnHelp(), 11);
        }

        private void ShowMissingBCardSignatures(
            IReadOnlyCollection<string> signatures,
            IReadOnlyDictionary<string, string> samples)
        {
            SendDiagnostic("===== Missing executable BCard shapes =====", 11);
            if (signatures == null || signatures.Count == 0)
            {
                SendDiagnostic("No executable BCard shapes without handlers have been observed.", 10);
                return;
            }

            int position = 1;
            foreach (string signature in signatures.Take(20))
            {
                SendDiagnostic($"{position++}. {DescribeBCardSignature(signature)}", 12);
                if (samples != null && samples.TryGetValue(signature, out string sample) &&
                    !string.IsNullOrWhiteSpace(sample))
                {
                    SendDiagnostic($"   Sample: {sample}", 11);
                }
            }

            if (signatures.Count > 20)
            {
                SendDiagnostic($"... and {signatures.Count - 20:N0} more signatures.", 11);
            }
        }

        private void ShowRegisteredBCardHandlers(IReadOnlyDictionary<BCardType.CardType, string> handlers)
        {
            SendDiagnostic("===== Registered executable BCard handlers =====", 11);
            if (handlers == null || handlers.Count == 0)
            {
                SendDiagnostic("No executable BCard handlers are registered.", 12);
                return;
            }

            int position = 1;
            foreach (KeyValuePair<BCardType.CardType, string> handler in handlers
                         .OrderBy(pair => (byte)pair.Key)
                         .Take(20))
            {
                string handlerName = string.IsNullOrWhiteSpace(handler.Value)
                    ? "unknown"
                    : handler.Value.Split('.').Last();
                SendDiagnostic(
                    $"{position++}. Type {(byte)handler.Key} ({ResolveBCardTypeName((byte)handler.Key)}) | {handlerName}");
            }

            if (handlers.Count > 20)
            {
                SendDiagnostic($"... and {handlers.Count - 20:N0} more handlers.", 11);
            }
        }

        private void HandleGuri(GuriPacket guriPacket)
        {
            Character character = Session?.Character;
            if (character?.MapInstance == null || Session.CurrentMapInstance == null)
            {
                return;
            }

            if (guriPacket.Type == (long)GuriType.RainbowBattleFlag)
            {
                if (character.isFreezed)
                {
                    return;
                }

                MapNpc npc = Session.CurrentMapInstance.Npcs.Find(s => s.MapNpcId == guriPacket.User);
                if (npc == null)
                {
                    return;
                }

                int distance = Map.GetDistance(
                    new MapCell { X = character.PositionX, Y = character.PositionY },
                    new MapCell { X = npc.MapX, Y = npc.MapY });
                if (distance > 5)
                {
                    return;
                }

                var rainbowTeam = ServerManager.Instance.RainbowBattleMembers
                    .Find(team => team.Session.Contains(Session));

                if (rainbowTeam == null ||
                    RainbowBattleManager.AlreadyHaveFlag(
                        rainbowTeam,
                        (RainbowNpcType)guriPacket.Argument,
                        (int)guriPacket.User))
                {
                    return;
                }

                RainbowBattleManager.AddFlag(
                    Session,
                    rainbowTeam,
                    (RainbowNpcType)guriPacket.Argument,
                    (int)guriPacket.User);
            }

            if (!guriPacket.Data.HasValue && guriPacket.Type == (long)GuriType.Emoticon)
            {
                return;
            }

            character.Event.EmitEvent(new GuriEvent
            {
                Type = guriPacket.Type,
                Argument = guriPacket.Argument,
                Data = guriPacket.Data ?? 0,
                User = guriPacket.User,
                Value = guriPacket.Value
            });
        }

        private void SendDiagnostic(string message, byte type = 10) =>
            Session.SendPacket(Session.Character.GenerateSay(message, type));

        private static HandlerSort ParseSort(string mode)
        {
            switch (mode)
            {
                case "count":
                case "calls":
                    return HandlerSort.Count;
                case "avg":
                case "average":
                    return HandlerSort.AverageTime;
                case "max":
                case "slow":
                    return HandlerSort.MaximumTime;
                case "error":
                case "errors":
                case "missing":
                    return HandlerSort.Errors;
                default:
                    return HandlerSort.TotalTime;
            }
        }

        private static string ResolveTypeName(long type)
        {
            if (type < int.MinValue || type > int.MaxValue)
            {
                return "Unknown";
            }

            int numericType = (int)type;
            return Enum.IsDefined(typeof(GuriType), numericType)
                ? ((GuriType)numericType).ToString()
                : "Unknown";
        }

        private static string DescribeBCardSignature(string signature)
        {
            string[] parts = (signature ?? string.Empty).Split(':');
            if (parts.Length != 3 ||
                !byte.TryParse(parts[0], out byte type) ||
                !byte.TryParse(parts[1], out byte subType) ||
                !byte.TryParse(parts[2], out byte phase))
            {
                return signature ?? "invalid";
            }

            string typeName = ResolveBCardTypeName(type);
            string phaseName = Enum.IsDefined(typeof(BCardExecutionPhase), phase)
                ? ((BCardExecutionPhase)phase).ToString()
                : phase.ToString();
            string meaning = ResolveBCardMeaning(type, subType);

            return $"Type {type} ({typeName}) | SubType {subType} ({meaning}) | Phase {phaseName}";
        }

        private static string ResolveBCardTypeName(byte type)
        {
            if (type == 104)
            {
                return "DealDamageAround";
            }

            return Enum.IsDefined(typeof(BCardType.CardType), type)
                ? ((BCardType.CardType)type).ToString()
                : "Unknown";
        }

        private static string ResolveBCardMeaning(byte type, byte subType)
        {
            switch (type)
            {
                case 29:
                    switch (subType)
                    {
                        case 11:
                            return "FlameExplosion";
                        case 12:
                            return "FlameExplosionNegated";
                        case 21:
                            return "SurroundingsExplosion";
                        case 22:
                            return "SurroundingsExplosionNegated";
                        case 31:
                            return "SurroundingsAttack";
                        case 32:
                            return "SurroundingsAttackNegated";
                    }
                    break;

                case 36:
                    if (subType == 41)
                    {
                        return "DisableHPConsumption";
                    }
                    break;

                case 39:
                    if (subType == 11)
                    {
                        return "DecreaseHPWithoutDeath";
                    }
                    if (subType == 12)
                    {
                        return "DecreaseHPWithoutKill";
                    }
                    break;

                case 41:
                    if (subType == 15)
                    {
                        return "UndocumentedMode15";
                    }
                    break;

                case 57:
                    switch (subType)
                    {
                        case 11:
                            return "AllInFieldReceiveDamage";
                        case 12:
                            return "AllInFieldsReceiveDamage";
                        case 41:
                            return "UndocumentedRaidEffect41";
                    }
                    break;

                case 104:
                    switch (subType)
                    {
                        case 11:
                            return "DamageDeflect";
                        case 12:
                            return "DamageDeflectNegated";
                        case 21:
                            return "DealAreaDamagePerSecond";
                        case 22:
                            return "DealAreaDamagePerSecondNegated";
                        case 31:
                            return "SummonOnDefend";
                        case 32:
                            return "SummonOnDefendDouble";
                        case 41:
                            return "NosmateAttackIncrease";
                        case 42:
                            return "NosmateAttackIncreaseNegated";
                        case 51:
                            return "EffectiveOnEnemyInAreaPerSecond";
                        case 52:
                            return "EffectiveOnEnemyInAreaPerSecondNegated";
                    }
                    break;
            }

            return "Undocumented";
        }

        #endregion
    }
}
