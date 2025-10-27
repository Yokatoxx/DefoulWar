using Proto3GD.FPS;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraDashEffect : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private PillarDashSystem pillarDashSystem;


    [SerializeField] private float dashEffectIntensity = -0.5f;
    [SerializeField] private float dashEffectDuration = 0.2f;

    private LensDistortion lensDistortion;

    private void Start()
    {
        if (volume == null)
        {
            volume = GetComponent<Volume>();
        }
        if (volume != null && volume.profile.TryGet<LensDistortion>(out lensDistortion))
        {
            // Initialiser l'effet à zéro
            lensDistortion.intensity.value = 0f;
        }
        else
        {
            Debug.LogError("LensDistortion not found in Volume profile.");
        }
    }

    private void Update()
    {
        // Pour tester l'effet avec la touche Left Shift
        if (pillarDashSystem.isDashing)
        {
            StartCoroutine(PlayDashEffect());
        }
    }

    private System.Collections.IEnumerator PlayDashEffect()
    {
        float elapsed = 0f;
        // Appliquer l'effet
        while (elapsed < dashEffectDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / dashEffectDuration;
            lensDistortion.intensity.value = Mathf.Lerp(dashEffectIntensity, 0f, normalizedTime);
            yield return null;
        }
        // S'assurer que l'effet est réinitialisé
        lensDistortion.intensity.value = 0f;
    }

}
