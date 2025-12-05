using UnityEngine;
using System.Collections.Generic;

namespace Ennemies
{
    /// <summary>
    /// Définit un chemin de patrouille avec des waypoints.
    /// Les enfants de ce GameObject sont automatiquement utilisés comme waypoints.
    /// </summary>
    public class WaypointPath : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Si true, la patrouille boucle. Sinon, fait un aller-retour.")]
        [SerializeField] private bool loop = true;

        [Tooltip("Couleur des gizmos de waypoints")]
        [SerializeField] private Color gizmoColor = Color.cyan;

        [Tooltip("Taille des sphères de waypoints dans l'éditeur")]
        [SerializeField] private float waypointGizmoSize = 0.5f;

        private List<Transform> waypoints = new List<Transform>();
        private bool isReversing = false;

        /// <summary>
        /// Nombre de waypoints dans le chemin.
        /// </summary>
        public int WaypointCount => waypoints.Count;

        private void Awake()
        {
            RefreshWaypoints();
        }

        /// <summary>
        /// Rafraîchit la liste des waypoints depuis les enfants.
        /// </summary>
        public void RefreshWaypoints()
        {
            waypoints.Clear();
            foreach (Transform child in transform)
            {
                waypoints.Add(child);
            }
        }

        /// <summary>
        /// Retourne le waypoint à l'index donné.
        /// </summary>
        public Transform GetWaypoint(int index)
        {
            if (waypoints.Count == 0) return null;
            index = Mathf.Clamp(index, 0, waypoints.Count - 1);
            return waypoints[index];
        }

        /// <summary>
        /// Retourne l'index du prochain waypoint.
        /// </summary>
        public int GetNextWaypointIndex(int currentIndex)
        {
            if (waypoints.Count == 0) return 0;

            if (loop)
            {
                return (currentIndex + 1) % waypoints.Count;
            }
            else
            {
                // Aller-retour
                if (isReversing)
                {
                    if (currentIndex <= 0)
                    {
                        isReversing = false;
                        return 1;
                    }
                    return currentIndex - 1;
                }
                else
                {
                    if (currentIndex >= waypoints.Count - 1)
                    {
                        isReversing = true;
                        return waypoints.Count - 2;
                    }
                    return currentIndex + 1;
                }
            }
        }

        /// <summary>
        /// Retourne l'index du waypoint le plus proche de la position donnée.
        /// </summary>
        public int GetClosestWaypointIndex(Vector3 position)
        {
            if (waypoints.Count == 0) return 0;

            int closestIndex = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < waypoints.Count; i++)
            {
                float distance = Vector3.Distance(position, waypoints[i].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        /// <summary>
        /// Retourne le waypoint le plus proche de la position donnée.
        /// </summary>
        public Transform GetClosestWaypoint(Vector3 position)
        {
            int index = GetClosestWaypointIndex(position);
            return GetWaypoint(index);
        }

        private void OnDrawGizmos()
        {
            // Rafraîchir en mode éditeur
            if (!Application.isPlaying)
            {
                RefreshWaypoints();
            }

            if (waypoints.Count == 0) return;

            Gizmos.color = gizmoColor;

            // Dessiner les waypoints
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;

                // Sphère au waypoint
                Gizmos.DrawSphere(waypoints[i].position, waypointGizmoSize);

                // Ligne vers le prochain waypoint
                int nextIndex = (i + 1) % waypoints.Count;
                if (nextIndex < waypoints.Count && waypoints[nextIndex] != null)
                {
                    if (loop || i < waypoints.Count - 1)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
                    }
                }
            }

            // Numéros des waypoints
            #if UNITY_EDITOR
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * 0.5f, $"WP {i}");
            }
            #endif
        }
    }
}

