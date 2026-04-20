using UnityEngine;

namespace PuzzleDungeon.Infrastructure
{
    /// <summary>
    /// Legacy bootstrap placeholder. The playable board scene now uses GameController directly.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Debug.LogWarning("GameBootstrap is deprecated. Use GameController in the PuzzleBoard scene as the runtime entry point.");
        }
    }
}
