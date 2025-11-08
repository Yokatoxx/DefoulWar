// HordeManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HordeManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static HordeManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject); // Optionnel
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Règles des Hordes")]
    [SerializeField] private int maxHordes = 2;
    [SerializeField] private int minHordeSize = 4;
    [SerializeField] private float checkAloneInterval = 5.0f; // Temps en secondes

    [Header("Listes (Debug)")]
    [SerializeField] private List<Horde> activeHordes = new List<Horde>();
    [SerializeField] private List<EnemyAI> aloneEnemies = new List<EnemyAI>();

    // --- Initialisation ---
    void Start()
    {
        // Lance la coroutine qui gère la logique avancée
        StartCoroutine(CheckAloneEnemiesLogic());
    }

    // --- Interface Publique (pour le BT et les Ennemis) ---

    /// <summary>
    /// Appelé par EnemyAI à son Start()
    /// </summary>
    public void RegisterEnemy(EnemyAI enemy)
    {
        if (!aloneEnemies.Contains(enemy))
        {
            aloneEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// Appelé par le Behavior Tree (Action: "Trouver Horde")
    /// </summary>
    public Horde FindNearestHorde(Vector3 position)
    {
        Horde nearestHorde = null;
        float minDistance = float.MaxValue;

        foreach (Horde horde in activeHordes)
        {
            // Met à jour le point de ralliement avant de check
            horde.UpdateRallyPoint();

            float distance = Vector3.Distance(position, horde.rallyPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestHorde = horde;
            }
        }
        return nearestHorde;
    }

    /// <summary>
    /// Appelé par le Behavior Tree (Condition: "Peut Créer Horde ?")
    /// </summary>
    public bool CanFormNewHorde()
    {
        return activeHordes.Count < maxHordes;
    }

    /// <summary>
    /// Appelé par le Behavior Tree (Action: "Créer Horde")
    /// </summary>
    public void CreateHorde(EnemyAI founder)
    {
        if (!CanFormNewHorde() || founder.currentHorde != null) return;

        Horde newHorde = new Horde(founder);
        activeHordes.Add(newHorde);

        // Le fondateur n'est plus seul
        if (aloneEnemies.Contains(founder))
        {
            aloneEnemies.Remove(founder);
        }
    }

    /// <summary>
    /// Appelé par le Behavior Tree (Action: "Rejoindre Horde")
    /// </summary>
    public void JoinHorde(EnemyAI enemy, Horde horde)
    {
        if (enemy.currentHorde != null) return; // Déjà dans une horde

        horde.AddMember(enemy);

        if (aloneEnemies.Contains(enemy))
        {
            aloneEnemies.Remove(enemy);
        }
    }

    /// <summary>
    /// Appelé par EnemyAI (OnDestroy) ou si l'ennemi s'enfuit
    /// </summary>
    public void LeaveHorde(EnemyAI enemy)
    {
        if (enemy.currentHorde == null) return;

        enemy.currentHorde.RemoveMember(enemy);

        // L'ennemi redevient "seul" (s'il n'est pas mort)
        if (enemy != null && enemy.gameObject.activeInHierarchy)
        {
            RegisterEnemy(enemy);
        }
    }

    /// <summary>
    /// Appelé par la classe Horde quand elle n'a plus de membres
    /// </summary>
    public void DisbandHorde(Horde horde)
    {
        if (activeHordes.Contains(horde))
        {
            activeHordes.Remove(horde);
        }
    }

    // --- Logique des Buffs ---

    public void ApplyBuff(EnemyAI enemy)
    {
        // === METTEZ VOTRE LOGIQUE DE BUFF ICI ===
        // Exemple:
        enemy.currentMoveSpeed = enemy.baseMoveSpeed * 1.5f;
        // enemy.GetComponent<Renderer>().material.color = Color.red;
        Debug.Log($"{enemy.name} a reçu un buff de horde !");
    }

    public void RemoveBuff(EnemyAI enemy)
    {
        // === METTEZ VOTRE LOGIQUE DE DEBUFF ICI ===
        // Assurez-vous de vérifier si l'ennemi n'est pas détruit
        if (enemy != null)
        {
            enemy.currentMoveSpeed = enemy.baseMoveSpeed;
            // enemy.GetComponent<Renderer>().material.color = Color.white;
            Debug.Log($"{enemy.name} a perdu son buff de horde.");
        }
    }

    // --- Logique Avancée (Votre cas spécial) ---

    /// <summary>
    /// Coroutine qui gère les ennemis "seuls" et forme des hordes "minables".
    /// </summary>
    private IEnumerator CheckAloneEnemiesLogic()
    {
        // Boucle infinie
        while (true)
        {
            // Attend X secondes avant le prochain check
            yield return new WaitForSeconds(checkAloneInterval);

            // Votre logique : "Si maxHorde est rempli, et qu'il reste des ennemis >= hordeMinSize"
            if (activeHordes.Count >= maxHordes && aloneEnemies.Count >= minHordeSize)
            {
                Debug.Log("Condition de petite horde remplie ! Création...");

                // Prend le premier ennemi seul comme fondateur
                EnemyAI founder = aloneEnemies[0];

                // Crée une "petite horde" (elle n'est pas limitée par maxHordes)
                Horde smallHorde = new Horde(founder);
                activeHordes.Add(smallHorde); // Ajoute à la liste
                aloneEnemies.RemoveAt(0); // Retire le fondateur des "seuls"

                // (Optionnel) Force les N-1 prochains ennemis seuls à la rejoindre
                int membersToAutoJoin = minHordeSize - 1;
                for (int i = 0; i < membersToAutoJoin && aloneEnemies.Count > 0; i++)
                {
                    JoinHorde(aloneEnemies[0], smallHorde);
                    // 'JoinHorde' s'occupe de le retirer de la liste 'aloneEnemies'
                }
            }
        }
    }
}