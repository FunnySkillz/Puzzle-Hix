using System.Collections.Generic;
using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    /// <summary>
    /// Describes a detected line or intersecting shape match.
    /// </summary>
    public sealed class MatchResult
    {
        public MatchResult(IEnumerable<Vector2Int> positions, bool isHorizontal, bool isVertical, bool isTShape, bool isLShape)
        {
            MatchedPositions = new List<Vector2Int>(positions);
            IsHorizontal = isHorizontal;
            IsVertical = isVertical;
            IsTShape = isTShape;
            IsLShape = isLShape;
        }

        public List<Vector2Int> MatchedPositions { get; }
        public int MatchSize => MatchedPositions.Count;
        public bool IsHorizontal { get; }
        public bool IsVertical { get; }
        public bool IsTShape { get; }
        public bool IsLShape { get; }
    }
}
