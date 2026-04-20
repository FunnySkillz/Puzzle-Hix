using NUnit.Framework;
using PuzzleDungeon.Core;

namespace PuzzleDungeon.Tests.EditMode
{
    /// <summary>
    /// Verifies puzzle rule behavior independently from scenes and Unity runtime components.
    /// </summary>
    public class GameRulesTests
    {
        [Test]
        public void TryMove_ReturnsSuccess_ForValidMove()
        {
            BoardState board = new BoardState(2, 1);
            board.PlaceTile(new Position(0, 0), new Tile("runner"));

            GameRules rules = new GameRules(moveLimit: 3, goalPosition: new Position(1, 0), goalTileId: "goal");

            MoveResult result = rules.TryMove(board, new Position(0, 0), new Position(1, 0));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsWin, Is.False);
            Assert.That(result.IsFail, Is.False);
            Assert.That(board.IsCellEmpty(new Position(0, 0)), Is.True);
            Assert.That(board.GetTile(new Position(1, 0)).Id, Is.EqualTo("runner"));
        }

        [Test]
        public void TryMove_ReturnsFailure_ForInvalidMoveToOccupiedCell()
        {
            BoardState board = new BoardState(2, 1);
            board.PlaceTile(new Position(0, 0), new Tile("a"));
            board.PlaceTile(new Position(1, 0), new Tile("b"));

            GameRules rules = new GameRules(moveLimit: 3, goalPosition: new Position(1, 0), goalTileId: "goal");

            MoveResult result = rules.TryMove(board, new Position(0, 0), new Position(1, 0));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Message, Does.Contain("occupied"));
            Assert.That(board.GetTile(new Position(0, 0)).Id, Is.EqualTo("a"));
            Assert.That(board.GetTile(new Position(1, 0)).Id, Is.EqualTo("b"));
        }

        [Test]
        public void TryMove_ReturnsFailure_ForOutOfBoundsMove()
        {
            BoardState board = new BoardState(2, 1);
            board.PlaceTile(new Position(0, 0), new Tile("a"));

            GameRules rules = new GameRules(moveLimit: 3, goalPosition: new Position(1, 0), goalTileId: "goal");

            MoveResult result = rules.TryMove(board, new Position(-1, 0), new Position(0, 0));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Message, Does.Contain("out of bounds"));
        }

        [Test]
        public void HasWon_ReturnsTrue_WhenGoalTileReachesGoalPosition()
        {
            BoardState board = new BoardState(2, 1);
            board.PlaceTile(new Position(0, 0), new Tile("goal"));

            GameRules rules = new GameRules(moveLimit: 5, goalPosition: new Position(1, 0), goalTileId: "goal");

            MoveResult result = rules.TryMove(board, new Position(0, 0), new Position(1, 0));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsWin, Is.True);
            Assert.That(rules.HasWon(board), Is.True);
            Assert.That(board.IsCompleted, Is.True);
        }

        [Test]
        public void HasLost_ReturnsTrue_WhenMoveLimitIsReachedWithoutWin()
        {
            BoardState board = new BoardState(2, 1);
            board.PlaceTile(new Position(0, 0), new Tile("runner"));

            GameRules rules = new GameRules(moveLimit: 1, goalPosition: new Position(1, 0), goalTileId: "goal");

            MoveResult result = rules.TryMove(board, new Position(0, 0), new Position(1, 0));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsWin, Is.False);
            Assert.That(result.IsFail, Is.True);
            Assert.That(rules.HasLost(board), Is.True);
        }
    }
}
