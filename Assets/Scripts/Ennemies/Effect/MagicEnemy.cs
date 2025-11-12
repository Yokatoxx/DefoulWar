using UnityEngine;
using FPS;

namespace Ennemies.Effect
{
    // Ennemi magique qui renvoie les tirs du joueur et soigne le joueur lorsqu'il est tué par un dash.

    [RequireComponent(typeof(EnemyHealth))]
    public class MagicEnemy : MonoBehaviour, IDamageInterceptor
    {
        
        [Tooltip("Mode de réflexion: true = Hitscan instantané, false = Projectile physique")]
        [SerializeField] private bool useHitscanReflect = false;
        
        [Tooltip("Vitesse du projectile magique renvoyé (ignoré si hitscan)")]
        [SerializeField] private float reflectedBulletSpeed = 30f;
        
        [Tooltip("Dégâts du projectile renvoyé au joueur")]
        [SerializeField] private float reflectedDamage = 15f;
        
        [Tooltip("Temps minimum entre deux réflexions (en secondes)")]
        [SerializeField] private float reflectCooldown = 0.15f;
        
        [Tooltip("Prefab du projectile magique renvoyé (si null, utilise Bullet.CreateBulletPrefab())")]
        [SerializeField] private GameObject magicBulletPrefab;
        
        [Tooltip("Effet visuel lors de la réflexion (optionnel)")]
        [SerializeField] private GameObject reflectEffectPrefab;
        
        [Tooltip("Durée de l'effet visuel de réflexion")]
        [SerializeField] private float reflectEffectDuration = 0.5f;
        
        [Header("Soin lors du dash")]
        [Tooltip("Points de vie rendus au joueur lorsqu'il tue cet ennemi avec un dash")]
        [SerializeField] private float healAmount = 30f;
        
        [Tooltip("Effet visuel de soin (optionnel)")]
        [SerializeField] private GameObject healEffectPrefab;
        
        [Tooltip("Durée de l'effet visuel de soin")]
        [SerializeField] private float healEffectDuration = 1f;
        
        [Header("Effet visuel de protection")]
        [Tooltip("Particules ou aura de protection magique (optionnel)")]
        [SerializeField] private GameObject magicShieldEffect;

        [SerializeField] private MagicEnemyHitScan hitScanFx;

        private EnemyHealth health;
        private float lastReflectTime = -999f;
        private GameObject cachedBulletPrefab;
        
        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.OnDeath.AddListener(OnDeath);
            }
        }
        
        private void Start()
        {
            // Activer l'effet de bouclier magique (si présent)
            if (magicShieldEffect != null)
            {
                magicShieldEffect.SetActive(true);
            }
            
            cachedBulletPrefab = magicBulletPrefab;
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
            // Vérifier si l'ennemi a été tué par un dash (centralisé via EnemyHealth)
            if (health != null && health.LastKillType == DamageType.Dash)
            {
                // Soigner le joueur
                HealPlayer();
            }
            
            // Désactiver l'effet de bouclier magique
            if (magicShieldEffect != null)
            {
                magicShieldEffect.SetActive(false);
            }
        }
        
        // IDamageInterceptor: intercepte les dégâts
        public bool OnBeforeDamage(ref DamageInfo damage)
        {
            // Autoriser le dash à passer (tue l'ennemi et soigne le joueur à la mort)
            if (damage.type == DamageType.Dash)
            {
                return true; // appliquer
            }
            
            // Bloquer les dégâts de balle et renvoyer vers le joueur
            if (damage.type == DamageType.Bullet)
            {
                // Cooldown
                if (Time.time - lastReflectTime < reflectCooldown)
                {
                    return false; // bloqué, pas de dégâts
                }
                lastReflectTime = Time.time;
                
                var player = FindFirstObjectByType<PlayerHealth>();
                if (player != null)
                {
                    if (useHitscanReflect)
                    {
                        ReflectHitscan(player);
                    }
                    else
                    {
                        CreateMagicBullet(player.transform.position);
                    }
                    // Effet visuel à l'impact si fourni
                    if (reflectEffectPrefab != null)
                    {
                        CreateReflectEffect(transform.position + Vector3.up * 1.5f);
                    }
                }
                
                return false; // on bloque le dégât d'origine
            }
            
            // Par défaut, laisser passer
            return true;
        }
        
        // Renvoie un tir hitscan instantané vers le joueur
        private void ReflectHitscan(PlayerHealth player)
        {
            player.TakeDamage(reflectedDamage);

            if (hitScanFx != null)
            {
                hitScanFx.FireTo(player.transform);
            }

            // Effet visuel de ligne/rayon entre l'ennemi et le joueur
            Debug.DrawLine(transform.position + Vector3.up * 1.5f, player.transform.position, 
                new Color(0.8f, 0.2f, 1f), 0.1f);
            
            Debug.Log("[MagicEnemy] Tir hitscan instantané renvoyé vers le joueur !");
        }
        
        // Crée un projectile magique
        private void CreateMagicBullet(Vector3 targetPosition)
        {
            if (cachedBulletPrefab == null)
            {
                Debug.LogWarning("[MagicEnemy] Aucun prefab de balle magique disponible !");
                return;
            }
            
            // Position de spawn
            Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;
            
            // Direction vers le joueur
            Vector3 directionToPlayer = (targetPosition - spawnPosition).normalized;
            
            // Créer le projectile
            GameObject magicBullet = Instantiate(
                cachedBulletPrefab,
                spawnPosition,
                Quaternion.LookRotation(directionToPlayer)
            );
            
            // Configurer le projectile
            var bulletScript = magicBullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(reflectedDamage, reflectedBulletSpeed);
            }
            else
            {
                // Si pas de script Bullet, utiliser directement le Rigidbody
                var rb = magicBullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = directionToPlayer * reflectedBulletSpeed;
                }
            }
        }
        
        // Soigne le joueur
        private void HealPlayer()
        {
            var player = FindFirstObjectByType<PlayerHealth>();
            if (player != null)
            {
                player.Heal(healAmount);
                Debug.Log($"[MagicEnemy] Le joueur a récupéré {healAmount} PV !");
                
                // Créer l'effet visuel de soin sur le joueur
                if (healEffectPrefab != null)
                {
                    CreateHealEffect(player.transform.position);
                }
            }
        }
        
        // Crée un effet visuel de réflexion
        private void CreateReflectEffect(Vector3 position)
        {
            GameObject effect = Instantiate(reflectEffectPrefab, position, Quaternion.identity);
            Destroy(effect, reflectEffectDuration);
        }
        
        // Crée un effet visuel de soin
        private void CreateHealEffect(Vector3 position)
        {
            GameObject effect = Instantiate(healEffectPrefab, position, Quaternion.identity);
            Destroy(effect, healEffectDuration);
        }
    }
}
