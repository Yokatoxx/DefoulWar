using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAgent : MonoBehaviour
{
    [Header("Horde settings (defaults, overridable per agent)")]
    public int hordeMax = 6;
    public int hordeMinSize = 2;
    public float hordeCheckInterval = 5f;
    public float hordeJoinRadius = 30f;

    [Header("Perception")]
    public float playerDetectRange = 10f;
    public LayerMask playerLayer;

    [Header("Movement/AI")]
    public NavMeshAgent nav;

    // État local synchronisé avec le Blackboard : utilisé par HordeManager / recherche d'ennemis
    [HideInInspector] public int hordeId = -1;
    [HideInInspector] public bool isAlone = false;
    [HideInInspector] public float lastHordeCheckTime = -999f;
    [HideInInspector] public bool seesPlayer = false;
    [HideInInspector] public Vector3 playerPosition = Vector3.zero;
    [HideInInspector] public Vector3 targetPosition = Vector3.zero;

    void Awake()
    {
        if (nav == null) nav = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        StartCoroutine(HordePeriodicCheck());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator HordePeriodicCheck()
    {
        // Petit délai aléatoire pour éviter que tous les agents s'exécutent exactement en même temps
        yield return new WaitForSeconds(Random.Range(0f, 0.5f));
        while (true)
        {
            yield return new WaitForSeconds(hordeCheckInterval);
            // L'appel ci-dessous mettra à jour l'état local ; le node BT doit également synchroniser le Blackboard si nécessaire.
            TryJoinOrFormHordeIfNeeded();
        }
    }

    // Méthode principale : essaie de rejoindre la horde la plus proche, sinon crée si assez d'agents non assignés
    // Cette méthode met à jour l'état local du composant (hordeId/isAlone/lastHordeCheckTime)
    public bool TryJoinOrFormHordeIfNeeded()
    {
        // Si déjà dans une horde et la horde existe toujours => success
        if (hordeId != -1)
        {
            var h = HordeManager.Instance.GetHordeById(hordeId);
            if (h != null) return true;
            // sinon on a perdu la horde
            hordeId = -1;
        }

        // Essaie de rejoindre la horde la plus proche non pleine
        var nearest = HordeManager.Instance.GetNearestJoinableHorde(transform.position, hordeJoinRadius);
        if (nearest != null)
        {
            nearest.AddMember(this);
            isAlone = false;
            lastHordeCheckTime = Time.time;
            return true;
        }

        // Sinon, si assez d'ennemis non assignés à proximité, créer une nouvelle horde
        int nearbyUnassigned = HordeManager.Instance.CountUnassignedNearby(transform.position, hordeJoinRadius);
        if (nearbyUnassigned >= hordeMinSize)
        {
            var newH = HordeManager.Instance.CreateHorde(transform.position, hordeMax);
            newH.AddMember(this);
            isAlone = false;
            lastHordeCheckTime = Time.time;
            return true;
        }

        // Sinon rester seul
        isAlone = true;
        lastHordeCheckTime = Time.time;
        return false;
    }

    // called by Horde.AddMember
    public void OnJoinedHorde(Horde h)
    {
        hordeId = h.Id;
        isAlone = false;
    }

    public void OnLeftHorde()
    {
        hordeId = -1;
    }

    // Perception simple: sphere check
    // Met à jour l'état local seesPlayer/playerPosition et renvoie le résultat
    public bool CheckSeesPlayer(out Vector3 playerPos)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, playerDetectRange, playerLayer);
        if (cols.Length > 0)
        {
            playerPos = cols[0].transform.position;
            seesPlayer = true;
            playerPosition = playerPos;
            return true;
        }
        playerPos = Vector3.zero;
        seesPlayer = false;
        playerPosition = Vector3.zero;
        return false;
    }

    // Actions utilisables par des nodes (mettent à jour l'état local également)
    // Essaie de rejoindre/créer la horde (local) et renvoie true si rejoint/créé
    public bool Action_TryJoinNearestHorde_Local()
    {
        bool res = TryJoinOrFormHordeIfNeeded();
        lastHordeCheckTime = Time.time;
        return res;
    }

    // Force création de horde (local)
    public bool Action_CreateHorde_Local()
    {
        var newH = HordeManager.Instance.CreateHorde(transform.position, hordeMax);
        newH.AddMember(this);
        isAlone = false;
        lastHordeCheckTime = Time.time;
        return true;
    }

    // Set alone and roam locally
    public bool Action_SetAloneAndRoam_Local()
    {
        isAlone = true;
        targetPosition = transform.position + Random.insideUnitSphere * 8f;
        targetPosition.y = transform.position.y;
        if (nav != null) nav.SetDestination(targetPosition);
        return true;
    }

    // Move to perceived player (local)
    public bool Action_MoveToPlayer_Local()
    {
        CheckSeesPlayer(out var p);
        if (nav != null) nav.SetDestination(p);
        return true;
    }
}