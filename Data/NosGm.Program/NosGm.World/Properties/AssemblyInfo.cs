using log4net.Config;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

// General Information about an assembly is controlled through the following set of attributes.
// Change these attribute values to modify the information associated with an assembly.
[assembly: AssemblyDescription("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyProduct("")]

// Version information for an assembly consists of the following four values:
//
// Major Version Minor Version Build Number Revision
//
// You can specify all the values or you can default the Build and Revision Numbers by using the '*'
// as shown below: [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.1.*")]
[assembly: AssemblyTitle("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible to COM components. If you
// need to access a type in this assembly from COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b0720365-a61c-407e-854f-2a93526a39fb")]
[assembly: XmlConfigurator(Watch = true)]

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Compatibility definition used by the C# compiler when targeting .NET
    /// Framework 4.8.1, whose reference assemblies predate module initializers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}

namespace NosGm.World
{
    /// <summary>
    /// Starts the optional local presence publisher without coupling gameplay or
    /// login code to Discord. The publisher is activated only for the local World
    /// identity used by the NosGM development launcher or through an explicit flag.
    /// </summary>
    internal static class LauncherPresenceModule
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LauncherPresencePublisher.StartFromEnvironment();
        }
    }

    internal sealed class LauncherPresencePublisher : IDisposable
    {
        private const int MaximumPayloadBytes = 8 * 1024;
        private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);
        private static LauncherPresencePublisher _instance;

        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, long> _sessionStartedAt =
            new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, string> _lastPayload =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, DateTime> _lastSentAt =
            new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer
        {
            MaxJsonLength = MaximumPayloadBytes
        };
        private readonly Task _worker;
        private bool _disposed;

        private LauncherPresencePublisher()
        {
            _worker = Task.Run(() => RunAsync(_stop.Token));
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        public static void StartFromEnvironment()
        {
            string explicitSetting = Environment.GetEnvironmentVariable(
                "NOSGM_LAUNCHER_PRESENCE_LOCAL_PIPE_ENABLED");
            string localWorldIdentity = Environment.GetEnvironmentVariable(
                "NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID");
            bool enabled = string.Equals(
                               explicitSetting,
                               "true",
                               StringComparison.OrdinalIgnoreCase) ||
                           string.IsNullOrWhiteSpace(explicitSetting) &&
                           string.Equals(
                               localWorldIdentity,
                               "world-local-1",
                               StringComparison.Ordinal);
            if (!enabled)
            {
                return;
            }

            Interlocked.CompareExchange(
                ref _instance,
                new LauncherPresencePublisher(),
                null)?.Dispose();
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    PublishCurrentSessions();
                }
                catch
                {
                    // Presence is observational. It must never affect World life.
                }

                try
                {
                    await Task.Delay(ScanInterval, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void PublishCurrentSessions()
        {
            var sessions = ServerManager.Instance.Sessions
                .Where(session =>
                    session != null &&
                    session.Account != null &&
                    session.Character != null &&
                    (session.CurrentMapInstance ?? session.Character.MapInstance) != null)
                .ToList();
            var activeRoutes = new HashSet<string>(StringComparer.Ordinal);
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            DateTime nowUtc = DateTime.UtcNow;

            foreach (ClientSession session in sessions)
            {
                string accountName = session.Account.Name;
                if (string.IsNullOrWhiteSpace(accountName))
                {
                    continue;
                }

                string route = BuildRoute(accountName);
                activeRoutes.Add(route);
                long startedAt = _sessionStartedAt.GetOrAdd(route, nowUnix);
                PresenceSnapshot snapshot = BuildSnapshot(session, startedAt);
                string payload = _json.Serialize(snapshot);
                if (Encoding.UTF8.GetByteCount(payload) > MaximumPayloadBytes)
                {
                    continue;
                }

                bool changed = !_lastPayload.TryGetValue(route, out string previous) ||
                               !string.Equals(previous, payload, StringComparison.Ordinal);
                bool heartbeatDue = !_lastSentAt.TryGetValue(route, out DateTime lastSent) ||
                                    nowUtc - lastSent >= HeartbeatInterval;
                if (!changed && !heartbeatDue)
                {
                    continue;
                }

                if (TrySend(route, payload))
                {
                    _lastPayload[route] = payload;
                    _lastSentAt[route] = nowUtc;
                }
            }

            foreach (string route in _sessionStartedAt.Keys)
            {
                if (activeRoutes.Contains(route))
                {
                    continue;
                }

                _sessionStartedAt.TryRemove(route, out _);
                _lastPayload.TryRemove(route, out _);
                _lastSentAt.TryRemove(route, out _);
            }
        }

        private static PresenceSnapshot BuildSnapshot(
            ClientSession session,
            long startedAt)
        {
            Character character = session.Character;
            MapInstance mapInstance = session.CurrentMapInstance ?? character.MapInstance;
            string mapName = mapInstance.Map?.Name;
            if (string.IsNullOrWhiteSpace(mapName))
            {
                int mapId = mapInstance.Map?.MapId ?? mapInstance.MapId;
                mapName = "Mapa " + mapId;
            }

            PresenceClassification classification = Classify(
                mapInstance.MapInstanceType,
                mapName);
            Group group = character.Group;
            int partyCurrent = group?.SessionCount ?? 0;
            int partyMaximum = ResolvePartyMaximum(group);
            string className = character.Class.ToString();

            return new PresenceSnapshot
            {
                SchemaVersion = 1,
                Activity = classification.Activity,
                Details = classification.Details,
                MapName = mapName,
                CharacterName = character.Name ?? string.Empty,
                Level = character.Level,
                HeroLevel = character.HeroLevel,
                ClassName = className,
                ChannelId = ServerManager.Instance.ChannelId,
                PartyCurrent = partyCurrent,
                PartyMaximum = partyMaximum,
                SessionStartedUnixSeconds = startedAt,
                LargeImageKey = "nosgm",
                LargeImageText = "NosGM",
                SmallImageKey = "class_" + className.ToLowerInvariant(),
                SmallImageText = className
            };
        }

        private static PresenceClassification Classify(
            MapInstanceType type,
            string mapName)
        {
            switch (type)
            {
                case MapInstanceType.LodInstance:
                    return new PresenceClassification(
                        "lod",
                        "Combatiendo en Tierra de la Muerte");
                case MapInstanceType.TimeSpaceInstance:
                    return new PresenceClassification(
                        "timespace",
                        "Explorando una Piedra del Tiempo");
                case MapInstanceType.RaidInstance:
                case MapInstanceType.FamilyRaidInstance:
                    return new PresenceClassification(
                        "raid",
                        "Participando en una raid");
                case MapInstanceType.CaligorInstance:
                    return new PresenceClassification(
                        "caligor",
                        "Luchando contra Caligor");
                case MapInstanceType.IceBreakerInstance:
                    return new PresenceClassification(
                        "icebreaker",
                        "Participando en Ice Breaker");
                case MapInstanceType.TalentArenaMapInstance:
                    return new PresenceClassification(
                        "talent_arena",
                        "Compitiendo en Talent Arena");
                case MapInstanceType.RainbowBattleInstance:
                    return new PresenceClassification(
                        "rainbow_battle",
                        "Participando en Rainbow Battle");
                case MapInstanceType.ArenaInstance:
                case MapInstanceType.PVPInstance:
                    return new PresenceClassification(
                        "arena",
                        "Compitiendo en la arena");
                case MapInstanceType.GlacernonShip:
                case MapInstanceType.Act4ShipAngel:
                case MapInstanceType.Act4ShipDemon:
                case MapInstanceType.Act7Ship:
                    return new PresenceClassification(
                        "ship",
                        "Viajando entre continentes");
                case MapInstanceType.Act4Morcos:
                case MapInstanceType.Act4Hatus:
                case MapInstanceType.Act4Calvina:
                case MapInstanceType.Act4Berios:
                case MapInstanceType.Act4Instance:
                    return new PresenceClassification(
                        "glacernon",
                        "Aventurándose por Glacernon");
                case MapInstanceType.CelestialSpire:
                    return new PresenceClassification(
                        "celestial_spire",
                        "Ascendiendo la Aguja Celestial");
                case MapInstanceType.EventGameInstance:
                case MapInstanceType.SheepGameInstance:
                    if (mapName.IndexOf("instant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mapName.IndexOf("instantánea", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return new PresenceClassification(
                            "instant_battle",
                            "Participando en Instant Battle");
                    }

                    return new PresenceClassification(
                        "event",
                        "Participando en un evento");
                default:
                    return new PresenceClassification(
                        "playing",
                        "Explorando " + mapName);
            }
        }

        private static int ResolvePartyMaximum(Group group)
        {
            if (group == null)
            {
                return 0;
            }

            switch (group.GroupType)
            {
                case GroupType.Group:
                    return 3;
                case GroupType.TalentArena:
                    return 6;
                case GroupType.Team:
                    return 8;
                case GroupType.RBBBlue:
                case GroupType.RBBRed:
                    return 10;
                case GroupType.BigTeam:
                    return 15;
                case GroupType.MediumTeam:
                    return 20;
                case GroupType.GiantTeam:
                    return 40;
                default:
                    return Math.Max(group.SessionCount, 1);
            }
        }

        private static bool TrySend(string route, string payload)
        {
            byte[] body = Encoding.UTF8.GetBytes(payload);
            byte[] length = BitConverter.GetBytes(body.Length);
            try
            {
                using (var pipe = new NamedPipeClientStream(
                           ".",
                           "nosgm-presence-" + route,
                           PipeDirection.Out,
                           PipeOptions.Asynchronous))
                {
                    pipe.Connect(75);
                    pipe.Write(length, 0, length.Length);
                    pipe.Write(body, 0, body.Length);
                    pipe.Flush();
                    return true;
                }
            }
            catch (Exception exception) when (
                exception is TimeoutException ||
                exception is IOException ||
                exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static string BuildRoute(string accountName)
        {
            string normalized = accountName.Trim().ToUpperInvariant();
            byte[] bytes = Encoding.UTF8.GetBytes(normalized);
            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(bytes);
            }

            var builder = new StringBuilder(24);
            for (int index = 0; index < 12; index++)
            {
                builder.Append(hash[index].ToString("x2"));
            }

            Array.Clear(bytes, 0, bytes.Length);
            Array.Clear(hash, 0, hash.Length);
            return builder.ToString();
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            _stop.Cancel();
            try
            {
                _worker.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // Process shutdown does not wait for optional presence cleanup.
            }

            _stop.Dispose();
        }

        private sealed class PresenceClassification
        {
            public PresenceClassification(string activity, string details)
            {
                Activity = activity;
                Details = details;
            }

            public string Activity { get; }
            public string Details { get; }
        }

        private sealed class PresenceSnapshot
        {
            public int SchemaVersion { get; set; }
            public string Activity { get; set; }
            public string Details { get; set; }
            public string MapName { get; set; }
            public string CharacterName { get; set; }
            public int Level { get; set; }
            public int HeroLevel { get; set; }
            public string ClassName { get; set; }
            public int ChannelId { get; set; }
            public int PartyCurrent { get; set; }
            public int PartyMaximum { get; set; }
            public long SessionStartedUnixSeconds { get; set; }
            public string LargeImageKey { get; set; }
            public string LargeImageText { get; set; }
            public string SmallImageKey { get; set; }
            public string SmallImageText { get; set; }
        }
    }
}
