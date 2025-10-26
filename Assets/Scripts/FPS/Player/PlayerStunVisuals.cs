using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Affiche un effet d'éclairs en overlay sur la caméra quand le joueur est stun.
    /// Ne dépend pas d'events: détecte l'état via PlayerStunAutoFire.IsStunned.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerStunVisuals : MonoBehaviour
    {
        [Header("Lightning Overlay")]
        [Tooltip("Sprites d'éclairs à afficher en overlay (facultatif mais recommandé)")]
        [SerializeField] private Sprite[] lightningSprites;

        [Tooltip("Couleur de base des éclairs")]
        [SerializeField] private Color lightningColor = new Color(0.6f, 0.85f, 1f, 0.8f);

        [Tooltip("Nombre approximatif d'éclairs simultanés")]
        [Min(1)]
        [SerializeField] private int concurrentBolts = 4;

        [Tooltip("Intervalle moyen entre les éclairs (secondes)")]
        [Min(0.01f)]
        [SerializeField] private float flickerInterval = 0.08f;

        [Tooltip("Variation aléatoire autour de l'intervalle moyen (secondes)")]
        [Min(0f)]
        [SerializeField] private float flickerJitter = 0.05f;

        [Tooltip("Durée de vie d'un éclair (secondes)")]
        [Min(0.01f)]
        [SerializeField] private float boltLifetime = 0.12f;

        [Header("Placement & Taille")]
        [Tooltip("Échelle min/max des éclairs en proportion de l'écran")]
        [SerializeField] private Vector2 boltScaleRange = new Vector2(0.6f, 1.2f);

        [Tooltip("Marge intérieure (en pixels) pour éviter de coller aux bords")]
        [Min(0f)]
        [SerializeField] private float screenMargin = 20f;

        [Header("Optionnel: Camera Shake")]
        [SerializeField] private bool useCameraShake = true;
        [SerializeField] private float shakeDuration = 0.08f;
        [SerializeField] private float shakePositionMagnitude = 0.02f;
        [SerializeField] private float shakeRotationMagnitude = 0.5f;

        private PlayerStunAutoFire stun;
        private Transform cameraTransform;
        private Camera targetCamera;
        private Canvas overlayCanvas;
        private RectTransform overlayRect;
        private readonly List<Image> activeBolts = new List<Image>();
        private Coroutine flickerRoutine;
        private bool lastStunState;

        private void Awake()
        {
            stun = GetComponent<PlayerStunAutoFire>();
            if (stun == null)
            {
                stun = GetComponentInParent<PlayerStunAutoFire>();
            }
        }

        private void Start()
        {
            // Tenter d'obtenir la caméra via FPSPlayerController (recommandé)
            var playerController = GetComponent<FPSPlayerController>();
            if (playerController != null)
            {
                cameraTransform = playerController.CameraTransform;
            }

            // Fallback: chercher une Camera dans les enfants
            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }

            if (cameraTransform != null)
            {
                targetCamera = cameraTransform.GetComponent<Camera>();
            }

            EnsureOverlay();
            SetOverlayActive(false);
        }

        private void Update()
        {
            bool isStunned = stun != null && stun.IsStunned;
            if (isStunned != lastStunState)
            {
                if (isStunned) OnStunStart(); else OnStunEnd();
                lastStunState = isStunned;
            }
        }

        private void OnDisable()
        {
            StopEffect();
            SetOverlayActive(false);
        }

        private void EnsureOverlay()
        {
            if (overlayCanvas != null) return;

            GameObject go = new GameObject("StunLightningOverlay", typeof(Canvas), typeof(CanvasGroup));
            if (cameraTransform != null) go.transform.SetParent(cameraTransform, false);
            overlayCanvas = go.GetComponent<Canvas>();
            var canvasGroup = go.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (targetCamera != null)
            {
                overlayCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                overlayCanvas.worldCamera = targetCamera;
                overlayCanvas.planeDistance = 0.3f; // proche de la caméra
            }
            else
            {
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            overlayRect = overlayCanvas.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
        }

        private void SetOverlayActive(bool value)
        {
            if (overlayCanvas != null)
            {
                overlayCanvas.enabled = value;
            }
        }

        private void OnStunStart()
        {
            EnsureOverlay();
            SetOverlayActive(true);
            StartEffect();
        }

        private void OnStunEnd()
        {
            StopEffect();
            ClearBolts();
            SetOverlayActive(false);
        }

        private void StartEffect()
        {
            if (flickerRoutine != null) StopCoroutine(flickerRoutine);
            flickerRoutine = StartCoroutine(FlickerLoop());
        }

        private void StopEffect()
        {
            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
            }
        }

        private IEnumerator FlickerLoop()
        {
            // Maintenir un nombre d'éclairs moyen et les renouveler rapidement
            while (stun != null && stun.IsStunned)
            {
                // Nettoyer les bolts expirés
                for (int i = activeBolts.Count - 1; i >= 0; i--)
                {
                    if (activeBolts[i] == null)
                    {
                        activeBolts.RemoveAt(i);
                    }
                }

                // Spawn de nouveaux éclairs si besoin
                int toSpawn = Mathf.Max(0, concurrentBolts - activeBolts.Count);
                for (int i = 0; i < toSpawn; i++)
                {
                    SpawnBolt();
                }

                // Camera shake léger (optionnel)
                if (useCameraShake && CameraShake.Instance != null)
                {
                    CameraShake.Instance.ShakeWithRotation(shakeDuration, shakePositionMagnitude, shakeRotationMagnitude);
                }

                // Recalcule l'intervalle (pour jitter)
                float nextDelay = NextInterval();
                yield return new WaitForSeconds(nextDelay);
            }
        }

        private float NextInterval()
        {
            if (flickerJitter <= 0f) return flickerInterval;
            return Mathf.Max(0.01f, flickerInterval + Random.Range(-flickerJitter, flickerJitter));
        }

        private void SpawnBolt()
        {
            if (overlayRect == null) return;

            var boltGo = new GameObject("Bolt", typeof(RectTransform), typeof(Image));
            boltGo.transform.SetParent(overlayRect, false);
            var rt = (RectTransform)boltGo.transform;
            var img = boltGo.GetComponent<Image>();

            // Sprite & couleur
            if (lightningSprites != null && lightningSprites.Length > 0)
            {
                img.sprite = lightningSprites[Random.Range(0, lightningSprites.Length)];
                img.preserveAspect = true;
            }
            else
            {
                // Fallback: rectangle fin vertical
                Texture2D tex = Texture2D.whiteTexture;
                img.sprite = Sprite.Create(tex, new Rect(0,0,tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            img.color = lightningColor;

            // Taille/position aléatoires
            float scale = Random.Range(boltScaleRange.x, boltScaleRange.y);
            float w = overlayRect.rect.width;
            float h = overlayRect.rect.height;
            float x = Random.Range(screenMargin, Mathf.Max(screenMargin, w - screenMargin));
            float y = Random.Range(screenMargin, Mathf.Max(screenMargin, h - screenMargin));

            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.localScale = Vector3.one * scale;
            rt.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            activeBolts.Add(img);
            StartCoroutine(BoltLifetime(img));
        }

        private IEnumerator BoltLifetime(Image img)
        {
            if (img == null) yield break;
            float t = 0f;
            float half = boltLifetime * 0.5f;
            var cg = overlayCanvas != null ? overlayCanvas.GetComponent<CanvasGroup>() : null;

            while (t < boltLifetime && img != null)
            {
                t += Time.deltaTime;
                float a;
                if (t < half)
                {
                    // fondu entrée
                    a = Mathf.InverseLerp(0f, half, t);
                }
                else
                {
                    // fondu sortie
                    a = 1f - Mathf.InverseLerp(half, boltLifetime, t);
                }

                Color c = img.color;
                c.a = a * lightningColor.a;
                img.color = c;

                // Légère pulsation globale possible via CanvasGroup (non obligatoire)
                if (cg != null)
                {
                    cg.alpha = Mathf.Lerp(0.9f, 1f, Random.value);
                }

                yield return null;
            }

            if (img != null)
            {
                activeBolts.Remove(img);
                if (img.gameObject != null)
                {
                    Destroy(img.gameObject);
                }
            }
        }

        private void ClearBolts()
        {
            for (int i = 0; i < activeBolts.Count; i++)
            {
                var img = activeBolts[i];
                if (img != null && img.gameObject != null)
                {
                    Destroy(img.gameObject);
                }
            }
            activeBolts.Clear();
        }
    }
}
