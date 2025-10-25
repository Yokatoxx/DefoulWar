using System.Collections.Generic;
using UnityEngine;

namespace Proto3GD.FPS
{

    // Synchronise automatiquement l'armure des zones de cet ennemi avec les niveaux du WaveManager
    // au démarrage et à chaque début de vague.

    [DisallowMultipleComponent]
    public class EnemyArmorSync : MonoBehaviour
    {
        private EnemyHealth enemyHealth;
        private WaveManager waveManager;
        
        private void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = GetComponentInChildren<EnemyHealth>();
            }
        }
        
        private void OnEnable()
        {
            TryFindWaveManager();
            Subscribe();
            ApplyCurrentArmor();
        }
        
        private void OnDisable()
        {
            Unsubscribe();
        }
        
        private void TryFindWaveManager()
        {
            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }
        }
        
        private void Subscribe()
        {
            if (waveManager != null)
            {
                waveManager.onWaveStart.AddListener(OnWaveStart);
            }
        }
        
        private void Unsubscribe()
        {
            if (waveManager != null)
            {
                waveManager.onWaveStart.RemoveListener(OnWaveStart);
            }
        }
        
        private void OnWaveStart(int wave)
        {
            ApplyCurrentArmor();
        }
        
        private void ApplyCurrentArmor()
        {
            if (enemyHealth == null)
                return;
            
            TryFindWaveManager();
            if (waveManager == null)
                return;
            
            
            var dict = new Dictionary<string, int>();
            foreach (var kv in waveManager.ArmorLevels)
            {
                dict[kv.Key] = kv.Value;
            }
            enemyHealth.ApplyArmorLevels(dict);
        }
    }
}

