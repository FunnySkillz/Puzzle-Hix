using System;
using PuzzleDungeon.Core;

namespace PuzzleDungeon.Gameplay
{
    /// <summary>
    /// Builds a runtime board state from LevelData so game logic can run independently from scene objects.
    /// </summary>
    public class LevelLoader
    {
        /// <summary>
        /// Converts a LevelData asset into a fully initialized BoardState.
        /// </summary>
        public BoardState Load(LevelData levelData)
        {
            if (levelData == null)
            {
                throw new ArgumentNullException(nameof(levelData));
            }

            BoardState boardState = new BoardState(levelData.BoardWidth, levelData.BoardHeight);

            // Apply author-defined tile placements from the level asset.
            foreach (LevelData.TilePlacement placement in levelData.InitialTileLayout)
            {
                if (string.IsNullOrWhiteSpace(placement.TileId))
                {
                    throw new InvalidOperationException("LevelData contains a tile placement with an empty tile id.");
                }

                Position position = new Position(placement.X, placement.Y);

                if (!boardState.IsInBounds(position))
                {
                    throw new InvalidOperationException(
                        $"Tile '{placement.TileId}' is out of bounds at position {position} for board {boardState.Width}x{boardState.Height}.");
                }

                if (!boardState.IsCellEmpty(position))
                {
                    throw new InvalidOperationException($"Multiple tiles are assigned to position {position}.");
                }

                boardState.PlaceTile(position, new Tile(placement.TileId));
            }

            return boardState;
        }
    }
}
