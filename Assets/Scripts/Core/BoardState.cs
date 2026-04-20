using System;

namespace PuzzleDungeon.Core
{
    /// <summary>
    /// Stores mutable runtime board data and tile placement state independently from UI and scene objects.
    /// </summary>
    public class BoardState
    {
        private readonly Cell[,] cells;

        /// <summary>
        /// Creates an empty board with a configurable width and height.
        /// </summary>
        public BoardState(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Board width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Board height must be greater than zero.");
            }

            Width = width;
            Height = height;
            IsCompleted = false;

            cells = new Cell[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[x, y] = new Cell(new Position(x, y));
                }
            }
        }

        public int Width { get; }
        public int Height { get; }
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Returns true when the position is inside the board dimensions.
        /// </summary>
        public bool IsInBounds(Position position)
        {
            return position.X >= 0 &&
                   position.X < Width &&
                   position.Y >= 0 &&
                   position.Y < Height;
        }

        /// <summary>
        /// Returns true when the requested cell does not currently contain a tile.
        /// </summary>
        public bool IsCellEmpty(Position position)
        {
            EnsureInBounds(position);
            return cells[position.X, position.Y].IsEmpty;
        }

        /// <summary>
        /// Returns the tile at a position, or null when the cell is empty.
        /// </summary>
        public Tile GetTile(Position position)
        {
            EnsureInBounds(position);
            return cells[position.X, position.Y].Tile;
        }

        /// <summary>
        /// Places a tile into an empty in-bounds cell.
        /// </summary>
        public void PlaceTile(Position position, Tile tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            EnsureInBounds(position);

            Cell targetCell = cells[position.X, position.Y];

            if (!targetCell.IsEmpty)
            {
                throw new InvalidOperationException($"Cannot place tile at {position} because the cell is already occupied.");
            }

            targetCell.SetTile(tile);
        }

        /// <summary>
        /// Moves a tile from one cell to another when the source has a tile and the target is empty.
        /// </summary>
        public bool MoveTile(Position from, Position to)
        {
            if (!IsInBounds(from) || !IsInBounds(to))
            {
                return false;
            }

            Cell fromCell = cells[from.X, from.Y];
            Cell toCell = cells[to.X, to.Y];

            if (fromCell.IsEmpty || !toCell.IsEmpty)
            {
                return false;
            }

            // Keep movement atomic by holding the tile reference before clearing the source.
            Tile tileToMove = fromCell.RemoveTile();
            toCell.SetTile(tileToMove);
            return true;
        }

        /// <summary>
        /// Marks the board as completed for high-level lifecycle checks.
        /// </summary>
        public void MarkCompleted()
        {
            IsCompleted = true;
        }

        /// <summary>
        /// Clears all tiles and resets completion state.
        /// </summary>
        public void ResetProgress()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    cells[x, y].RemoveTile();
                }
            }

            IsCompleted = false;
        }

        private void EnsureInBounds(Position position)
        {
            if (!IsInBounds(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is outside the board bounds {Width}x{Height}.");
            }
        }
    }
}
