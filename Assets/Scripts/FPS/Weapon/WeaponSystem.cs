using Proto3GD.FPS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [SerializeField] private WeaponSettings weaponSettings;
    [SerializeField] private WeaponShake weaponShake;

    private float lastShootTime;
    private float currentAmmo;

    public Transform bulletSpawnPoint;
    public GameObject weapon;

    [Header("Aiming")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Zone Damage Multipliers (relative to Body)")]
    [SerializeField]
    private List<HitZoneMultiplier> zoneDamageMultipliers = new List<HitZoneMultiplier>
    {
        new HitZoneMultiplier("Body", 1f),
        new HitZoneMultiplier("Head", 2f)
    };

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
        if (weaponShake != null) weaponShake.Shake();

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

            // 3) Occlusion proche
            float distToDesired = Vector3.Distance(bulletSpawnPoint.position, desiredPoint);
            bool muzzleHit = Physics.Raycast(bulletSpawnPoint.position, muzzleDir, out RaycastHit muzzleHitInfo, distToDesired, hitMask, QueryTriggerInteraction.Ignore);

            Vector3 endPoint = muzzleHit ? muzzleHitInfo.point : desiredPoint;
            Vector3 endNormal = muzzleHit ? muzzleHitInfo.normal : (camHit ? camHitInfo.normal : -camDir);
            Collider hitCollider = muzzleHit ? muzzleHitInfo.collider : (camHit ? camHitInfo.collider : null);

            if (weaponSettings.bulletTrail != null)
            {
                TrailRenderer trail = Instantiate(weaponSettings.bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, endPoint, endNormal, hitCollider));
            }
        }

        lastShootTime = Time.time;
    }

    private Ray GetCenterRay()
    {
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam != null)
            return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
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

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, Collider hitCollider)
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

        // Impact FX si on a touché un collider
        if (hitCollider != null && weaponSettings.ImpactParticleSystem != null)
            Instantiate(weaponSettings.ImpactParticleSystem, hitPoint, Quaternion.LookRotation(hitNormal));

        // Appliquer les dégâts à la fin du trail (si un collider a été touché)
        if (hitCollider != null)
            ApplyDamage(hitCollider, hitPoint);

        if (animator != null) animator.SetBool("isShooting", false);
        Destroy(trail.gameObject, trail.time);
    }

    // Nouvelle application de dégâts à partir d’un collider + point d’impact (utilisée par le trail)
    private void ApplyDamage(Collider hitCollider, Vector3 hitPoint)
    {
        if (hitCollider == null) return;

        var enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) return;

        float finalDamage = weaponSettings.bulletDammage;

        var hitZone = hitCollider.GetComponent<HitZone>();
        if (hitZone != null)
        {
            var multipliers = BuildZoneMultiplierDict();
            if (multipliers.TryGetValue(hitZone.ZoneName, out float mult))
            {
                finalDamage *= mult;
            }
            hitZone.FlashOnHit();
            Debug.Log("Aie Aie Aie Aie Aie Aie Aie");
            enemyHealth.TakeDamage(weaponSettings.bulletDammage);

        }

        string zoneName = hitZone != null ? hitZone.ZoneName : "Body";
    }

    private Dictionary<string, float> BuildZoneMultiplierDict()
    {
        var dict = new Dictionary<string, float>();
        foreach (var z in zoneDamageMultipliers)
        {
            if (!string.IsNullOrEmpty(z.zoneName))
            {
                dict[z.zoneName] = Mathf.Max(0f, z.multiplier);
            }
        }
        if (!dict.ContainsKey("Body")) dict["Body"] = 1f;
        return dict;
    }
}