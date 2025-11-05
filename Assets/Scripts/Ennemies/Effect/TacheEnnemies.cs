using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine.UI;
using FPS;

namespace Ennemies.Effect
{

    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class TacheEnnemies : MonoBehaviour
    {
        [Header("Dash sur le joueur")]
        [SerializeField] private float dashDetectionRange = 10f;
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.5f;
        [SerializeField] private float dashCooldown = 3f;
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float explosionDamage = 50f;

        [Header("Tache Caméra")]
        [SerializeField] private Sprite tacheSprite;
        [SerializeField] private float tacheDuration = 5f;
        [SerializeField, Range(0f, 1f)] private float tacheMaxAlpha = 0.7f;
        [SerializeField] private Vector2 tacheSizeRange = new Vector2(280, 520);
        [SerializeField] private bool allowStacking = true;
        [SerializeField] private int maxStackedTaches = 5;
        [SerializeField] private float fadeInTime = 0.08f;
        [SerializeField] private float fadeOutTime = 0.5f;

        private EnemyHealth health;
        private NavMeshAgent agent;
        private Transform player;

        private bool isDashing;
        private float dashTimer;
        private float cooldownTimer;
        private Vector3 dashDirection;

        private static readonly Collider[] ExplosionBuffer = new Collider[32];

        private static Canvas _tacheCanvas;
        private static readonly List<Image> ActiveTaches = new List<Image>();
        private static int _overlaySortingOrder = 5000;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            agent = GetComponent<NavMeshAgent>();

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;
            if (agent == null || player == null) return;
            
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }
            
            if (isDashing)
            {
                dashTimer += Time.deltaTime;
                
                if (dashTimer >= dashDuration)
                {
                    Explode();
                    return;
                }

                transform.position += dashDirection * (dashSpeed * Time.deltaTime);
                return;
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= dashDetectionRange && cooldownTimer <= 0f)
            {
                StartDash();
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }

        private void StartDash()
        {
            isDashing = true;
            dashTimer = 0f;
            cooldownTimer = dashCooldown;
            
            dashDirection = (player.position - transform.position).normalized;
            agent.enabled = false;

            transform.forward = dashDirection;
        }

        private void Explode()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, ExplosionBuffer);
            
            for (int i = 0; i < count; i++)
            {
                var col = ExplosionBuffer[i];
                if (col == null) continue;

                var healthComp = col.GetComponent<EnemyHealth>();
                if (healthComp != null)
                {
                    healthComp.TakeDamage(explosionDamage);
                    continue;
                }

                var playerHealth = col.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(explosionDamage);
                }
            }

            TrySpawnCameraTache();
            Destroy(gameObject);
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
            
            var coroutineRunner = _tacheCanvas.GetComponent<TacheCoroutineRunner>();
            if (coroutineRunner == null)
            {
                coroutineRunner = _tacheCanvas.gameObject.AddComponent<TacheCoroutineRunner>();
            }
            coroutineRunner.StartCoroutine(FadeAndRemove(img, tacheDuration, tacheMaxAlpha, fadeInTime, fadeOutTime));
        }

        private static IEnumerator FadeAndRemove(Image img, float duration, float maxAlpha, float fadeIn, float fadeOut)
        {
            if (img == null) yield break;

            fadeIn = Mathf.Max(0.01f, fadeIn);
            fadeOut = Mathf.Max(0.01f, fadeOut);

            float t = 0f;
            while (t < fadeIn)
            {
                if (img == null) yield break;
                t += Time.deltaTime;
                float a = Mathf.Lerp(0f, maxAlpha, t / fadeIn);
                var c = img.color; c.a = a; img.color = c;
                yield return null;
            }

            float remain = Mathf.Max(0f, duration - fadeIn - fadeOut);
            if (remain > 0f) yield return new WaitForSeconds(remain);

            t = 0f;
            while (t < fadeOut)
            {
                if (img == null) yield break;
                t += Time.deltaTime;
                float a = Mathf.Lerp(maxAlpha, 0f, t / fadeOut);
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
            Gizmos.DrawWireSphere(transform.position, dashDetectionRange);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }

    public class TacheCoroutineRunner : MonoBehaviour
    {
    }
}
