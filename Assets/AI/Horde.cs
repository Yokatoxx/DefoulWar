using System.Collections.Generic;
using UnityEngine;

public class Horde
{
    public int Id { get; private set; }
    public List<EnemyAgent> Members { get; private set; }
    public Vector3 Position { get; private set; }
    public int MaxSize { get; private set; }
    public int OriginatingSpawnId { get; private set; }

    public Horde(int id, Vector3 position, int maxSize, int originatingSpawnId = -1)
    {
        Id = id;
        Position = position;
        MaxSize = maxSize;
        OriginatingSpawnId = originatingSpawnId;
        Members = new List<EnemyAgent>();
    }

    public bool IsFull => Members.Count >= MaxSize;

    public void AddMember(EnemyAgent agent)
    {
        if (!Members.Contains(agent) && !IsFull)
        {
            Members.Add(agent);
            agent.OnJoinedHorde(this);
            RecalculatePosition();
        }
    }

    public void RemoveMember(EnemyAgent agent)
    {
        if (Members.Remove(agent))
        {
            agent.OnLeftHorde();
            RecalculatePosition();
        }
    }

    private void RecalculatePosition()
    {
        if (Members.Count == 0) return;
        Vector3 sum = Vector3.zero;
        foreach (var m in Members) sum += m.transform.position;
        Position = sum / Members.Count;
    }
}