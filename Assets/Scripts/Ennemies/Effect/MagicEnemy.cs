using UnityEngine;
using System.Collections;
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
        
        [Header("Délai de réflexion")]
        [Tooltip("Délai avant le renvoi de la balle (avec ligne de prédiction verte)")]
        [SerializeField] private float reflectDelay = 0.5f;
        
        [Tooltip("Prefab du projectile magique renvoyé (si null, utilise Bullet.CreateBulletPrefab())")]
        [SerializeField] private GameObject magicBulletPrefab;
        
        [Tooltip("Effet visuel lors de la réflexion (optionnel)")]
        [SerializeField] private GameObject reflectEffectPrefab;
        
        [Tooltip("Durée de l'effet visuel de réflexion")]
        [SerializeField] private float reflectEffectDuration = 0.5f;
        
        [Header("Munitions lors du dash")]
        [Tooltip("Munitions rendues au joueur lorsqu'il tue cet ennemi avec un dash")]
        [SerializeField] private int ammoAmount = 10;
        
        [Header("Effet visuel de protection")]
        [Tooltip("Particules ou aura de protection magique (optionnel)")]
        [SerializeField] private GameObject magicShieldEffect;

        [SerializeField] private MagicEnemyHitScan hitScanFx;

        private EnemyHealth health;
        private float lastReflectTime = -999f;
        private GameObject cachedBulletPrefab;
        
        private LineRenderer laserLine;
        private Coroutine pendingReflectCoroutine;
        private bool isReflecting = false;
        
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
            
            CreateLaserLine();
        }
        
        private void CreateLaserLine()
        {
            GameObject laserObj = new GameObject("MagicReflectLaser");
            laserObj.transform.SetParent(transform);
            laserLine = laserObj.AddComponent<LineRenderer>();
            laserLine.startWidth = 0.05f;
            laserLine.endWidth = 0.05f;
            laserLine.positionCount = 2;
            laserLine.material = new Material(Shader.Find("Sprites/Default"));
            laserLine.startColor = Color.green;
            laserLine.endColor = Color.green;
            laserLine.enabled = false;
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
                // Donner des munitions au joueur
                GiveAmmoToPlayer();
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
            
            // Bloquer les dégâts de balle et renvoyer vers le joueur avec délai
            if (damage.type == DamageType.Bullet)
            {
                // Cooldown et vérifier si pas déjà en train de réfléchir
                if (Time.time - lastReflectTime < reflectCooldown || isReflecting)
                {
                    return false; // bloqué, pas de dégâts
                }
                lastReflectTime = Time.time;
                
                var player = FindFirstObjectByType<PlayerHealth>();
                if (player != null)
                {
                    pendingReflectCoroutine = StartCoroutine(ReflectWithDelay(player));
                }
                
                return false; // on bloque le dégât d'origine
            }
            
            // Par défaut, laisser passer
            return true;
        }
        
        // Coroutine de réflexion avec délai et ligne de prédiction verte
        private IEnumerator ReflectWithDelay(PlayerHealth player)
        {
            isReflecting = true;
            Vector3 shootOrigin = transform.position + Vector3.up * 1.5f;
            
            // Calculer la position prédite (verrouillée au moment du tir)
            Vector3 lockedTargetPosition;
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // Prédiction basée sur la vélocité du joueur
                lockedTargetPosition = player.transform.position + playerRb.linearVelocity * reflectDelay + Vector3.up;
            }
            else
            {
                lockedTargetPosition = player.transform.position + Vector3.up;
            }
            
            // Afficher la ligne verte vers la position prédite
            if (laserLine != null)
            {
                laserLine.enabled = true;
            }
            
            float timer = reflectDelay;
            while (timer > 0f)
            {
                shootOrigin = transform.position + Vector3.up * 1.5f;
                
                // Ligne fixe vers la position verrouillée (ne suit PAS le joueur)
                if (laserLine != null)
                {
                    laserLine.SetPosition(0, shootOrigin);
                    laserLine.SetPosition(1, lockedTargetPosition);
                }
                
                timer -= Time.deltaTime;
                yield return null;
            }
            
            // Désactiver la ligne et tirer vers la position prédite
            if (laserLine != null) laserLine.enabled = false;
            
            if (player != null)
            {
                if (useHitscanReflect)
                {
                    ReflectHitscanToPosition(lockedTargetPosition, player);
                }
                else
                {
                    CreateMagicBullet(lockedTargetPosition);
                }
                
                if (reflectEffectPrefab != null)
                {
                    CreateReflectEffect(transform.position + Vector3.up * 1.5f);
                }
            }
            
            isReflecting = false;
        }
        
        // Renvoie un tir hitscan instantané vers le joueur
        private void ReflectHitscan(PlayerHealth player)
        {
            player.TakeDamage(reflectedDamage, transform.position);

            if (hitScanFx != null)
            {
                hitScanFx.FireTo(player.transform);
            }

            // Effet visuel de ligne/rayon entre l'ennemi et le joueur
            Debug.DrawLine(transform.position + Vector3.up * 1.5f, player.transform.position, 
                new Color(0.8f, 0.2f, 1f), 0.1f);
            
            Debug.Log("[MagicEnemy] Tir hitscan instantané renvoyé vers le joueur !");
        }
        
        // Hitscan vers une position prédite (le joueur peut esquiver)
        private void ReflectHitscanToPosition(Vector3 targetPosition, PlayerHealth player)
        {
            // Vérifier si le joueur est proche de la position prédite
            float hitRadius = 1.5f;
            float distanceToTarget = Vector3.Distance(player.transform.position + Vector3.up, targetPosition);
            
            if (distanceToTarget <= hitRadius)
            {
                player.TakeDamage(reflectedDamage, transform.position);
                Debug.Log("[MagicEnemy] Hitscan prédit touche le joueur !");
            }
            else
            {
                Debug.Log($"[MagicEnemy] Hitscan prédit manqué ! Distance: {distanceToTarget:F2}m");
            }

            // Effet visuel vers la position prédite (pas le joueur actuel)
            if (hitScanFx != null)
            {
                // Créer un point temporaire pour l'effet
                GameObject tempTarget = new GameObject("TempHitscanTarget");
                tempTarget.transform.position = targetPosition;
                hitScanFx.FireTo(tempTarget.transform);
                Destroy(tempTarget, 0.5f);
            }

            Debug.DrawLine(transform.position + Vector3.up * 1.5f, targetPosition, 
                new Color(0.8f, 0.2f, 1f), 0.5f);
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
        
        // Donne des munitions au joueur
        private void GiveAmmoToPlayer()
        {
            var playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null) return;
            
            var weaponSystem = playerHealth.GetComponentInChildren<WeaponSystem>();
            if (weaponSystem == null) return;
            
            if (ammoAmount > 0)
            {
                weaponSystem.AddAmmo(ammoAmount);
            }
        }
        
        // Crée un effet visuel de réflexion
        private void CreateReflectEffect(Vector3 position)
        {
            GameObject effect = Instantiate(reflectEffectPrefab, position, Quaternion.identity);
            Destroy(effect, reflectEffectDuration);
        }
    }
}
