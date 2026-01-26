using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace FPS
{
    /// <summary>
    /// Gère l'esquive des balles pour les ennemis.
    /// L'ennemi peut effectuer un bond latéral pour éviter les tirs du joueur.
    /// Si l'esquive réussit, les dégâts du tir sont annulés.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyDodge : MonoBehaviour
    {
        [Header("Override Settings (optionnel)")]
        [Tooltip("Si null, utilise les settings de EnemyBehaviour")]
        [SerializeField] private EnemyBehaviorSettings overrideSettings;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;
        
        private Rigidbody rb;
        private NavMeshAgent agent;
        private EnemyHealth enemyHealth;
        private EnemyKnockback knockback;
        private EnemyBehaviorSettings settings;
        
        private bool isDodging;
        private float lastDodgeTime = -100f;
        private Coroutine dodgeCoroutine;
        
        // Flag pour indiquer qu'on vient de lancer une esquive (pour annuler les dégâts)
        private bool justStartedDodge;
        
        // Buffer pour le raycast
        private static readonly RaycastHit[] WallCheckBuffer = new RaycastHit[4];
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            enemyHealth = GetComponent<EnemyHealth>();
            knockback = GetComponent<EnemyKnockback>();
            
            if (overrideSettings == null)
            {
                var behaviour = GetComponent<Ennemies.EnemyBehaviour>();
                if (behaviour != null)
                    settings = behaviour.Settings;
            }
            else
            {
                settings = overrideSettings;
            }
        }
        
        private void OnEnable()
        {
            WeaponSystem.OnPlayerAimAtEnemy += OnPlayerAimAtEnemy;
        }
        
        private void OnDisable()
        {
            WeaponSystem.OnPlayerAimAtEnemy -= OnPlayerAimAtEnemy;
        }
        
        private void OnDestroy()
        {
            if (dodgeCoroutine != null)
                StopCoroutine(dodgeCoroutine);
        }
        
        private void OnPlayerAimAtEnemy(GameObject targetEnemy, ref bool cancelDamage)
        {
            if (targetEnemy != gameObject) return;
            
            // Si esquive réussie, annuler les dégâts
            if (TryDodge())
            {
                cancelDamage = true;
            }
        }
        
        /// <summary>
        /// Tente d'effectuer une esquive. Retourne true si l'esquive a été lancée.
        /// </summary>
        public bool TryDodge()
        {
            if (!CanDodge()) return false;
            
            // Check probabilité
            if (Random.value > settings.dodgeChance)
            {
                if (showDebugLogs)
                    Debug.Log($"[EnemyDodge] {gameObject.name}: Pas d'esquive (random)");
                return false;
            }
            
            Vector3 dodgeDirection = ChooseDodgeDirection();
            if (dodgeDirection == Vector3.zero) return false;
            
            if (dodgeCoroutine != null)
                StopCoroutine(dodgeCoroutine);
            
            dodgeCoroutine = StartCoroutine(DodgeCoroutine(dodgeDirection));
            return true;
        }
        
        private bool CanDodge()
        {
            if (settings == null) return false;
            if (!settings.canDodge) return false;
            if (isDodging) return false;
            if (enemyHealth != null && enemyHealth.IsDead) return false;
            if (knockback != null && knockback.IsKnockbackActive) return false;
            
            if (Time.time < lastDodgeTime + settings.dodgeCooldown) return false;
            
            return true;
        }
        
        private Vector3 ChooseDodgeDirection()
        {
            Vector3 right = transform.right;
            Vector3 left = -transform.right;
            Vector3 back = -transform.forward;
            Vector3 backRight = (back + right).normalized;
            Vector3 backLeft = (back + left).normalized;
            
            // Toutes les directions possibles (priorise latérales puis diagonales arrière)
            Vector3[] directions = new Vector3[]
            {
                right, left, backRight, backLeft, back
            };
            
            // Mélanger les 2 premières (gauche/droite) pour du hasard
            if (Random.value > 0.5f)
            {
                (directions[0], directions[1]) = (directions[1], directions[0]);
            }
            
            // Mélanger les diagonales arrière aussi
            if (Random.value > 0.5f)
            {
                (directions[2], directions[3]) = (directions[3], directions[2]);
            }
            
            // Trouver la première direction libre
            foreach (Vector3 dir in directions)
            {
                if (IsDirectionClear(dir))
                    return dir;
            }
            
            if (showDebugLogs)
                Debug.Log($"[EnemyDodge] {gameObject.name}: Aucune direction d'esquive disponible");
            
            return Vector3.zero;
        }
        
        private bool IsDirectionClear(Vector3 direction)
        {
            float checkDistance = settings.dodgeForce * settings.dodgeDuration * 0.5f;
            checkDistance = Mathf.Max(1.5f, checkDistance);
            
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            
            int hits = Physics.RaycastNonAlloc(
                origin,
                direction,
                WallCheckBuffer,
                checkDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );
            
            for (int i = 0; i < hits; i++)
            {
                var hit = WallCheckBuffer[i];
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                    continue;
                    
                return false;
            }
            
            return true;
        }
        
        private IEnumerator DodgeCoroutine(Vector3 direction)
        {
            isDodging = true;
            lastDodgeTime = Time.time;
            
            if (showDebugLogs)
                Debug.Log($"[EnemyDodge] {gameObject.name}: ESQUIVE vers {direction}");
            
            direction.y = 0f;
            direction = direction.normalized;
            
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
            
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            
            Vector3 dodgeForce = direction * settings.dodgeForce + Vector3.up * (settings.dodgeForce * 0.3f);
            rb.AddForce(dodgeForce, ForceMode.VelocityChange);
            
            float elapsed = 0f;
            while (elapsed < settings.dodgeDuration)
            {
                if (enemyHealth != null && enemyHealth.IsDead)
                {
                    isDodging = false;
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            
            if (agent != null)
            {
                if (NavMesh.SamplePosition(rb.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    agent.enabled = true;
                    agent.Warp(hit.position);
                    agent.isStopped = false;
                }
                else
                {
                    agent.enabled = true;
                    agent.isStopped = false;
                }
            }
            
            isDodging = false;
            dodgeCoroutine = null;
        }
        
        public bool IsDodging => isDodging;
    }
}
