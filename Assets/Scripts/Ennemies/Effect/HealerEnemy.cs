using UnityEngine;
using FPS;

namespace Ennemies.Effect
{
    /// <summary>
    /// Ennemi Healer qui soigne le joueur lorsqu'il est tué par un dash.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class HealerEnemy : MonoBehaviour
    {
        [Header("Soin lors du dash")]
        [Tooltip("Points de vie rendus au joueur lorsqu'il tue cet ennemi avec un dash")]
        [SerializeField] private float healAmount = 30f;
        
        [Tooltip("Effet visuel de soin (optionnel)")]
        [SerializeField] private GameObject healEffectPrefab;
        
        [Tooltip("Durée de l'effet visuel de soin")]
        [SerializeField] private float healEffectDuration = 1f;

        private EnemyHealth health;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.OnDeath.AddListener(OnDeath);
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath.RemoveListener(OnDeath);
            }
        }

        private void OnDeath()
        {
            // Vérifier si l'ennemi a été tué par un dash
            if (health != null && health.LastKillType == DamageType.Dash)
            {
                HealPlayer();
            }
        }

        private void HealPlayer()
        {
            var player = FindFirstObjectByType<PlayerHealth>();
            if (player != null)
            {
                player.Heal(healAmount);
                Debug.Log($"[HealerEnemy] Le joueur a récupéré {healAmount} PV !");
                
                // Créer l'effet visuel de soin sur le joueur
                if (healEffectPrefab != null)
                {
                    GameObject effect = Instantiate(healEffectPrefab, player.transform.position, Quaternion.identity);
                    Destroy(effect, healEffectDuration);
                }
            }
        }
    }
}
