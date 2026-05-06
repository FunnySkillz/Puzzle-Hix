using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    public sealed class LevelResult
    {
        public LevelResult(
            int levelNumber,
            string levelId,
            bool won,
            int score,
            int movesRemaining,
            int movesUsed,
            int cascadeCount,
            StarRating starRating,
            int previousStars,
            int xpAwarded)
        {
            LevelNumber = Mathf.Max(1, levelNumber);
            LevelId = string.IsNullOrWhiteSpace(levelId) ? $"match3_level_{LevelNumber:00}" : levelId;
            Won = won;
            Score = Mathf.Max(0, score);
            MovesRemaining = Mathf.Max(0, movesRemaining);
            MovesUsed = Mathf.Max(0, movesUsed);
            CascadeCount = Mathf.Max(0, cascadeCount);
            StarRating = starRating;
            PreviousStars = Mathf.Clamp(previousStars, 0, 3);
            XpAwarded = Mathf.Max(0, xpAwarded);
        }

        public int LevelNumber { get; }
        public string LevelId { get; }
        public bool Won { get; }
        public int Score { get; }
        public int MovesRemaining { get; }
        public int MovesUsed { get; }
        public int CascadeCount { get; }
        public StarRating StarRating { get; }
        public int Stars => (int)StarRating;
        public int PreviousStars { get; }
        public int StarsGained => Mathf.Max(0, Stars - PreviousStars);
        public int XpAwarded { get; }

        public static LevelResult Create(Match3LevelData level, bool won, int score, int movesRemaining, int movesUsed, int cascadeCount, int previousStars)
        {
            StarRating stars = CalculateStars(level, won, score, movesRemaining);
            int starsGained = Mathf.Max(0, (int)stars - Mathf.Clamp(previousStars, 0, 3));
            int xpAwarded = CalculateXpForStarGain(starsGained, movesRemaining, cascadeCount);
            int levelNumber = level != null ? level.LevelNumber : 1;
            string levelId = level != null ? level.LevelId : $"match3_level_{levelNumber:00}";

            return new LevelResult(levelNumber, levelId, won, score, movesRemaining, movesUsed, cascadeCount, stars, previousStars, xpAwarded);
        }

        public static StarRating CalculateStars(Match3LevelData level, bool won, int score, int movesRemaining)
        {
            if (!won || level == null)
            {
                return StarRating.None;
            }

            int stars = 1;
            int target = Mathf.Max(10, level.TargetScore);
            int moves = Mathf.Max(1, level.Moves);
            bool twoStarScore = score >= Mathf.CeilToInt(target * 1.25f);
            bool threeStarScore = score >= Mathf.CeilToInt(target * 1.50f);
            bool twoStarMoves = movesRemaining >= Mathf.CeilToInt(moves * 0.20f);
            bool threeStarMoves = movesRemaining >= Mathf.CeilToInt(moves * 0.35f);

            if (twoStarScore || twoStarMoves)
            {
                stars = 2;
            }

            if (threeStarScore || threeStarMoves)
            {
                stars = 3;
            }

            return (StarRating)stars;
        }

        public static int CalculateXpForStarGain(int starsGained, int movesRemaining, int cascadeCount)
        {
            if (starsGained <= 0)
            {
                return 0;
            }

            return (starsGained * 60) + (Mathf.Max(0, movesRemaining) * 2) + (Mathf.Max(0, cascadeCount) * 10);
        }
    }
}
