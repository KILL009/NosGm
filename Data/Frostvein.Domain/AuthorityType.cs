using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Domain
{
    public enum AuthorityType : short
    {
        Closed = -3,
        Banned = -2,
        Unconfirmed = -1,
        User = 0,
        GS = 1,
        GM = 2,
        ADMIN = 3,
        DEV = 4
    }

    /// <summary>
    /// Result captured after a staff command handler returns, throws or is denied.
    /// </summary>
    public enum GmCommandAuditOutcome : byte
    {
        Executed = 1,
        Failed = 2,
        Denied = 3
    }

    /// <summary>
    /// Granular capabilities that can narrow the commands available to one staff account.
    /// The legacy AuthorityType remains the upper ceiling: permissions never elevate an
    /// account above the authority required by a packet header.
    /// </summary>
    [Flags]
    public enum StaffPermission : long
    {
        None = 0,
        Investigation = 1L << 0,
        Moderation = 1L << 1,
        Economy = 1L << 2,
        Events = 1L << 3,
        Content = 1L << 4,
        Operations = 1L << 5,
        Security = 1L << 6,
        All = Investigation | Moderation | Economy | Events | Content | Operations | Security
    }

    public sealed class StaffAuthorizationResult
    {
        public bool Allowed { get; set; }

        public bool ProfileEnabled { get; set; }

        public StaffPermission RequiredPermission { get; set; }

        public StaffPermission GrantedPermissions { get; set; }

        public string Reason { get; set; }
    }

    /// <summary>
    /// Stable command-to-capability map. Unknown commands fail toward the safest category
    /// according to their legacy authority requirement.
    /// </summary>
    public static class StaffPermissionCatalog
    {
        private static readonly HashSet<string> SecurityCommands = Set(
            "$staffperm", "$gmaudit", "$itemtrace");

        private static readonly HashSet<string> InvestigationCommands = Set(
            "$perf", "$serverinfo", "$searchitem", "$searchmonster", "$monsterinfo",
            "$npcinfo", "$mapstats", "$stat", "$charstat", "$usercount", "$position",
            "$channelinfo", "$penaltylog", "$raidboxinfo", "$drops", "$channel");

        private static readonly HashSet<string> ModerationCommands = Set(
            "$ban", "$unban", "$mute", "$unmute", "$kick", "$kicksession", "$warning",
            "$sanction", "$gmcase", "$userlog", "$adduserlog", "$removeuserlog", "$blockpm",
            "$undercover", "$invisible", "$shout", "$shouthere", "$summon", "$teleport", "$gogo");

        private static readonly HashSet<string> EconomyCommands = Set(
            "$gold", "$bank", "$gift", "$createitem", "$cloneitem", "$itemrain",
            "$clearinventory", "$rarify", "$upgrade", "$setperfection", "$drop",
            "$droprate", "$golddroprate", "$goldrate", "$xprate", "$heroxprate",
            "$fairyxprate", "$reputationrate", "$sprefill", "$classpack");

        private static readonly HashSet<string> EventCommands = Set(
            "$event", "$globalevent", "$act4", "$act4stat", "$arenawinner", "$team",
            "$mapdance", "$itemrain", "$mobrain", "$blockxp", "$blockfxp", "$blockrep");

        private static readonly HashSet<string> ContentCommands = Set(
            "$addmonster", "$addnpc", "$addportal", "$addquest", "$addskill",
            "$addshellEffect", "$removemob", "$removeportal", "$clearmap", "$mappvp",
            "$mob", "$npc", "$portalto", "$changemobname", "$changeshopname",
            "$music", "$effect", "$resize", "$morph");

        private static readonly HashSet<string> OperationsCommands = Set(
            "$shutdown", "$shutdownall", "$restart", "$restartall", "$reload",
            "$maintenance", "$configuration", "$sudo", "$addaccount", "$promote",
            "$demote", "$editor", "$editormode", "$language", "$translate");

        public static IEnumerable<StaffPermission> Categories => new[]
        {
            StaffPermission.Investigation,
            StaffPermission.Moderation,
            StaffPermission.Economy,
            StaffPermission.Events,
            StaffPermission.Content,
            StaffPermission.Operations,
            StaffPermission.Security
        };

        public static StaffPermission Resolve(string header, AuthorityType requiredAuthority)
        {
            string normalized = NormalizeHeader(header);
            if (SecurityCommands.Contains(normalized)) return StaffPermission.Security;
            if (InvestigationCommands.Contains(normalized)) return StaffPermission.Investigation;
            if (ModerationCommands.Contains(normalized)) return StaffPermission.Moderation;
            if (EconomyCommands.Contains(normalized)) return StaffPermission.Economy;
            if (EventCommands.Contains(normalized)) return StaffPermission.Events;
            if (ContentCommands.Contains(normalized)) return StaffPermission.Content;
            if (OperationsCommands.Contains(normalized)) return StaffPermission.Operations;

            if (normalized.StartsWith("$add", StringComparison.Ordinal) ||
                normalized.StartsWith("$remove", StringComparison.Ordinal) ||
                normalized.StartsWith("$change", StringComparison.Ordinal))
            {
                return StaffPermission.Content;
            }

            if (normalized.Contains("rate") || normalized.Contains("gold") ||
                normalized.Contains("item") || normalized.Contains("upgrade"))
            {
                return StaffPermission.Economy;
            }

            if (normalized.Contains("event") || normalized.Contains("arena") ||
                normalized.Contains("raid"))
            {
                return StaffPermission.Events;
            }

            if (requiredAuthority >= AuthorityType.ADMIN) return StaffPermission.Operations;
            if (requiredAuthority >= AuthorityType.GM) return StaffPermission.Moderation;
            return StaffPermission.Investigation;
        }

        public static bool IsManagementCommand(string header) =>
            string.Equals(NormalizeHeader(header), "$staffperm", StringComparison.Ordinal);

        public static bool TryParse(string value, out StaffPermission permission)
        {
            permission = StaffPermission.None;
            if (string.IsNullOrWhiteSpace(value)) return false;

            switch (value.Trim().ToLowerInvariant())
            {
                case "investigation":
                case "investigate":
                case "inspect":
                case "info":
                    permission = StaffPermission.Investigation;
                    return true;
                case "moderation":
                case "moderator":
                case "mod":
                    permission = StaffPermission.Moderation;
                    return true;
                case "economy":
                case "economic":
                case "eco":
                    permission = StaffPermission.Economy;
                    return true;
                case "events":
                case "event":
                    permission = StaffPermission.Events;
                    return true;
                case "content":
                case "world":
                    permission = StaffPermission.Content;
                    return true;
                case "operations":
                case "operation":
                case "ops":
                    permission = StaffPermission.Operations;
                    return true;
                case "security":
                case "secure":
                case "sec":
                    permission = StaffPermission.Security;
                    return true;
                case "all":
                case "*":
                    permission = StaffPermission.All;
                    return true;
                case "none":
                    permission = StaffPermission.None;
                    return true;
                default:
                    return false;
            }
        }

        public static string Format(StaffPermission permissions)
        {
            if (permissions == StaffPermission.None) return "None";
            if ((permissions & StaffPermission.All) == StaffPermission.All) return "All";

            return string.Join(", ", Categories
                .Where(category => (permissions & category) == category)
                .Select(category => category.ToString()));
        }

        private static string NormalizeHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return string.Empty;
            string normalized = header.Trim().ToLowerInvariant();
            return normalized.StartsWith("$", StringComparison.Ordinal)
                ? normalized
                : "$" + normalized;
        }

        private static HashSet<string> Set(params string[] values) =>
            new HashSet<string>(values.Select(NormalizeHeader), StringComparer.Ordinal);
    }
}
