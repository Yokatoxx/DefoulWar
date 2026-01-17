using UnityEngine;
using Ennemies.Settings;
using Ennemies; // EnnemiBehaviour
using Ennemies.Behaviors; // FollowCompanionBehavior

namespace Ennemies.Effect
{
    public class HealEnemyPower : MonoBehaviour
    {
        [Header("Line Effect")]
        [SerializeField] private GameObject companionLinePrefab;
        [SerializeField] private float companionLineShowDistance = 12f;
        [SerializeField] private float companionLineYOffset = 0.5f;

        [Header("Recherche auto (fallback si pas de FollowCompanionBehavior)")]
        [SerializeField] private bool enableAutoSearchFallback = true;
        [SerializeField] private float autoSearchRange = 25f;
        [SerializeField] private float searchInterval = 1.0f;

        private GameObject activeCompanionLine;
        private LineRenderer activeLineRenderer;

        private Transform owner;
        private float nextSearchTime;

        private FollowCompanionBehavior followBehavior; // comportement suiveur à partir duquel on lit la cible
        private Transform currentCompanion;             // cible effective (mise à jour chaque frame)

        private void Awake()
        {
            owner = transform;
            nextSearchTime = Time.time + Random.Range(0f, Mathf.Max(0.01f, searchInterval));
            TryBindFollowBehavior();
        }

        private void Update()
        {
            // 1) Si FollowCompanionBehavior est présent: utiliser sa cible
            if (followBehavior != null)
            {
                currentCompanion = followBehavior.GetCompanionTarget();
            }
            else if (enableAutoSearchFallback)
            {
                // Fallback: recherche périodique d'un compagnon éligible
                bool needsSearch = currentCompanion == null
                                   || !currentCompanion.gameObject.activeInHierarchy
                                   || Time.time >= nextSearchTime;

                if (needsSearch)
                {
                    nextSearchTime = Time.time + Mathf.Max(0.01f, searchInterval);
                    currentCompanion = FindClosestEligibleCompanion(owner.position, Mathf.Max(0.01f, autoSearchRange));
                }
            }

            // 2) Mettre à jour l’effet de ligne
            UpdateCompanionLine();
        }

        private void TryBindFollowBehavior()
        {
            // Récupérer le contrôleur modulaire
            var controller = GetComponent<EnemyBehaviour>();
            if (controller == null) return;

            // Si le type configuré est CompanionFollower, essayer de récupérer le comportement
            if (controller.Settings != null && controller.Settings.behaviorType == EnemyBehaviorType.CompanionFollower)
            {
                // Essayer via réflexion de lire le champ privé 'currentBehavior' et le caster
                var field = typeof(EnemyBehaviour).GetField("currentBehavior",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    var beh = field.GetValue(controller) as IEnemyBehavior;
                    followBehavior = beh as FollowCompanionBehavior;
                }

                // Si le contrôleur expose une API publique à l’avenir, remplacer par:
                // followBehavior = controller.GetCurrentBehavior() as FollowCompanionBehavior;
            }
        }

        private void UpdateCompanionLine()
        {
            // Pas de cible ou pas de prefab -> retirer l'effet
            if (currentCompanion == null || companionLinePrefab == null)
            {
                CleanupCompanionLine();
                return;
            }

            // Cible désactivée -> retirer
            if (!currentCompanion.gameObject.activeInHierarchy)
            {
                CleanupCompanionLine();
                return;
            }

            float dist = Vector3.Distance(owner.position, currentCompanion.position);
            bool shouldShow = dist <= Mathf.Max(0.01f, companionLineShowDistance);

            if (!shouldShow)
            {
                CleanupCompanionLine();
                return;
            }

            // Instancier la ligne si nécessaire
            if (activeCompanionLine == null)
            {
                activeCompanionLine = Instantiate(companionLinePrefab);
                activeLineRenderer = activeCompanionLine.GetComponent<LineRenderer>();
                if (activeLineRenderer == null)
                {
                    Debug.LogWarning($"[HealEnemyPower] Le prefab '{companionLinePrefab.name}' ne contient pas de LineRenderer.");
                }
            }

            // Mettre à jour les positions de la ligne
            Vector3 start = owner.position + Vector3.up * companionLineYOffset;
            Vector3 end = currentCompanion.position + Vector3.up * companionLineYOffset;

            if (activeLineRenderer != null)
            {
                activeLineRenderer.positionCount = 2;
                activeLineRenderer.SetPosition(0, start);
                activeLineRenderer.SetPosition(1, end);
            }
            else
            {
                activeCompanionLine.transform.position = (start + end) * 0.5f;
                activeCompanionLine.transform.rotation = Quaternion.LookRotation(end - start);
            }
        }

        private void CleanupCompanionLine()
        {
            if (activeCompanionLine != null)
            {
                Destroy(activeCompanionLine);
                activeCompanionLine = null;
                activeLineRenderer = null;
            }
        }

        private Transform FindClosestEligibleCompanion(Vector3 fromPos, float range)
        {
            float bestDist = float.PositiveInfinity;
            Transform best = null;

            var enemies = Object.FindObjectsByType<EnemyBehaviour>(FindObjectsSortMode.None);
            foreach (var eb in enemies)
            {
                if (eb == null) continue;
                var t = eb.transform;
                if (t == owner) continue;

                // Exclure ceux configurés en CompanionFollower (on ne trace pas vers eux)
                var s = eb.Settings;
                if (s != null && s.behaviorType == EnemyBehaviorType.CompanionFollower)
                    continue;

                float d = Vector3.Distance(fromPos, t.position);
                if (d > range) continue;

                if (d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }

            return best;
        }

        private void OnDisable()
        {
            CleanupCompanionLine();
        }

        private void OnDestroy()
        {
            CleanupCompanionLine();
        }
    }
}