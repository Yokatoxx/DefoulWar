using UnityEngine;
using Ennemies.Settings;
using Ennemies; // EnemyBehaviour
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

        private EnemyBehaviour controller;              // contrôleur modulaire
        private FollowCompanionBehavior followBehavior; // comportement suivi à lire
        private Transform currentCompanion;             // cible effective (mise à jour chaque frame)

        private void Awake()
        {
            owner = transform;
            nextSearchTime = Time.time + Random.Range(0f, Mathf.Max(0.01f, searchInterval));
        }

        private void Start()
        {
            // Binder après Start pour laisser EnemyBehaviour créer son currentBehavior
            controller = GetComponent<EnemyBehaviour>();
            StartCoroutine(BindFollowBehaviorWhenReady());
        }

        private System.Collections.IEnumerator BindFollowBehaviorWhenReady()
        {
            // Attendre au moins une frame que EnemyBehaviour.InitializeBehavior s’exécute
            yield return null;

            TryBindFollowBehavior();

            // Si pas trouvé, réessayer quelques fois (dans le cas de settings réassignés tardivement)
            const int maxTries = 10;
            int tries = 0;
            while (followBehavior == null && tries < maxTries)
            {
                yield return new WaitForSeconds(0.1f);
                TryBindFollowBehavior();
                tries++;
            }
        }

        private void Update()
        {
            // Si le contrôleur change de settings à runtime, re-binder
            if (controller != null &&
                controller.Settings != null &&
                controller.Settings.behaviorType == EnemyBehaviorType.CompanionFollower &&
                followBehavior == null)
            {
                TryBindFollowBehavior();
            }

            // 1) Utiliser la cible du FollowCompanionBehavior si présent
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
            if (controller == null) return;
            var settings = controller.Settings;
            if (settings == null || settings.behaviorType != EnemyBehaviorType.CompanionFollower)
                return;

            // Lire le champ privé 'currentBehavior' via réflexion (EnemyBehaviour instancie IEnemyBehavior dans Start)
            var field = typeof(EnemyBehaviour).GetField("currentBehavior",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                var beh = field.GetValue(controller) as IEnemyBehavior;
                followBehavior = beh as FollowCompanionBehavior;
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
                else
                {
                    activeLineRenderer.useWorldSpace = true;
                    activeLineRenderer.enabled = true;
                    if (activeLineRenderer.sharedMaterial == null)
                    {
                        Debug.LogWarning("[HealEnemyPower] LineRenderer n’a pas de material. Assignez-en un au prefab pour voir la ligne.");
                    }
                }
            }

            // Mettre à jour les positions de la ligne
            Vector3 start = owner.position + Vector3.up * companionLineYOffset;
            Vector3 end = currentCompanion.position + Vector3.up * companionLineYOffset;

            if (activeLineRenderer != null)
            {
                if (activeLineRenderer.positionCount != 2)
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