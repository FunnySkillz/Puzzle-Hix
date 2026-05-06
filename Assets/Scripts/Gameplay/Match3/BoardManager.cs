using System.Collections;
using System.Collections.Generic;
using PuzzleDungeon.Services;
using PuzzleDungeon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleDungeon.Gameplay.Match3
{
    /// <summary>
    /// Owns board generation, input, swapping, match detection, special pieces, gravity, cascades, and level flow.
    /// </summary>
    [DisallowMultipleComponent]
    public class BoardManager : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private BoardConfig config;
        [SerializeField] private PrototypeTheme theme;
        [SerializeField] private Match3LevelCatalog levelCatalog;

        private Piece[,] pieces;
        private Piece selectedPiece;
        private Transform boardRoot;
        private GameManager gameManager;
        private UIManager uiManager;
        private Match3ProgressService progressService;
        private IMatch3AnalyticsSink analyticsSink;
        private Match3LevelData currentLevel;
        private int currentWidth;
        private int currentHeight;
        private int currentLevelIndex;
        private bool inputBlocked;
        private bool hintRoutineRunning;
        private bool levelEndReported;
        private float levelStartTime;
        private float lastInputTime;

        public bool IsInputBlocked => inputBlocked;
        public int CurrentScore => gameManager != null ? gameManager.Score : 0;
        public int MovesRemaining => gameManager != null ? gameManager.MovesRemaining : 0;
        public int MovesUsed => gameManager != null ? gameManager.MovesUsed : 0;
        public int TargetScore => gameManager != null ? gameManager.TargetScore : 0;
        public int CurrentLevelIndex => currentLevelIndex;
        public int CurrentLevelNumber => currentLevel != null ? currentLevel.LevelNumber : 1;
        public ObjectiveType CurrentObjectiveType => gameManager != null ? gameManager.ObjectiveType : ObjectiveType.Score;
        public bool IsGameOver => gameManager != null && gameManager.IsGameOver;
        public bool HasWon => gameManager != null && gameManager.HasWon;
        public bool HasNextLevel => ActiveLevelCatalog != null && currentLevelIndex + 1 < ActiveLevelCatalog.LevelCount;

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

        private Match3LevelCatalog ActiveLevelCatalog
        {
            get
            {
                if (levelCatalog == null)
                {
                    levelCatalog = Match3LevelCatalog.LoadDefault();
                }

                return levelCatalog;
            }
        }

        private void Awake()
        {
            EnsureManagers();
            StartNewGame();
        }

        private void Update()
        {
            if (inputBlocked || IsGameOver || hintRoutineRunning || pieces == null)
            {
                return;
            }

            if (Time.time - lastInputTime >= ActiveConfig.HintDelay)
            {
                StartCoroutine(HintRoutine());
            }
        }

        public void StartNewGame()
        {
            EnsureManagers();
            int levelCount = ActiveLevelCatalog != null ? ActiveLevelCatalog.LevelCount : 1;
            int savedIndex = progressService.GetCurrentLevelIndex(levelCount);
            StartLevel(savedIndex, false);
        }

        public void RetryCurrentLevel()
        {
            analyticsSink.LevelRetried(CurrentLevelNumber);
            StartLevel(currentLevelIndex, true);
        }

        public void GoToNextLevel()
        {
            if (!HasNextLevel)
            {
                return;
            }

            StartLevel(currentLevelIndex + 1, true);
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

            lastInputTime = Time.time;

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

            lastInputTime = Time.time;
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

        public int GetCollectedCount(PieceType pieceType)
        {
            return gameManager != null ? gameManager.GetCollectedCount(pieceType) : 0;
        }

        public int GetColorGoal(PieceType pieceType)
        {
            return gameManager != null ? gameManager.GetColorGoal(pieceType) : 0;
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
            currentLevelIndex = 0;
            currentLevel = ScriptableObject.CreateInstance<Match3LevelData>();
            currentLevel.ConfigureForTesting(1, boardTypes.GetLength(0), boardTypes.GetLength(1), moves, targetScore, 6, ObjectiveType.Score, null, 0);
            BuildBoardFromTypes(boardTypes);
            selectedPiece = null;
            inputBlocked = false;
            levelEndReported = false;
            levelStartTime = Time.time;
            lastInputTime = Time.time;
            gameManager.Initialize(currentLevel, ActiveConfig, uiManager);
            uiManager.SetStatus("Test board ready.");
        }

        public void SetLevelForTesting(Match3LevelData level, PieceType[,] boardTypes)
        {
            StopAllCoroutines();
            EnsureManagers();
            currentLevelIndex = Mathf.Max(0, level != null ? level.LevelNumber - 1 : 0);
            currentLevel = level != null ? level : CreateFallbackLevel();
            BuildBoardFromTypes(boardTypes);
            selectedPiece = null;
            inputBlocked = false;
            levelEndReported = false;
            levelStartTime = Time.time;
            lastInputTime = Time.time;
            gameManager.Initialize(currentLevel, ActiveConfig, uiManager);
            uiManager.SetStatus("Test level ready.");
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

            List<MatchResult> matches = FindMatches(board);
            bool createsMatch = ContainsMatchAt(matches, from) || ContainsMatchAt(matches, to);

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

        public static SpecialPieceType DetermineSpecialPieceType(IEnumerable<MatchResult> matches, Vector2Int preferredPosition)
        {
            SpecialPieceType bestType = SpecialPieceType.None;

            foreach (MatchResult match in matches)
            {
                if (!match.MatchedPositions.Contains(preferredPosition) && bestType != SpecialPieceType.None)
                {
                    continue;
                }

                SpecialPieceType candidate = GetSpecialPieceTypeForMatch(match);

                if (GetSpecialPriority(candidate) > GetSpecialPriority(bestType))
                {
                    bestType = candidate;
                }
            }

            return bestType;
        }

        private void StartLevel(int levelIndex, bool saveCurrentLevel)
        {
            StopAllCoroutines();
            EnsureManagers();

            Match3LevelCatalog catalog = ActiveLevelCatalog;
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, catalog != null ? catalog.LevelCount - 1 : 0));
            currentLevel = catalog != null ? catalog.GetLevelByIndex(currentLevelIndex) : null;

            if (currentLevel == null)
            {
                currentLevel = CreateFallbackLevel();
            }

            if (saveCurrentLevel)
            {
                progressService.SetCurrentLevelIndex(currentLevelIndex, catalog != null ? catalog.LevelCount : 1);
            }

            PieceType[] availableTypes = ActiveConfig.GetAvailablePieceTypes(currentLevel.AvailablePieceTypeCount);
            PieceType[,] boardTypes = GenerateBoardTypes(currentLevel.Width, currentLevel.Height, availableTypes);
            BuildBoardFromTypes(boardTypes);

            selectedPiece = null;
            inputBlocked = false;
            hintRoutineRunning = false;
            levelEndReported = false;
            levelStartTime = Time.time;
            lastInputTime = Time.time;
            gameManager.Initialize(currentLevel, ActiveConfig, uiManager);
            uiManager.SetStatus($"Level {currentLevel.LevelNumber}: make smart swaps.");
            analyticsSink.LevelStarted(CurrentLevelNumber, CurrentObjectiveType, MovesRemaining);
        }

        private IEnumerator AttemptSwapRoutine(Piece first, Piece second)
        {
            inputBlocked = true;
            SelectPiece(null);

            Vector2Int firstStart = new Vector2Int(first.GridX, first.GridY);
            Vector2Int secondStart = new Vector2Int(second.GridX, second.GridY);

            yield return SwapPieces(first, second, ActiveConfig.SwapDuration);

            List<MatchResult> matches = FindMatches(ToTypeBoard());

            if (matches.Count == 0 || (!ContainsMatchAt(matches, firstStart) && !ContainsMatchAt(matches, secondStart)))
            {
                uiManager.SetStatus("No match. Try another swap.");
                Vector2 offset = new Vector2(secondStart.x - firstStart.x, secondStart.y - firstStart.y) * 14f;
                StartCoroutine(first.AnimateInvalidBounce(-offset, ActiveConfig.InvalidSwapDuration));
                StartCoroutine(second.AnimateInvalidBounce(offset, ActiveConfig.InvalidSwapDuration));

                if (ActiveConfig.InvalidSwapDuration > 0f)
                {
                    yield return new WaitForSeconds(ActiveConfig.InvalidSwapDuration);
                }

                yield return SwapPieces(first, second, ActiveConfig.SwapDuration);
                analyticsSink.SwapResolved(CurrentLevelNumber, false, MovesRemaining, CurrentScore);
                inputBlocked = false;
                yield break;
            }

            gameManager.ConsumeMove();
            Vector2Int specialPosition = SelectSpecialCreationPosition(matches, firstStart, secondStart);
            SpecialPieceType createdSpecialType = DetermineSpecialPieceType(matches, specialPosition);
            yield return ResolveMatches(matches, createdSpecialType, specialPosition);
            yield return EnsurePlayableBoard();

            analyticsSink.SwapResolved(CurrentLevelNumber, true, MovesRemaining, CurrentScore);
            bool ended = gameManager.EvaluateEndState();

            if (ended)
            {
                HandleLevelEnded();
            }

            inputBlocked = gameManager.IsGameOver;
            lastInputTime = Time.time;
        }

        private IEnumerator ResolveMatches(List<MatchResult> initialMatches, SpecialPieceType createdSpecialType, Vector2Int specialPosition)
        {
            List<MatchResult> matches = initialMatches;
            int multiplier = 1;
            bool canCreateSpecial = createdSpecialType != SpecialPieceType.None;

            while (matches.Count > 0)
            {
                HashSet<Vector2Int> matchedPositions = CollectMatchedPositions(matches);
                HashSet<Vector2Int> expandedPositions = ExpandMatchedPositionsForSpecials(matchedPositions);
                List<PieceType> clearedTypes = new List<PieceType>();
                yield return ClearMatchedPieces(expandedPositions, canCreateSpecial ? specialPosition : (Vector2Int?)null, createdSpecialType, clearedTypes);

                int scoreGain = clearedTypes.Count * 10 * multiplier;
                gameManager.RecordClearedPieces(clearedTypes, scoreGain);
                uiManager.SetStatus(multiplier == 1 ? $"Match! +{scoreGain}" : $"Cascade x{multiplier}! +{scoreGain}");
                uiManager.ShowFloatingFeedback(multiplier == 1 ? $"+{scoreGain}" : $"x{multiplier} +{scoreGain}", Vector2.zero, Color.white);

                yield return ApplyGravityAndSpawn();

                if (ActiveConfig.CascadeDelay > 0f)
                {
                    yield return new WaitForSeconds(ActiveConfig.CascadeDelay);
                }

                matches = FindMatches(ToTypeBoard());
                multiplier++;
                canCreateSpecial = false;
            }
        }

        private IEnumerator ClearMatchedPieces(HashSet<Vector2Int> matchedPositions, Vector2Int? specialCreationPosition, SpecialPieceType createdSpecialType, List<PieceType> clearedTypes)
        {
            List<Piece> clearingPieces = new List<Piece>();

            foreach (Vector2Int position in matchedPositions)
            {
                Piece piece = pieces[position.x, position.y];

                if (piece == null)
                {
                    continue;
                }

                if (specialCreationPosition.HasValue && position == specialCreationPosition.Value && createdSpecialType != SpecialPieceType.None)
                {
                    piece.SetSpecialPieceType(createdSpecialType);
                    uiManager.ShowFloatingFeedback(DescribeSpecial(createdSpecialType), GetCanvasPosition(piece), Color.yellow);
                    continue;
                }

                pieces[position.x, position.y] = null;
                clearedTypes.Add(piece.Type);
                clearingPieces.Add(piece);
                uiManager.ShowPieceBurst(GetCanvasPosition(piece), ActiveConfig.GetPieceColor(piece.Type));
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
            PieceType[] types = ActiveConfig.GetAvailablePieceTypes(currentLevel != null ? currentLevel.AvailablePieceTypeCount : 6);

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
                    Piece spawnedPiece = CreatePiece(x, writeY, type, SpecialPieceType.None);
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

            PieceType[] availableTypes = ActiveConfig.GetAvailablePieceTypes(currentLevel != null ? currentLevel.AvailablePieceTypeCount : 6);
            PieceType[,] boardTypes = GenerateBoardTypes(currentWidth, currentHeight, availableTypes);
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
                    pieces[x, y] = CreatePiece(x, y, boardTypes[x, y], SpecialPieceType.None);
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
            rect.anchoredPosition = new Vector2(0f, -130f);
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

        private Piece CreatePiece(int x, int y, PieceType type, SpecialPieceType specialType)
        {
            GameObject pieceObject = new GameObject($"Piece_{x}_{y}", typeof(RectTransform), typeof(Image), typeof(Piece));
            pieceObject.transform.SetParent(boardRoot, false);

            RectTransform rect = pieceObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(ActiveConfig.CellSize * 0.86f, ActiveConfig.CellSize * 0.86f);
            rect.anchoredPosition = GetAnchoredPosition(x, y);

            Piece piece = pieceObject.GetComponent<Piece>();
            Sprite sprite = ActiveTheme != null ? ActiveTheme.TileSprite : null;
            piece.Initialize(this, x, y, type, specialType, sprite, ActiveConfig.GetPieceColor(type));
            return piece;
        }

        private Vector2 GetAnchoredPosition(int x, int y)
        {
            float pitch = ActiveConfig.CellSize + ActiveConfig.CellSpacing;
            float originX = -((currentWidth - 1) * pitch * 0.5f);
            float originY = -((currentHeight - 1) * pitch * 0.5f);
            return new Vector2(originX + (x * pitch), originY + (y * pitch));
        }

        private Vector2 GetCanvasPosition(Piece piece)
        {
            RectTransform rootRect = boardRoot as RectTransform;
            RectTransform pieceRect = piece != null ? piece.GetComponent<RectTransform>() : null;

            if (rootRect == null || pieceRect == null)
            {
                return Vector2.zero;
            }

            return rootRect.anchoredPosition + pieceRect.anchoredPosition;
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

        private HashSet<Vector2Int> ExpandMatchedPositionsForSpecials(HashSet<Vector2Int> matchedPositions)
        {
            HashSet<Vector2Int> expanded = new HashSet<Vector2Int>(matchedPositions);

            foreach (Vector2Int position in matchedPositions)
            {
                Piece piece = pieces[position.x, position.y];

                if (piece == null)
                {
                    continue;
                }

                switch (piece.SpecialPieceType)
                {
                    case SpecialPieceType.LineHorizontal:
                        for (int x = 0; x < currentWidth; x++)
                        {
                            expanded.Add(new Vector2Int(x, position.y));
                        }

                        break;
                    case SpecialPieceType.LineVertical:
                        for (int y = 0; y < currentHeight; y++)
                        {
                            expanded.Add(new Vector2Int(position.x, y));
                        }

                        break;
                    case SpecialPieceType.Bomb:
                        for (int y = position.y - 1; y <= position.y + 1; y++)
                        {
                            for (int x = position.x - 1; x <= position.x + 1; x++)
                            {
                                if (IsInBounds(x, y, currentWidth, currentHeight))
                                {
                                    expanded.Add(new Vector2Int(x, y));
                                }
                            }
                        }

                        break;
                    case SpecialPieceType.ColorClear:
                        PieceType targetType = piece.Type;

                        for (int y = 0; y < currentHeight; y++)
                        {
                            for (int x = 0; x < currentWidth; x++)
                            {
                                Piece candidate = pieces[x, y];

                                if (candidate != null && candidate.Type == targetType)
                                {
                                    expanded.Add(new Vector2Int(x, y));
                                }
                            }
                        }

                        break;
                }
            }

            return expanded;
        }

        private void HandleLevelEnded()
        {
            if (levelEndReported)
            {
                return;
            }

            levelEndReported = true;
            float sessionSeconds = Mathf.Max(0f, Time.time - levelStartTime);
            analyticsSink.LevelEnded(CurrentLevelNumber, HasWon, MovesRemaining, CurrentScore, sessionSeconds);

            if (HasWon)
            {
                int levelCount = ActiveLevelCatalog != null ? ActiveLevelCatalog.LevelCount : 1;
                progressService.MarkLevelComplete(currentLevelIndex, levelCount, currentLevel.LevelId, MovesUsed);
            }
        }

        private IEnumerator HintRoutine()
        {
            hintRoutineRunning = true;

            if (TryFindFirstValidSwap(out Vector2Int from, out Vector2Int to))
            {
                Piece first = GetPieceAt(from.x, from.y);
                Piece second = GetPieceAt(to.x, to.y);

                if (first != null)
                {
                    StartCoroutine(first.AnimateHintPulse(0.45f));
                }

                if (second != null)
                {
                    StartCoroutine(second.AnimateHintPulse(0.45f));
                }

                uiManager.SetStatus("Hint: try the pulsing pair.");
            }

            yield return new WaitForSeconds(0.6f);
            lastInputTime = Time.time;
            hintRoutineRunning = false;
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

            if (progressService == null)
            {
                progressService = new Match3ProgressService(new SaveService());
            }

            if (analyticsSink == null)
            {
                analyticsSink = new NoOpMatch3AnalyticsSink();
            }

            uiManager.Initialize(this, ActiveTheme);
        }

        private Match3LevelData CreateFallbackLevel()
        {
            Match3LevelData fallbackLevel = ScriptableObject.CreateInstance<Match3LevelData>();
            fallbackLevel.ConfigureForTesting(
                1,
                ActiveConfig.Width,
                ActiveConfig.Height,
                ActiveConfig.StartingMoves,
                ActiveConfig.TargetScore,
                ActiveConfig.GetAvailablePieceTypes().Length,
                ObjectiveType.Score,
                null,
                0);
            return fallbackLevel;
        }

        private Vector2Int SelectSpecialCreationPosition(List<MatchResult> matches, Vector2Int firstPreferred, Vector2Int secondPreferred)
        {
            if (ContainsMatchAt(matches, firstPreferred))
            {
                return firstPreferred;
            }

            if (ContainsMatchAt(matches, secondPreferred))
            {
                return secondPreferred;
            }

            return matches.Count > 0 && matches[0].MatchedPositions.Count > 0 ? matches[0].MatchedPositions[0] : firstPreferred;
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

        private static bool ContainsMatchAt(IEnumerable<MatchResult> matches, Vector2Int position)
        {
            foreach (MatchResult match in matches)
            {
                if (match.MatchedPositions.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        private static SpecialPieceType GetSpecialPieceTypeForMatch(MatchResult match)
        {
            if (match.IsTShape || match.IsLShape)
            {
                return SpecialPieceType.Bomb;
            }

            if (match.MatchSize >= 5)
            {
                return SpecialPieceType.ColorClear;
            }

            if (match.MatchSize >= 4)
            {
                return match.IsVertical ? SpecialPieceType.LineVertical : SpecialPieceType.LineHorizontal;
            }

            return SpecialPieceType.None;
        }

        private static int GetSpecialPriority(SpecialPieceType specialType)
        {
            switch (specialType)
            {
                case SpecialPieceType.ColorClear:
                    return 4;
                case SpecialPieceType.Bomb:
                    return 3;
                case SpecialPieceType.LineHorizontal:
                case SpecialPieceType.LineVertical:
                    return 2;
                default:
                    return 0;
            }
        }

        private static string DescribeSpecial(SpecialPieceType specialType)
        {
            switch (specialType)
            {
                case SpecialPieceType.LineHorizontal:
                    return "Line!";
                case SpecialPieceType.LineVertical:
                    return "Line!";
                case SpecialPieceType.Bomb:
                    return "Bomb!";
                case SpecialPieceType.ColorClear:
                    return "Color!";
                default:
                    return string.Empty;
            }
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
