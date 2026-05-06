using System.Collections.Generic;
using NUnit.Framework;
using PuzzleDungeon.Gameplay.Match3;
using UnityEngine;

namespace PuzzleDungeon.Tests.EditMode.Match3
{
    public class Match3BoardModelTests
    {
        [Test]
        public void GenerateBoardTypes_WithSameSeed_CreatesSamePlayableBoard()
        {
            PieceType[,] first = Match3BoardModel.GenerateBoardTypes(8, 8, AllTypes(), 12345);
            PieceType[,] second = Match3BoardModel.GenerateBoardTypes(8, 8, AllTypes(), 12345);

            AssertBoardsEqual(first, second);
            Assert.That(Match3BoardModel.HasAnyMatches(Match3BoardModel.ToNullableBoard(first)), Is.False);
            Assert.That(Match3BoardModel.TryFindPossibleMove(Match3BoardModel.ToNullableBoard(first), out _, out _), Is.True);
        }

        [Test]
        public void TrySwap_AcceptsOnlyAdjacentMatchCreatingSwaps()
        {
            PieceType?[,] board = CreateSwapBoard();
            Assert.That(Match3BoardModel.HasAnyMatches(board), Is.False);

            Match3BoardModel model = CreateModelFromBoard(board);
            Assert.That(model.TrySwap(new Vector2Int(1, 0), new Vector2Int(1, 1), out List<MatchResult> matches), Is.True);
            Assert.That(matches.Count, Is.GreaterThan(0));

            model = CreateModelFromBoard(CreateSwapBoard());
            Assert.That(model.TrySwap(new Vector2Int(0, 0), new Vector2Int(1, 1), out _), Is.False);
            Assert.That(model.TrySwap(new Vector2Int(0, 1), new Vector2Int(0, 2), out _), Is.False);
        }

        [Test]
        public void ResolveCascades_LeavesBoardFilledAndStable()
        {
            Match3BoardModel model = CreateModelFromBoard(CreateCascadeBoard());

            BoardResolveResult result = model.ResolveCascades();

            Assert.That(result.Score, Is.GreaterThanOrEqualTo(30));
            Assert.That(result.Cascades, Is.GreaterThanOrEqualTo(1));
            Assert.That(model.HasNoNullCells(), Is.True);
            Assert.That(Match3BoardModel.HasAnyMatches(model.CopyBoard()), Is.False);
        }

        [Test]
        public void SimulateValidMoves_CanRunOneThousandMovesWithoutBreakingBoard()
        {
            Match3BoardModel model = new Match3BoardModel(8, 8, AllTypes(), 777);

            int completedMoves = model.SimulateValidMoves(1000);

            Assert.That(completedMoves, Is.EqualTo(1000));
            Assert.That(model.HasNoNullCells(), Is.True);
            Assert.That(Match3BoardModel.HasAnyMatches(model.CopyBoard()), Is.False);
            Assert.That(model.TryFindPossibleMove(out _, out _), Is.True);
        }

        [Test]
        public void BoardActionQueue_ProcessesActionsInOrder()
        {
            BoardActionQueue queue = new BoardActionQueue();
            BoardAction swap = new BoardAction(BoardActionType.Swap, Vector2Int.zero, Vector2Int.right);
            BoardAction clear = new BoardAction(BoardActionType.Clear, Vector2Int.zero, Vector2Int.zero, 3);

            queue.Enqueue(swap);
            queue.Enqueue(clear);

            Assert.That(queue.IsBusy, Is.True);
            Assert.That(queue.TryStartNext(out BoardAction current), Is.True);
            Assert.That(current, Is.SameAs(swap));
            Assert.That(queue.TryStartNext(out _), Is.False);

            queue.CompleteCurrent();
            Assert.That(queue.TryStartNext(out current), Is.True);
            Assert.That(current, Is.SameAs(clear));

            queue.Clear();
            Assert.That(queue.IsBusy, Is.False);
        }

        private static Match3BoardModel CreateModelFromBoard(PieceType?[,] board)
        {
            return Match3BoardModel.CreateForTesting(board, AllTypes(), 19);
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

        private static PieceType?[,] CreateCascadeBoard()
        {
            PieceType?[,] board = new PieceType?[3, 3];
            board[0, 0] = PieceType.Red;
            board[1, 0] = PieceType.Red;
            board[2, 0] = PieceType.Red;
            board[0, 1] = PieceType.Blue;
            board[1, 1] = PieceType.Green;
            board[2, 1] = PieceType.Yellow;
            board[0, 2] = PieceType.Green;
            board[1, 2] = PieceType.Yellow;
            board[2, 2] = PieceType.Blue;
            return board;
        }

        private static void AssertBoardsEqual(PieceType[,] first, PieceType[,] second)
        {
            Assert.That(first.GetLength(0), Is.EqualTo(second.GetLength(0)));
            Assert.That(first.GetLength(1), Is.EqualTo(second.GetLength(1)));

            for (int y = 0; y < first.GetLength(1); y++)
            {
                for (int x = 0; x < first.GetLength(0); x++)
                {
                    Assert.That(first[x, y], Is.EqualTo(second[x, y]), $"Mismatch at {x},{y}.");
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
