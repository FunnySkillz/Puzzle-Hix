using System;
using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    /// <summary>
    /// Configurable level settings for the first match-3 prototype.
    /// </summary>
    [CreateAssetMenu(fileName = "Match3BoardConfig", menuName = "PuzzleDungeon/Match 3/Board Config")]
    public class BoardConfig : ScriptableObject
    {
        public const string DefaultResourcePath = "Match3BoardConfig";

        [Header("Board")]
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private float cellSize = 72f;
        [SerializeField] private float cellSpacing = 6f;
        [SerializeField] private int availablePieceTypeCount = 6;

        [Header("Level")]
        [SerializeField] private int startingMoves = 25;
        [SerializeField] private int targetScore = 1000;

        [Header("Input and Timing")]
        [SerializeField] private float dragThreshold = 42f;
        [SerializeField] private float swapDuration = 0.12f;
        [SerializeField] private float invalidSwapDuration = 0.16f;
        [SerializeField] private float clearDuration = 0.10f;
        [SerializeField] private float fallDuration = 0.16f;
        [SerializeField] private float cascadeDelay = 0.08f;
        [SerializeField] private float hintDelay = 5f;
        [SerializeField] private float hintPulseDuration = 0.55f;
        [SerializeField] private float specialCreateDuration = 0.18f;
        [SerializeField] private float specialActivationPause = 0.08f;
        [SerializeField] private float floatingFeedbackDuration = 0.75f;
        [SerializeField] private float matchPopScale = 1.16f;

        [Header("Debug")]
        [SerializeField] private bool useDeterministicSeed;
        [SerializeField] private int deterministicSeed = 12037;

        [Header("Piece Colors")]
        [SerializeField] private Color redColor = new Color(0.90f, 0.22f, 0.20f, 1f);
        [SerializeField] private Color blueColor = new Color(0.22f, 0.50f, 0.95f, 1f);
        [SerializeField] private Color greenColor = new Color(0.28f, 0.72f, 0.38f, 1f);
        [SerializeField] private Color yellowColor = new Color(0.96f, 0.78f, 0.22f, 1f);
        [SerializeField] private Color purpleColor = new Color(0.62f, 0.34f, 0.88f, 1f);
        [SerializeField] private Color orangeColor = new Color(0.95f, 0.48f, 0.22f, 1f);

        public int Width => Mathf.Max(3, width);
        public int Height => Mathf.Max(3, height);
        public float CellSize => Mathf.Max(32f, cellSize);
        public float CellSpacing => Mathf.Max(0f, cellSpacing);
        public int StartingMoves => Mathf.Max(1, startingMoves);
        public int TargetScore => Mathf.Max(10, targetScore);
        public float DragThreshold => Mathf.Max(10f, dragThreshold);
        public float SwapDuration => Mathf.Max(0f, swapDuration);
        public float InvalidSwapDuration => Mathf.Max(0f, invalidSwapDuration);
        public float ClearDuration => Mathf.Max(0f, clearDuration);
        public float FallDuration => Mathf.Max(0f, fallDuration);
        public float CascadeDelay => Mathf.Max(0f, cascadeDelay);
        public float HintDelay => Mathf.Max(1f, hintDelay);
        public float HintPulseDuration => Mathf.Max(0.1f, hintPulseDuration);
        public float SpecialCreateDuration => Mathf.Max(0f, specialCreateDuration);
        public float SpecialActivationPause => Mathf.Max(0f, specialActivationPause);
        public float FloatingFeedbackDuration => Mathf.Max(0.1f, floatingFeedbackDuration);
        public float MatchPopScale => Mathf.Max(1f, matchPopScale);
        public bool UseDeterministicSeed => useDeterministicSeed;
        public int DeterministicSeed => deterministicSeed;

        public int GetSeedForLevel(int levelNumber)
        {
            return deterministicSeed + (Mathf.Max(1, levelNumber) * 9973);
        }

        public PieceType[] GetAvailablePieceTypes()
        {
            PieceType[] allTypes = (PieceType[])Enum.GetValues(typeof(PieceType));
            int count = Mathf.Clamp(availablePieceTypeCount, 3, allTypes.Length);
            PieceType[] result = new PieceType[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = allTypes[i];
            }

            return result;
        }

        public PieceType[] GetAvailablePieceTypes(int countOverride)
        {
            PieceType[] allTypes = (PieceType[])Enum.GetValues(typeof(PieceType));
            int count = Mathf.Clamp(countOverride, 3, allTypes.Length);
            PieceType[] result = new PieceType[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = allTypes[i];
            }

            return result;
        }

        public Color GetPieceColor(PieceType pieceType)
        {
            switch (pieceType)
            {
                case PieceType.Red:
                    return redColor;
                case PieceType.Blue:
                    return blueColor;
                case PieceType.Green:
                    return greenColor;
                case PieceType.Yellow:
                    return yellowColor;
                case PieceType.Purple:
                    return purpleColor;
                case PieceType.Orange:
                    return orangeColor;
                default:
                    return Color.white;
            }
        }
    }
}
