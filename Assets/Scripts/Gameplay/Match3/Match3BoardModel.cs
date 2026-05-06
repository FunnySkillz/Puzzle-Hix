using System.Collections.Generic;
using UnityEngine;

namespace PuzzleDungeon.Gameplay.Match3
{
    public sealed class BoardResolveResult
    {
        public BoardResolveResult(int score, int cascades, int clearedPieces)
        {
            Score = Mathf.Max(0, score);
            Cascades = Mathf.Max(0, cascades);
            ClearedPieces = Mathf.Max(0, clearedPieces);
        }

        public int Score { get; }
        public int Cascades { get; }
        public int ClearedPieces { get; }
    }

    public sealed class Match3BoardModel
    {
        private const int MaxGenerationAttempts = 250;
        private const int MaxCascadePasses = 50;

        private readonly PieceType[] availableTypes;
        private readonly System.Random random;

        private PieceType?[,] board;
        private int spawnStep;

        public Match3BoardModel(int width, int height, PieceType[] types, int? seed = null)
        {
            Width = Mathf.Max(3, width);
            Height = Mathf.Max(3, height);
            availableTypes = NormalizeTypes(types);
            Seed = seed;
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            board = ToNullableBoard(GenerateBoardTypes(Width, Height, availableTypes, seed));
        }

        public int Width { get; }
        public int Height { get; }
        public int? Seed { get; }

        public PieceType?[,] CopyBoard()
        {
            return CopyBoard(board);
        }

        public PieceType? GetPieceType(int x, int y)
        {
            return IsInBounds(x, y, Width, Height) ? board[x, y] : null;
        }

        public static Match3BoardModel CreateForTesting(PieceType?[,] sourceBoard, PieceType[] types, int? seed = null)
        {
            Match3BoardModel model = new Match3BoardModel(sourceBoard.GetLength(0), sourceBoard.GetLength(1), types, seed);
            model.board = CopyBoard(sourceBoard);
            return model;
        }

        public bool TrySwap(Vector2Int from, Vector2Int to, out List<MatchResult> matches)
        {
            matches = new List<MatchResult>();

            if (!CanSwap(from, to))
            {
                return false;
            }

            SwapInPlace(board, from, to);
            matches = FindMatches(board);
            bool createsMatch = ContainsMatchAt(matches, from) || ContainsMatchAt(matches, to);

            if (!createsMatch)
            {
                SwapInPlace(board, from, to);
                return false;
            }

            return true;
        }

        public BoardResolveResult ResolveCascades()
        {
            int totalScore = 0;
            int cascades = 0;
            int clearedPieces = 0;
            int multiplier = 1;

            for (int safety = 0; safety < MaxCascadePasses; safety++)
            {
                List<MatchResult> matches = FindMatches(board);

                if (matches.Count == 0)
                {
                    return new BoardResolveResult(totalScore, cascades, clearedPieces);
                }

                HashSet<Vector2Int> positions = CollectMatchedPositions(matches);

                foreach (Vector2Int position in positions)
                {
                    board[position.x, position.y] = null;
                }

                totalScore += positions.Count * 10 * multiplier;
                clearedPieces += positions.Count;
                CollapseAndFillInstance();
                cascades++;
                multiplier++;
            }

            return new BoardResolveResult(totalScore, cascades, clearedPieces);
        }

        public bool EnsurePlayableBoard()
        {
            if (!HasAnyMatches(board) && TryFindPossibleMove(board, out _, out _))
            {
                return false;
            }

            board = ToNullableBoard(GenerateBoardTypes(Width, Height, availableTypes, Seed.HasValue ? Seed.Value + spawnStep + 1 : (int?)null));
            spawnStep++;
            return true;
        }

        public bool TryFindPossibleMove(out Vector2Int from, out Vector2Int to)
        {
            return TryFindPossibleMove(board, out from, out to);
        }

        public int SimulateValidMoves(int maxMoves)
        {
            int completedMoves = 0;

            for (int i = 0; i < maxMoves; i++)
            {
                EnsurePlayableBoard();

                if (!TryFindPossibleMove(out Vector2Int from, out Vector2Int to))
                {
                    break;
                }

                if (!TrySwap(from, to, out _))
                {
                    break;
                }

                ResolveCascades();
                EnsurePlayableBoard();
                completedMoves++;
            }

            return completedMoves;
        }

        public bool HasNoNullCells()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!board[x, y].HasValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static PieceType[,] GenerateBoardTypes(int width, int height, PieceType[] availableTypes, int? seed = null)
        {
            width = Mathf.Max(3, width);
            height = Mathf.Max(3, height);
            PieceType[] types = NormalizeTypes(availableTypes);
            System.Random random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            PieceType[,] fallback = BuildFallbackBoard(width, height, types);

            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
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

        public static List<MatchResult> FindMatches(PieceType?[,] sourceBoard)
        {
            List<MatchResult> results = new List<MatchResult>();
            List<MatchResult> horizontalResults = new List<MatchResult>();
            List<MatchResult> verticalResults = new List<MatchResult>();
            int width = sourceBoard.GetLength(0);
            int height = sourceBoard.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                int x = 0;

                while (x < width)
                {
                    PieceType? type = sourceBoard[x, y];

                    if (!type.HasValue)
                    {
                        x++;
                        continue;
                    }

                    int start = x;

                    while (x < width && sourceBoard[x, y].HasValue && sourceBoard[x, y].Value == type.Value)
                    {
                        x++;
                    }

                    AddLineMatch(horizontalResults, start, x, y, true);
                }
            }

            for (int x = 0; x < width; x++)
            {
                int y = 0;

                while (y < height)
                {
                    PieceType? type = sourceBoard[x, y];

                    if (!type.HasValue)
                    {
                        y++;
                        continue;
                    }

                    int start = y;

                    while (y < height && sourceBoard[x, y].HasValue && sourceBoard[x, y].Value == type.Value)
                    {
                        y++;
                    }

                    AddLineMatch(verticalResults, start, y, x, false);
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

        public static bool HasAnyMatches(PieceType?[,] sourceBoard)
        {
            return FindMatches(sourceBoard).Count > 0;
        }

        public static bool SwapCreatesMatch(PieceType?[,] sourceBoard, Vector2Int from, Vector2Int to)
        {
            int width = sourceBoard.GetLength(0);
            int height = sourceBoard.GetLength(1);

            if (!IsInBounds(from.x, from.y, width, height) ||
                !IsInBounds(to.x, to.y, width, height) ||
                !AreAdjacent(from, to) ||
                !sourceBoard[from.x, from.y].HasValue ||
                !sourceBoard[to.x, to.y].HasValue)
            {
                return false;
            }

            SwapInPlace(sourceBoard, from, to);
            List<MatchResult> matches = FindMatches(sourceBoard);
            bool createsMatch = ContainsMatchAt(matches, from) || ContainsMatchAt(matches, to);
            SwapInPlace(sourceBoard, from, to);
            return createsMatch;
        }

        public static bool TryFindPossibleMove(PieceType?[,] sourceBoard, out Vector2Int from, out Vector2Int to)
        {
            int width = sourceBoard.GetLength(0);
            int height = sourceBoard.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int current = new Vector2Int(x, y);

                    if (x + 1 < width)
                    {
                        Vector2Int right = new Vector2Int(x + 1, y);

                        if (SwapCreatesMatch(sourceBoard, current, right))
                        {
                            from = current;
                            to = right;
                            return true;
                        }
                    }

                    if (y + 1 < height)
                    {
                        Vector2Int up = new Vector2Int(x, y + 1);

                        if (SwapCreatesMatch(sourceBoard, current, up))
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

        public static void CollapseAndFillTypes(PieceType?[,] sourceBoard, PieceType[] availableTypes, int? seed = null)
        {
            PieceType[] types = NormalizeTypes(availableTypes);
            System.Random random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            CollapseAndFillTypes(sourceBoard, types, random);
        }

        public static int ResolveBoardTypesForTesting(PieceType?[,] sourceBoard, PieceType[] availableTypes, int? seed, out int cascades)
        {
            Match3BoardModel model = new Match3BoardModel(sourceBoard.GetLength(0), sourceBoard.GetLength(1), availableTypes, seed);
            model.board = CopyBoard(sourceBoard);
            BoardResolveResult result = model.ResolveCascades();
            cascades = result.Cascades;
            CopyInto(model.board, sourceBoard);
            return result.Score;
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

        public static bool ContainsMatchAt(IEnumerable<MatchResult> matches, Vector2Int position)
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

        private bool CanSwap(Vector2Int from, Vector2Int to)
        {
            return IsInBounds(from.x, from.y, Width, Height) &&
                   IsInBounds(to.x, to.y, Width, Height) &&
                   AreAdjacent(from, to) &&
                   board[from.x, from.y].HasValue &&
                   board[to.x, to.y].HasValue;
        }

        private void CollapseAndFillInstance()
        {
            CollapseAndFillTypes(board, availableTypes, random);
            spawnStep++;
        }

        private static void CollapseAndFillTypes(PieceType?[,] sourceBoard, PieceType[] types, System.Random random)
        {
            int width = sourceBoard.GetLength(0);
            int height = sourceBoard.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                int writeY = 0;

                for (int y = 0; y < height; y++)
                {
                    if (sourceBoard[x, y].HasValue)
                    {
                        sourceBoard[x, writeY] = sourceBoard[x, y];

                        if (writeY != y)
                        {
                            sourceBoard[x, y] = null;
                        }

                        writeY++;
                    }
                }

                while (writeY < height)
                {
                    sourceBoard[x, writeY] = types[random.Next(types.Length)];
                    writeY++;
                }
            }
        }

        private static void AddLineMatch(List<MatchResult> results, int start, int end, int fixedCoordinate, bool horizontal)
        {
            int length = end - start;

            if (length < 3)
            {
                return;
            }

            List<Vector2Int> positions = new List<Vector2Int>();

            for (int i = start; i < end; i++)
            {
                positions.Add(horizontal ? new Vector2Int(i, fixedCoordinate) : new Vector2Int(fixedCoordinate, i));
            }

            results.Add(new MatchResult(positions, horizontal, !horizontal, false, false));
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

        private static PieceType PickSafeType(PieceType?[,] sourceBoard, int x, int y, PieceType[] types, System.Random random)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                PieceType candidate = types[random.Next(types.Length)];

                if (!WouldCreateImmediateMatch(sourceBoard, x, y, candidate))
                {
                    return candidate;
                }
            }

            for (int i = 0; i < types.Length; i++)
            {
                if (!WouldCreateImmediateMatch(sourceBoard, x, y, types[i]))
                {
                    return types[i];
                }
            }

            return types[0];
        }

        private static bool WouldCreateImmediateMatch(PieceType?[,] sourceBoard, int x, int y, PieceType type)
        {
            bool horizontal = x >= 2 &&
                              sourceBoard[x - 1, y].HasValue &&
                              sourceBoard[x - 2, y].HasValue &&
                              sourceBoard[x - 1, y].Value == type &&
                              sourceBoard[x - 2, y].Value == type;

            bool vertical = y >= 2 &&
                            sourceBoard[x, y - 1].HasValue &&
                            sourceBoard[x, y - 2].HasValue &&
                            sourceBoard[x, y - 1].Value == type &&
                            sourceBoard[x, y - 2].Value == type;

            return horizontal || vertical;
        }

        private static PieceType[,] BuildFallbackBoard(int width, int height, PieceType[] types)
        {
            PieceType[,] fallbackBoard = new PieceType[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    fallbackBoard[x, y] = types[(x + (y * 2)) % types.Length];
                }
            }

            fallbackBoard[0, 0] = types[0];
            fallbackBoard[1, 0] = types[1];
            fallbackBoard[2, 0] = types[0];
            fallbackBoard[1, 1] = types[0];
            return fallbackBoard;
        }

        private static PieceType[,] ToConcreteBoard(PieceType?[,] nullableBoard, PieceType fallback)
        {
            int width = nullableBoard.GetLength(0);
            int height = nullableBoard.GetLength(1);
            PieceType[,] concreteBoard = new PieceType[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    concreteBoard[x, y] = nullableBoard[x, y].HasValue ? nullableBoard[x, y].Value : fallback;
                }
            }

            return concreteBoard;
        }

        private static PieceType?[,] CopyBoard(PieceType?[,] sourceBoard)
        {
            int width = sourceBoard.GetLength(0);
            int height = sourceBoard.GetLength(1);
            PieceType?[,] copy = new PieceType?[width, height];
            CopyInto(sourceBoard, copy);
            return copy;
        }

        private static void CopyInto(PieceType?[,] sourceBoard, PieceType?[,] targetBoard)
        {
            int width = Mathf.Min(sourceBoard.GetLength(0), targetBoard.GetLength(0));
            int height = Mathf.Min(sourceBoard.GetLength(1), targetBoard.GetLength(1));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    targetBoard[x, y] = sourceBoard[x, y];
                }
            }
        }

        private static void SwapInPlace(PieceType?[,] sourceBoard, Vector2Int from, Vector2Int to)
        {
            PieceType? fromType = sourceBoard[from.x, from.y];
            sourceBoard[from.x, from.y] = sourceBoard[to.x, to.y];
            sourceBoard[to.x, to.y] = fromType;
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
