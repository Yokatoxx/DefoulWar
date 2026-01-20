using UnityEngine;
using Ennemies.Settings;
using Ennemies; // EnemyBehaviour
using Ennemies.Behaviors; // FollowCompanionBehavior
using FPS; // EnemyHealth

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

        [Header("Healing")]
        [Tooltip("PV par seconde rendus au compagnon (si à portée).")]
        [SerializeField] private float healPerSecond = 10f;
        [Tooltip("Intervalle de tick du soin (secondes).")]
        [SerializeField] private float tickInterval = 0.25f;

        [Header("Fallback: Blink + Spawn si aucun compagnon")]
        [Tooltip("Composant responsable de l'effet de clignotement (doit exposer StartBlink(float)).")]
        [SerializeField] private MonoBehaviour blinkEffectScript; // ex: EnemyBlinkEffect
        [Tooltip("Durée du clignotement avant le spawn (secondes).")]
        [SerializeField] private float blinkDuration = 1.5f;
        [Tooltip("Temps sans compagnon avant de déclencher le blink (secondes).")]
        [SerializeField] private float noCompanionTimeout = 3f;

        private GameObject activeCompanionLine;
        private LineRenderer activeLineRenderer;

        private Transform owner;
        private float nextSearchTime;

        private EnemyBehaviour controller;              // contrôleur modulaire
        private FollowCompanionBehavior followBehavior; // comportement suivi à lire
        private Transform currentCompanion;             // cible effective (mise à jour chaque frame)

        // Healing runtime
        private EnemyHealth companionHealth;
        private float nextHealTickTime;

        private float noCompanionSince = -1f;
        private bool fallbackStarted = false;

        private void Awake()
        {
            owner = transform;
            nextSearchTime = Time.time + Random.Range(0f, Mathf.Max(0.01f, searchInterval));
        }

        private void Start()
        {
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
            // Rebind si settings changés
            if (controller != null &&
                controller.Settings != null &&
                controller.Settings.behaviorType == EnemyBehaviorType.CompanionFollower &&
                followBehavior == null)
            {
                TryBindFollowBehavior();
            }

            // 1) Déterminer le compagnon via FollowBehavior, sinon fallback
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

            HandleNoCompanionTimer();

            // 2) Mettre à jour la ligne
            UpdateCompanionLine();

            // 3) Mettre à jour le healing périodique
            UpdateCompanionHealing();
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
                if (activeLineRenderer != null)
                {
                    activeLineRenderer.useWorldSpace = true;
                    activeLineRenderer.enabled = true;
                }
                else
                {
                    Debug.LogWarning($"[HealEnemyPower] Le prefab '{companionLinePrefab.name}' ne contient pas de LineRenderer.");
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

        private void UpdateCompanionHealing()
        {
            // Conditions: cible valide, à portée, santé récupérée
            if (currentCompanion == null || !currentCompanion.gameObject.activeInHierarchy)
            {
                companionHealth = null;
                return;
            }

            float dist = Vector3.Distance(owner.position, currentCompanion.position);
            if (dist > Mathf.Max(0.01f, companionLineShowDistance))
            {
                // Trop loin => pas de heal (et pas de ligne)
                return;
            }

            // Récupérer/mémoriser EnemyHealth du compagnon
            if (companionHealth == null)
            {
                companionHealth = currentCompanion.GetComponentInParent<EnemyHealth>() ?? currentCompanion.GetComponent<EnemyHealth>();
                nextHealTickTime = Time.time + tickInterval;
            }

            if (companionHealth == null || companionHealth.IsDead)
                return;

            // Tick périodique
            if (Time.time >= nextHealTickTime)
            {
                nextHealTickTime = Time.time + Mathf.Max(0.01f, tickInterval);
                float amount = Mathf.Max(0f, healPerSecond) * Mathf.Max(0.01f, tickInterval);
                companionHealth.Heal(amount);
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

        private void HandleNoCompanionTimer()
        {
            if (currentCompanion != null)
            {
                noCompanionSince = -1f;
                fallbackStarted = false;
                return;
            }

            if (noCompanionSince < 0f)
            {
                noCompanionSince = Time.time;
            }
            else
            {
                float timeSpent = Time.time - noCompanionSince;
                if (!fallbackStarted && timeSpent >= noCompanionTimeout)
                {
                    fallbackStarted = true;
                    StartBlinkAndSpawnFallback();
                }
            }
        }

        private void StartBlinkAndSpawnFallback()
        {
            if (blinkEffectScript != null)
            {
                // Démarrer le clignotement
                var blinkMethod = blinkEffectScript.GetType().GetMethod("StartBlink",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (blinkMethod != null)
                {
                    // Appeler la méthode de clignotement avec la durée spécifiée
                    blinkMethod.Invoke(blinkEffectScript, new object[] { blinkDuration });
                }
                else
                {
                    Debug.LogWarning($"[HealEnemyPower] La méthode 'StartBlink' n'a pas été trouvée dans {blinkEffectScript.GetType().Name}.");
                }
            }
            else
            {
                Debug.LogWarning($"[HealEnemyPower] Composant de clignotement manquant sur {gameObject.name}.");
            }
        }

        private void OnDisable()
        {
            CleanupCompanionLine();
            companionHealth = null;
        }

        private void OnDestroy()
        {
            CleanupCompanionLine();
            companionHealth = null;
        }
    }
}