using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Outils de debug en jeu: tuer tous les ennemis.
    /// </summary>
    public class FPSDebugTools : MonoBehaviour
    {
        [Header("Hotkeys")]
        [SerializeField] private KeyCode killAllKey = KeyCode.F6;
        [SerializeField] private bool showButtons = true;
        
        private void Update()
        {
            if (Input.GetKeyDown(killAllKey))
            {
                KillAllEnemies();
            }
        }
        
        private void OnGUI()
        {
            if (!showButtons) return;
            const int w = 200;
            const int h = 28;
            int x = 10;
            int y = Screen.height - (h * 3 + 20);
            
            if (GUI.Button(new Rect(x, y, w, h), $"Kill All Enemies ({killAllKey})"))
            {
                KillAllEnemies();
            }
        }
        
        private void KillAllEnemies()
        {
            // Utiliser le registry au lieu de FindObjectsByType
            int count = 0;
            foreach (var enemy in EnemyRegistry.Instance.GetAliveEnemies())
            {
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.KillImmediate();
                    count++;
                }
            }
            Debug.Log($"[Debug] Killed {count} enemies.");
        }
    }
}
