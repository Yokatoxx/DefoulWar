// EnemyAI.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))] // Force la présence du composant
public class EnemyAI : MonoBehaviour
{
    [Header("Composants")]
    public NavMeshAgent agent; // Référence à l'agent
    // Le Behavior Tree va lire et écrire cette variable.
    // Le HordeManager va aussi l'utiliser.
    [Header("Horde Info")]
    public Horde currentHorde = null;

    [Header("Stats")]
    public float baseMoveSpeed = 3.5f;
    public float currentMoveSpeed;
    // ... autres stats comme la vie ...

    // Assurez-vous d'avoir une méthode Awake ou Start pour initialiser
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentMoveSpeed = baseMoveSpeed;
        agent.speed = currentMoveSpeed;
    }

    void Start()
    {
        if (HordeManager.instance != null)
        {
            HordeManager.instance.RegisterEnemy(this);
        }
    }

    void OnDestroy()
    {
        // Très important : se désinscrire en mourant !
        if (HordeManager.instance != null && currentHorde != null)
        {
            HordeManager.instance.LeaveHorde(this);
        }
    }

    // Cette méthode est juste pour l'exemple.
    // Votre Behavior Tree appellera les actions de mouvement.
    void Update()
    {
        // Si je suis dans une horde, je suis son point de ralliement
        if (currentHorde != null)
        {
            // Simule le mouvement vers le point de ralliement
            // (Votre BT gérera ça)
            // Vector3 direction = (currentHorde.rallyPoint - transform.position).normalized;
            // transform.position += direction * currentMoveSpeed * Time.deltaTime;
        }
    }

    // --- MÉTHODE CLÉ POUR LE BEHAVIOR TREE ---

    /// <summary>
    /// Dit à l'IA de se déplacer vers une destination
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (agent.isActiveAndEnabled)
        {
            agent.SetDestination(destination);
        }
    }

    /// <summary>
    /// Dit à l'IA d'arrêter de bouger
    /// </summary>
    public void StopMoving()
    {
        if (agent.isActiveAndEnabled)
        {
            agent.ResetPath();
        }
    }
}