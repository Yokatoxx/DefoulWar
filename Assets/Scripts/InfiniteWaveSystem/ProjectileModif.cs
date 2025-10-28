using System.Collections.Generic;
using Proto3GD.FPS;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPS
{
    public class ProjectileModif : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private bool useHitscan = false;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float fireRate = 0.1f;
        [SerializeField] private float bulletSpeed = 50f;
        [SerializeField] private int maxAmmo = 30;
        [SerializeField] private int currentAmmo;
        [SerializeField] private float reloadTime = 2f;
        [SerializeField] private bool autoReloadWhenEmpty = true;
        [SerializeField] private float maxAimDistance = 100f;
        [SerializeField] private LayerMask aimMask = ~0;

        [Header("Zone Damage Multipliers (relative to Body)")]
        [SerializeField]
        private List<HitZoneMultiplier> zoneDamageMultipliers = new List<HitZoneMultiplier>
        {
            new HitZoneMultiplier("Body", 1f),
            new HitZoneMultiplier("Head", 2f)
        };

        [Header("Recoil")]
        [SerializeField] private float recoilAmount = 0.5f;
        [SerializeField] private float recoilRecoverySpeed = 5f;

        [Header("References")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform weaponModel;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform cameraTransform;

        [Header("FX")]
        [Tooltip("Option 1: référence directe à un ParticleSystem placé sur l'arme")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [Tooltip("Option 2: prefab d'effet à instancier au moment du tir (sera parenté au firePoint)")]
        [SerializeField] private GameObject muzzleFlashPrefab;

        [Header("Input Mode")]
        [SerializeField] private bool useNewInputSystem = false;
        [SerializeField] private bool controlledExternally = false;

        private float nextFireTime;
        private bool isReloading;
        private bool isFiring;
        private Vector3 currentRecoil;

        private float? fireRateOverride;
        private float EffectiveFireRate => Mathf.Max(0.01f, fireRateOverride ?? fireRate);
        public void SetFireRateOverride(float? secondsBetweenShots) => fireRateOverride = secondsBetweenShots;

        private void Awake()
        {
            currentAmmo = maxAmmo;

            if (GetComponent<WeaponController>() != null)
                controlledExternally = true;

            if (cameraTransform == null)
                cameraTransform = Camera.main?.transform;

            if (firePoint == null && weaponModel != null)
            {
                firePoint = weaponModel.Find("MuzzlePoint");
                if (firePoint == null)
                {
                    GameObject muzzle = new GameObject("MuzzlePoint");
                    muzzle.transform.SetParent(weaponModel);
                    muzzle.transform.localPosition = new Vector3(0, 0, 0.45f);
                    firePoint = muzzle.transform;
                }
            }
        }

        private void Update()
        {
            if (!controlledExternally && !useNewInputSystem)
                HandleOldInput();

            if (currentRecoil.magnitude > 0.01f)
                currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

            if (!controlledExternally && isFiring && !isReloading)
                TryShoot();
        }

        private void HandleOldInput()
        {
            isFiring = Input.GetMouseButton(0);

            if (Input.GetKeyDown(KeyCode.R))
                StartReload();
        }

        private void TryShoot()
        {
            if (Time.time < nextFireTime) return;
            if (currentAmmo <= 0) return;

            Shoot();
            nextFireTime = Time.time + EffectiveFireRate;
        }

        private void Shoot()
        {
            currentAmmo--;

            PlayMuzzleFlash();

            float randomX = Random.Range(-recoilAmount, recoilAmount);
            float randomY = Random.Range(-recoilAmount, recoilAmount);
            currentRecoil += new Vector3(recoilAmount, randomY, randomX);

            if (useHitscan)
            {
                Vector3 rayOrigin = cameraTransform != null ? cameraTransform.position : transform.position;
                Vector3 rayDirection = cameraTransform != null ? cameraTransform.forward : transform.forward;

                if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
                {
                    if (!TryApplyEnemyDamage(hit, damage))
                    {
                        var playerHealth = hit.collider.GetComponent<PlayerHealth>();
                        if (playerHealth != null)
                            playerHealth.TakeDamage(damage);
                    }
                }
            }
            else if (bulletPrefab != null)
            {
                Vector3 spawnPos = firePoint != null ? firePoint.position : (weaponModel != null ? weaponModel.position : transform.position);
                Quaternion baseRot = cameraTransform != null ? cameraTransform.rotation : transform.rotation;

                Vector3 aimPoint;
                if (cameraTransform != null)
                {
                    Vector3 camPos = cameraTransform.position;
                    Vector3 camFwd = cameraTransform.forward;
                    if (Physics.Raycast(camPos, camFwd, out RaycastHit hitInfo, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
                        aimPoint = hitInfo.point;
                    else
                        aimPoint = camPos + camFwd * maxAimDistance;
                }
                else
                {
                    aimPoint = transform.position + baseRot * Vector3.forward * maxAimDistance;
                }

                Vector3 camAimDir = (aimPoint - spawnPos).normalized;
                float maxDist = Mathf.Min(maxAimDistance, Vector3.Distance(spawnPos, aimPoint) + 0.001f);
                if (Physics.Raycast(spawnPos, camAimDir, out RaycastHit muzzleBlock, maxDist, aimMask, QueryTriggerInteraction.Ignore))
                    aimPoint = muzzleBlock.point;

                Vector3 aimDir = (aimPoint - spawnPos).normalized;
                GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(aimDir));

                Bullet bulletScript = bullet.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(damage, bulletSpeed);
                    var dict = BuildZoneMultiplierDict();
                    bulletScript.SetZoneMultipliers(dict);
                }
                else
                {
                    Rigidbody rb = bullet.GetComponent<Rigidbody>();
                    if (rb != null)
                        rb.linearVelocity = aimDir * bulletSpeed; // correction
                }
            }

            if (autoReloadWhenEmpty && currentAmmo <= 0)
                StartReload();
        }

        // Applique des dégâts à l'ennemi géré par AIWaveManager.
        private bool TryApplyEnemyDamage(RaycastHit hit, float baseDamage)
        {
            var hook = hit.collider.GetComponentInParent<AIWaveManager.ManagedEnemyHook>();
            if (hook == null || hook.IsDead) return false;

            float finalDamage = baseDamage;

            string zoneName = "Body";
            var hitZone = hit.collider.GetComponent<HitZone>();
            if (hitZone != null)
            {
                zoneName = hitZone.ZoneName;
                var multipliers = BuildZoneMultiplierDict();
                if (multipliers.TryGetValue(zoneName, out float mult))
                    finalDamage *= mult;

                hitZone.FlashOnHit();
            }

            hook.TakeDamage(finalDamage, zoneName);
            return true;
        }

        private void PlayMuzzleFlash()
        {
            if (firePoint == null && muzzleFlash == null && muzzleFlashPrefab == null) return;

            if (muzzleFlash != null)
            {
                if (firePoint != null)
                    muzzleFlash.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);

                muzzleFlash.Play(true);
                return;
            }

            if (muzzleFlashPrefab != null)
            {
                Transform parent = firePoint != null ? firePoint : transform;
                GameObject fx = Instantiate(muzzleFlashPrefab, parent.position, parent.rotation, parent);

                float lifeTime = 2f;
                var ps = fx.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    lifeTime = main.duration + main.startLifetime.constantMax;
                }
                Destroy(fx, lifeTime);
            }
        }

        private Dictionary<string, float> BuildZoneMultiplierDict()
        {
            var dict = new Dictionary<string, float>();
            foreach (var z in zoneDamageMultipliers)
            {
                if (!string.IsNullOrEmpty(z.zoneName))
                    dict[z.zoneName] = Mathf.Max(0f, z.multiplier);
            }
            if (!dict.ContainsKey("Body")) dict["Body"] = 1f;
            return dict;
        }

        public void StartReload()
        {
            if (isReloading || currentAmmo == maxAmmo) return;
            isReloading = true;
            Invoke(nameof(FinishReload), reloadTime);
        }

        private void FinishReload()
        {
            currentAmmo = maxAmmo;
            isReloading = false;
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (controlledExternally) return;

            if (useNewInputSystem)
            {
                if (context.started) isFiring = true;
                else if (context.canceled) isFiring = false;
            }
        }

        public void OnReload(InputAction.CallbackContext context)
        {
            if (controlledExternally) return;
            if (useNewInputSystem && context.performed) StartReload();
        }

        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public bool IsReloading => isReloading;
        public Transform WeaponModel => weaponModel;
        public bool ControlledExternally { get => controlledExternally; set => controlledExternally = value; }
        public void SetWeaponModel(Transform model) => weaponModel = model;
        public void SetFirePoint(Transform point) => firePoint = point;

        public bool ExternalTryShoot()
        {
            if (isReloading) return false;
            if (Time.time < nextFireTime) return false;
            if (currentAmmo <= 0) return false;

            Shoot();
            nextFireTime = Time.time + EffectiveFireRate;
            return true;
        }
    }
}