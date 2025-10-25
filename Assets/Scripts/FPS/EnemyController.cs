using Proto3GD.FPS;
using UnityEngine;
using UnityEngine.AI;

namespace FPS
{
    /// <summary>
    /// Contrôle le mouvement et le comportement de l'ennemi avec NavMesh.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float rotationSpeed = 5f;
        
        [Header("Detection")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private LayerMask playerMask;
        
        [Header("Attack Settings")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.5f;
        
        private NavMeshAgent agent;
        private Transform player;
        private EnemyHealth enemyHealth;
        private float lastAttackTime;

        public enum EnemyState
        {
            Idle,
            Chasing,
            Attacking
        }
        
        private EnemyState currentState = EnemyState.Idle;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            enemyHealth = GetComponent<EnemyHealth>();
            
            // Trouver le joueur
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            
            agent.speed = patrolSpeed;
        }
        
        private void Update()
        {
            if (enemyHealth.IsDead || player == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // Machine à états
            switch (currentState)
            {
                case EnemyState.Idle:
                    if (distanceToPlayer <= detectionRange)
                    {
                        currentState = EnemyState.Chasing;
                        agent.speed = chaseSpeed;
                    }
                    break;
                    
                case EnemyState.Chasing:
                    agent.SetDestination(player.position);
                    
                    if (distanceToPlayer <= attackRange)
                    {
                        currentState = EnemyState.Attacking;
                        agent.isStopped = true;
                    }
                    else if (distanceToPlayer > detectionRange * 1.5f)
                    {
                        currentState = EnemyState.Idle;
                        agent.speed = patrolSpeed;
                    }
                    break;
                    
                case EnemyState.Attacking:
                    // Regarder vers le joueur
                    Vector3 direction = (player.position - transform.position).normalized;
                    Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
                    
                    // Attaquer
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        Attack();
                        lastAttackTime = Time.time;
                    }
                    
                    // Retourner en mode chasse si trop loin
                    if (distanceToPlayer > attackRange * 1.5f)
                    {
                        currentState = EnemyState.Chasing;
                        agent.isStopped = false;
                    }
                    break;
            }
        }
        
        private void Attack()
        {
            // Vérifier si le joueur est toujours à portée
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= attackRange)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"Enemy attacked player for {attackDamage} damage!");
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Visualiser les ranges
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
        
        // Propriétés publiques
        public EnemyState CurrentState => currentState;
    }
}

