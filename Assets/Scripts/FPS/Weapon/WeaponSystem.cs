using FPS;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSystem : MonoBehaviour
{
    [SerializeField] private WeaponSettings weaponSettings;
    [SerializeField] private WeaponShake weaponShake;

    [SerializeField] private string enemyTag = "Enemy";

    [SerializeField] private TextMeshProUGUI textAmmo;
    private bool isReloading = false;
    public bool IsReloading => isReloading;

    private float lastShootTime;

    // Ammo: chargeur + réserve
    private int currentMagazine;
    private int currentReserve;

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

    [Header("UI")]
    [SerializeField] private CrosshairAnim crosshair;

    [Header("Spread avancé")]
    [Tooltip("Longueur (m) près du canon sans aucun spread. Au-delà, le spread s’applique.")]
    [SerializeField] private float noSpreadNearDistance = 2f;

    private Animator animator;

    private void Awake()
    {
        animator = weapon != null ? weapon.GetComponent<Animator>() : GetComponentInChildren<Animator>();
        if (weaponSettings != null && weaponSettings.bulletTrail != null)
            weaponSettings.bulletTrail.widthMultiplier = weaponSettings.shootTrailWidth;

        // Init munitions
        currentMagazine = weaponSettings.magazineSize;
        currentReserve = weaponSettings.maxAmmo;
        UpdateAmmoUI();

        if (crosshair == null) crosshair = FindAnyObjectByType<CrosshairAnim>();
    }

    private void Update()
    {
        if (!isReloading)
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

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartReload();
        }

        if (currentMagazine <= 0 && currentReserve > 0 && !isReloading)
        {
            StartReload();
        }
    }

    public void Shoot()
    {
        if (isReloading) return;
        if (lastShootTime + weaponSettings.shotDelay >= Time.time) return;
        if (currentMagazine <= 0)
        {
            StartReload();
            return;
        }

        if (animator != null) animator.SetBool("isShooting", true);
        if (weaponShake != null) weaponShake.Shake();
        if (crosshair != null) crosshair.PlayShoot();

        int shotsToFire = Mathf.Min(weaponSettings.bulletsPerShot, currentMagazine);

        for (int i = 0; i < shotsToFire; i++)
        {
            if (weaponSettings.muzzleFlash != null && bulletSpawnPoint != null)
                Instantiate(weaponSettings.muzzleFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

            // 1) Ray centre caméra SANS spread: on vise le point exact au centre écran
            Ray camRay = GetCenterRay();
            Vector3 camDir = camRay.direction;

            bool camHit = Physics.Raycast(
                camRay.origin,
                camDir,
                out RaycastHit camHitInfo,
                weaponSettings.shootingDistance,
                hitMask,
                QueryTriggerInteraction.Ignore
            );

            Vector3 desiredPoint = camHit
                ? camHitInfo.point
                : camRay.origin + camDir * weaponSettings.shootingDistance;

            // 2) BaseDir depuis le canon VERS le point visé (sans spread)
            Vector3 baseDir = (desiredPoint - bulletSpawnPoint.position).normalized;

            // 3) Spread appliqué pour la partie LOINTAINE uniquement
            Vector3 spreadDir = camDir;
            if (weaponSettings.addBulletSpread)
            {
                spreadDir = ApplySpreadFromMuzzle(baseDir);
            }

            float totalDist = weaponSettings.shootingDistance;
            float nearDist = Mathf.Clamp(noSpreadNearDistance, 0f, totalDist);
            float farDist = Mathf.Max(0f, totalDist - nearDist);

            Vector3 startNear = bulletSpawnPoint.position;

            // 4) Premier segment SANS spread (proche du canon)
            if (nearDist > 0f)
            {
                if (Physics.Raycast(
                    startNear,
                    baseDir,
                    out RaycastHit hitNear,
                    nearDist,
                    hitMask,
                    QueryTriggerInteraction.Ignore))
                {
                    // Touché dans la zone sans spread: fin ici
                    FinalizeShot(hitNear.point, hitNear.normal, hitNear.collider);
                    ConsumeAmmoAndFinish(ref shotsToFire);
                    continue;
                }
            }

            // 5) Second segment AVEC spread (lointain)
            Vector3 startFar = startNear + baseDir * nearDist;
            bool hitFar = false;
            RaycastHit hitFarInfo = default;

            if (farDist > 0f)
            {
                hitFar = Physics.Raycast(
                    startFar,
                    spreadDir,
                    out hitFarInfo,
                    farDist,
                    hitMask,
                    QueryTriggerInteraction.Ignore
                );
            }

            Vector3 endPoint = hitFar
                ? hitFarInfo.point
                : startFar + spreadDir * farDist;

            Vector3 endNormal = hitFar
                ? hitFarInfo.normal
                : -spreadDir;

            Collider hitCollider = hitFar ? hitFarInfo.collider : null;

            FinalizeShot(endPoint, endNormal, hitCollider);

            // Consommer 1 munition par projectile
            currentMagazine--;
            if (currentMagazine == 0) break;
        }

        lastShootTime = Time.time;
        UpdateAmmoUI();
    }

    private void FinalizeShot(Vector3 endPoint, Vector3 endNormal, Collider hitCollider)
    {
        // Trail
        if (weaponSettings.bulletTrail != null)
        {
            TrailRenderer trail = Instantiate(weaponSettings.bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
            StartCoroutine(SpawnTrail(trail, endPoint, endNormal, hitCollider));
        }

        // Dégâts
        if (hitCollider != null)
            ApplyDamage(hitCollider);
    }

    private void ConsumeAmmoAndFinish(ref int shotsToFire)
    {
        currentMagazine--;
        shotsToFire = Mathf.Min(shotsToFire, currentMagazine);
    }

    private Ray GetCenterRay()
    {
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam != null)
            return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return new Ray(bulletSpawnPoint.position, bulletSpawnPoint.forward);
    }

    // Spread appliqué autour de l’axe du canon
    private Vector3 ApplySpreadFromMuzzle(Vector3 baseDir)
    {
        Vector3 v = weaponSettings.bulletSpreadVaraiance;
        Vector3 right = bulletSpawnPoint.right;
        Vector3 up = bulletSpawnPoint.up;

        Vector3 dir = baseDir + right * Random.Range(-v.x, v.x) + up * Random.Range(-v.y, v.y);
        return dir.normalized;
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, Collider hitCollider)
    {
        float t = 0f;
        Vector3 start = trail.transform.position;
        float travelTime = Mathf.Max(0.01f, trail.time);

        while (t < 1f)
        {
            trail.transform.position = Vector3.Lerp(bulletSpawnPoint.position, hitPoint, t);
            t += Time.deltaTime / travelTime;
            yield return null;
        }

        trail.transform.position = hitPoint;

        // Impact FX si on a touché un collider non-ennemi
        if (hitCollider != null && weaponSettings.ImpactParticleSystem != null && !hitCollider.CompareTag(enemyTag))
            Instantiate(weaponSettings.ImpactParticleSystem, hitPoint, Quaternion.LookRotation(hitNormal));

        if (animator != null) animator.SetBool("isShooting", false);
        Destroy(trail.gameObject, trail.time);
    }

    private void ApplyDamage(Collider hitCollider)
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
        }

        string zoneName = hitZone != null ? hitZone.ZoneName : "Body";
        enemyHealth.TakeDamage(finalDamage, zoneName);
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

    // Rechargement
    public void StartReload()
    {
        if (isReloading) return;
        if (currentMagazine >= weaponSettings.magazineSize) return;
        if (currentReserve <= 0) return;

        isReloading = true;
        if (animator != null) animator.SetBool("isReloading", true);

        Invoke(nameof(FinishReload), weaponSettings.reloadTime);
    }

    private void FinishReload()
    {
        int spaceInMag = weaponSettings.magazineSize - currentMagazine;
        int toLoad = Mathf.Min(spaceInMag, currentReserve);

        currentMagazine += toLoad;
        currentReserve -= toLoad;

        isReloading = false;
        if (animator != null) animator.SetBool("isReloading", false);

        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (textAmmo != null)
        {
            textAmmo.text = $"{currentMagazine} / {currentReserve}";
        }
    }
}