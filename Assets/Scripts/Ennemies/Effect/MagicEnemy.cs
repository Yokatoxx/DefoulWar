using UnityEngine;
using FPS;

namespace Ennemies.Effect
{
    // Ennemi magique qui renvoie les tirs du joueur et soigne le joueur lorsqu'il est tué par un dash.
    [RequireComponent(typeof(EnemyHealth))]
    public class MagicEnemy : MonoBehaviour, IDamageInterceptor
    {
        [Header("Réflexion")]
        [Tooltip("true = Hitscan instantané, false = projectile physique")]
        [SerializeField] private bool useHitscanReflect = false;

        [Tooltip("Dégâts renvoyés au joueur")]
        [SerializeField] private float reflectedDamage = 15f;

        [Tooltip("Temps minimum entre deux réflexions (s)")]
        [SerializeField] private float reflectCooldown = 0.15f;

        [Header("Projectile (si non-hitscan)")]
        [Tooltip("Vitesse du projectile magique renvoyé")]
        [SerializeField] private float reflectedBulletSpeed = 30f;
        [Tooltip("Prefab du projectile magique renvoyé")]
        [SerializeField] private GameObject magicBulletPrefab;

        [Header("FX Réflexion")]
        [Tooltip("Effet visuel sur l’ennemi au moment de la réflexion (optionnel)")]
        [SerializeField] private GameObject reflectEffectPrefab;
        [SerializeField] private float reflectEffectDuration = 0.5f;

        [Tooltip("FX de hitscan (trace TrailRenderer). Optionnel mais recommandé en mode hitscan.")]
        [SerializeField] private MagicEnemyHitScan hitScanFx;

        [Header("Soin lors du dash")]
        [Tooltip("PV rendus au joueur si l’ennemi est tué par un dash")]
        [SerializeField] private float healAmount = 30f;
        [Tooltip("Effet visuel de soin (optionnel)")]
        [SerializeField] private GameObject healEffectPrefab;
        [SerializeField] private float healEffectDuration = 1f;

        [Header("Effet visuel de protection")]
        [Tooltip("Particules/aura de protection magique (optionnel)")]
        [SerializeField] private GameObject magicShieldEffect;

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
            if (magicShieldEffect != null)
                magicShieldEffect.SetActive(true);

            cachedBulletPrefab = magicBulletPrefab;

            if (hitScanFx == null)
                hitScanFx = GetComponent<MagicEnemyHitScan>(); // auto-récup si présent
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath.RemoveListener(OnDeath);
        }

        private void OnDeath()
        {
            var dashSystem = FindFirstObjectByType<DashSystem>();
            if (dashSystem != null && DashSystem.WasKilledByDash(transform.root.gameObject))
            {
                HealPlayer();
            }

            if (magicShieldEffect != null)
                magicShieldEffect.SetActive(false);
        }

        // IDamageInterceptor: intercepte les dégâts avant application
        public bool OnBeforeDamage(ref DamageInfo damage)
        {
            // Laisser passer les dégâts de dash
            if (damage.type == DamageType.Dash)
                return true;

            // Bloquer les balles et renvoyer
            if (damage.type == DamageType.Bullet)
            {
                if (Time.time - lastReflectTime < reflectCooldown)
                    return false; // bloqué

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

                    if (reflectEffectPrefab != null)
                        CreateReflectEffect(transform.position + Vector3.up * 1.5f);
                }

                return false; // on bloque le dégât d'origine
            }

            // Autres types: laisser passer
            return true;
        }

        // Renvoie un tir hitscan instantané vers le joueur + FX trail
        private void ReflectHitscan(PlayerHealth player)
        {
            // Appliquer le dégât instantané
            player.TakeDamage(reflectedDamage);

            // FX visuel de hitscan
            if (hitScanFx != null)
            {
                hitScanFx.FireTo(player.transform);
            }
            else
            {
                // Fallback debug
                Debug.DrawLine(transform.position + Vector3.up * 1.5f, player.transform.position,
                    new Color(0.8f, 0.2f, 1f), 0.1f);
            }

            Debug.Log("[MagicEnemy] Tir hitscan renvoyé vers le joueur !");
        }

        // Crée un projectile magique
        private void CreateMagicBullet(Vector3 targetPosition)
        {
            if (cachedBulletPrefab == null)
            {
                Debug.LogWarning("[MagicEnemy] Aucun prefab de projectile magique assigné.");
                return;
            }

            Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;
            Vector3 directionToPlayer = (targetPosition - spawnPosition).normalized;

            GameObject magicBullet = Instantiate(
                cachedBulletPrefab,
                spawnPosition,
                Quaternion.LookRotation(directionToPlayer)
            );

            var bulletScript = magicBullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(reflectedDamage, reflectedBulletSpeed);
            }
            else
            {
                var rb = magicBullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = directionToPlayer * reflectedBulletSpeed;
                }
            }
        }

        // Soigne le joueur (lorsque tué par dash)
        private void HealPlayer()
        {
            var player = FindFirstObjectByType<PlayerHealth>();
            if (player != null)
            {
                player.Heal(healAmount);
                Debug.Log($"[MagicEnemy] Le joueur a récupéré {healAmount} PV !");
                if (healEffectPrefab != null)
                    CreateHealEffect(player.transform.position);
            }
        }

        private void CreateReflectEffect(Vector3 position)
        {
            GameObject effect = Instantiate(reflectEffectPrefab, position, Quaternion.identity);
            Destroy(effect, reflectEffectDuration);
        }

        private void CreateHealEffect(Vector3 position)
        {
            GameObject effect = Instantiate(healEffectPrefab, position, Quaternion.identity);
            Destroy(effect, healEffectDuration);
        }
    }
}