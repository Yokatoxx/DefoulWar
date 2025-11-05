using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FPS
{
    // Gère la santé de l'ennemi avec zones de dégâts et tracking des hits.
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Health Settings")] [SerializeField]
        private float maxHealth = 100f;

        [SerializeField] private float currentHealth;

        [Header("Spawn Invulnerability")]
        [Tooltip("Durée d'invulnérabilité après l'apparition (secondes)")]
        [SerializeField]
        private float spawnInvulnerabilityDuration = 0f;

        [Tooltip(
            "Si vrai, invulnérable à tous les types de dégâts pendant l'invulnérabilité; sinon seulement aux balles")]
        [SerializeField]
        private bool spawnInvulnerableAllDamage = false;

        private float spawnInvulnerableUntil;

        [Header("Hit Tracking")] [SerializeField]
        private Dictionary<string, int> zoneHitCount = new Dictionary<string, int>();

        [Header("Events")] public UnityEvent OnDeath;
        public UnityEvent<float, string> OnDamageTaken;

        private bool isDead;
        private WaveManager waveManager;

        [SerializeField] private InstantiationEffect instantiationEffect;

        private bool canBeActive = true; //evite le spawn de trop de sphere
        [SerializeField] private bool kill;
        private bool isHitGun;
        public Vector3 hitPosition;
        

        private void Awake()
        {
            currentHealth = maxHealth;
            spawnInvulnerableUntil = Time.time + Mathf.Max(0f, spawnInvulnerabilityDuration);
            waveManager = FindFirstObjectByType<WaveManager>();
            instantiationEffect = instantiationEffect.GetComponent<InstantiationEffect>();
        }

        private void Update()
        {
            if (kill)
            {
                KillImmediate();
                kill = false;
            }


            if (!isDead)
            {
                canBeActive = true;
            }
            else if (isDead && canBeActive)
            {
                instantiationEffect.OnDeathEvent?.Invoke(transform.position);

                canBeActive = false;
            }

           

            if (isHitGun)
            {
                instantiationEffect.OnHitEvent?.Invoke(hitPosition); //faire avec un transform
                isHitGun = false;
            }
        }

        public void KilledByDashEvent()
        {
            instantiationEffect.OnDashedEvent?.Invoke(transform.position);
        }

        private void EnsureWaveManager()
        {
            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }
        }

        private bool IsSpawnInvulnerableFor(DamageType type)
        {
            if (Time.time < spawnInvulnerableUntil)
            {
                if (spawnInvulnerableAllDamage) return true;
                return type == DamageType.Bullet;
            }

            return false;
        }

        // Nouveau pipeline: tente d'appliquer un dégât détaillé. Retourne true si appliqué.
        public bool TryApplyDamage(DamageInfo info)
        {
            if (isDead) return false;
            isHitGun = true;
            // Intercepteurs (ex: MagicEnemy) — peuvent bloquer et déclencher des effets (renvoi)
            bool allow = true;
            var interceptors = GetComponents<IDamageInterceptor>();
            if (interceptors != null && interceptors.Length > 0)
            {
                for (int i = 0; i < interceptors.Length; i++)
                {
                    try
                    {
                        allow = interceptors[i].OnBeforeDamage(ref info) && allow;
                    }
                    catch
                    {
                    }
                }
            }

            if (!allow) return false;

            // Invulnérabilité de spawn propre (bloque sans "retirer puis remettre")
            if (IsSpawnInvulnerableFor(info.type))
            {
                return false;
            }

            // Appliquer
            float damage = Mathf.Max(0f, info.amount);
            string zoneName = string.IsNullOrWhiteSpace(info.zoneName) ? "Body" : info.zoneName;
            currentHealth -= damage;

            // Enregistrer le hit
            string key = NormalizeZoneKey(zoneName);
            if (!zoneHitCount.ContainsKey(key)) zoneHitCount[key] = 0;
            zoneHitCount[key]++;

            // Événement de dégâts pris (après application)
            OnDamageTaken?.Invoke(damage, zoneName);

            // Déclencher l'effet électrique si c'est un ennemi électrique mais seulement si les dégâts ne viennent pas déjà d'une décharge électrique
            if (info.type != DamageType.Electric)
            {
                var electricEnemy = GetComponent<Ennemies.Effect.ElectricEnnemis>();
                if (electricEnemy != null)
                {
                    electricEnemy.TriggerElectricDischarge();
                }
            }

            if (currentHealth <= 0)
            {
                Die();
            }

            return true;
        }

        // Compat: Inflige des dégâts à l'ennemi et enregistre la zone touchée.
        public void TakeDamage(float damage, string zoneName)
        {
            // Considéré comme dégât de balle par défaut
            var info = new DamageInfo(damage, zoneName, DamageType.Bullet);
            TryApplyDamage(info);
        }

        // Overload simple (zone par défaut)
        public void TakeDamage(float damage)
        {
            TakeDamage(damage, "Body");
        }

        // Nouveau: API directe avec DamageInfo
        public void TakeDamage(in DamageInfo info)
        {
            TryApplyDamage(info);
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            OnDeath?.Invoke();

            // Notifier le wave manager qu'un ennemi est mort
            EnsureWaveManager();
            if (waveManager != null)
            {
                waveManager.OnEnemyDeath(this);
            }

            Destroy(gameObject);
        }


        private static string NormalizeZoneKey(string zone)
        {
            return string.IsNullOrWhiteSpace(zone) ? string.Empty : zone.Trim().ToLowerInvariant();
        }

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public Dictionary<string, int> ZoneHitCount => zoneHitCount;


        // Tue immédiatement cet ennemi sans enregistrer de hit

        public void KillImmediate()
        {
            if (isDead) return;
            currentHealth = 0f;
            Die();
        }
    }
}