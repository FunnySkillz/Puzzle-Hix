using System;

namespace PuzzleDungeon.Core
{
    /// <summary>
    /// Represents one board cell and stores the tile currently occupying that position.
    /// </summary>
    public class Cell
    {
        /// <summary>
        /// Creates a board cell for a specific coordinate.
        /// </summary>
        public Cell(Position position)
        {
            Position = position;
        }

        /// <summary>
        /// Gets the coordinate represented by this cell.
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Gets the tile currently in this cell, or null when empty.
        /// </summary>
        public Tile Tile { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the cell is empty.
        /// </summary>
        public bool IsEmpty => Tile == null;

        /// <summary>
        /// Places a tile into this cell.
        /// </summary>
        public void SetTile(Tile tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            Tile = tile;
        }

        /// <summary>
        /// Removes and returns the tile currently in this cell.
        /// </summary>
        public Tile RemoveTile()
        {
            // Returning the removed tile makes move operations explicit and easy to verify.
            Tile removedTile = Tile;
            Tile = null;
            return removedTile;
        }
    }
}
