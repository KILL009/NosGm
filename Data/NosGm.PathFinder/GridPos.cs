using System;

namespace NosGm.PathFinder
{
    public class GridPos : IEquatable<GridPos>
    {
        #region Methods

        public bool IsWalkable() => Value == 0 || Value == 2 || Value >= 16 && Value <= 19;

        public double DistanceTo(GridPos other)
        {
            if (other == null) return 0;
            int dx = Math.Abs(X - other.X);
            int dy = Math.Abs(Y - other.Y);
            return Math.Max(dx, dy) + (1.41421356 - 1) * Math.Min(dx, dy);
        }

        public bool Equals(GridPos other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj) => Equals(obj as GridPos);

        public override int GetHashCode() => (X << 16) ^ Y;

        #endregion

        #region Properties

        public byte Value { get; set; }

        public short X { get; set; }

        public short Y { get; set; }

        #endregion
    }
}