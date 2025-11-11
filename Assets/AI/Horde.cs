// Horde.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Horde
{
    public List<EnemyAI> members;
    public EnemyAI leader; // Optional leader (used as rally point)
    public Vector3 rallyPoint;

    public Horde(EnemyAI founder)
    {
        members = new List<EnemyAI>();
        leader = founder;
        AddMember(founder);
    }

    public void AddMember(EnemyAI enemy)
    {
        if (enemy == null) return;

        if (!members.Contains(enemy))
        {
            members.Add(enemy);
            enemy.currentHorde = this;
            if (HordeManager.instance != null)
                HordeManager.instance.ApplyBuff(enemy);
        }
        UpdateRallyPoint();
    }

    public void RemoveMember(EnemyAI enemy)
    {
        if (enemy == null) return;

        if (members.Contains(enemy))
        {
            members.Remove(enemy);
            // Clear enemy's horde ref and remove buff
            if (enemy != null)
            {
                enemy.currentHorde = null;
                if (HordeManager.instance != null)
                    HordeManager.instance.RemoveBuff(enemy);
            }

            // If leader left, pick a new leader
            if (leader == enemy)
            {
                leader = members.Count > 0 ? members[0] : null;
            }
        }

        if (members.Count == 0)
        {
            if (HordeManager.instance != null)
                HordeManager.instance.DisbandHorde(this);
        }
        else
        {
            UpdateRallyPoint();
        }
    }

    public void UpdateRallyPoint()
    {
        if (members == null || members.Count == 0) return;

        if (leader != null)
        {
            rallyPoint = leader.transform.position;
        }
        else
        {
            Vector3 center = Vector3.zero;
            foreach (var member in members)
            {
                if (member != null)
                    center += member.transform.position;
            }
            rallyPoint = center / members.Count;
        }
    }
}