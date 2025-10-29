using UnityEngine;

public class WeaponShake : MonoBehaviour
{
    [SerializeField] private WeaponSettings weaponSettings;

    [Header("Cible du shake")]
    [Tooltip("Transform sur lequel appliquer le shake (souvent un enfant visuel). Si null, utilise ce GameObject.")]
    [SerializeField] private Transform shakeTransform;

    [Header("Paramètres")]
    [SerializeField] private float defaultDuration = 0.1f;
    [SerializeField] private float defaultMagnitude = 0.1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private bool usePerlin = true;
    [SerializeField] private float perlinFrequency = 25f;

    private Vector3 currentOffset;
    private Coroutine shakeRoutine;
    private float perlinSeedX;
    private float perlinSeedY;

    private void Awake()
    {
        if (shakeTransform == null) shakeTransform = transform;
        // Seeds aléatoires pour le bruit Perlin
        perlinSeedX = Random.value * 1000f;
        perlinSeedY = Random.value * 1000f;
    }

    public void Shake()
    {
        float mag = weaponSettings != null ? weaponSettings.shakeAmount : defaultMagnitude;
        Shake(mag, defaultDuration);
    }

    public void Shake(float magnitude, float duration)
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            ApplyOffset(Vector3.zero); // retirer l’offset restant
        }
        shakeRoutine = StartCoroutine(ShakeCoroutine(Mathf.Max(0f, duration), Mathf.Max(0f, magnitude)));
    }

    private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = duration > 0f ? elapsed / duration : 1f;
            float strength = fadeCurve != null ? fadeCurve.Evaluate(t) : 1f;

            Vector2 r;
            if (usePerlin)
            {
                float time = Time.time * perlinFrequency;
                float px = Mathf.PerlinNoise(perlinSeedX, time) * 2f - 1f;
                float py = Mathf.PerlinNoise(perlinSeedY, time) * 2f - 1f;
                r = new Vector2(px, py);
            }
            else
            {
                r = Random.insideUnitCircle;
            }

            Vector3 newOffset = new Vector3(r.x, r.y, 0f) * (magnitude * strength);

            // Applique seulement le delta pour ne pas écraser la position de base
            ApplyOffset(newOffset);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Retire l’offset à la fin
        ApplyOffset(Vector3.zero);
        shakeRoutine = null;
    }

    private void ApplyOffset(Vector3 newOffset)
    {
        // Ajouter seulement la différence pour ne pas se battre avec d’autres scripts qui positionnent le Transform
        Vector3 delta = newOffset - currentOffset;
        if (delta.sqrMagnitude != 0f && shakeTransform != null)
        {
            shakeTransform.localPosition += delta;
        }
        currentOffset = newOffset;
    }

    private void OnDisable()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        ApplyOffset(Vector3.zero);
    }
}