// Horde.cs
using System.Collections.Generic;
using UnityEngine;

// Pas un MonoBehaviour, juste une classe pour stocker des données.
[System.Serializable]
public class Horde
{
    public List<EnemyAI> members;
    public EnemyAI leader; // Le premier membre, ou un point de ralliement
    public Vector3 rallyPoint;

    public Horde(EnemyAI founder)
    {
        members = new List<EnemyAI>();
        leader = founder;
        AddMember(founder);
    }

    public void AddMember(EnemyAI enemy)
    {
        if (!members.Contains(enemy))
        {
            members.Add(enemy);
            enemy.currentHorde = this; // Informe l'ennemi de sa nouvelle horde
            HordeManager.instance.ApplyBuff(enemy); // Applique le buff
        }
        UpdateRallyPoint();
    }

    public void RemoveMember(EnemyAI enemy)
    {
        if (members.Contains(enemy))
        {
            members.Remove(enemy);
            enemy.currentHorde = null; // L'ennemi est à nouveau seul
            HordeManager.instance.RemoveBuff(enemy); // Retire le buff

            // Si le leader part, désignez-en un nouveau ou dissolvez
            if (leader == enemy && members.Count > 0)
            {
                leader = members[0];
            }
        }

        if (members.Count == 0)
        {
            HordeManager.instance.DisbandHorde(this);
        }
        else
        {
            UpdateRallyPoint();
        }
    }

    // Calcule le point central de la horde
    public void UpdateRallyPoint()
    {
        if (members.Count == 0) return;

        // Stratégie simple : le point de ralliement est la position du leader
        if (leader != null)
        {
            rallyPoint = leader.transform.position;
        }
        else // Stratégie alternative : le barycentre (centre de masse)
        {
            Vector3 center = Vector3.zero;
            foreach (var member in members)
            {
                center += member.transform.position;
            }
            rallyPoint = center / members.Count;
        }
    }
}