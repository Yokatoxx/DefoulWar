using UnityEngine;

public class WeaponShake : MonoBehaviour
{
    [Header("Cible")]
    [Tooltip("Transform sur lequel appliquer le recul/shake (souvent un enfant visuel). Si null, utilise ce GameObject.")]
    [SerializeField] private Transform shakeTransform;

    [Header("Recoil (défauts)")]
    [SerializeField] private float recoilKickBack = 0.045f;   // m, déplacement vers -Z local
    [SerializeField] private float recoilKickUp = 1.5f;       // ° pitch (X)
    [SerializeField] private float recoilRandomYaw = 0.6f;    // ° (Y) aléatoire
    [SerializeField] private float recoilRandomRoll = 0.25f;  // ° (Z) aléatoire
    [SerializeField] private float recoilInDuration = 0.06f;  // s vers le pic
    [SerializeField] private float recoilOutDuration = 0.12f; // s retour à neutre
    [SerializeField] private AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shake léger")]
    [SerializeField] private float shakeDuration = 0.08f;
    [SerializeField] private float shakeMagnitude = 0.008f; // m
    [SerializeField] private bool shakeUsePerlin = true;
    [SerializeField] private float shakePerlinFreq = 25f;

    // Offsets séparés (recoil + shake), appliqués de manière additive
    private Vector3 posOffRecoil, posOffShake, appliedPos;
    private Quaternion rotOffRecoil = Quaternion.identity, rotOffShake = Quaternion.identity, appliedRot = Quaternion.identity;

    private Coroutine recoilCo;
    private Coroutine shakeCo;
    private float perlinSeedX, perlinSeedY;

    private void Awake()
    {
        if (shakeTransform == null) shakeTransform = transform;
        perlinSeedX = Random.value * 1000f;
        perlinSeedY = Random.value * 2000f;
        appliedPos = Vector3.zero;
        appliedRot = Quaternion.identity;
    }

    // API simple: utilise les valeurs par défaut
    public void Recoil()
    {
        Recoil(recoilKickBack, recoilKickUp, recoilRandomYaw, recoilRandomRoll, recoilInDuration, recoilOutDuration);
    }

    // API avancée
    public void Recoil(float kickBack, float kickUpDeg, float yawRandDeg, float rollRandDeg, float inDur, float outDur)
    {
        if (recoilCo != null) StopCoroutine(recoilCo);
        recoilCo = StartCoroutine(RecoilCoroutine(kickBack, kickUpDeg, yawRandDeg, rollRandDeg, inDur, outDur));
    }

    public void Shake()
    {
        if (shakeCo != null) StopCoroutine(shakeCo);
        shakeCo = StartCoroutine(ShakeCoroutine(shakeDuration, shakeMagnitude));
    }

    private System.Collections.IEnumerator RecoilCoroutine(float kickBack, float kickUpDeg, float yawRandDeg, float rollRandDeg, float inDur, float outDur)
    {
        // Point de départ = offsets actuels pour une superposition fluide
        Vector3 startPos = posOffRecoil;
        Quaternion startRot = rotOffRecoil;

        // Cible: déplacement vers -Z et rotation
        float yaw = Random.Range(-yawRandDeg, yawRandDeg);
        float roll = Random.Range(-rollRandDeg, rollRandDeg);

        Vector3 targetPos = startPos + new Vector3(0f, 0f, -kickBack);
        Quaternion targetRot = Quaternion.Euler(-kickUpDeg, yaw, roll) * startRot;

        // Aller au pic
        float t = 0f;
        float inTime = Mathf.Max(0.0001f, inDur);
        while (t < 1f)
        {
            float k = recoilCurve != null ? recoilCurve.Evaluate(t) : t;
            posOffRecoil = Vector3.LerpUnclamped(startPos, targetPos, k);
            rotOffRecoil = Quaternion.SlerpUnclamped(startRot, targetRot, k);
            ApplyCombinedOffsets();
            t += Time.deltaTime / inTime;
            yield return null;
        }
        posOffRecoil = targetPos;
        rotOffRecoil = targetRot;
        ApplyCombinedOffsets();

        // Retour
        t = 0f;
        float outTime = Mathf.Max(0.0001f, outDur);
        while (t < 1f)
        {
            float k = recoilCurve != null ? 1f - recoilCurve.Evaluate(t) : 1f - t;
            posOffRecoil = Vector3.LerpUnclamped(Vector3.zero, targetPos, k);
            rotOffRecoil = Quaternion.SlerpUnclamped(Quaternion.identity, targetRot, k);
            ApplyCombinedOffsets();
            t += Time.deltaTime / outTime;
            yield return null;
        }

        posOffRecoil = Vector3.zero;
        rotOffRecoil = Quaternion.identity;
        ApplyCombinedOffsets();
        recoilCo = null;
    }

    private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector2 rnd;
            if (shakeUsePerlin)
            {
                float t = Time.time * shakePerlinFreq;
                float x = Mathf.PerlinNoise(perlinSeedX, t) * 2f - 1f;
                float y = Mathf.PerlinNoise(perlinSeedY, t) * 2f - 1f;
                rnd = new Vector2(x, y);
            }
            else
            {
                rnd = Random.insideUnitCircle;
            }

            posOffShake = new Vector3(rnd.x, rnd.y, 0f) * magnitude;
            rotOffShake = Quaternion.identity; // on peut ajouter un léger roll si souhaité

            ApplyCombinedOffsets();
            elapsed += Time.deltaTime;
            yield return null;
        }

        posOffShake = Vector3.zero;
        rotOffShake = Quaternion.identity;
        ApplyCombinedOffsets();
        shakeCo = null;
    }

    // Applique uniquement le delta entre l’offset total courant et celui déjà appliqué
    private void ApplyCombinedOffsets()
    {
        if (shakeTransform == null) return;

        Vector3 totalPos = posOffRecoil + posOffShake;
        Quaternion totalRot = rotOffRecoil * rotOffShake;

        Vector3 dPos = totalPos - appliedPos;
        Quaternion dRot = totalRot * Quaternion.Inverse(appliedRot);

        if (dPos.sqrMagnitude > 0f)
            shakeTransform.localPosition += dPos;

        shakeTransform.localRotation = shakeTransform.localRotation * dRot;

        appliedPos = totalPos;
        appliedRot = totalRot;
    }

    private void OnDisable()
    {
        // Retire proprement tout offset restant
        if (shakeTransform == null) return;
        // Revenir à l’état sans offset
        shakeTransform.localPosition -= appliedPos;
        shakeTransform.localRotation = shakeTransform.localRotation * Quaternion.Inverse(appliedRot);

        posOffRecoil = posOffShake = appliedPos = Vector3.zero;
        rotOffRecoil = rotOffShake = appliedRot = Quaternion.identity;

        if (recoilCo != null) { StopCoroutine(recoilCo); recoilCo = null; }
        if (shakeCo != null) { StopCoroutine(shakeCo); shakeCo = null; }
    }
}