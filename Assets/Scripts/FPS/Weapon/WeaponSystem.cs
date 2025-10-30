using Ennemies.Effect;
using Proto3GD.FPS;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSystem : MonoBehaviour
{
    [SerializeField] private WeaponSettings weaponSettings;
    [SerializeField] private WeaponShake weaponShake;
    [SerializeField] private SoundPlayer soundPlayer;

    [SerializeField] private string enemyTag = "Enemy";

    [SerializeField] private TextMeshProUGUI textAmmo;
    private bool isReloading = false;

    [SerializeField] private bool looseAmmo = false;
    public bool IsReloading => isReloading;

    private float lastShootTime;

    // Nouveau: gestion chargeur + réserve
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

        // Auto-reload si chargeur vide et reserve disponible
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
            // Chargeur vide: tenter reload
            StartReload();
            return;
        }

        if (animator != null) animator.SetBool("isShooting", true);
        if (weaponShake != null) weaponShake.Recoil();

        // Ne pas tirer plus de balles que le chargeur restant
        int shotsToFire = Mathf.Min(weaponSettings.bulletsPerShot, currentMagazine);

        for (int i = 0; i < shotsToFire; i++)
        {
            if (weaponSettings.muzzleFlash != null && bulletSpawnPoint != null)
                Instantiate(weaponSettings.muzzleFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

            if (soundPlayer != null && weaponSettings.shootSound != null)
                soundPlayer.PlayOneShot(weaponSettings.shootSound, 0.5f, Random.Range(0.8f, 1));

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

            // Consommer 1 munition par projectile émis
            currentMagazine--;
            if (currentMagazine == 0) break;
        }

        lastShootTime = Time.time;
        UpdateAmmoUI();
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

        // Impact FX si on a touché un collider non-ennemi
        if (hitCollider != null && weaponSettings.ImpactParticleSystem != null && !hitCollider.CompareTag(enemyTag))
            Instantiate(weaponSettings.ImpactParticleSystem, hitPoint, Quaternion.LookRotation(hitNormal));

        // Appliquer les dégâts à la fin du trail (si un collider a été touché)
        if (hitCollider != null)
            ApplyDamage(hitCollider);

        if (animator != null) animator.SetBool("isShooting", false);
        Destroy(trail.gameObject, trail.time);
    }

    // Application des dégâts via le collider touché
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

        if (hitCollider.GetComponentInParent<MagicEnemy>() == null)
        {

            if (hitZone.ZoneName == "Head")
            {
                if (soundPlayer != null)
                    soundPlayer.PlayOneShot("HeadShot", 0.7f, Random.Range(0.9f, 1.1f));
            }
            else if (hitZone.ZoneName == "Body")
            {
                if (soundPlayer != null)
                    soundPlayer.PlayOneShot("Hitmarker", 0.5f, Random.Range(0.9f, 1.1f));
            }
            enemyHealth.TakeDamage(finalDamage, zoneName);
        }
        else
        {
            if (soundPlayer != null)
                soundPlayer.PlayOneShot("YellowShield", 0.2f, Random.Range(0.9f, 1.1f));
        }
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
        // Conditions: déjà en reload, chargeur plein, ou aucune réserve
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
        if(looseAmmo) currentReserve -= toLoad;

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