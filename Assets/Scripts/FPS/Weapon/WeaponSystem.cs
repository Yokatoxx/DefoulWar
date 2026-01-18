using FPS;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class WeaponSystem : MonoBehaviour
{
    // Delegate pour l'event d'esquive avec possibilité d'annuler les dégâts
    public delegate void PlayerAimAtEnemyHandler(GameObject target, ref bool cancelDamage);
    public static event PlayerAimAtEnemyHandler OnPlayerAimAtEnemy;
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

    [Header("Blood Bullets - Tir avec la vie")]
    [Tooltip("Permet de tirer en utilisant la vie quand plus de munitions")]
    [SerializeField] private bool enableBloodBullets = true;
    [Tooltip("Coût en vie par balle tirée")]
    [SerializeField] private float healthCostPerBullet = 5f;
    [Tooltip("Vie minimum requise pour pouvoir tirer (ne peut pas se suicider)")]
    [SerializeField] private float minHealthToShoot = 1f;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Events")]
    [Tooltip("Déclenché après chaque tir (paramètre: munitions restantes dans le chargeur)")]
    public UnityEvent<int> OnMagazineChanged;
    
    [Tooltip("Déclenché quand le reload est terminé")]
    public UnityEvent OnReloadComplete;

    // Runtime
    private int currentMagazine;
    private int currentReserve;
    private float lastShootTime;
    private bool isReloading;
    private Dictionary<string, float> zoneMultDict;
    private bool isUsingBloodBullets; // Flag pour savoir si on tire avec la vie

    // Nouveau: contrôle de la possibilité de tirer
    private bool canShoot = true;
    private Coroutine disableShootingRoutine;

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
        
        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void OnDisable()
    {
        // Cancel any pending disable timer and ensure shooting is allowed when disabled
        if (disableShootingRoutine != null)
        {
            StopCoroutine(disableShootingRoutine);
            disableShootingRoutine = null;
        }
        canShoot = true;
    }

    private void OnDestroy()
    {
        if (disableShootingRoutine != null)
        {
            StopCoroutine(disableShootingRoutine);
            disableShootingRoutine = null;
        }
        canShoot = true;
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

        // Respecter le flag canShoot. Cependant, si le joueur est stunné et que le stun provoque des tirs automatiques,
        // on autorise quand même les tirs forcés par le stun (priorité stun).
        if (!canShoot)
        {
            var stunComp = GetComponentInParent<FPS.PlayerStunAutoFire>();
            if (stunComp == null || !stunComp.IsStunned)
                return;
        }

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
        // Respecter le flag canShoot (même logique que dans HandleFireInput)
        if (!canShoot)
        {
            var stunComp = GetComponentInParent<FPS.PlayerStunAutoFire>();
            if (stunComp == null || !stunComp.IsStunned)
                return;
        }

        if (Time.time < lastShootTime + weaponSettings.shotDelay) return;
        
        // Vérifier si on peut tirer normalement ou avec la vie
        if (currentMagazine <= 0)
        {
            if (currentReserve > 0)
            {
                StartReload();
                return;
            }
            
            // Plus de munitions du tout - tenter le tir avec la vie
            if (CanUseBloodBullets())
            {
                isUsingBloodBullets = true;
            }
            else
            {
                isUsingBloodBullets = false;
                return;
            }
        }
        else
        {
            isUsingBloodBullets = false;
        }

        lastShootTime = Time.time;
        PerformShotBurst();
    }
    
    private bool CanUseBloodBullets()
    {
        if (!enableBloodBullets) return false;
        if (playerHealth == null) return false;
        if (playerHealth.IsDead) return false;
        return playerHealth.CurrentHealth > minHealthToShoot;
    }

    private void PerformShotBurst()
    {
        int shots;
        
        if (isUsingBloodBullets)
        {
            // En mode blood bullets, on tire une balle à la fois
            shots = 1;
        }
        else
        {
            shots = Mathf.Min(weaponSettings.bulletsPerShot, currentMagazine);
        }

        if (animator != null) animator.SetBool("isShooting", true);
        if (crosshair != null) crosshair.PlayShoot();
        if (weaponShake != null) weaponShake.Shake();
        if (soundPlayer != null && weaponSettings.shootSound != null)
            soundPlayer.PlayOneShot(weaponSettings.shootSound, 1f, Random.Range(0.95f, 1.05f));

        for (int i = 0; i < shots; i++)
        {
            if (isUsingBloodBullets)
            {
                // Vérifier qu'on a assez de vie pour cette balle
                if (!CanUseBloodBullets()) break;
                
                // Consommer la vie pour tirer
                ConsumeHealthForBullet();
            }
            else
            {
                currentMagazine--;
            }
            
            FireSingleRay();
            
            if (!isUsingBloodBullets && currentMagazine <= 0) break;
        }

        UpdateAmmoUI();
        OnMagazineChanged?.Invoke(currentMagazine);
    }
    
    private void ConsumeHealthForBullet()
    {
        if (playerHealth == null) return;
        
        // Infliger les dégâts au joueur (bypass invulnérabilité via méthode directe)
        float newHealth = Mathf.Max(minHealthToShoot, playerHealth.CurrentHealth - healthCostPerBullet);
        float actualDamage = playerHealth.CurrentHealth - newHealth;
        
        if (actualDamage > 0)
        {
            playerHealth.TakeDamage(actualDamage);
        }
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
        {
            // Notifier l'ennemi ciblé AVANT d'appliquer les dégâts (permet l'esquive)
            bool cancelDamage = false;
            var enemyHealth = hitInfo.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                OnPlayerAimAtEnemy?.Invoke(enemyHealth.gameObject, ref cancelDamage);
            }
            
            // Si l'ennemi a esquivé, ne pas appliquer de dégâts
            if (!cancelDamage)
            {
                ApplyDamage(hitInfo.collider);
            }
        }
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

        // 1) Priorité: weak point
        var weakPoint = collider.GetComponentInParent<WeakPointTarget>()
                        ?? collider.GetComponent<WeakPointTarget>();
        if (weakPoint != null && weakPoint.isActiveAndEnabled)
        {
            // Infliger des dégâts au point faible et sortir (ne pas toucher la santé de l'ennemi)
            weakPoint.TakeWeakPointDamage(weaponSettings.bulletDammage);

            // Feedback d’impact si voulu (éviter double impact sur Enemy)
            var hitZone = collider.GetComponent<HitZone>();
            if (hitZone != null) hitZone.FlashOnHit();
            return;
        }

        // 2) Sinon: dégâts classiques à l'ennemi
        var enemyHealth = collider.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) return;

        float dmg = weaponSettings.bulletDammage;
        var hz = collider.GetComponent<HitZone>();
        string zoneName = hz != null ? hz.ZoneName : "Body";
        if (hz != null) hz.FlashOnHit();

        if (zoneMultDict.TryGetValue(zoneName, out float mult))
            dmg *= mult;

        var info = new DamageInfo(
            amount: dmg,
            zoneName: zoneName,
            type: DamageType.Bullet,
            hitPoint: collider.ClosestPoint(bulletSpawnPoint != null ? bulletSpawnPoint.position : collider.transform.position),
            hitNormal: Vector3.zero,
            attacker: FindFirstObjectByType<PlayerHealth>()?.transform,
            hitCollider: collider
        );

        enemyHealth.TakeDamage(info);
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
        OnReloadComplete?.Invoke();
    }

    private void UpdateAmmoUI()
    {
        if (textAmmo == null) return;
        
        if (isUsingBloodBullets && currentMagazine <= 0 && currentReserve <= 0)
        {
            // Afficher que le joueur utilise sa vie pour tirer
            textAmmo.text = "<color=red>BLOOD</color>";
        }
        else
        {
            textAmmo.text = $"{currentMagazine} / {currentReserve}";
        }
    }
    
    // Propriétés publiques pour l'état des munitions
    public bool IsOutOfAmmo => currentMagazine <= 0 && currentReserve <= 0;
    public bool IsUsingBloodBullets => isUsingBloodBullets && IsOutOfAmmo;
    public int CurrentMagazine => currentMagazine;
    public int CurrentReserve => currentReserve;

    /// <summary>
    /// Ajoute des munitions à la réserve.
    /// </summary>
    public void AddAmmo(int amount)
    {
        if (weaponSettings == null) return;
        currentReserve = Mathf.Min(currentReserve + amount, weaponSettings.maxAmmo);
        UpdateAmmoUI();
    }

    // Nouvelle API publique : désactiver le tir pendant une durée realtime/unscaled
    public void DisableShootingFor(float seconds)
    {
        // Stop previous routine if present
        if (disableShootingRoutine != null)
        {
            StopCoroutine(disableShootingRoutine);
            disableShootingRoutine = null;
        }

        if (seconds <= 0f)
        {
            canShoot = true;
            return;
        }

        disableShootingRoutine = StartCoroutine(DisableShootingCoroutine(seconds));
    }

    private IEnumerator DisableShootingCoroutine(float seconds)
    {
        canShoot = false;
        // Wait in real time (unaffected by timeScale/slowmo)
        yield return new WaitForSecondsRealtime(seconds);

        canShoot = true;
        disableShootingRoutine = null;
    }
}

