using System.Collections;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [SerializeField] private WeaponSettings weaponSettings;

    private float lastShootTime;
    private float currentAmmo;

    public Transform bulletSpawnPoint;
    public GameObject weapon;

    [Header("Aiming")]
    [SerializeField] private Camera aimCamera; // si null => Camera.main
    [SerializeField] private LayerMask hitMask = ~0;

    private Animator animator;

    private void Awake()
    {
        animator = weapon != null ? weapon.GetComponent<Animator>() : GetComponentInChildren<Animator>();
        if (weaponSettings != null && weaponSettings.bulletTrail != null)
            weaponSettings.bulletTrail.widthMultiplier = weaponSettings.shootTrailWidth;
    }

    private void Update()
    {
        if (weaponSettings.isAutomatic)
        {
            if (Input.GetMouseButton(0)) Shoot();
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) Shoot();
        }
    }

    public void Shoot()
    {
        if (lastShootTime + weaponSettings.shotDelay >= Time.time) return;

        if (animator != null) animator.SetBool("isShooting", true);

        for (int i = 0; i < weaponSettings.bulletsPerShot; i++)
        {
            if (weaponSettings.muzzleFlash != null && bulletSpawnPoint != null)
                Instantiate(weaponSettings.muzzleFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

            // 1) Ray depuis le centre de la caméra
            Ray camRay = GetCenterRay();
            Vector3 camDir = ApplySpread(camRay.direction);

            bool camHit = Physics.Raycast(camRay.origin, camDir, out RaycastHit camHitInfo, weaponSettings.shootingDistance, hitMask, QueryTriggerInteraction.Ignore);
            Vector3 desiredPoint = camHit ? camHitInfo.point : camRay.origin + camDir * weaponSettings.shootingDistance;

            // 2) Direction depuis le canon vers le point visé (évite la parallaxe)
            Vector3 muzzleDir = (desiredPoint - bulletSpawnPoint.position).normalized;

            // 3) Occlusion proche: si un obstacle est entre le canon et desiredPoint, on tape l’obstacle
            float distToDesired = Vector3.Distance(bulletSpawnPoint.position, desiredPoint);
            bool muzzleHit = Physics.Raycast(bulletSpawnPoint.position, muzzleDir, out RaycastHit muzzleHitInfo, distToDesired, hitMask, QueryTriggerInteraction.Ignore);

            Vector3 endPoint = muzzleHit ? muzzleHitInfo.point : desiredPoint;
            Vector3 endNormal = muzzleHit ? muzzleHitInfo.normal : (camHit ? camHitInfo.normal : -camDir);

            if (weaponSettings.bulletTrail != null)
            {
                TrailRenderer trail = Instantiate(weaponSettings.bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, endPoint, endNormal, muzzleHit || camHit));
            }
        }

        lastShootTime = Time.time;
    }

    private Ray GetCenterRay()
    {
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam != null)
            return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        // Fallback si pas de caméra
        return new Ray(bulletSpawnPoint.position, bulletSpawnPoint.forward);
    }

    private Vector3 ApplySpread(Vector3 baseDir)
    {
        if (!weaponSettings.addBulletSpread) return baseDir.normalized;

        Vector3 v = weaponSettings.bulletSpreadVaraiance;
        Vector3 dir = baseDir + new Vector3(
            Random.Range(-v.x, v.x),
            Random.Range(-v.y, v.y),
            Random.Range(-v.z, v.z)
        );
        return dir.normalized;
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool spawnImpact)
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

        if (spawnImpact && weaponSettings.ImpactParticleSystem != null)
            Instantiate(weaponSettings.ImpactParticleSystem, hitPoint, Quaternion.LookRotation(hitNormal));

        if (animator != null) animator.SetBool("isShooting", false);
        Destroy(trail.gameObject, trail.time);
    }
}