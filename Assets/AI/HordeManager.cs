using System.Collections.Generic;
using UnityEngine;

public class HordeManager : MonoBehaviour
{
    public static HordeManager Instance { get; private set; }

    [Tooltip("Distance max pour considérer une horde 'proche' lorsqu'un agent cherche une horde")]
    public float hordeSearchRadius = 30f;

    public Dictionary<int, Horde> hordes = new Dictionary<int, Horde>();
    private int nextHordeId = 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // Nettoyage des hordes vides (optionnel)
        var toRemove = new List<int>();
        foreach (var kv in hordes)
        {
            if (kv.Value.Members.Count == 0)
                toRemove.Add(kv.Key);
        }
        foreach (var id in toRemove) hordes.Remove(id);
    }

    public Horde CreateHorde(Vector3 position, int maxSize, int originatingSpawnId = -1)
    {
        var h = new Horde(nextHordeId++, position, maxSize, originatingSpawnId);
        hordes[h.Id] = h;
        return h;
    }

    public Horde GetHordeById(int id)
    {
        hordes.TryGetValue(id, out var h);
        return h;
    }

    // Retourne la horde la plus proche qui n'est pas pleine (dans radius), sinon null
    public Horde GetNearestJoinableHorde(Vector3 position, float maxDistance = -1f)
    {
        if (maxDistance < 0f) maxDistance = hordeSearchRadius;
        Horde best = null;
        float bestDist = float.MaxValue;
        foreach (var h in hordes.Values)
        {
            if (h.IsFull) continue;
            float d = Vector3.SqrMagnitude(h.Position - position);
            if (d <= maxDistance * maxDistance && d < bestDist)
            {
                best = h;
                bestDist = d;
            }
        }
        return best;
    }

    // Compte le nombre d'agents sans horde autour d'une position
    // NOTE: cette méthode s'appuie sur la donnée locale "hordeId" du composant EnemyAgent.
    public int CountUnassignedNearby(Vector3 position, float radius)
    {
        int count = 0;
        var agents = FindObjectsOfType<EnemyAgent>();
        float r2 = radius * radius;
        foreach (var a in agents)
        {
            if (a.hordeId != -1) continue;
            if (Vector3.SqrMagnitude(a.transform.position - position) <= r2)
                count++;
        }
        return count;
    }
}