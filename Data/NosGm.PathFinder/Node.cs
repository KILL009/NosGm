using System;

namespace NosGm.PathFinder
{
    public class Node : GridPos, IComparable<Node>, IEquatable<Node>
    {
        #region Instantiation

        public Node(GridPos node)
        {
            Value = node.Value;
            X = node.X;
            Y = node.Y;
        }

        public Node()
        {
        }

        #endregion

        #region Properties

        public bool Closed { get; internal set; }

        public double F { get; internal set; }

        public double N { get; internal set; }

        public bool Opened { get; internal set; }

        public Node Parent { get; internal set; }

        #endregion

        #region Methods

        public static bool operator !=(Node left, Node right)
        {
            return !(left == right);
        }

        public static bool operator <(Node left, Node right)
        {
            return left is null ? !(right is null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Node left, Node right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator ==(Node left, Node right)
        {
            if (left is null) return right is null;

            return left.Equals(right);
        }

        public static bool operator >(Node left, Node right)
        {
            return !(left is null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Node left, Node right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        public int CompareTo(Node other)
        {
            return F > other.F ? 1 : F < other.F ? -1 : 0;
        }

        public bool Equals(Node other)
        {
            return ReferenceEquals(this, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            if (obj is null) return false;

            return false;
        }

        public override int GetHashCode()
        {
            return GetHashCode();
        }

        #endregion
    }
}