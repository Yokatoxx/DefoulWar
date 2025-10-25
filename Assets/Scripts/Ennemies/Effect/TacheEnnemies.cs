using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine.UI;
using Proto3GD.FPS;

namespace Ennemies.Effect
{
    /// <summary>
    /// Ennemi : chasse rapidement les balles détectées, puis revient au suivi simple du joueur.
    /// À la mort (hors dash), affiche une tache aléatoire en overlay sur l'écran.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class TacheEnnemies : MonoBehaviour
    {
        [Header("Réaction aux balles")]
        [SerializeField] private float bulletDetectionRadius = 15f;
        [SerializeField] private float chaseBulletSpeedMultiplier = 5.5f;
        [SerializeField] private float maxChaseBulletTime = 1.25f;
        [SerializeField] private float bulletScanInterval = 0.05f;
        [SerializeField] private float chaseAccelerationMultiplier = 5f;
        [SerializeField] private float chaseAngularSpeed = 720f;

        [Header("IA externe à désactiver pendant la chasse (optionnel)")]
        [SerializeField] private MonoBehaviour[] behavioursToDisableDuringChase;

        [Header("Tache Caméra (à la mort par balle)")]
        [SerializeField] private Sprite tacheSprite;
        [SerializeField] private float tacheDuration = 5f;
        [SerializeField, Range(0f, 1f)] private float tacheMaxAlpha = 0.7f;
        [SerializeField] private Vector2 tacheSizeRange = new Vector2(280, 520);
        [SerializeField] private bool allowStacking = true;
        [SerializeField] private int maxStackedTaches = 5;
        [SerializeField] private float fadeInTime = 0.08f;
        [SerializeField] private float fadeOutTime = 0.5f;

        // Références
        private EnemyHealth health;
        private NavMeshAgent agent;
        private Transform player;

        // Sauvegarde des valeurs agent
        private float baseSpeed;
        private float baseAcceleration;
        private float baseAngularSpeed;

        // État chasse
        private Transform currentBullet;
        private float chaseTimer;
        private float bulletScanTimer;

        // Buffers non alloc
        private static readonly Collider[] BulletScanBuffer = new Collider[64];

        // Overlay taches (persistant)
        private static Canvas _tacheCanvas;
        private static readonly List<Image> ActiveTaches = new List<Image>();
        private static int _overlaySortingOrder = 5000;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            agent = GetComponent<NavMeshAgent>();

            baseSpeed = agent.speed;
            baseAcceleration = agent.acceleration;
            baseAngularSpeed = agent.angularSpeed;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        private void OnEnable()
        {
            if (health != null)
                health.OnDeath.AddListener(OnEnemyDeath);
        }

        private void OnDisable()
        {
            if (health != null)
                health.OnDeath.RemoveListener(OnEnemyDeath);

            // Réactivation de sécurité
            ToggleExternalBehaviours(true);
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;
            if (agent == null) return;

            // 1) Poursuite d'une balle en cours
            if (currentBullet != null)
            {
                chaseTimer += Time.deltaTime;

                if (currentBullet == null || currentBullet.gameObject == null)
                {
                    StopChasingAndFollowPlayer();
                }
                else
                {
                    // Paramètres "très rapides"
                    agent.speed = baseSpeed * chaseBulletSpeedMultiplier;
                    agent.acceleration = baseAcceleration * chaseAccelerationMultiplier;
                    agent.angularSpeed = chaseAngularSpeed;
                    agent.autoBraking = false;
                    agent.isStopped = false;
                    agent.SetDestination(currentBullet.position);

                    // Timeout / trop loin -> arrêt de la chasse
                    if (chaseTimer >= maxChaseBulletTime ||
                        Vector3.Distance(transform.position, currentBullet.position) > bulletDetectionRadius * 1.5f)
                    {
                        StopChasingAndFollowPlayer();
                    }
                }
                return;
            }

            // 2) Scanner périodiquement les balles
            bulletScanTimer += Time.deltaTime;
            if (bulletScanTimer >= bulletScanInterval)
            {
                bulletScanTimer = 0f;
                TryAcquireBulletTarget();
            }

            // 3) Suivi simple du joueur (fallback)
            if (player != null)
            {
                SetAgentBaseParams();
                agent.autoBraking = true;
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }

        private void TryAcquireBulletTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, bulletDetectionRadius, BulletScanBuffer);
            Transform best = null;
            float bestDist = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                var h = BulletScanBuffer[i];
                if (h == null) continue;

                var bullet = h.GetComponentInParent<Bullet>() ?? h.GetComponent<Bullet>();
                if (bullet == null) continue;

                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb == null) continue;

                // Privilégier les balles qui bougent réellement
                if (rb.linearVelocity.sqrMagnitude < 0.05f) continue;

                float dist = Vector3.Distance(transform.position, bullet.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = bullet.transform;
                }
            }

            if (best != null)
            {
                StartChasingBullet(best);
            }
        }

        private void StartChasingBullet(Transform bullet)
        {
            currentBullet = bullet;
            chaseTimer = 0f;

            // Désactiver l'IA externe optionnelle
            ToggleExternalBehaviours(false);

            agent.ResetPath();
            agent.speed = baseSpeed * chaseBulletSpeedMultiplier;
            agent.acceleration = baseAcceleration * chaseAccelerationMultiplier;
            agent.angularSpeed = chaseAngularSpeed;
            agent.autoBraking = false;
            agent.isStopped = false;
        }

        private void StopChasingAndFollowPlayer()
        {
            currentBullet = null;
            chaseTimer = 0f;

            // Réactiver l'IA externe optionnelle
            ToggleExternalBehaviours(true);

            // Revenir au suivi joueur simple
            agent.ResetPath();
            SetAgentBaseParams();
            agent.autoBraking = true;
            if (player != null)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }

        private void SetAgentBaseParams()
        {
            agent.speed = baseSpeed;
            agent.acceleration = baseAcceleration;
            agent.angularSpeed = baseAngularSpeed;
        }

        private void ToggleExternalBehaviours(bool enable)
        {
            if (behavioursToDisableDuringChase == null) return;
            for (int i = 0; i < behavioursToDisableDuringChase.Length; i++)
            {
                var b = behavioursToDisableDuringChase[i];
                if (b == null) continue;
                b.enabled = enable;
            }
        }

        private void OnEnemyDeath()
        {
            GameObject root = health != null ? health.transform.root.gameObject : gameObject;
            bool dashKill = PillarDashSystem.WasKilledByDash(root);
            if (dashKill) return;
            TrySpawnCameraTache();
        }

        private static Sprite _fallbackTacheSprite;
        private static Sprite GetFallbackSprite()
        {
            if (_fallbackTacheSprite != null) return _fallbackTacheSprite;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 ink = new Color32(60, 10, 10, 200);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float edge = Mathf.SmoothStep(1f, 0.9f + Mathf.PerlinNoise(nx * 4f + 0.5f, ny * 4f + 0.5f) * 0.1f, r);
                    tex.SetPixel(x, y, r <= edge ? ink : clear);
                }
            }
            tex.Apply(false, true);
            _fallbackTacheSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _fallbackTacheSprite;
        }

        private void TrySpawnCameraTache()
        {
            if (tacheSprite == null)
            {
                tacheSprite = GetFallbackSprite();
            }

            EnsureOverlayCanvas();
            if (_tacheCanvas == null) return;

            if (!allowStacking && ActiveTaches.Count > 0)
            {
                var old = ActiveTaches[0];
                if (old != null) Destroy(old.gameObject);
                ActiveTaches.Clear();
            }
            else if (allowStacking && ActiveTaches.Count >= maxStackedTaches)
            {
                var first = ActiveTaches[0];
                if (first != null) Destroy(first.gameObject);
                ActiveTaches.RemoveAt(0);
            }

            GameObject go = new GameObject("TacheOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_tacheCanvas.transform, false);

            Image img = go.GetComponent<Image>();
            img.sprite = tacheSprite;
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float size = Random.Range(tacheSizeRange.x, tacheSizeRange.y);
            rt.sizeDelta = new Vector2(size, size);

            float margin = size * 0.5f + 8f;
            float w = Screen.width;
            float h = Screen.height;
            float x = Random.Range(margin, Mathf.Max(margin, w - margin));
            float y = Random.Range(margin, Mathf.Max(margin, h - margin));
            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            ActiveTaches.Add(img);
            StartCoroutine(FadeAndRemove(img, tacheDuration, tacheMaxAlpha));
        }

        private IEnumerator FadeAndRemove(Image img, float duration, float maxAlpha)
        {
            if (img == null) yield break;

            float t = 0f;
            while (t < fadeInTime)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(0f, maxAlpha, Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeInTime)));
                var c = img.color; c.a = a; img.color = c;
                yield return null;
            }

            float remain = Mathf.Max(0f, duration - fadeInTime - fadeOutTime);
            if (remain > 0f) yield return new WaitForSeconds(remain);

            t = 0f;
            while (t < fadeOutTime)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(maxAlpha, 0f, Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeOutTime)));
                var c = img.color; c.a = a; img.color = c;
                yield return null;
            }

            if (img != null)
            {
                ActiveTaches.Remove(img);
                Destroy(img.gameObject);
            }
        }

        private void EnsureOverlayCanvas()
        {
            if (_tacheCanvas != null) return;

            var existing = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .FirstOrDefault(c => c != null && c.name == "TacheOverlayCanvas");
            if (existing != null)
            {
                _tacheCanvas = existing;
                _tacheCanvas.sortingOrder = _overlaySortingOrder;
                return;
            }

            GameObject go = new GameObject("TacheOverlayCanvas");
            _tacheCanvas = go.AddComponent<Canvas>();
            _tacheCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _tacheCanvas.sortingOrder = _overlaySortingOrder;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.6f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, bulletDetectionRadius);
        }
    }
}
