using System;
using System.Collections;
using UnityEngine;

namespace FPS
{

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
        
        public float GetFinalDamageMultiplier()
        {
            return baseDamageMultiplier;
        }

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
        
        public float BaseMultiplier => baseDamageMultiplier;
        
        public void SetZoneName(string name)
        {
            zoneName = name;
        }
        
        public void SetBaseDamageMultiplier(float multiplier)
        {
            baseDamageMultiplier = Mathf.Max(0f, multiplier);
        }
    }
}
