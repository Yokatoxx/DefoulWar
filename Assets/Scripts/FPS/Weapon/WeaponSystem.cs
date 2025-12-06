using FPS;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private WeaponSettings weaponSettings;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("References")]
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private CrosshairAnim crosshair;
    [SerializeField] private SoundPlayer soundPlayer; // optionnel
    [SerializeField] private TextMeshProUGUI textAmmo;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private Animator animator; // auto-récup si null
    [SerializeField] private WeaponShake weaponShake; // AJOUT: shake d’arme
    [SerializeField] private WeaponReloaderMovement weaponReloadRotation; // AJOUT: rotation reload d’arme

    [Header("Damage Zones")]
    [SerializeField]
    private List<HitZoneMultiplier> zoneDamageMultipliers = new()
    {
        new HitZoneMultiplier("Body", 1f),
        new HitZoneMultiplier("Head", 2f)
    };

    // Runtime
    private int currentMagazine;
    private int currentReserve;
    private float lastShootTime;
    private bool isReloading;
    private Dictionary<string, float> zoneMultDict;

    public bool IsReloading => isReloading;

    private void Awake()
    {
        if (animator == null && bulletSpawnPoint != null)
            animator = bulletSpawnPoint.GetComponentInParent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (weaponShake == null)
            weaponShake = GetComponentInChildren<WeaponShake>(); // auto-find du shake

        InitAmmo();
        BuildZoneDictionary();
        UpdateAmmoUI();

        if (crosshair == null)
            crosshair = FindAnyObjectByType<CrosshairAnim>();
    }

    private void Update()
    {
        HandleFireInput();
        HandleReloadInput();
    }

    private void HandleFireInput()
    {
        if (isReloading) return;
        if (weaponSettings == null || bulletSpawnPoint == null) return;

        bool wantShoot = weaponSettings.isAutomatic
            ? Input.GetMouseButton(0)
            : Input.GetMouseButtonDown(0);

        if (wantShoot)
            Shoot();
    }

    private void HandleReloadInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
            StartReload();

        if (currentMagazine <= 0 && currentReserve > 0 && !isReloading)
            StartReload();
    }

    private void InitAmmo()
    {
        if (weaponSettings == null) return;
        currentMagazine = weaponSettings.magazineSize;
        currentReserve = weaponSettings.maxAmmo;
    }

    private void BuildZoneDictionary()
    {
        zoneMultDict = new Dictionary<string, float>();
        foreach (var z in zoneDamageMultipliers)
        {
            if (!string.IsNullOrEmpty(z.zoneName))
                zoneMultDict[z.zoneName] = Mathf.Max(0f, z.multiplier);
        }
        if (!zoneMultDict.ContainsKey("Body"))
            zoneMultDict["Body"] = 1f;
    }

    public void Shoot()
    {
        if (Time.time < lastShootTime + weaponSettings.shotDelay) return;
        if (currentMagazine <= 0)
        {
            StartReload();
            return;
        }

        lastShootTime = Time.time;
        PerformShotBurst();
    }

    private void PerformShotBurst()
    {
        int shots = Mathf.Min(weaponSettings.bulletsPerShot, currentMagazine);

        if (animator != null) animator.SetBool("isShooting", true);
        if (crosshair != null) crosshair.PlayShoot();
        if (weaponShake != null) weaponShake.Shake(); // AJOUT: jouer le shake au tir
        if (soundPlayer != null && weaponSettings.shootSound != null)
            soundPlayer.PlayOneShot(weaponSettings.shootSound, 1f, Random.Range(0.95f, 1.05f));

        for (int i = 0; i < shots; i++)
        {
            FireSingleRay();
            currentMagazine--;
            if (currentMagazine <= 0) break;
        }

        UpdateAmmoUI();
    }

    private void FireSingleRay()
    {
        if (bulletSpawnPoint == null) return;

        if (weaponSettings.muzzleFlash != null)
            Instantiate(weaponSettings.muzzleFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        // Direction de base: du canon vers le point centre écran
        Vector3 aimPoint = GetCenterAimPoint();
        Vector3 baseDir = (aimPoint - bulletSpawnPoint.position).normalized;

        // Spread angulaire (cône)
        Vector3 finalDir = weaponSettings.addBulletSpread
            ? ApplyRadialAngularSpread(baseDir)
            : baseDir;

        bool hit = Physics.Raycast(
            bulletSpawnPoint.position,
            finalDir,
            out RaycastHit hitInfo,
            weaponSettings.shootingDistance,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        Vector3 endPoint = hit
            ? hitInfo.point
            : bulletSpawnPoint.position + finalDir * weaponSettings.shootingDistance;

        Vector3 normal = hit
            ? hitInfo.normal
            : -finalDir;

        if (weaponSettings.bulletTrail != null)
            SpawnTrail(endPoint, normal, hit ? hitInfo.collider : null);

        if (hit)
            ApplyDamage(hitInfo.collider);
    }

    private Vector3 ApplyRadialAngularSpread(Vector3 baseDir)
    {
        float maxAngleDeg = Mathf.Max(0f, weaponSettings.bulletSpreadVaraiance.x);
        if (maxAngleDeg <= 0.0001f) return baseDir;

        float r = maxAngleDeg * Mathf.Sqrt(Random.value);
        float theta = Random.value * Mathf.PI * 2f;

        float yawDeg = r * Mathf.Cos(theta);
        float pitchDeg = r * Mathf.Sin(theta);

        Vector3 upAxis = bulletSpawnPoint.up;
        Vector3 rightAxis = bulletSpawnPoint.right;

        Quaternion rot = Quaternion.AngleAxis(yawDeg, upAxis) * Quaternion.AngleAxis(pitchDeg, rightAxis);
        return (rot * baseDir).normalized;
    }

    private Vector3 GetCenterAimPoint()
    {
        if (aimCamera == null)
            return bulletSpawnPoint.position + bulletSpawnPoint.forward * weaponSettings.shootingDistance;

        Ray camRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(camRay.origin, camRay.direction, out RaycastHit camHit,
            weaponSettings.shootingDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            return camHit.point;
        }
        return camRay.origin + camRay.direction * weaponSettings.shootingDistance;
    }

    private void SpawnTrail(Vector3 endPoint, Vector3 normal, Collider hitCollider)
    {
        TrailRenderer trail = Instantiate(weaponSettings.bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
        StartCoroutine(AnimateTrail(trail, endPoint, normal, hitCollider));
    }

    private IEnumerator AnimateTrail(TrailRenderer trail, Vector3 endPoint, Vector3 normal, Collider hitCollider)
    {
        float startTime = Time.time;
        Vector3 start = trail.transform.position;
        float travelTime = Mathf.Max(0.01f, trail.time);

        while (true)
        {
            float t = (Time.time - startTime) / travelTime;
            if (t >= 1f) break;
            trail.transform.position = Vector3.Lerp(start, endPoint, t);
            yield return null;
        }

        trail.transform.position = endPoint;

        if (hitCollider != null &&
            weaponSettings.ImpactParticleSystem != null &&
            !hitCollider.CompareTag(enemyTag))
        {
            Instantiate(weaponSettings.ImpactParticleSystem, endPoint, Quaternion.LookRotation(normal));
        }

        if (animator != null) animator.SetBool("isShooting", false);
        Destroy(trail.gameObject, trail.time);
    }

    private void ApplyDamage(Collider collider)
    {
        if (collider == null) return;
        var enemyHealth = collider.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) return;

        float dmg = weaponSettings.bulletDammage;
        var hitZone = collider.GetComponent<HitZone>();
        string zoneName = hitZone != null ? hitZone.ZoneName : "Body";

        if (hitZone != null)
            hitZone.FlashOnHit();

        if (zoneMultDict.TryGetValue(zoneName, out float mult))
            dmg *= mult;

        enemyHealth.TakeDamage(dmg, zoneName);
    }

    public void StartReload()
    {
        if (isReloading) return;
        if (currentMagazine >= weaponSettings.magazineSize) return;
        if (currentReserve <= 0) return;

        isReloading = true;
        if (weaponReloadRotation != null) weaponReloadRotation.TriggerRotateOnEmpty();
        if (animator != null) animator.SetBool("isReloading", true);
        Invoke(nameof(FinishReload), weaponSettings.reloadTime);
    }

    private void FinishReload()
    {
        int space = weaponSettings.magazineSize - currentMagazine;
        int toLoad = Mathf.Min(space, currentReserve);
        currentMagazine += toLoad;
        currentReserve -= toLoad;

        isReloading = false;
        if (animator != null) animator.SetBool("isReloading", false);
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (textAmmo != null)
            textAmmo.text = $"{currentMagazine} / {currentReserve}";
    }
}