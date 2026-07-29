using System;

namespace NosGm.Cluster.Contracts.V1
{
    public readonly struct ClusterContractVersion : IEquatable<ClusterContractVersion>
    {
        public const ushort CurrentMajor = 1;
        public const ushort CurrentMinor = 0;

        public ClusterContractVersion(ushort major, ushort minor)
        {
            Major = major;
            Minor = minor;
        }

        public static ClusterContractVersion Current =>
            new ClusterContractVersion(CurrentMajor, CurrentMinor);

        public ushort Major { get; }

        public ushort Minor { get; }

        public bool IsSupported =>
            Major == CurrentMajor && Minor <= CurrentMinor;

        public bool Equals(ClusterContractVersion other) =>
            Major == other.Major && Minor == other.Minor;

        public override bool Equals(object obj) =>
            obj is ClusterContractVersion other && Equals(other);

        public override int GetHashCode() =>
            (Major * 397) ^ Minor;

        public override string ToString() =>
            $"{Major}.{Minor}";

        public static bool operator ==(
            ClusterContractVersion left,
            ClusterContractVersion right) =>
            left.Equals(right);

        public static bool operator !=(
            ClusterContractVersion left,
            ClusterContractVersion right) =>
            !left.Equals(right);
    }
}
