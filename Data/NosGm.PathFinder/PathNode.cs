using System;

namespace NosGm.PathFinder
{
    public struct PathNode : IEquatable<PathNode>, IComparable<PathNode>
    {
        public short X;
        public short Y;
        public double G;
        public double H;
        public double F;
        public int ParentIndex;
        public bool Closed;
        public bool Opened;

        public PathNode(short x, short y)
        {
            X = x;
            Y = y;
            G = 0;
            H = 0;
            F = 0;
            ParentIndex = -1;
            Closed = false;
            Opened = false;
        }

        public bool Equals(PathNode other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is PathNode other && Equals(other);
        public override int GetHashCode() => (X << 16) ^ Y;
        public int CompareTo(PathNode other) => F.CompareTo(other.F);
    }
}
