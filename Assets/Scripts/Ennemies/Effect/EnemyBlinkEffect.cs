using System.Collections;
using UnityEngine;

namespace Ennemies.Effect
{
    [DisallowMultipleComponent]
    public class EnemyBlinkEffect : MonoBehaviour
    {
        [Header("Blink Object")]
        [Tooltip("Prefab optionnel du visuel de blink. Si null, un simple Sphere primitive est créée.")]
        [SerializeField] private GameObject blinkVisualPrefab;
        [Tooltip("Taille minimale du blink (échelle locale).")]
        [SerializeField] private float minScale = 0.8f;
        [Tooltip("Taille maximale du blink (échelle locale).")]
        [SerializeField] private float maxScale = 1.3f;
        [Tooltip("Vitesse d’oscillation (cycles par seconde).")]
        [SerializeField] private float pulseFrequency = 3f;
        [Tooltip("Offset de position du visuel par rapport au centre de l’ennemi.")]
        [SerializeField] private Vector3 visualOffset = new Vector3(0f, 1f, 0f);
        [Tooltip("Couleur du visuel si primitive générée.")]
        [SerializeField] private Color visualColor = new Color(1f, 1f, 1f, 0.35f);

        private GameObject spawnedVisual;
        private Coroutine routine;

        public void StartBlink(float duration)
        {
            if (duration <= 0f) duration = 0.1f;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(BlinkRoutine(duration));
        }

        private IEnumerator BlinkRoutine(float duration)
        {
            SpawnVisualIfNeeded();
            if (spawnedVisual == null) yield break;

            Transform vis = spawnedVisual.transform;
            Vector3 baseScale = Vector3.one;
            float t = 0f;
            float twoPi = Mathf.PI * 2f;
            float freq = Mathf.Max(0.1f, pulseFrequency);

            while (t < duration && spawnedVisual != null)
            {
                t += Time.deltaTime;
                // Oscillation sinusoïdale entre minScale et maxScale
                float s = 0.5f + 0.5f * Mathf.Sin(t * twoPi * freq);
                float scale = Mathf.Lerp(minScale, maxScale, s);
                vis.localScale = baseScale * scale;

                // Suivre le transform parent (au cas où il bouge)
                vis.position = transform.position + visualOffset;
                yield return null;
            }

            CleanupVisual();
            routine = null;
        }

        private void SpawnVisualIfNeeded()
        {
            CleanupVisual();

            if (blinkVisualPrefab != null)
            {
                spawnedVisual = Instantiate(blinkVisualPrefab, transform.position + visualOffset, Quaternion.identity);
            }
            else
            {
                // Fallback: créer une sphère translucide
                spawnedVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spawnedVisual.transform.position = transform.position + visualOffset;
                var mr = spawnedVisual.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = new Material(Shader.Find("HDRP/Lit"));
                    mat.SetColor("_BaseColor", visualColor);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mr.material = mat;
                }
                // Optional: retirer le collider
                var col = spawnedVisual.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            // Parenté pour faciliter le nettoyage
            spawnedVisual.transform.SetParent(null); // en world pour éviter scaling parent
            spawnedVisual.transform.localScale = Vector3.one * minScale;
        }

        private void CleanupVisual()
        {
            if (spawnedVisual != null)
            {
                Destroy(spawnedVisual);
                spawnedVisual = null;
            }
        }

        private void OnDisable()
        {
            CleanupVisual();
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private void OnDestroy()
        {
            CleanupVisual();
        }
    }
}