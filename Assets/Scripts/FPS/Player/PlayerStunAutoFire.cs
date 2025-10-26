using System.Collections;
using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Étourdit le joueur et déclenche un tir automatique pendant une durée.
    /// Bloque les inputs via FPSPlayerController (déjà branché) et tente ExternalTryShoot() côté arme.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerStunAutoFire : MonoBehaviour
    {
        [Header("Stun & Auto-Fire Settings")]
        [SerializeField] private float defaultStunDuration = 2.5f;
        [SerializeField] private float autoFireInterval = 0.12f;
        [SerializeField] private bool reloadIfEmptyDuringStun = true;

        private bool isStunned;
        private float stunEndTime;
        private Coroutine routine;
        private float? overrideInterval;

        private WeaponController weaponController;
        private global::FPS.ProjectileWeaponController projectileController;

        private void Awake()
        {
            weaponController = GetComponentInChildren<WeaponController>();
            if (weaponController == null)
            {
                projectileController = GetComponentInChildren<global::FPS.ProjectileWeaponController>();
            }
        }

        private void OnEnable()
        {
            if (weaponController == null)
                weaponController = GetComponentInChildren<WeaponController>();
            if (weaponController == null && projectileController == null)
                projectileController = GetComponentInChildren<global::FPS.ProjectileWeaponController>();
        }

        private void Update()
        {
            if (isStunned && Time.time >= stunEndTime)
            {
                ClearStun();
            }
        }

        public void ApplyStun(float duration)
        {
            ApplyStun(duration, null);
        }

        public void ApplyStun(float duration, float? customAutoFireInterval)
        {
            float d = duration > 0f ? duration : defaultStunDuration;
            isStunned = true;
            stunEndTime = Time.time + d;
            overrideInterval = customAutoFireInterval;

            // Configurer l'override du fireRate sur l'arme si fourni
            var pc = weaponController != null ? weaponController.GetComponent<global::FPS.ProjectileWeaponController>() : projectileController;
            if (pc != null)
            {
                pc.SetFireRateOverride(overrideInterval);
            }

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(AutoFireLoop());
        }

        private IEnumerator AutoFireLoop()
        {
            float interval = Mathf.Max(0.01f, overrideInterval ?? autoFireInterval);
            var wait = new WaitForSeconds(interval);
            while (isStunned)
            {
                bool shot = false;
                if (weaponController != null)
                {
                    var pc = weaponController.GetComponent<global::FPS.ProjectileWeaponController>();
                    if (pc != null) shot = pc.ExternalTryShoot();
                }
                else if (projectileController != null)
                {
                    shot = projectileController.ExternalTryShoot();
                }

                if (!shot && reloadIfEmptyDuringStun)
                {
                    if (weaponController != null)
                    {
                        if (!weaponController.IsReloading && weaponController.CurrentAmmo <= 0)
                            weaponController.StartReload();
                    }
                    else if (projectileController != null)
                    {
                        if (!projectileController.IsReloading && projectileController.CurrentAmmo <= 0)
                            projectileController.StartReload();
                    }
                }

                // Mettre à jour l'interval si override change (sécurité)
                float newInterval = Mathf.Max(0.01f, overrideInterval ?? autoFireInterval);
                if (!Mathf.Approximately(newInterval, interval))
                {
                    interval = newInterval;
                    wait = new WaitForSeconds(interval);
                }

                yield return wait;
            }
        }

        private void ClearStun()
        {
            isStunned = false;
            // Retirer l'override de fireRate de l'arme
            var pc = weaponController != null ? weaponController.GetComponent<global::FPS.ProjectileWeaponController>() : projectileController;
            if (pc != null)
            {
                pc.SetFireRateOverride(null);
            }
            overrideInterval = null;
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        public bool IsStunned => isStunned;

        [ContextMenu("Test Stun (default duration)")]
        private void ContextTestStunDefault()
        {
            ApplyStun(defaultStunDuration);
        }

        [ContextMenu("Test Stun (1.5s fast fire)")]
        private void ContextTestStunFast()
        {
            ApplyStun(1.5f, 0.05f);
        }
    }
}
