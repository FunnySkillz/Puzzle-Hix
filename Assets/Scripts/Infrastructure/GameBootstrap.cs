using UnityEngine;

namespace PuzzleDungeon.Infrastructure
{
    /// <summary>
    /// Optional scene bootstrap entry point reserved for cross-scene wiring; active gameplay starts in the scene controller.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("GameBootstrap is active. Gameplay runtime is managed by the active scene controller.");
        }
    }
}
