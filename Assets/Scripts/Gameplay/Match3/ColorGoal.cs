using System;
using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    [Serializable]
    public struct ColorGoal
    {
        [SerializeField] private PieceType pieceType;
        [SerializeField] private int targetCount;

        public ColorGoal(PieceType type, int count)
        {
            pieceType = type;
            targetCount = count;
        }

        public PieceType PieceType => pieceType;
        public int TargetCount => Mathf.Max(0, targetCount);
    }
}
