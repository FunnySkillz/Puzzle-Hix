using System.Collections;
using System.Collections.Generic;
using PuzzleDungeon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleDungeon.Gameplay.Match3
{
    /// <summary>
    /// Owns board generation, input, swapping, match detection, gravity, spawning, cascades, and dead-board handling.
    /// </summary>
    [DisallowMultipleComponent]
    public class BoardManager : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private BoardConfig config;
        [SerializeField] private PrototypeTheme theme;

        private Piece[,] pieces;
        private Piece selectedPiece;
        private Transform boardRoot;
        private GameManager gameManager;
        private UIManager uiManager;
        private int currentWidth;
        private int currentHeight;
        private bool inputBlocked;

        public bool IsInputBlocked => inputBlocked;
        public int CurrentScore => gameManager != null ? gameManager.Score : 0;
        public int MovesRemaining => gameManager != null ? gameManager.MovesRemaining : 0;
        public int TargetScore => gameManager != null ? gameManager.TargetScore : 0;
        public bool IsGameOver => gameManager != null && gameManager.IsGameOver;
        public bool HasWon => gameManager != null && gameManager.HasWon;

        private BoardConfig ActiveConfig
        {
            get
            {
                if (config == null)
                {
                    config = Resources.Load<BoardConfig>(BoardConfig.DefaultResourcePath);
                }

                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<BoardConfig>();
                }

                return config;
            }
        }

        private PrototypeTheme ActiveTheme
        {
            get
            {
                if (theme == null)
                {
                    theme = PrototypeTheme.LoadDefault();
                }

                return theme;
            }
        }

        private void Awake()
        {
            EnsureManagers();
            StartNewGame();
        }

        public void StartNewGame()
        {
            StopAllCoroutines();
            EnsureManagers();

            BoardConfig activeConfig = ActiveConfig;
            PieceType[,] boardTypes = GenerateBoardTypes(activeConfig.Width, activeConfig.Height, activeConfig.GetAvailablePieceTypes());
            BuildBoardFromTypes(boardTypes);

            selectedPiece = null;
            inputBlocked = false;
            gameManager.Initialize(activeConfig, uiManager);
            uiManager.SetStatus("Swap adjacent pieces to match 3 or more.");
        }

        public void ReturnToMenu()
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }

        public void HandlePieceClicked(Piece piece)
        {
            if (piece == null || inputBlocked || IsGameOver)
            {
                return;
            }

            if (selectedPiece == null)
            {
                SelectPiece(piece);
                return;
            }

            if (selectedPiece == piece)
            {
                SelectPiece(null);
                uiManager.SetStatus("Selection cleared.");
                return;
            }

            if (!AreAdjacent(new Vector2Int(selectedPiece.GridX, selectedPiece.GridY), new Vector2Int(piece.GridX, piece.GridY)))
            {
                SelectPiece(piece);
                uiManager.SetStatus("Choose an adjacent neighbor.");
                return;
            }

            StartCoroutine(AttemptSwapRoutine(selectedPiece, piece));
        }

        public void HandlePieceDrag(Piece piece, Vector2 dragDelta)
        {
            if (piece == null || inputBlocked || IsGameOver || dragDelta.magnitude < ActiveConfig.DragThreshold)
            {
                return;
            }

            Vector2Int direction;

            if (Mathf.Abs(dragDelta.x) >= Mathf.Abs(dragDelta.y))
            {
                direction = dragDelta.x >= 0f ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                direction = dragDelta.y >= 0f ? Vector2Int.up : Vector2Int.down;
            }

            int targetX = piece.GridX + direction.x;
            int targetY = piece.GridY + direction.y;

            if (!IsInBounds(targetX, targetY, currentWidth, currentHeight))
            {
                return;
            }

            Piece targetPiece = pieces[targetX, targetY];

            if (targetPiece != null)
            {
                StartCoroutine(AttemptSwapRoutine(piece, targetPiece));
            }
        }

        public Piece GetPieceAt(int x, int y)
        {
            if (pieces == null || !IsInBounds(x, y, currentWidth, currentHeight))
            {
                return null;
            }

            return pieces[x, y];
        }

        public bool TryFindFirstValidSwap(out Vector2Int from, out Vector2Int to)
        {
            return TryFindPossibleMove(ToTypeBoard(), out from, out to);
        }

        public bool TryFindFirstInvalidAdjacentSwap(out Vector2Int from, out Vector2Int to)
        {
            PieceType?[,] board = ToTypeBoard();
            int width = board.GetLength(0);
            int height = board.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int current = new Vector2Int(x, y);

                    if (x + 1 < width)
                    {
                        Vector2Int right = new Vector2Int(x + 1, y);

                        if (!SwapCreatesMatch(board, current, right))
                        {
                            from = current;
                            to = right;
                            return true;
                        }
                    }

                    if (y + 1 < height)
                    {
                        Vector2Int up = new Vector2Int(x, y + 1);

                        if (!SwapCreatesMatch(board, current, up))
                        {
                            from = current;
                            to = up;
                            return true;
                        }
                    }
                }
            }

            from = default;
            to = default;
            return false;
        }

        public void SetBoardForTesting(PieceType[,] boardTypes, int moves, int targetScore)
        {
            StopAllCoroutines();
            EnsureManagers();
            BuildBoardFromTypes(boardTypes);
            selectedPiece = null;
            inputBlocked = false;
            gameManager.InitializeForTest(moves, targetScore, uiManager);
            uiManager.SetStatus("Test board ready.");
        }

        public static PieceType[,] GenerateBoardTypes(int width, int height, PieceType[] availableTypes, int? seed = null)
        {
            width = Mathf.Max(3, width);
            height = Mathf.Max(3, height);
            PieceType[] types = NormalizeTypes(availableTypes);
            System.Random random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            PieceType[,] fallback = BuildFallbackBoard(width, height, types);

            for (int attempt = 0; attempt < 250; attempt++)
            {
                PieceType?[,] working = new PieceType?[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        working[x, y] = PickSafeType(working, x, y, types, random);
                    }
                }

                if (!HasAnyMatches(working) && TryFindPossibleMove(working, out _, out _))
                {
                    return ToConcreteBoard(working, types[0]);
                }
            }

            return fallback;
        }

        public static List<MatchResult> FindMatches(PieceType?[,] board)
        {
            List<MatchResult> results = new List<MatchResult>();
            List<MatchResult> horizontalResults = new List<MatchResult>();
            List<MatchResult> verticalResults = new List<MatchResult>();
            int width = board.GetLength(0);
            int height = board.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                int x = 0;

                while (x < width)
                {
                    PieceType? type = board[x, y];

                    if (!type.HasValue)
                    {
                        x++;
                        continue;
                    }

                    int start = x;

                    while (x < width && board[x, y].HasValue && board[x, y].Value == type.Value)
                    {
                        x++;
                    }

                    int length = x - start;

                    if (length >= 3)
                    {
                        List<Vector2Int> positions = new List<Vector2Int>();

                        for (int matchX = start; matchX < x; matchX++)
                        {
                            positions.Add(new Vector2Int(matchX, y));
                        }

                        horizontalResults.Add(new MatchResult(positions, true, false, false, false));
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                int y = 0;

                while (y < height)
                {
                    PieceType? type = board[x, y];

                    if (!type.HasValue)
                    {
                        y++;
                        continue;
                    }

                    int start = y;

                    while (y < height && board[x, y].HasValue && board[x, y].Value == type.Value)
                    {
                        y++;
                    }

                    int length = y - start;

                    if (length >= 3)
                    {
                        List<Vector2Int> positions = new List<Vector2Int>();

                        for (int matchY = start; matchY < y; matchY++)
                        {
                            positions.Add(new Vector2Int(x, matchY));
                        }

                        verticalResults.Add(new MatchResult(positions, false, true, false, false));
                    }
                }
            }

            results.AddRange(horizontalResults);
            results.AddRange(verticalResults);
            AddShapeMatches(results, horizontalResults, verticalResults);
            return results;
        }

        public static HashSet<Vector2Int> CollectMatchedPositions(IEnumerable<MatchResult> matches)
        {
            HashSet<Vector2Int> positions = new HashSet<Vector2Int>();

            foreach (MatchResult match in matches)
            {
                for (int i = 0; i < match.MatchedPositions.Count; i++)
                {
                    positions.Add(match.MatchedPositions[i]);
                }
            }

            return positions;
        }

        public static bool HasAnyMatches(PieceType?[,] board)
        {
            return FindMatches(board).Count > 0;
        }

        public static bool SwapCreatesMatch(PieceType?[,] board, Vector2Int from, Vector2Int to)
        {
            int width = board.GetLength(0);
            int height = board.GetLength(1);

            if (!IsInBounds(from.x, from.y, width, height) ||
                !IsInBounds(to.x, to.y, width, height) ||
                !AreAdjacent(from, to) ||
                !board[from.x, from.y].HasValue ||
                !board[to.x, to.y].HasValue)
            {
                return false;
            }

            PieceType? fromType = board[from.x, from.y];
            board[from.x, from.y] = board[to.x, to.y];
            board[to.x, to.y] = fromType;

            bool createsMatch = HasAnyMatches(board);

            PieceType? backType = board[from.x, from.y];
            board[from.x, from.y] = board[to.x, to.y];
            board[to.x, to.y] = backType;
            return createsMatch;
        }

        public static bool TryFindPossibleMove(PieceType?[,] board, out Vector2Int from, out Vector2Int to)
        {
            int width = board.GetLength(0);
            int height = board.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int current = new Vector2Int(x, y);

                    if (x + 1 < width)
                    {
                        Vector2Int right = new Vector2Int(x + 1, y);

                        if (SwapCreatesMatch(board, current, right))
                        {
                            from = current;
                            to = right;
                            return true;
                        }
                    }

                    if (y + 1 < height)
                    {
                        Vector2Int up = new Vector2Int(x, y + 1);

                        if (SwapCreatesMatch(board, current, up))
                        {
                            from = current;
                            to = up;
                            return true;
                        }
                    }
                }
            }

            from = default;
            to = default;
            return false;
        }

        public static void CollapseAndFillTypes(PieceType?[,] board, PieceType[] availableTypes, int? seed = null)
        {
            PieceType[] types = NormalizeTypes(availableTypes);
            System.Random random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            int width = board.GetLength(0);
            int height = board.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                int writeY = 0;

                for (int y = 0; y < height; y++)
                {
                    if (board[x, y].HasValue)
                    {
                        board[x, writeY] = board[x, y];

                        if (writeY != y)
                        {
                            board[x, y] = null;
                        }

                        writeY++;
                    }
                }

                while (writeY < height)
                {
                    board[x, writeY] = types[random.Next(types.Length)];
                    writeY++;
                }
            }
        }

        public static int ResolveBoardTypesForTesting(PieceType?[,] board, PieceType[] availableTypes, int? seed, out int cascades)
        {
            int totalScore = 0;
            int multiplier = 1;
            cascades = 0;

            for (int safety = 0; safety < 50; safety++)
            {
                List<MatchResult> matches = FindMatches(board);

                if (matches.Count == 0)
                {
                    return totalScore;
                }

                HashSet<Vector2Int> positions = CollectMatchedPositions(matches);

                foreach (Vector2Int position in positions)
                {
                    board[position.x, position.y] = null;
                }

                totalScore += positions.Count * 10 * multiplier;
                CollapseAndFillTypes(board, availableTypes, seed.HasValue ? seed.Value + safety : null);
                cascades++;
                multiplier++;
            }

            return totalScore;
        }

        public static PieceType?[,] ToNullableBoard(PieceType[,] concreteBoard)
        {
            int width = concreteBoard.GetLength(0);
            int height = concreteBoard.GetLength(1);
            PieceType?[,] nullableBoard = new PieceType?[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    nullableBoard[x, y] = concreteBoard[x, y];
                }
            }

            return nullableBoard;
        }

        public static bool AreAdjacent(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) == 1;
        }

        private IEnumerator AttemptSwapRoutine(Piece first, Piece second)
        {
            inputBlocked = true;
            SelectPiece(null);

            yield return SwapPieces(first, second, ActiveConfig.SwapDuration);

            List<MatchResult> matches = FindMatches(ToTypeBoard());

            if (matches.Count == 0)
            {
                uiManager.SetStatus("No match. Try another swap.");
                yield return SwapPieces(first, second, ActiveConfig.SwapDuration);
                inputBlocked = false;
                yield break;
            }

            gameManager.ConsumeMove();
            yield return ResolveMatches(matches);
            yield return EnsurePlayableBoard();

            gameManager.EvaluateEndState();
            inputBlocked = gameManager.IsGameOver;
        }

        private IEnumerator ResolveMatches(List<MatchResult> initialMatches)
        {
            List<MatchResult> matches = initialMatches;
            int multiplier = 1;

            while (matches.Count > 0)
            {
                HashSet<Vector2Int> matchedPositions = CollectMatchedPositions(matches);
                gameManager.AddScore(matchedPositions.Count * 10 * multiplier);
                uiManager.SetStatus(multiplier == 1 ? "Match!" : $"Cascade x{multiplier}!");

                yield return ClearMatchedPieces(matchedPositions);
                yield return ApplyGravityAndSpawn();

                if (ActiveConfig.CascadeDelay > 0f)
                {
                    yield return new WaitForSeconds(ActiveConfig.CascadeDelay);
                }

                matches = FindMatches(ToTypeBoard());
                multiplier++;
            }
        }

        private IEnumerator ClearMatchedPieces(HashSet<Vector2Int> matchedPositions)
        {
            List<Piece> clearingPieces = new List<Piece>();

            foreach (Vector2Int position in matchedPositions)
            {
                Piece piece = pieces[position.x, position.y];

                if (piece == null)
                {
                    continue;
                }

                pieces[position.x, position.y] = null;
                clearingPieces.Add(piece);
                StartCoroutine(piece.AnimateClear(ActiveConfig.ClearDuration));
            }

            if (ActiveConfig.ClearDuration > 0f)
            {
                yield return new WaitForSeconds(ActiveConfig.ClearDuration);
            }

            for (int i = 0; i < clearingPieces.Count; i++)
            {
                if (clearingPieces[i] != null)
                {
                    Destroy(clearingPieces[i].gameObject);
                }
            }
        }

        private IEnumerator ApplyGravityAndSpawn()
        {
            List<Piece> movingPieces = new List<Piece>();
            PieceType[] types = ActiveConfig.GetAvailablePieceTypes();

            for (int x = 0; x < currentWidth; x++)
            {
                int writeY = 0;

                for (int y = 0; y < currentHeight; y++)
                {
                    Piece piece = pieces[x, y];

                    if (piece == null)
                    {
                        continue;
                    }

                    if (writeY != y)
                    {
                        pieces[x, writeY] = piece;
                        pieces[x, y] = null;
                        piece.SetGridPosition(x, writeY);
                        movingPieces.Add(piece);
                    }

                    writeY++;
                }

                while (writeY < currentHeight)
                {
                    PieceType type = types[Random.Range(0, types.Length)];
                    Piece spawnedPiece = CreatePiece(x, writeY, type);
                    spawnedPiece.SetAnchoredPosition(GetAnchoredPosition(x, currentHeight + (writeY % 3) + 1));
                    pieces[x, writeY] = spawnedPiece;
                    movingPieces.Add(spawnedPiece);
                    writeY++;
                }
            }

            for (int i = 0; i < movingPieces.Count; i++)
            {
                Piece piece = movingPieces[i];
                StartCoroutine(piece.AnimateTo(GetAnchoredPosition(piece.GridX, piece.GridY), ActiveConfig.FallDuration));
            }

            if (movingPieces.Count > 0 && ActiveConfig.FallDuration > 0f)
            {
                yield return new WaitForSeconds(ActiveConfig.FallDuration);
            }
        }

        private IEnumerator EnsurePlayableBoard()
        {
            if (TryFindPossibleMove(ToTypeBoard(), out _, out _))
            {
                yield break;
            }

            uiManager.SetStatus("No moves available. Reshuffling.");

            if (ActiveConfig.CascadeDelay > 0f)
            {
                yield return new WaitForSeconds(ActiveConfig.CascadeDelay);
            }

            PieceType[,] boardTypes = GenerateBoardTypes(currentWidth, currentHeight, ActiveConfig.GetAvailablePieceTypes());
            BuildBoardFromTypes(boardTypes);
        }

        private IEnumerator SwapPieces(Piece first, Piece second, float duration)
        {
            SwapPieceData(first, second);
            StartCoroutine(first.AnimateTo(GetAnchoredPosition(first.GridX, first.GridY), duration));
            StartCoroutine(second.AnimateTo(GetAnchoredPosition(second.GridX, second.GridY), duration));

            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
        }

        private void SwapPieceData(Piece first, Piece second)
        {
            int firstX = first.GridX;
            int firstY = first.GridY;
            int secondX = second.GridX;
            int secondY = second.GridY;

            pieces[firstX, firstY] = second;
            pieces[secondX, secondY] = first;
            first.SetGridPosition(secondX, secondY);
            second.SetGridPosition(firstX, firstY);
        }

        private void BuildBoardFromTypes(PieceType[,] boardTypes)
        {
            currentWidth = boardTypes.GetLength(0);
            currentHeight = boardTypes.GetLength(1);
            pieces = new Piece[currentWidth, currentHeight];

            ClearBoardVisuals();
            CreateBoardRoot();
            CreateCellBackgrounds();

            for (int y = 0; y < currentHeight; y++)
            {
                for (int x = 0; x < currentWidth; x++)
                {
                    pieces[x, y] = CreatePiece(x, y, boardTypes[x, y]);
                }
            }
        }

        private void ClearBoardVisuals()
        {
            if (boardRoot == null)
            {
                return;
            }

            boardRoot.gameObject.SetActive(false);
            Destroy(boardRoot.gameObject);
            boardRoot = null;
        }

        private void CreateBoardRoot()
        {
            GameObject boardObject = new GameObject("Match3BoardRoot", typeof(RectTransform));
            boardObject.transform.SetParent(uiManager.CanvasTransform, false);

            RectTransform rect = boardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(
                (ActiveConfig.CellSize * currentWidth) + (ActiveConfig.CellSpacing * (currentWidth - 1)),
                (ActiveConfig.CellSize * currentHeight) + (ActiveConfig.CellSpacing * (currentHeight - 1)));

            boardRoot = boardObject.transform;
        }

        private void CreateCellBackgrounds()
        {
            for (int y = 0; y < currentHeight; y++)
            {
                for (int x = 0; x < currentWidth; x++)
                {
                    GameObject cellObject = new GameObject($"Match3Cell_{x}_{y}", typeof(RectTransform), typeof(Image));
                    cellObject.transform.SetParent(boardRoot, false);

                    RectTransform rect = cellObject.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(ActiveConfig.CellSize, ActiveConfig.CellSize);
                    rect.anchoredPosition = GetAnchoredPosition(x, y);

                    Image image = cellObject.GetComponent<Image>();
                    image.sprite = ActiveTheme != null ? ActiveTheme.EmptyCellSprite : null;
                    image.color = ActiveTheme != null ? ActiveTheme.EmptyCellColor : new Color(0.18f, 0.20f, 0.24f, 1f);
                    image.raycastTarget = false;
                }
            }
        }

        private Piece CreatePiece(int x, int y, PieceType type)
        {
            GameObject pieceObject = new GameObject($"Piece_{x}_{y}", typeof(RectTransform), typeof(Image), typeof(Piece));
            pieceObject.transform.SetParent(boardRoot, false);

            RectTransform rect = pieceObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(ActiveConfig.CellSize * 0.86f, ActiveConfig.CellSize * 0.86f);
            rect.anchoredPosition = GetAnchoredPosition(x, y);

            Piece piece = pieceObject.GetComponent<Piece>();
            Sprite sprite = ActiveTheme != null ? ActiveTheme.TileSprite : null;
            piece.Initialize(this, x, y, type, sprite, ActiveConfig.GetPieceColor(type));
            return piece;
        }

        private Vector2 GetAnchoredPosition(int x, int y)
        {
            float pitch = ActiveConfig.CellSize + ActiveConfig.CellSpacing;
            float originX = -((currentWidth - 1) * pitch * 0.5f);
            float originY = -((currentHeight - 1) * pitch * 0.5f);
            return new Vector2(originX + (x * pitch), originY + (y * pitch));
        }

        private void SelectPiece(Piece piece)
        {
            if (selectedPiece != null)
            {
                selectedPiece.SetSelected(false);
            }

            selectedPiece = piece;

            if (selectedPiece != null)
            {
                selectedPiece.SetSelected(true);
                uiManager.SetStatus("Choose an adjacent piece.");
            }
        }

        private PieceType?[,] ToTypeBoard()
        {
            PieceType?[,] board = new PieceType?[currentWidth, currentHeight];

            for (int y = 0; y < currentHeight; y++)
            {
                for (int x = 0; x < currentWidth; x++)
                {
                    board[x, y] = pieces[x, y] != null ? pieces[x, y].Type : (PieceType?)null;
                }
            }

            return board;
        }

        private void EnsureManagers()
        {
            gameManager = GetComponent<GameManager>();

            if (gameManager == null)
            {
                gameManager = gameObject.AddComponent<GameManager>();
            }

            uiManager = GetComponent<UIManager>();

            if (uiManager == null)
            {
                uiManager = gameObject.AddComponent<UIManager>();
            }

            uiManager.Initialize(this, ActiveTheme);
        }

        private static void AddShapeMatches(List<MatchResult> results, List<MatchResult> horizontalResults, List<MatchResult> verticalResults)
        {
            for (int h = 0; h < horizontalResults.Count; h++)
            {
                for (int v = 0; v < verticalResults.Count; v++)
                {
                    Vector2Int? intersection = FindIntersection(horizontalResults[h], verticalResults[v]);

                    if (!intersection.HasValue)
                    {
                        continue;
                    }

                    HashSet<Vector2Int> shapePositions = new HashSet<Vector2Int>(horizontalResults[h].MatchedPositions);

                    for (int i = 0; i < verticalResults[v].MatchedPositions.Count; i++)
                    {
                        shapePositions.Add(verticalResults[v].MatchedPositions[i]);
                    }

                    bool horizontalMiddle = IsMiddle(horizontalResults[h], intersection.Value);
                    bool verticalMiddle = IsMiddle(verticalResults[v], intersection.Value);
                    bool isTShape = horizontalMiddle || verticalMiddle;
                    results.Add(new MatchResult(shapePositions, true, true, isTShape, !isTShape));
                }
            }
        }

        private static Vector2Int? FindIntersection(MatchResult first, MatchResult second)
        {
            for (int i = 0; i < first.MatchedPositions.Count; i++)
            {
                for (int j = 0; j < second.MatchedPositions.Count; j++)
                {
                    if (first.MatchedPositions[i] == second.MatchedPositions[j])
                    {
                        return first.MatchedPositions[i];
                    }
                }
            }

            return null;
        }

        private static bool IsMiddle(MatchResult match, Vector2Int position)
        {
            int index = match.MatchedPositions.IndexOf(position);
            return index > 0 && index < match.MatchedPositions.Count - 1;
        }

        private static PieceType PickSafeType(PieceType?[,] board, int x, int y, PieceType[] types, System.Random random)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                PieceType candidate = types[random.Next(types.Length)];

                if (!WouldCreateImmediateMatch(board, x, y, candidate))
                {
                    return candidate;
                }
            }

            for (int i = 0; i < types.Length; i++)
            {
                if (!WouldCreateImmediateMatch(board, x, y, types[i]))
                {
                    return types[i];
                }
            }

            return types[0];
        }

        private static bool WouldCreateImmediateMatch(PieceType?[,] board, int x, int y, PieceType type)
        {
            bool horizontal = x >= 2 &&
                              board[x - 1, y].HasValue &&
                              board[x - 2, y].HasValue &&
                              board[x - 1, y].Value == type &&
                              board[x - 2, y].Value == type;

            bool vertical = y >= 2 &&
                            board[x, y - 1].HasValue &&
                            board[x, y - 2].HasValue &&
                            board[x, y - 1].Value == type &&
                            board[x, y - 2].Value == type;

            return horizontal || vertical;
        }

        private static PieceType[,] BuildFallbackBoard(int width, int height, PieceType[] types)
        {
            PieceType[,] board = new PieceType[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    board[x, y] = types[((x * 2) + (y * 3)) % types.Length];
                }
            }

            board[0, 0] = types[0];
            board[1, 0] = types[1];
            board[2, 0] = types[0];
            board[1, 1] = types[0];
            return board;
        }

        private static PieceType[,] ToConcreteBoard(PieceType?[,] nullableBoard, PieceType fallback)
        {
            int width = nullableBoard.GetLength(0);
            int height = nullableBoard.GetLength(1);
            PieceType[,] board = new PieceType[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    board[x, y] = nullableBoard[x, y].HasValue ? nullableBoard[x, y].Value : fallback;
                }
            }

            return board;
        }

        private static PieceType[] NormalizeTypes(PieceType[] availableTypes)
        {
            if (availableTypes != null && availableTypes.Length >= 3)
            {
                return availableTypes;
            }

            return new[] { PieceType.Red, PieceType.Blue, PieceType.Green, PieceType.Yellow, PieceType.Purple, PieceType.Orange };
        }

        private static bool IsInBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}
