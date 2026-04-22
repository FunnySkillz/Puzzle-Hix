using UnityEngine;

namespace PuzzleDungeon.Infrastructure
{
    /// <summary>
    /// Optional scene bootstrap entry point reserved for cross-scene wiring; gameplay currently starts via GameController.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("GameBootstrap is active. Gameplay runtime is currently managed by GameController in PuzzleBoard.");
        }
    }
}
