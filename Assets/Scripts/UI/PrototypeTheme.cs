using PuzzleDungeon.Core;
using UnityEngine;

namespace PuzzleDungeon.UI
{
    /// <summary>
    /// Small prototype skin that keeps art optional while letting scenes fall back to generated UI.
    /// </summary>
    [CreateAssetMenu(fileName = "PrototypeTheme", menuName = "PuzzleDungeon/Prototype Theme")]
    public class PrototypeTheme : ScriptableObject
    {
        public const string DefaultResourcePath = "PrototypeTheme";

        [Header("Board Sprites")]
        [SerializeField] private Sprite emptyCellSprite;
        [SerializeField] private Sprite tileSprite;
        [SerializeField] private Sprite goalCellSprite;
        [SerializeField] private Sprite selectedCellSprite;
        [SerializeField] private Sprite invalidCellSprite;
        [SerializeField] private Sprite goalIconSprite;

        [Header("UI Sprites")]
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite playIconSprite;
        [SerializeField] private Sprite retryIconSprite;
        [SerializeField] private Sprite nextIconSprite;
        [SerializeField] private Sprite menuIconSprite;
        [SerializeField] private Sprite awardIconSprite;

        [Header("Colors")]
        [SerializeField] private Color canvasBackgroundColor = new Color(0.09f, 0.10f, 0.12f, 1f);
        [SerializeField] private Color emptyCellColor = new Color(0.18f, 0.20f, 0.24f, 1f);
        [SerializeField] private Color occupiedCellColor = new Color(0.28f, 0.55f, 0.78f, 1f);
        [SerializeField] private Color selectedCellColor = new Color(0.95f, 0.76f, 0.25f, 1f);
        [SerializeField] private Color goalCellColor = new Color(0.30f, 0.70f, 0.40f, 1f);
        [SerializeField] private Color invalidCellColor = new Color(0.90f, 0.18f, 0.16f, 1f);
        [SerializeField] private Color panelColor = new Color(0.08f, 0.09f, 0.11f, 0.92f);
        [SerializeField] private Color buttonColor = new Color(0.20f, 0.40f, 0.62f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color tileLabelColor = Color.white;

        private static readonly Color[] TilePalette =
        {
            new Color(0.32f, 0.62f, 0.90f, 1f),
            new Color(0.82f, 0.35f, 0.74f, 1f),
            new Color(0.90f, 0.48f, 0.28f, 1f),
            new Color(0.35f, 0.72f, 0.44f, 1f)
        };

        public Sprite EmptyCellSprite => emptyCellSprite;
        public Sprite TileSprite => tileSprite;
        public Sprite GoalCellSprite => goalCellSprite;
        public Sprite SelectedCellSprite => selectedCellSprite;
        public Sprite InvalidCellSprite => invalidCellSprite;
        public Sprite GoalIconSprite => goalIconSprite;
        public Sprite PanelSprite => panelSprite;
        public Sprite ButtonSprite => buttonSprite;
        public Sprite PlayIconSprite => playIconSprite;
        public Sprite RetryIconSprite => retryIconSprite;
        public Sprite NextIconSprite => nextIconSprite;
        public Sprite MenuIconSprite => menuIconSprite;
        public Sprite AwardIconSprite => awardIconSprite;

        public Color CanvasBackgroundColor => canvasBackgroundColor;
        public Color EmptyCellColor => emptyCellColor;
        public Color OccupiedCellColor => occupiedCellColor;
        public Color SelectedCellColor => selectedCellColor;
        public Color GoalCellColor => goalCellColor;
        public Color InvalidCellColor => invalidCellColor;
        public Color PanelColor => panelColor;
        public Color ButtonColor => buttonColor;
        public Color TextColor => textColor;
        public Color TileLabelColor => tileLabelColor;

        public static PrototypeTheme LoadDefault()
        {
            return Resources.Load<PrototypeTheme>(DefaultResourcePath);
        }

        public Color GetTileColor(Tile tile)
        {
            if (tile == null)
            {
                return occupiedCellColor;
            }

            if (tile.Id == "goal")
            {
                return goalCellColor;
            }

            int hash = 0;

            for (int i = 0; i < tile.Id.Length; i++)
            {
                hash += tile.Id[i];
            }

            return TilePalette[Mathf.Abs(hash) % TilePalette.Length];
        }
    }
}
