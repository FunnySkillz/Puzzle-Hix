using System.Collections.Generic;
using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    public enum BoardActionType
    {
        Swap,
        InvalidSwap,
        Clear,
        SpecialActivate,
        Gravity,
        Spawn,
        Cascade,
        Reshuffle,
        InputUnlock
    }

    public sealed class BoardAction
    {
        public BoardAction(BoardActionType actionType, Vector2Int from, Vector2Int to, int affectedCount = 0, SpecialPieceType specialPieceType = SpecialPieceType.None)
        {
            ActionType = actionType;
            From = from;
            To = to;
            AffectedCount = Mathf.Max(0, affectedCount);
            SpecialPieceType = specialPieceType;
        }

        public BoardActionType ActionType { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }
        public int AffectedCount { get; }
        public SpecialPieceType SpecialPieceType { get; }

        public static BoardAction Simple(BoardActionType actionType)
        {
            return new BoardAction(actionType, Vector2Int.zero, Vector2Int.zero);
        }
    }

    public sealed class BoardActionQueue
    {
        private readonly Queue<BoardAction> actions = new Queue<BoardAction>();

        public BoardAction CurrentAction { get; private set; }
        public int PendingCount => actions.Count;
        public bool HasCurrentAction => CurrentAction != null;
        public bool IsBusy => HasCurrentAction || PendingCount > 0;

        public void Enqueue(BoardAction action)
        {
            if (action != null)
            {
                actions.Enqueue(action);
            }
        }

        public bool TryStartNext(out BoardAction action)
        {
            if (CurrentAction != null || actions.Count == 0)
            {
                action = CurrentAction;
                return false;
            }

            CurrentAction = actions.Dequeue();
            action = CurrentAction;
            return true;
        }

        public void CompleteCurrent()
        {
            CurrentAction = null;
        }

        public void Clear()
        {
            actions.Clear();
            CurrentAction = null;
        }
    }
}
