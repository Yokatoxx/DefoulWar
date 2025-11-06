using System.Collections;
using UnityEngine;

public class MagicEnemyHitScan : MonoBehaviour
{
    [Header("FX")]
    [SerializeField] private TrailRenderer trailPrefab;
    [SerializeField] private ParticleSystem impactFX;
    [Tooltip("Largeur appliquée au trail instancié (optionnel). 0 = laisser le prefab tel quel.")]
    [SerializeField] private float trailWidth = 0f;

    [Header("Point de tir")]
    [Tooltip("Point d’origine du tir. Si null, utilise transform.position + upOffset.")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float upOffset = 1.5f;

    public void FireTo(Vector3 targetPosition, Vector3? hitNormal = null)
    {
        if (trailPrefab == null)
        {
            Debug.LogWarning("[MagicEnemyHitScan] Trail prefab non assigné.");
            return;
        }

        Vector3 start = firePoint != null ? firePoint.position : (transform.position + Vector3.up * upOffset);
        Vector3 end = targetPosition;
        Vector3 normal = hitNormal ?? (start == end ? Vector3.back : -(end - start).normalized);

        TrailRenderer trail = Instantiate(trailPrefab, start, Quaternion.identity);
        if (trailWidth > 0f) trail.widthMultiplier = trailWidth;

        StartCoroutine(AnimateTrail(trail, end, normal));
    }

    public void FireTo(Transform target, Vector3? hitNormal = null)
    {
        if (target == null) return;

        // Tente de viser le centre du collider si présent, sinon la position
        Vector3 aimPoint = target.position;
        var col = target.GetComponentInChildren<Collider>();
        if (col != null) aimPoint = col.bounds.center;

        FireTo(aimPoint, hitNormal);
    }

    private IEnumerator AnimateTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal)
    {
        float t = 0f;
        Vector3 start = trail.transform.position;
        float travelTime = Mathf.Max(0.01f, trail.time);

        while (t < 1f)
        {
            trail.transform.position = Vector3.Lerp(start, hitPoint, t);
            t += Time.deltaTime / travelTime;
            yield return null;
        }

        trail.transform.position = hitPoint;

        if (impactFX != null)
        {
            Instantiate(impactFX, hitPoint, Quaternion.LookRotation(hitNormal));
        }

        Destroy(trail.gameObject, trail.time);
    }
}