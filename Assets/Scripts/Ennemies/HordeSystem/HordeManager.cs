using System.Collections.Generic;
using UnityEngine;

namespace HordeSystem
{
    /// <summary>
    /// Gestionnaire global des hordes d'ennemis.
    /// Gère la création, fusion et assignation des hordes.
    /// Coordonne le comportement collectif de chaque horde.
    /// </summary>
    public class HordeManager : MonoBehaviour
    {
        public static HordeManager Instance { get; private set; }
        
        [Header("Horde Settings")]
        [Tooltip("Taille maximale d'une horde principale")]
        [SerializeField] private int maxHordeSize = 10;
        
        [Tooltip("Taille minimale pour former une petite horde")]
        [SerializeField] private int minHordeSize = 3;
        
        [Tooltip("Nombre maximum de hordes actives")]
        [SerializeField] private int maxHordes = 2;
        
        [Tooltip("Distance maximale pour rejoindre une horde")]
        [SerializeField] private float maxJoinDistance = 50f;
        
        [Tooltip("Intervalle de vérification pour les ennemis isolés (secondes)")]
        [SerializeField] private float aloneCheckInterval = 5f;
        
        private List<HordeData> activeHordes = new List<HordeData>();
        private List<NormalEnemyAI> aloneEnemies = new List<NormalEnemyAI>();
        private int nextHordeId;
        
        public int MaxHordeSize => maxHordeSize;
        public int MinHordeSize => minHordeSize;
        public int MaxHordes => maxHordes;
        public float MaxJoinDistance => maxJoinDistance;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            InvokeRepeating(nameof(CheckAloneEnemies), aloneCheckInterval, aloneCheckInterval);
            InvokeRepeating(nameof(UpdateHordeBehaviors), 0.5f, 0.5f);
        }
        
        /// <summary>
        /// Met à jour le comportement collectif de toutes les hordes.
        /// </summary>
        private void UpdateHordeBehaviors()
        {
            foreach (var horde in activeHordes)
            {
                if (horde == null || !horde.IsValid) continue;
                
                UpdateHordeBehavior(horde);
            }
        }
        
        /// <summary>
        /// Détermine et applique le comportement collectif d'une horde.
        /// </summary>
        private void UpdateHordeBehavior(HordeData horde)
        {
            if (horde.PlayerTarget != null)
            {
                float distanceToPlayer = Vector3.Distance(horde.RallyPoint, horde.PlayerTarget.position);
                
                if (distanceToPlayer < 10f)
                {
                    horde.SetBehavior(HordeBehavior.Surround);
                    horde.Formation = FormationType.Circle;
                    horde.SetCollectiveTarget(horde.PlayerTarget.position);
                }
                else if (distanceToPlayer < 30f)
                {
                    horde.SetBehavior(HordeBehavior.Chase);
                    horde.Formation = FormationType.Wedge;
                    horde.SetCollectiveTarget(horde.PlayerTarget.position);
                }
                else
                {
                    horde.SetBehavior(HordeBehavior.Chase);
                    horde.Formation = FormationType.Line;
                    horde.SetCollectiveTarget(horde.PlayerTarget.position);
                }
            }
            else
            {
                horde.SetBehavior(HordeBehavior.Patrol);
                horde.Formation = FormationType.Scatter;
                horde.SetCollectiveTarget(horde.RallyPoint);
            }
        }
        
        /// <summary>
        /// Enregistre un nouvel ennemi dans le système.
        /// </summary>
        public void RegisterEnemy(NormalEnemyAI enemy)
        {
            if (enemy == null) return;
            
            HordeData horde = FindBestHordeForEnemy(enemy);
            
            if (horde != null)
            {
                horde.TryAddMember(enemy);
                enemy.AssignToHorde(horde);
            }
            else
            {
                if (TryCreateNewHorde(enemy))
                {
                    // Ennemi assigné à la nouvelle horde
                }
                else
                {
                    enemy.SetAlone(true);
                    aloneEnemies.Add(enemy);
                }
            }
        }
        
        /// <summary>
        /// Retire un ennemi du système (généralement à sa mort).
        /// </summary>
        public void UnregisterEnemy(NormalEnemyAI enemy)
        {
            if (enemy == null) return;
            
            if (enemy.CurrentHorde != null)
            {
                enemy.CurrentHorde.RemoveMember(enemy);
                CleanupEmptyHordes();
            }
            
            aloneEnemies.Remove(enemy);
        }
        
        /// <summary>
        /// Trouve la meilleure horde pour un ennemi.
        /// </summary>
        private HordeData FindBestHordeForEnemy(NormalEnemyAI enemy)
        {
            CleanupHordes();
            
            HordeData closestHorde = null;
            float closestDistance = maxJoinDistance;
            
            foreach (var horde in activeHordes)
            {
                if (horde.IsFull) continue;
                
                float distance = Vector3.Distance(enemy.transform.position, horde.RallyPoint);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestHorde = horde;
                }
            }
            
            return closestHorde;
        }
        
        /// <summary>
        /// Essaie de créer une nouvelle horde avec cet ennemi comme fondateur.
        /// </summary>
        private bool TryCreateNewHorde(NormalEnemyAI enemy)
        {
            CleanupEmptyHordes();
            
            if (activeHordes.Count < maxHordes)
            {
                CreateHorde(enemy.transform.position, maxHordeSize, enemy);
                return true;
            }
            
            List<NormalEnemyAI> nearbyAlone = GetNearbyAloneEnemies(enemy.transform.position, maxJoinDistance);
            
            if (nearbyAlone.Count + 1 >= minHordeSize)
            {
                HordeData smallHorde = CreateHorde(enemy.transform.position, minHordeSize, enemy);
                
                foreach (var alone in nearbyAlone)
                {
                    if (smallHorde.IsFull) break;
                    
                    if (smallHorde.TryAddMember(alone))
                    {
                        alone.AssignToHorde(smallHorde);
                        aloneEnemies.Remove(alone);
                    }
                }
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Crée une nouvelle horde.
        /// </summary>
        private HordeData CreateHorde(Vector3 position, int size, NormalEnemyAI firstMember = null)
        {
            HordeData horde = new HordeData(nextHordeId++, position, size);
            activeHordes.Add(horde);
            
            if (firstMember != null)
            {
                horde.TryAddMember(firstMember);
                firstMember.AssignToHorde(horde);
            }
            
            Debug.Log($"[HordeManager] Nouvelle horde créée (ID: {horde.HordeId}, Max: {size})");
            return horde;
        }
        
        /// <summary>
        /// Récupère les ennemis isolés proches d'une position.
        /// </summary>
        private List<NormalEnemyAI> GetNearbyAloneEnemies(Vector3 position, float maxDistance)
        {
            List<NormalEnemyAI> nearby = new List<NormalEnemyAI>();
            
            foreach (var enemy in aloneEnemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance <= maxDistance)
                {
                    nearby.Add(enemy);
                }
            }
            
            return nearby;
        }
        
        /// <summary>
        /// Vérifie périodiquement si les ennemis isolés peuvent rejoindre une horde.
        /// </summary>
        private void CheckAloneEnemies()
        {
            CleanupHordes();
            
            List<NormalEnemyAI> toCheck = new List<NormalEnemyAI>(aloneEnemies);
            
            foreach (var enemy in toCheck)
            {
                if (enemy == null || enemy.IsDead)
                {
                    aloneEnemies.Remove(enemy);
                    continue;
                }
                
                HordeData horde = FindBestHordeForEnemy(enemy);
                
                if (horde != null)
                {
                    if (horde.TryAddMember(enemy))
                    {
                        enemy.AssignToHorde(horde);
                        aloneEnemies.Remove(enemy);
                        Debug.Log($"[HordeManager] Ennemi isolé rejoint la horde {horde.HordeId}");
                    }
                }
                else
                {
                    TryCreateNewHorde(enemy);
                }
            }
        }
        
        /// <summary>
        /// Nettoie les hordes (membres morts, etc.).
        /// </summary>
        private void CleanupHordes()
        {
            foreach (var horde in activeHordes)
            {
                horde.CleanupMembers();
            }
        }
        
        /// <summary>
        /// Supprime les hordes vides.
        /// </summary>
        private void CleanupEmptyHordes()
        {
            activeHordes.RemoveAll(h => !h.IsValid);
        }
        
        /// <summary>
        /// Appelé quand une nouvelle vague commence - force la revérification.
        /// </summary>
        public void OnNewWaveStarted()
        {
            Debug.Log("[HordeManager] Nouvelle vague détectée - revérification des ennemis isolés");
            CheckAloneEnemies();
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        // Méthodes de debug
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || activeHordes == null) return;
            
            foreach (var horde in activeHordes)
            {
                if (horde == null) continue;
                
                Gizmos.color = horde.IsFull ? Color.red : Color.green;
                Gizmos.DrawWireSphere(horde.RallyPoint, 2f);
                
                Gizmos.color = Color.yellow;
                foreach (var member in horde.Members)
                {
                    if (member != null)
                    {
                        Gizmos.DrawLine(member.transform.position, horde.RallyPoint);
                    }
                }
            }
        }
    }
}

