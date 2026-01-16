using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FPS
{
    // Gère la santé de l'ennemi avec zones de dégâts et tracking des hits.
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Damage Type Modifiers")]
        [Tooltip("Multiplicateur de dégâts des balles (0.5 = prend 2x moins de dégâts)")]
        [SerializeField] private float bulletDamageMultiplier = 1f;
        [Tooltip("Multiplicateur de dégâts du dash (0.33 = prend 3x moins de dégâts)")]
        [SerializeField] private float dashDamageMultiplier = 1f;
        [Tooltip("Multiplicateur de dégâts électriques")]
        [SerializeField] private float electricDamageMultiplier = 1f;

        [Header("Spawn Invulnerability")]
        [Tooltip("Durée d'invulnérabilité après l'apparition (secondes)")]
        [SerializeField] private float spawnInvulnerabilityDuration = 0f;
        [Tooltip("Si vrai, invulnérable à tous les types de dégâts pendant l'invulnérabilité; sinon seulement aux balles")]
        [SerializeField] private bool spawnInvulnerableAllDamage = false;
        private float spawnInvulnerableUntil;

        [Header("Hit Tracking")]
        [SerializeField] private Dictionary<string, int> zoneHitCount = new Dictionary<string, int>();

        [Header("Events")]
        public UnityEvent OnDeath;
        public UnityEvent<float, string> OnDamageTaken;

        private bool isDead;

        // Centralisation: suivi de l'origine des dégâts/kill
        private DamageType lastHitType = DamageType.Other;
        private DamageType lastKillType = DamageType.Other;
        private Transform lastAttacker;

        private void Awake()
        {
            currentHealth = maxHealth;
            spawnInvulnerableUntil = Time.time + Mathf.Max(0f, spawnInvulnerabilityDuration);
            
            // S'enregistrer dans le registry
            EnemyRegistry.Instance.Register(this);
        }

        private void OnDestroy()
        {
            // Se désenregistrer du registry
            if (EnemyRegistry.Instance != null)
            {
                EnemyRegistry.Instance.Unregister(this);
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
            // Intercepteurs (ex: MagicEnemy) — peuvent bloquer et déclencher des effets (renvoi)
            bool allow = true;
            // Récupérer les intercepteurs présents sur cet objet ET ses enfants (pour inclure EnemyShield sur un child)
            var interceptors = GetComponentsInChildren<IDamageInterceptor>();
            if (interceptors != null && interceptors.Length > 0)
            {
                for (int i = 0; i < interceptors.Length; i++)
                {
                    try { allow = interceptors[i].OnBeforeDamage(ref info) && allow; } catch { }
                }
            }

            // Si un intercepteur a bloqué l'application
            if (!allow)
            {
                // Mais s'il demande que l'attaque soit comptée comme un hit (ex: rebond de dash), appliquer les effets nécessaires sans enlever de vie
                if (info.countAsHit)
                {
                    // Enregistrer la source du dernier coup
                    lastHitType = info.type;
                    lastAttacker = info.attacker;

                    // Enregistrer le hit statistique (zone)
                    string hitZoneNameBlocked = string.IsNullOrWhiteSpace(info.zoneName) ? "Body" : info.zoneName;
                    string zoneKeyBlocked = NormalizeZoneKey(hitZoneNameBlocked);
                    if (!zoneHitCount.ContainsKey(zoneKeyBlocked)) zoneHitCount[zoneKeyBlocked] = 0;
                    zoneHitCount[zoneKeyBlocked]++;

                    // Déclencher les callbacks/effets associés à un hit sans dégâts
                    OnDamageTaken?.Invoke(0f, hitZoneNameBlocked);

                    // Si c'est un dash, appliquer le knockback/effets de dash
                    if (info.type == DamageType.Dash)
                    {
                        TryApplyDashKnockback(info);
                    }

                    return true; // considérer comme appliqué (pour que DashCible sache que le dash a touché)
                }

                return false;
            }

            // Invulnérabilité de spawn propre (bloque sans "retirer puis remettre")
            if (IsSpawnInvulnerableFor(info.type))
            {
                return false;
            }

            // Appliquer le multiplicateur de dégâts selon le type
            float damage = Mathf.Max(0f, info.amount * GetDamageMultiplier(info.type));
            string zoneName = string.IsNullOrWhiteSpace(info.zoneName) ? "Body" : info.zoneName;
            currentHealth -= damage;

            // Centralisation: enregistrer la source du dernier coup
            lastHitType = info.type;
            lastAttacker = info.attacker;

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

            // Appliquer le knockback si c'est un dash et que l'ennemi ne résiste pas
            if (info.type == DamageType.Dash)
            {
                TryApplyDashKnockback(info);
            }

            if (currentHealth <= 0)
            {
                // Centralisation: consigner le type de kill avant la mort
                lastKillType = info.type;
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

        // Centralisation: exposer la cause de mort et la source
        public DamageType LastHitType => lastHitType;
        public DamageType LastKillType => lastKillType;
        public Transform LastAttacker => lastAttacker;

        /// <summary>
        /// Retourne le multiplicateur de dégâts pour un type donné.
        /// </summary>
        private float GetDamageMultiplier(DamageType type)
        {
            return type switch
            {
                DamageType.Bullet => bulletDamageMultiplier,
                DamageType.Dash => dashDamageMultiplier,
                DamageType.Melee => dashDamageMultiplier, // Melee = Dash dans ce jeu
                DamageType.Electric => electricDamageMultiplier,
                _ => 1f
            };
        }

        // Tue immédiatement cet ennemi sans enregistrer de hit
        public void KillImmediate()
        {
            if (isDead) return;
            currentHealth = 0f;
            lastKillType = DamageType.Other;
            Die();
        }

        /// <summary>
        /// Tente d'appliquer le knockback lors d'un dash si l'ennemi n'y résiste pas.
        /// </summary>
        private void TryApplyDashKnockback(DamageInfo info)
        {
            // Vérifier si l'ennemi résiste au dash (ex: ElectricEnnemis avec ResistToDash)
            var electricEnemy = GetComponent<Ennemies.Effect.ElectricEnnemis>();
            if (electricEnemy != null && electricEnemy.ResistToDash)
            {
                return;
            }

            // Récupérer ou créer le composant de knockback
            var knockback = GetComponent<EnemyKnockback>();
            if (knockback == null)
            {
                // Auto-ajouter le Rigidbody s'il n'existe pas
                var rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                }

                // Auto-ajouter le composant EnemyKnockback
                knockback = gameObject.AddComponent<EnemyKnockback>();
            }

            if (knockback.ResistToKnockback)
            {
                return;
            }

            // Calculer la direction du knockback (depuis l'attaquant vers l'ennemi)
            Vector3 knockbackDirection;
            if (info.attacker != null)
            {
                knockbackDirection = (transform.position - info.attacker.position).normalized;
            }
            else if (info.hitNormal != Vector3.zero)
            {
                // Utiliser l'inverse de la normale de hit comme direction
                knockbackDirection = -info.hitNormal.normalized;
            }
            else
            {
                // Fallback: direction par défaut (vers l'arrière de l'ennemi)
                knockbackDirection = -transform.forward;
            }

            // Récupérer les paramètres de knockback depuis le DashCible du joueur
            float force = 15f;      // Valeur par défaut
            float duration = 0.5f;  // Valeur par défaut
            bool affectsY = false;  // Valeur par défaut

            // Essayer de récupérer les paramètres du DashDefinition via le joueur
            if (info.attacker != null)
            {
                var dashCible = info.attacker.GetComponent<DashCible>();
                if (dashCible != null)
                {
                    var dashDef = GetDashDefinitionFromPlayer(dashCible);
                    if (dashDef != null)
                    {
                        force = dashDef.knockbackForce;
                        duration = dashDef.knockbackDuration;
                        affectsY = dashDef.knockbackAffectsYAxis;
                    }
                }
            }

            // Appliquer le knockback
            knockback.ApplyKnockback(knockbackDirection, force, duration, affectsY);
        }

        /// <summary>
        /// Récupère le DashDefinition depuis le DashCible via réflexion (pour éviter de modifier DashCible).
        /// </summary>
        private DashDefinition GetDashDefinitionFromPlayer(DashCible dashCible)
        {
            // Utiliser la réflexion pour accéder au champ privé dashDefinition
            var field = typeof(DashCible).GetField("dashDefinition",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(dashCible) as DashDefinition;
            }
            return null;
        }

        public void Heal(float amount)
        {
            if (isDead) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
    }
}
