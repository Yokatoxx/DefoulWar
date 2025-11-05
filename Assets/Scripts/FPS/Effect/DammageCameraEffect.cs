using FPS;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DammageCameraEffect : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Volume volume;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Camera targetCamera;

    [Header("Vignette")]
    [SerializeField, Range(0f, 1f)] private float intensityAtFullHealth = 0f;
    [SerializeField, Range(0f, 1f)] private float intensityAtZeroHealth = 0.8f;
    [SerializeField] private Color vignetteColor = new Color(0.6f, 0f, 0f, 1f);
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField, Tooltip("Désactive la vignette si l'intensité est en dessous de ce seuil.")]
    private float hideThreshold = 0.01f;

    [Header("Volume")]
    [SerializeField, Tooltip("Priorité du Volume utilisé pour l'effet.")]
    private float volumePriority = 100f;

    private Vignette vignette;
    private float targetIntensity;

    private void Awake()
    {
        // Camera et Post-Processing
        if (targetCamera == null) targetCamera = Camera.main;
        var camData = targetCamera != null ? targetCamera.GetComponent<UniversalAdditionalCameraData>() : null;
        if (camData != null) camData.renderPostProcessing = true;

        // Volume
        if (volume == null)
        {
            volume = GetComponent<Volume>();
            if (volume == null && targetCamera != null)
                volume = targetCamera.GetComponent<Volume>();
            if (volume == null)
                volume = FindObjectOfType<Volume>();
        }
        if (volume == null)
        {
            Debug.LogError("[DammageCameraEffect] Aucun Volume trouvé. Placez/assignez un Global Volume.");
            enabled = false;
            return;
        }

        volume.enabled = true;
        volume.isGlobal = true;
        volume.weight = 1f;
        volume.priority = volumePriority;

        if (volume.profile == null)
        {
            Debug.LogError("[DammageCameraEffect] Volume.profile manquant. Assignez un Volume Profile.");
            enabled = false;
            return;
        }

        if (!volume.profile.TryGet(out vignette))
            vignette = volume.profile.Add<Vignette>(true);

        vignette.active = true;
        vignette.color.overrideState = true;
        vignette.color.value = vignetteColor;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0;
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
    }

    private void Update()
    {
        if (vignette == null) return;

        float current = vignette.intensity.value;
        float next = Mathf.Lerp(current, targetIntensity, Time.deltaTime * smoothSpeed);
        vignette.intensity.value = next;
        ApplyActiveFlag();
    }

    private void OnHealthChanged(float normalizedHealth)
    {
        SetTargetFromHealth(normalizedHealth);
    }

    private void SetTargetFromHealth(float health01)
    {
        float t = 1f - Mathf.Clamp01(health01);
        targetIntensity = Mathf.Lerp(intensityAtFullHealth, intensityAtZeroHealth, t);
    }

    private void ApplyActiveFlag()
    {
        // Masque la vignette quand l’intensité est négligeable
        if (vignette != null)
            vignette.active = vignette.intensity.value >= hideThreshold;
    }
}