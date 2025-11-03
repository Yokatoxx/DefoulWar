// filepath: e:\Documents\Projet Unity\Proto3GD\Assets\Scripts\FPS\FPSDebugTools.cs
using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Outils de debug en jeu: tuer tous les ennemis, augmenter/réinitialiser l'armure.
    /// </summary>
    public class FPSDebugTools : MonoBehaviour
    {
        [Header("Hotkeys")]
        [SerializeField] private KeyCode killAllKey = KeyCode.F6;
        [SerializeField] private bool showButtons = true;
        
        private WaveManager waveManager;
        
        private void Awake()
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }
        
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
            var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (!e.IsDead)
                {
                    // Ne pas enregistrer de hit (sinon fausse les stats de la vague)
                    e.KillImmediate();
                }
            }
            Debug.Log($"[Debug] Killed {enemies.Length} enemies.");
            
            // Enchaîner la prochaine vague immédiatement avec progression d'armure
            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }
            if (waveManager != null)
            {
                waveManager.ForceNextWaveNow();
            }
        }
    }
}
