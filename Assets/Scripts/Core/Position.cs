using System;

namespace PuzzleDungeon.Core
{
    /// <summary>
    /// Represents an immutable zero-based coordinate on the puzzle board.
    /// </summary>
    public readonly struct Position : IEquatable<Position>
    {
        /// <summary>
        /// Initializes a board coordinate using zero-based x and y values.
        /// </summary>
        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets the horizontal coordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the vertical coordinate.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Compares two positions for value equality.
        /// </summary>
        public bool Equals(Position other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Position other && Equals(other);
        }

        public override int GetHashCode()
        {
            // Value-based hash keeps dictionary/set usage consistent with coordinate equality.
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Position left, Position right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
