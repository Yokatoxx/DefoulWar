using System.Collections;
using UnityEngine;
using Ennemies.Settings;
using Ennemies.Behaviors;

namespace Ennemies.Effect
{
    public class SpawnOnNoCompanion : MonoBehaviour
    {
        [Header("Détection du compagnon")]
        [Tooltip("Activer la recherche auto si le behavior 'CompanionFollower' n'est pas accessible.")]
        [SerializeField] private bool enableAutoSearchFallback = true;
        [Tooltip("Distance maximale pour considérer un compagnon trouvé.")]
        [SerializeField] private float companionDetectRange = 20f;
        [Tooltip("Intervalle de recherche auto (secondes).")]
        [SerializeField] private float searchInterval = 1.0f;

        [Header("Clignotement + Invocation")]
        [Tooltip("Composant responsable de l'effet de clignotement.")]
        [SerializeField] private EnemyBlinkEffect blinkEffectScript; // ex: EnemyBlinkEffect
        [Tooltip("Durée de l'effet de clignotement avant le spawn.")]
        [SerializeField] private float blinkDuration = 1.5f;
        [Tooltip("Temps sans compagnon avant de déclencher le clignotement/invocation.")]
        [SerializeField] private float noCompanionTimeout = 3.0f;

        [Header("Spawn des ennemis")]
        [Tooltip("Prefabs d'ennemis à faire spawn (choisis aléatoirement).")]
        [SerializeField] private GameObject[] enemyPrefabs;
        [Tooltip("Nombre d’ennemis à faire spawn.")]
        [SerializeField] private int spawnCount = 3;
        [Tooltip("Rayon autour du healer pour le spawn.")]
        [SerializeField] private float spawnRadius = 6f;
        [Tooltip("LayerMask pour ajuster la position sur le sol via raycast (optionnel).")]
        [SerializeField] private LayerMask groundMask = ~0;

        private EnemyBehaviour controller;
        private FollowCompanionBehavior followBehavior;
        private Transform currentCompanion;

        private Transform owner;
        private float nextSearchTime;
        private float noCompanionSince = -1f;
        private bool actionStarted = false;

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

        private IEnumerator BindFollowBehaviorWhenReady()
        {
            // Laisser EnemyBehaviour initialiser son comportement
            yield return null;
            TryBindFollowBehavior();

            // Tentatives supplémentaires si settings assignés tardivement
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
            // Si le controller change de settings à runtime, re-binder
            if (controller != null &&
                controller.Settings != null &&
                controller.Settings.behaviorType == EnemyBehaviorType.CompanionFollower &&
                followBehavior == null)
            {
                TryBindFollowBehavior();
            }

            // Obtenir la cible compagnon via le behavior (sinon fallback)
            if (followBehavior != null)
            {
                currentCompanion = followBehavior.GetCompanionTarget();
            }
            else if (enableAutoSearchFallback && Time.time >= nextSearchTime)
            {
                nextSearchTime = Time.time + Mathf.Max(0.01f, searchInterval);
                currentCompanion = FindClosestEligibleCompanion(owner.position, companionDetectRange);
            }

            HandleNoCompanionTimer();
        }

        private void TryBindFollowBehavior()
        {
            if (controller == null) return;
            var settings = controller.Settings;
            if (settings == null || settings.behaviorType != EnemyBehaviorType.CompanionFollower)
                return;

            var field = typeof(EnemyBehaviour).GetField("currentBehavior",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var beh = field.GetValue(controller) as IEnemyBehavior;
                followBehavior = beh as FollowCompanionBehavior;
            }
        }

        private void HandleNoCompanionTimer()
        {
            bool hasCompanion = currentCompanion != null
                                && currentCompanion.gameObject.activeInHierarchy
                                && Vector3.Distance(owner.position, currentCompanion.position) <= companionDetectRange;

            if (hasCompanion)
            {
                // Reset du timer tant qu'un compagnon est présent à portée
                noCompanionSince = -1f;
                return;
            }

            // Démarrer le timer si pas en cours
            if (noCompanionSince < 0f)
            {
                noCompanionSince = Time.time;
            }

            // Déclencher l’action si le timeout est dépassé et pas déjà lancé
            if (!actionStarted && noCompanionSince > 0f && Time.time - noCompanionSince >= Mathf.Max(0.01f, noCompanionTimeout))
            {
                actionStarted = true;
                StartCoroutine(BlinkSpawnDestroySequence());
            }
        }

        private IEnumerator BlinkSpawnDestroySequence()
        {
            // Jouer le clignotement si disponible (appel direct, sans réflexion)
            if (blinkEffectScript != null)
            {
                blinkEffectScript.StartBlink(Mathf.Max(0.01f, blinkDuration));
            }
            else
            {
                Debug.LogWarning("[SpawnOnNoCompanion] Aucun EnemyBlinkEffect assigné.");
            }

            // Attendre la fin du clignotement
            yield return new WaitForSeconds(blinkDuration);

            // Re-check: si un compagnon a été trouvé pendant l'attente, annuler le spawn et l’auto-destruction
            if (currentCompanion != null && currentCompanion.gameObject.activeInHierarchy
                && Vector3.Distance(owner.position, currentCompanion.position) <= companionDetectRange)
            {
                actionStarted = false;
                noCompanionSince = -1f;
                yield break;
            }

            // Spawn des ennemis autour
            SpawnEnemiesAroundSelf();

            // Détruire le healer
            Destroy(gameObject);
        }

        private void SpawnEnemiesAroundSelf()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0 || spawnCount <= 0) return;

            for (int i = 0; i < spawnCount; i++)
            {
                var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                if (prefab == null) continue;

                // Position aléatoire autour
                Vector2 rand = Random.insideUnitCircle * spawnRadius;
                Vector3 pos = owner.position + new Vector3(rand.x, 0f, rand.y);

                // Ajuster sur le sol via raycast (optionnel)
                Vector3 from = pos + Vector3.up * 2f;
                if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 5f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    pos = hit.point;
                }

                Instantiate(prefab, pos, Quaternion.identity);
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
                    continue; // on ne cible pas un autre follower

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

        // Ajoutez ce helper pour auto-renseigner la référence en Editor
        private void OnValidate()
        {
            if (blinkEffectScript == null)
                blinkEffectScript = GetComponentInChildren<EnemyBlinkEffect>();
        }
    }
}