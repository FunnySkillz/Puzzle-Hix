using System;

namespace PuzzleDungeon.Core
{
    /// <summary>
    /// Represents a logical tile entity placed inside a board cell.
    /// </summary>
    public class Tile
    {
        /// <summary>
        /// Creates a tile with a stable identifier used by game logic and tests.
        /// </summary>
        public Tile(string id)
        {
            // A stable identifier makes tiles easy to assert in tests and logs.
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Tile id must be a non-empty value.", nameof(id));
            }

            Id = id;
        }

        /// <summary>
        /// Gets the tile identifier.
        /// </summary>
        public string Id { get; }
    }
}
