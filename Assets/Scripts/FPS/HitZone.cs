using System;
using System.Collections;
using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Représente une zone spécifique de l'ennemi (tête, torse, bras, jambes).
    /// </summary>
    public class HitZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private string zoneName = "Body";
        [SerializeField] private float baseDamageMultiplier = 1f;
        
        [Header("Hit Feedback")]
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashDuration = 0.08f;
        
        private EnemyHealth enemyHealth;
        private Renderer cachedRenderer;
        private Color baseColor;
        private Coroutine flashRoutine;
        
        private void Awake()
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer != null)
            {
                baseColor = cachedRenderer.material.color;
            }
        }
        
        /// <summary>
        /// Multiplicateur final (sans armure désormais).
        /// </summary>
        public float GetFinalDamageMultiplier()
        {
            return baseDamageMultiplier;
        }
        
        /// <summary>
        /// Feedback visuel court lors d'un impact (flash).
        /// </summary>
        public void FlashOnHit()
        {
            if (cachedRenderer == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashCoroutine());
        }
        
        private IEnumerator FlashCoroutine()
        {
            var mat = cachedRenderer.material;
            Color before = mat.color;
            mat.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);
            mat.color = before;
            flashRoutine = null;
        }
        
        // Propriétés publiques
        public string ZoneName => zoneName;
        public EnemyHealth EnemyHealth => enemyHealth;
        
        /// <summary>
        /// Détaille le multiplicateur de base (sans armure).
        /// </summary>
        public float BaseMultiplier => baseDamageMultiplier;
        
        /// <summary>
        /// Définit le nom de la zone (ex: Head, Body).
        /// </summary>
        public void SetZoneName(string name)
        {
            zoneName = name;
        }
        
        /// <summary>
        /// Définit le multiplicateur de dégâts de base (ex: tête=2, corps=1).
        /// </summary>
        public void SetBaseDamageMultiplier(float multiplier)
        {
            baseDamageMultiplier = Mathf.Max(0f, multiplier);
        }
    }
}
