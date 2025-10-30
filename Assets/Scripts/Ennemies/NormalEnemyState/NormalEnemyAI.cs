using UnityEngine;
using UnityEngine.AI;

namespace HordeSystem
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NormalEnemyAI : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Vitesse de déplacement normale")]
        [SerializeField] private float moveSpeed = 3.5f;
        
        [Tooltip("Vitesse lors de la poursuite du joueur")]
        [SerializeField] private float chaseSpeed = 5f;
        
        [Header("Combat Settings")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private LayerMask playerMask;
        
        [Header("Detection Settings")]
        [Tooltip("Distance de détection du joueur")]
        [SerializeField] private float detectionRange = 15f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Composants
        private NavMeshAgent agent;
        private BaseEnemyState currentState;
        
        // État de la horde
        private HordeData currentHorde;
        private bool isAlone;
        private bool isDead;
        
        // Combat
        private float lastAttackTime;
        
        // Propriétés publiques
        public NavMeshAgent Agent => agent;
        public HordeData CurrentHorde => currentHorde;
        public bool IsAlone => isAlone;
        public bool IsDead => isDead;
        public float MoveSpeed => moveSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float DetectionRange => detectionRange;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            
            // Configurer la vitesse initiale
            if (agent != null)
            {
                agent.speed = moveSpeed;
            }
        }
        
        private void Start()
        {
            // S'enregistrer auprès du HordeManager
            if (HordeManager.Instance != null)
            {
                HordeManager.Instance.RegisterEnemy(this);
            }
            
            // Démarrer dans l'état Idle
            ChangeState(new NormalIdleState(this));
        }
        
        private void Update()
        {
            if (isDead) return;
            
            // Mettre à jour l'état actuel
            currentState?.OnUpdate();
        }
        
        public void ChangeState(BaseEnemyState newState)
        {
            if (currentState != null)
            {
                currentState.OnExit();
            }
            
            currentState = newState;
            
            if (currentState != null)
            {
                currentState.OnEnter();
                
                if (showDebugInfo)
                {
                    Debug.Log($"[{name}] Changement d'état: {currentState.GetStateName()}");
                }
            }
        }
        
        public void AssignToHorde(HordeData horde)
        {
            currentHorde = horde;
            isAlone = false;
            
            if (showDebugInfo)
            {
                Debug.Log($"[{name}] Assigné à la horde {horde.HordeId}");
            }
            
            // Si on est en recherche, passer à JoiningHorde
            if (currentState is SearchingHordeState || currentState is NormalIdleState)
            {
                ChangeState(new JoiningHordeState(this));
            }
        }

        // Marque cet ennemi comme isolé.
        public void SetAlone(bool alone)
        {
            isAlone = alone;
            
            if (alone)
            {
                currentHorde = null;
                
                if (showDebugInfo)
                {
                    Debug.Log($"[{name}] Marqué comme isolé");
                }
            }
        }
        
        // Alerte toute la horde qu'un ennemi a détecté le joueur.
        public void AlertHorde(Transform playerTransform)
        {
            if (currentHorde == null || playerTransform == null) return;
            
            // Passer la horde en mode alerte
            currentHorde.SetPlayerTarget(playerTransform);
            
            // Alerter tous les membres de la horde
            foreach (var member in currentHorde.Members)
            {
                if (member != null && member != this && !member.IsDead)
                {
                    // Augmenter la vitesse pour la poursuite
                    if (member.Agent != null)
                    {
                        member.Agent.speed = member.chaseSpeed;
                    }
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[{name}] Alerte la horde {currentHorde.HordeId} - Tous poursuivent le joueur !");
            }
        }
        
        public void TryAttack()
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }
        
        private void PerformAttack()
        {
            // Détection du joueur dans la portée d'attaque
            Collider[] hits = new Collider[5];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 2f, hits, playerMask);
            
            for (int i = 0; i < hitCount; i++)
            {
                // Tentative d'appliquer des dégâts au joueur
                var healthComponent = hits[i].GetComponent<MonoBehaviour>();
                if (healthComponent != null)
                {
                    // Utiliser réflexion pour compatibilité avec différents systèmes de santé
                    var takeDamageMethod = healthComponent.GetType().GetMethod("TakeDamage");
                    if (takeDamageMethod != null)
                    {
                        takeDamageMethod.Invoke(healthComponent, new object[] { attackDamage });
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"[{name}] Attaque le joueur pour {attackDamage} dégâts");
                        }
                    }
                }
            }
        }
        
        public void Die()
        {
            if (isDead) return;
            
            isDead = true;
            
            // Se désenregistrer du HordeManager
            if (HordeManager.Instance != null)
            {
                HordeManager.Instance.UnregisterEnemy(this);
            }
            
            // Arrêter le NavMeshAgent
            if (agent != null)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[{name}] Mort");
            }
            
            // Destruction de l'objet (ou animation de mort)
            Destroy(gameObject, 2f);
        }
        
        public void TakeDamage(float damage)
        {
            if (isDead) return;
            
            var healthComponents = GetComponents<MonoBehaviour>();
            foreach (var component in healthComponents)
            {
                var componentType = component.GetType();
                if (componentType.Name.Contains("Health") || componentType.Name.Contains("health"))
                {
                    var takeDamageMethod = componentType.GetMethod("TakeDamage");
                    if (takeDamageMethod != null)
                    {
                        takeDamageMethod.Invoke(component, new object[] { damage });
                        
                        var isDeadProp = componentType.GetProperty("IsDead");
                        if (isDeadProp != null && (bool)isDeadProp.GetValue(component))
                        {
                            Die();
                        }
                        return;
                    }
                }
            }
            
        }
        
        private void OnDestroy()
        {
            if (!isDead && HordeManager.Instance != null)
            {
                HordeManager.Instance.UnregisterEnemy(this);
            }
        }
        
        // Debug Gizmos
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !showDebugInfo) return;
            
            // Afficher la connexion avec la horde
            if (currentHorde != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, currentHorde.RallyPoint);
                
#if UNITY_EDITOR
                // Afficher l'état
                UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, 
                    $"State: {currentState?.GetStateName()}\nHorde: {currentHorde.HordeId}\nSpeed: {agent?.speed:F1}");
#endif
            }
            else if (isAlone)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 1f);
                
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, 
                    $"State: {currentState?.GetStateName()}\nALONE");
#endif
            }
            
            // Afficher la portée de détection
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}

