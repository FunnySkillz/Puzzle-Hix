using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleDungeon.Gameplay.Match3;
using UnityEngine;

namespace PuzzleDungeon.Tests.EditMode.Match3
{
    public class BoardManagerTests
    {
        [Test]
        public void GenerateBoardTypes_CreatesFullBoardWithoutImmediateMatchesAndWithMove()
        {
            PieceType[,] board = BoardManager.GenerateBoardTypes(8, 8, AllTypes(), 44);
            PieceType?[,] nullableBoard = BoardManager.ToNullableBoard(board);

            AssertEveryCellFilled(nullableBoard);
            Assert.That(BoardManager.HasAnyMatches(nullableBoard), Is.False);
            Assert.That(BoardManager.TryFindPossibleMove(nullableBoard, out _, out _), Is.True);
        }

        [Test]
        public void FindMatches_DetectsHorizontalVerticalMatchFourAndMatchFive()
        {
            PieceType?[,] board = new PieceType?[6, 6];
            SetRow(board, 0, 0, 3, PieceType.Red);
            SetColumn(board, 4, 0, 4, PieceType.Blue);
            SetRow(board, 5, 0, 5, PieceType.Green);

            List<MatchResult> matches = BoardManager.FindMatches(board);

            Assert.That(matches.Any(match => match.IsHorizontal && match.MatchSize == 3 && match.MatchedPositions.Contains(new Vector2Int(0, 0))), Is.True);
            Assert.That(matches.Any(match => match.IsVertical && match.MatchSize == 4 && match.MatchedPositions.Contains(new Vector2Int(4, 0))), Is.True);
            Assert.That(matches.Any(match => match.IsHorizontal && match.MatchSize == 5 && match.MatchedPositions.Contains(new Vector2Int(0, 5))), Is.True);
        }

        [Test]
        public void FindMatches_DetectsTShapeWithoutDuplicateRemovalPositions()
        {
            PieceType?[,] board = new PieceType?[5, 5];
            SetRow(board, 2, 1, 3, PieceType.Red);
            SetColumn(board, 2, 1, 3, PieceType.Red);

            List<MatchResult> matches = BoardManager.FindMatches(board);
            HashSet<Vector2Int> matchedPositions = BoardManager.CollectMatchedPositions(matches);

            Assert.That(matches.Any(match => match.IsTShape), Is.True);
            Assert.That(matchedPositions.Count, Is.EqualTo(5));
        }

        [Test]
        public void FindMatches_DetectsLShapeWithoutDuplicateRemovalPositions()
        {
            PieceType?[,] board = new PieceType?[5, 5];
            SetRow(board, 1, 1, 3, PieceType.Blue);
            SetColumn(board, 1, 1, 3, PieceType.Blue);

            List<MatchResult> matches = BoardManager.FindMatches(board);
            HashSet<Vector2Int> matchedPositions = BoardManager.CollectMatchedPositions(matches);

            Assert.That(matches.Any(match => match.IsLShape), Is.True);
            Assert.That(matchedPositions.Count, Is.EqualTo(5));
        }

        [Test]
        public void SwapCreatesMatch_OnlyAllowsAdjacentValidSwaps()
        {
            PieceType?[,] board = CreateSwapBoard();

            Assert.That(BoardManager.SwapCreatesMatch(board, new Vector2Int(1, 0), new Vector2Int(1, 1)), Is.True);
            Assert.That(BoardManager.SwapCreatesMatch(board, new Vector2Int(0, 0), new Vector2Int(1, 1)), Is.False);
            Assert.That(BoardManager.SwapCreatesMatch(board, new Vector2Int(0, 0), new Vector2Int(2, 0)), Is.False);
            Assert.That(BoardManager.SwapCreatesMatch(board, new Vector2Int(0, 1), new Vector2Int(0, 2)), Is.False);
        }

        [Test]
        public void CollapseAndFillTypes_LeavesEveryCellFilled()
        {
            PieceType?[,] board = new PieceType?[4, 4];
            board[0, 0] = PieceType.Red;
            board[0, 3] = PieceType.Blue;
            board[1, 2] = PieceType.Green;
            board[2, 0] = PieceType.Yellow;
            board[3, 1] = PieceType.Purple;

            BoardManager.CollapseAndFillTypes(board, AllTypes(), 8);

            AssertEveryCellFilled(board);
        }

        [Test]
        public void ResolveBoardTypesForTesting_ClearsCascadesUntilStable()
        {
            PieceType?[,] board = new PieceType?[3, 3];
            SetRow(board, 0, 0, 3, PieceType.Red);
            board[0, 1] = PieceType.Blue;
            board[1, 1] = PieceType.Green;
            board[2, 1] = PieceType.Yellow;
            board[0, 2] = PieceType.Green;
            board[1, 2] = PieceType.Yellow;
            board[2, 2] = PieceType.Blue;

            int score = BoardManager.ResolveBoardTypesForTesting(board, AllTypes(), 17, out int cascades);

            Assert.That(score, Is.GreaterThanOrEqualTo(30));
            Assert.That(cascades, Is.GreaterThanOrEqualTo(1));
            AssertEveryCellFilled(board);
            Assert.That(BoardManager.HasAnyMatches(board), Is.False);
        }

        private static PieceType?[,] CreateSwapBoard()
        {
            PieceType?[,] board = new PieceType?[3, 3];
            board[0, 0] = PieceType.Red;
            board[1, 0] = PieceType.Blue;
            board[2, 0] = PieceType.Red;
            board[0, 1] = PieceType.Green;
            board[1, 1] = PieceType.Red;
            board[2, 1] = PieceType.Blue;
            board[0, 2] = PieceType.Blue;
            board[1, 2] = PieceType.Green;
            board[2, 2] = PieceType.Yellow;
            return board;
        }

        private static void SetRow(PieceType?[,] board, int y, int startX, int length, PieceType type)
        {
            for (int x = startX; x < startX + length; x++)
            {
                board[x, y] = type;
            }
        }

        private static void SetColumn(PieceType?[,] board, int x, int startY, int length, PieceType type)
        {
            for (int y = startY; y < startY + length; y++)
            {
                board[x, y] = type;
            }
        }

        private static void AssertEveryCellFilled(PieceType?[,] board)
        {
            for (int y = 0; y < board.GetLength(1); y++)
            {
                for (int x = 0; x < board.GetLength(0); x++)
                {
                    Assert.That(board[x, y].HasValue, Is.True, $"Expected {x},{y} to contain a piece.");
                }
            }
        }

        private static PieceType[] AllTypes()
        {
            return new[]
            {
                PieceType.Red,
                PieceType.Blue,
                PieceType.Green,
                PieceType.Yellow,
                PieceType.Purple,
                PieceType.Orange
            };
        }
    }
}
