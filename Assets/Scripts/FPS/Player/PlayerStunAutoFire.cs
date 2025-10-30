using System.Collections;
using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Étourdit le joueur et déclenche des tirs automatiques via WeaponSystem pendant une durée.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerStunAutoFire : MonoBehaviour
    {
        [Header("Stun & Auto-Fire Settings")]
        [SerializeField] private float defaultStunDuration = 2.5f;
        [SerializeField, Tooltip("Intervalle entre deux tentatives de tir pendant le stun.")]
        private float autoFireInterval = 0.12f;

        [Header("FX")]
        [SerializeField] private SoundPlayer sound;

        private bool isStunned;
        private float stunEndTime;
        private Coroutine routine;
        private float? overrideInterval;

        // Nouvelle référence unique au système d'arme
        [SerializeField] private WeaponSystem weaponSystem;

        private void Awake()
        {
            if (weaponSystem == null)
                weaponSystem = GetComponentInChildren<WeaponSystem>();
        }

        private void OnEnable()
        {
            if (weaponSystem == null)
                weaponSystem = GetComponentInChildren<WeaponSystem>();
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

        /// <param name="customAutoFireInterval">Intervalle personnalisé entre tirs (null pour utiliser autoFireInterval)</param>
        public void ApplyStun(float duration, float? customAutoFireInterval)
        {
            float d = duration > 0f ? duration : defaultStunDuration;
            isStunned = true;
            stunEndTime = Time.time + d;
            overrideInterval = customAutoFireInterval;

            if (sound != null)
            {
                sound.PlayOneShot("Taser");
            }

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(AutoFireLoop());
        }

        private IEnumerator AutoFireLoop()
        {
            if (weaponSystem == null)
            {
                Debug.LogWarning("[PlayerStunAutoFire] WeaponSystem introuvable. Auto-fire annulé.", this);
                yield break;
            }

            float interval = Mathf.Max(0.01f, overrideInterval ?? autoFireInterval);
            var wait = new WaitForSeconds(interval);

            while (isStunned)
            {
                // Tente un tir; WeaponSystem gère cadence, munitions et reload interne.
                weaponSystem.Shoot();

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