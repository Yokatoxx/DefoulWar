using System.Collections.Generic;
using UnityEngine;

namespace HordeSystem
{
    public class HordeData
    {
        public int HordeId { get; private set; }
        public Vector3 RallyPoint { get; set; }
        public List<NormalEnemyAI> Members { get; private set; }
        public int MaxSize { get; private set; }
        public Transform PlayerTarget { get; private set; }
        public bool IsAlerted => PlayerTarget != null;
        public bool IsFull => Members.Count >= MaxSize;
        public bool IsValid => Members.Count > 0;
        
        // Comportement collectif
        public HordeBehavior CurrentBehavior { get; private set; }
        public Vector3 CollectiveTarget { get; private set; }
        public float LastBehaviorUpdate { get; set; }
        
        // Formation
        public FormationType Formation { get; set; }
        private Dictionary<NormalEnemyAI, Vector3> formationPositions;
        
        public HordeData(int id, Vector3 rallyPoint, int maxSize)
        {
            HordeId = id;
            RallyPoint = rallyPoint;
            MaxSize = maxSize;
            Members = new List<NormalEnemyAI>();
            CurrentBehavior = HordeBehavior.Patrol;
            Formation = FormationType.Scatter;
            formationPositions = new Dictionary<NormalEnemyAI, Vector3>();
            CollectiveTarget = rallyPoint;
        }
        
        // Ajoute un membre à la horde si possible.
        public bool TryAddMember(NormalEnemyAI enemy)
        {
            if (IsFull || Members.Contains(enemy))
                return false;
                
            Members.Add(enemy);
            return true;
        }
        public void RemoveMember(NormalEnemyAI enemy)
        {
            Members.Remove(enemy);
        }
        
        public void CleanupMembers()
        {
            Members.RemoveAll(m => m == null || m.IsDead);
        }
        
        // Calcule le centre moyen de la horde basé sur les positions des membres.
        public Vector3 CalculateCenter()
        {
            if (Members.Count == 0)
                return RallyPoint;
                
            Vector3 center = Vector3.zero;
            foreach (var member in Members)
            {
                if (member != null)
                    center += member.transform.position;
            }
            return center / Members.Count;
        }
        
        // Définit la cible du joueur pour toute la horde.
        public void SetPlayerTarget(Transform target)
        {
            PlayerTarget = target;
            SetBehavior(HordeBehavior.Chase);
        }
        
        public void ClearPlayerTarget()
        {
            PlayerTarget = null;
            SetBehavior(HordeBehavior.Patrol);
        }
        
        // Définit le comportement collectif de toute la horde.
        public void SetBehavior(HordeBehavior behavior)
        {
            CurrentBehavior = behavior;
            LastBehaviorUpdate = Time.time;
        }
        
        public void SetCollectiveTarget(Vector3 target)
        {
            CollectiveTarget = target;
        }
        
        public Vector3 GetFormationPosition(NormalEnemyAI member)
        {
            if (!Members.Contains(member)) return RallyPoint;
            
            int index = Members.IndexOf(member);
            Vector3 center = CollectiveTarget;
            
            switch (Formation)
            {
                case FormationType.Line:
                    return CalculateLineFormation(index, center);
                case FormationType.Circle:
                    return CalculateCircleFormation(index, center);
                case FormationType.Wedge:
                    return CalculateWedgeFormation(index, center);
                case FormationType.Scatter:
                default:
                    return CalculateScatterFormation(index, center);
            }
        }
        
        private Vector3 CalculateLineFormation(int index, Vector3 center)
        {
            float spacing = 2f;
            int middleIndex = Members.Count / 2;
            float offset = (index - middleIndex) * spacing;
            
            Vector3 direction = Vector3.right;
            if (PlayerTarget != null)
            {
                Vector3 toPlayer = (PlayerTarget.position - center).normalized;
                direction = Vector3.Cross(toPlayer, Vector3.up).normalized;
            }
            
            return center + direction * offset;
        }
        
        private Vector3 CalculateCircleFormation(int index, Vector3 center)
        {
            float radius = 5f;
            float angleStep = 360f / Members.Count;
            float angle = index * angleStep * Mathf.Deg2Rad;
            
            return center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
        }
        
        private Vector3 CalculateWedgeFormation(int index, Vector3 center)
        {
            float spacing = 2f;
            int row = Mathf.FloorToInt(Mathf.Sqrt(index));
            int posInRow = index - (row * row);
            
            Vector3 forward = Vector3.forward;
            if (PlayerTarget != null)
            {
                forward = (PlayerTarget.position - center).normalized;
            }
            
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
            
            return center 
                + forward * (row * spacing) 
                + right * ((posInRow - row / 2f) * spacing);
        }
        
        private Vector3 CalculateScatterFormation(int index, Vector3 center)
        {
            float radius = 5f;
            float angle = (index * 137.5f) * Mathf.Deg2Rad;
            float distance = Mathf.Sqrt(index) * (radius / Mathf.Sqrt(Members.Count));
            
            return center + new Vector3(
                Mathf.Cos(angle) * distance,
                0,
                Mathf.Sin(angle) * distance
            );
        }
    }
    
    public enum HordeBehavior
    {
        Patrol,
        Chase,
        Attack,
        Surround,
        Retreat
    }
    public enum FormationType
    {
        Scatter,
        Line,
        Circle,
        Wedge
    }
}

