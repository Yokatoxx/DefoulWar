using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Affiche des informations de debug à l'écran pour tester le système.
    /// </summary>
    public class FPSDebugDisplay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        
        private WaveManager waveManager;
        private PlayerHealth playerHealth;
        private WeaponController weaponController;
        
        private void Awake()
        {
            waveManager = FindFirstObjectByType<WaveManager>();
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            weaponController = FindFirstObjectByType<WeaponController>();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showDebugInfo = !showDebugInfo;
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            int yOffset = 10;
            int lineHeight = 20;
            
            // Style
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            
            // Fond semi-transparent
            GUI.Box(new Rect(10, 10, 360, 320), "");
            
            // FPS
            float fps = 1f / Time.deltaTime;
            GUI.Label(new Rect(20, yOffset, 340, lineHeight), $"FPS: {fps:F0}", style);
            yOffset += lineHeight;
            
            // Informations joueur
            if (playerHealth != null)
            {
                GUI.Label(new Rect(20, yOffset, 340, lineHeight), 
                    $"Health: {playerHealth.CurrentHealth:F0}/{playerHealth.MaxHealth}", style);
                yOffset += lineHeight;
            }
            
            if (weaponController != null)
            {
                GUI.Label(new Rect(20, yOffset, 340, lineHeight), 
                    $"Ammo: {weaponController.CurrentAmmo}/{weaponController.MaxAmmo}", style);
                yOffset += lineHeight;
            }
            
            yOffset += 10;
            
            // Informations de vague
            if (waveManager != null)
            {
                GUI.Label(new Rect(20, yOffset, 340, lineHeight), 
                    $"Wave: {waveManager.CurrentWave}", style);
                yOffset += lineHeight;
                
                GUI.Label(new Rect(20, yOffset, 340, lineHeight), 
                    $"Enemies: {waveManager.EnemiesRemaining}/{waveManager.TotalEnemiesInWave}", style);
                yOffset += lineHeight;
                
                yOffset += 10;
                
                // Statistiques de hits
                GUI.Label(new Rect(20, yOffset, 340, lineHeight), "Hit Statistics:", style);
                yOffset += lineHeight;
                
                var hits = waveManager.CurrentWaveHits;
                foreach (var kvp in hits)
                {
                    GUI.Label(new Rect(30, yOffset, 330, lineHeight), 
                        $"  {kvp.Key}: {kvp.Value} hits", style);
                    yOffset += lineHeight;
                }
                
                yOffset += 10;
                GUI.Label(new Rect(20, yOffset, 340, lineHeight), "Armor Levels (persistent):", style);
                yOffset += lineHeight;
                var armor = waveManager.ArmorLevels;
                if (armor != null && armor.Count > 0)
                {
                    foreach (var kvp in armor)
                    {
                        GUI.Label(new Rect(30, yOffset, 330, lineHeight), $"  {kvp.Key}: L{kvp.Value}", style);
                        yOffset += lineHeight;
                    }
                }
                else
                {
                    GUI.Label(new Rect(30, yOffset, 330, lineHeight), "  (none)", style);
                    yOffset += lineHeight;
                }
            }
            
            // Instructions
            yOffset += 10;
            style.fontSize = 12;
            GUI.Label(new Rect(20, yOffset, 340, lineHeight), 
                $"Press {toggleKey} to toggle debug info", style);
        }
    }
}
