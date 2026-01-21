using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ennemies.Effect
{
    [DisallowMultipleComponent]
    public class EnemyBlinkEffect : MonoBehaviour
    {
        [Header("Blink")]
        [SerializeField] private Color blinkEmissionColor = Color.white;
        [SerializeField, Min(1)] private int flashes = 6;

        private struct EmissionBackup
        {
            public Renderer r;
            public bool hadEmission;
            public Color baseEmission;
        }

        private readonly List<EmissionBackup> backups = new List<EmissionBackup>();
        private Coroutine routine;

        private void Awake()
        {
            backups.Clear();
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null || r.sharedMaterial == null) continue;
                var mat = r.sharedMaterial;
                bool hasProp = mat.HasProperty("_EmissionColor");
                if (!hasProp) continue;

                bool hadEmission = mat.IsKeywordEnabled("_EMISSION");
                Color baseEmission = mat.GetColor("_EmissionColor");
                backups.Add(new EmissionBackup { r = r, hadEmission = hadEmission, baseEmission = baseEmission });
            }
        }

        public void StartBlink(float duration)
        {
            if (duration <= 0f) duration = 0.1f;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(BlinkRoutine(duration));
        }

        private IEnumerator BlinkRoutine(float duration)
        {
            if (backups.Count == 0) yield break;

            // Activer l’émission pendant le blink
            foreach (var b in backups)
            {
                if (b.r == null) continue;
                foreach (var m in b.r.materials)
                {
                    if (m != null && m.HasProperty("_EmissionColor"))
                        m.EnableKeyword("_EMISSION");
                }
            }

            float t = 0f;
            float freq = Mathf.Max(1, flashes) / duration; // nb de flashs sur la durée
            while (t < duration)
            {
                t += Time.deltaTime;
                float s = 0.5f + 0.5f * Mathf.Sin(t * freq * Mathf.PI * 2f); // 0..1
                Color target = blinkEmissionColor * Mathf.LinearToGammaSpace(s);

                foreach (var b in backups)
                {
                    if (b.r == null) continue;
                    var mats = b.r.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i];
                        if (m != null && m.HasProperty("_EmissionColor"))
                            m.SetColor("_EmissionColor", target);
                    }
                }

                yield return null;
            }

            // Restaurer l’état initial
            foreach (var b in backups)
            {
                if (b.r == null) continue;
                var mats = b.r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_EmissionColor")) continue;
                    m.SetColor("_EmissionColor", b.baseEmission);
                    if (!b.hadEmission) m.DisableKeyword("_EMISSION");
                }
            }

            routine = null;
        }
    }
}