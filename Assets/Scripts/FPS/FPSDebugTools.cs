// filepath: e:\Documents\Projet Unity\Proto3GD\Assets\Scripts\FPS\FPSDebugTools.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Outils de debug en jeu: tuer tous les ennemis, augmenter/réinitialiser l'armure.
    /// </summary>
    public class FPSDebugTools : MonoBehaviour
    {
        [Header("Hotkeys")]
        [SerializeField] private KeyCode killAllKey = KeyCode.F6;
        [SerializeField] private KeyCode armorLevelUpKey = KeyCode.F7;
        [SerializeField] private KeyCode armorResetKey = KeyCode.F8;
        [SerializeField] private bool showButtons = true;
        
        [Header("Armor Settings")]
        [SerializeField] private int armorDeltaPerPress = 1; // +1 niveau par pression
        
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
            if (Input.GetKeyDown(armorLevelUpKey))
            {
                IncreaseArmorForAllZones(armorDeltaPerPress);
            }
            if (Input.GetKeyDown(armorResetKey))
            {
                ResetArmorLevels();
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
            y += h + 4;
            if (GUI.Button(new Rect(x, y, w, h), $"Armor +{armorDeltaPerPress} All Zones ({armorLevelUpKey})"))
            {
                IncreaseArmorForAllZones(armorDeltaPerPress);
            }
            y += h + 4;
            if (GUI.Button(new Rect(x, y, w, h), $"Reset Armor ({armorResetKey})"))
            {
                ResetArmorLevels();
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
        
        private void IncreaseArmorForAllZones(int delta)
        {
            if (waveManager == null) return;
            // Collecter la liste des noms de zones depuis les ennemis actifs
            HashSet<string> zones = new HashSet<string>();
            var hitZones = FindObjectsByType<HitZone>(FindObjectsSortMode.None);
            foreach (var z in hitZones)
            {
                if (!string.IsNullOrEmpty(z.ZoneName))
                {
                    zones.Add(z.ZoneName);
                }
            }
            if (zones.Count == 0) return;
            
            // Construire un dictionnaire des niveaux actuels + delta et persister
            var deltas = new Dictionary<string, int>();
            foreach (var z in zones)
            {
                deltas[z] = delta;
            }
            waveManager.IncreaseArmorLevels(deltas);
            waveManager.ReapplyArmorToActiveEnemies();
            
            Debug.Log($"[Debug] Increased persistent armor by +{delta} for zones: {string.Join(", ", zones)}");
        }
        
        private void ResetArmorLevels()
        {
            if (waveManager == null) return;
            // Réinitialiser dans le WaveManager et sur les ennemis présents
            waveManager.ResetAllArmorLevels();
            waveManager.ReapplyArmorToActiveEnemies();
            Debug.Log("[Debug] Reset persistent armor levels and reapplied (no armor). ");
        }
    }
}
