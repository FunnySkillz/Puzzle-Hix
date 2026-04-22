using NUnit.Framework;
using PuzzleDungeon.Core;

namespace PuzzleDungeon.Tests.EditMode
{
    /// <summary>
    /// Verifies board state operations independently from scene and UI runtime objects.
    /// </summary>
    public class BoardStateTests
    {
        [Test]
        public void Constructor_CreatesBoardWithConfiguredDimensions()
        {
            BoardState board = new BoardState(4, 3);

            Assert.That(board.Width, Is.EqualTo(4));
            Assert.That(board.Height, Is.EqualTo(3));
            Assert.That(board.IsCompleted, Is.False);
        }

        [Test]
        public void PlaceTile_ThenGetTile_ReturnsPlacedTile()
        {
            BoardState board = new BoardState(2, 2);
            Position position = new Position(1, 0);
            Tile tile = new Tile("tile_a");

            board.PlaceTile(position, tile);

            Assert.That(board.GetTile(position), Is.SameAs(tile));
            Assert.That(board.IsCellEmpty(position), Is.False);
        }

        [Test]
        public void IsInBounds_ReturnsExpectedValues()
        {
            BoardState board = new BoardState(3, 2);

            Assert.That(board.IsInBounds(new Position(0, 0)), Is.True);
            Assert.That(board.IsInBounds(new Position(2, 1)), Is.True);
            Assert.That(board.IsInBounds(new Position(-1, 0)), Is.False);
            Assert.That(board.IsInBounds(new Position(3, 1)), Is.False);
            Assert.That(board.IsInBounds(new Position(2, 2)), Is.False);
        }

        [Test]
        public void ClearCell_RemovesTileFromCell()
        {
            BoardState board = new BoardState(2, 2);
            Position position = new Position(0, 1);
            board.PlaceTile(position, new Tile("tile_b"));

            board.ClearCell(position);

            Assert.That(board.IsCellEmpty(position), Is.True);
            Assert.That(board.GetTile(position), Is.Null);
        }
    }
}
