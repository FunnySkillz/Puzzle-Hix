using System;
using System.Reflection;
using NUnit.Framework;
using PuzzleDungeon.Gameplay;
using UnityEngine;

namespace PuzzleDungeon.Tests.EditMode
{
    /// <summary>
    /// Verifies LevelLoader validates layout data before producing a runtime board state.
    /// </summary>
    public class LevelLoaderTests
    {
        [Test]
        public void Load_Throws_WhenPlacementIsOutOfBounds()
        {
            LevelData levelData = CreateLevelData(
                width: 2,
                height: 2,
                placements: new[]
                {
                    CreatePlacement("tile_a", 3, 0)
                });

            LevelLoader loader = new LevelLoader();

            try
            {
                Assert.Throws<InvalidOperationException>(() => loader.Load(levelData));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(levelData);
            }
        }

        [Test]
        public void Load_Throws_WhenMultipleTilesShareCell()
        {
            LevelData levelData = CreateLevelData(
                width: 2,
                height: 2,
                placements: new[]
                {
                    CreatePlacement("tile_a", 1, 1),
                    CreatePlacement("tile_b", 1, 1)
                });

            LevelLoader loader = new LevelLoader();

            try
            {
                Assert.Throws<InvalidOperationException>(() => loader.Load(levelData));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(levelData);
            }
        }

        private static LevelData CreateLevelData(int width, int height, LevelData.TilePlacement[] placements)
        {
            LevelData levelData = ScriptableObject.CreateInstance<LevelData>();

            SetPrivateField(levelData, "boardWidth", width);
            SetPrivateField(levelData, "boardHeight", height);
            SetPrivateField(levelData, "moveLimit", 10);
            SetPrivateField(levelData, "goalTileId", "goal");
            SetPrivateField(levelData, "goalX", 0);
            SetPrivateField(levelData, "goalY", 0);
            SetPrivateField(levelData, "initialTileLayout", placements);

            return levelData;
        }

        private static LevelData.TilePlacement CreatePlacement(string tileId, int x, int y)
        {
            LevelData.TilePlacement placement = default;

            SetPrivateStructField(ref placement, "tileId", tileId);
            SetPrivateStructField(ref placement, "x", x);
            SetPrivateStructField(ref placement, "y", y);

            return placement;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            field.SetValue(target, value);
        }

        private static void SetPrivateStructField<TStruct>(ref TStruct target, string fieldName, object value)
            where TStruct : struct
        {
            object boxed = target;
            FieldInfo field = boxed.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                throw new MissingFieldException(boxed.GetType().Name, fieldName);
            }

            field.SetValue(boxed, value);
            target = (TStruct)boxed;
        }
    }
}
