using System;
using System.Collections;
using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Représente une zone spécifique de l'ennemi (tête, torse, bras, jambes).
    /// </summary>
    public class HitZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private string zoneName = "Body";
        [SerializeField] private float baseDamageMultiplier = 1f;
        
        [Header("Armor Settings")]
        [SerializeField] private bool hasArmor = false;
        [SerializeField] private int armorLevel = 0; // 0=none, 1=bleu, 2=violet, 3=orange
        [SerializeField] private float armorDamageReduction = 0f; // proportion (0.3 -> -30% dmg)
        
        [Header("Colors (Feedback)")]
        [SerializeField] private Color level1Color = Color.blue;           // Bleu
        [SerializeField] private Color level2Color = new Color(0.6f, 0.2f, 0.8f); // Violet
        [SerializeField] private Color level3Color = new Color(1f, 0.5f, 0f);     // Orange
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashDuration = 0.08f;
        
        private EnemyHealth enemyHealth;
        private Renderer cachedRenderer;
        private Color baseColor;
        private Color currentColor;
        private Coroutine flashRoutine;
        
        private void Awake()
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer != null)
            {
                baseColor = cachedRenderer.material.color;
                currentColor = baseColor;
            }
        }
        
        /// <summary>
        /// Ajoute une armure à cette zone (compatibilité ancienne API).
        /// </summary>
        public void AddArmor(float damageReduction = 0.5f)
        {
            hasArmor = true;
            armorLevel = Math.Max(armorLevel, 1);
            armorDamageReduction = Mathf.Clamp01(damageReduction);
            UpdateVisualColor();
        }
        
        /// <summary>
        /// Définit directement le niveau d'armure (1=Bleu,2=Violet,3=Orange) et la réduction associée.
        /// </summary>
        public void SetArmorLevel(int level)
        {
            armorLevel = Mathf.Clamp(level, 0, 3);
            hasArmor = armorLevel > 0;
            armorDamageReduction = armorLevel switch
            {
                1 => 0.3f, // Bleu: -30% dégâts
                2 => 0.5f, // Violet: -50% dégâts
                3 => 0.7f, // Orange: -70% dégâts
                _ => 0f
            };
            UpdateVisualColor();
            #if UNITY_EDITOR
            Debug.Log($"[HitZone] {gameObject.name} zone '{zoneName}': SetArmorLevel -> {armorLevel}, reduction={armorDamageReduction:P0}");
            #endif
        }
        
        /// <summary>
        /// Retire l'armure de cette zone.
        /// </summary>
        public void RemoveArmor()
        {
            hasArmor = false;
            armorLevel = 0;
            armorDamageReduction = 0f;
            UpdateVisualColor();
            #if UNITY_EDITOR
            Debug.Log($"[HitZone] {gameObject.name} zone '{zoneName}': RemoveArmor");
            #endif
        }
        
        /// <summary>
        /// Donne le multiplicateur final avec armure.
        /// </summary>
        public float GetFinalDamageMultiplier()
        {
            float final = baseDamageMultiplier;
            if (hasArmor)
            {
                final *= (1f - armorDamageReduction);
            }
            return final;
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
            Color before = currentColor;
            cachedRenderer.material.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);
            cachedRenderer.material.color = before;
            flashRoutine = null;
        }
        
        private void UpdateVisualColor()
        {
            if (cachedRenderer == null) return;
            currentColor = armorLevel switch
            {
                1 => level1Color,
                2 => level2Color,
                3 => level3Color,
                _ => baseColor
            };
            cachedRenderer.material.color = currentColor;
        }
        
        // Propriétés publiques
        public string ZoneName => zoneName;
        public float DamageMultiplier => GetFinalDamageMultiplier();
        public bool HasArmor => hasArmor;
        public int ArmorLevel => armorLevel;
        public EnemyHealth EnemyHealth => enemyHealth;
        
        /// <summary>
        /// Multiplicateur de base (sans armure) de la zone.
        /// </summary>
        public float BaseMultiplier => baseDamageMultiplier;
        
        /// <summary>
        /// Facteur d'armure seul (1 = pas d'armure, <1 = réduction).
        /// </summary>
        public float ArmorFactor => hasArmor ? (1f - armorDamageReduction) : 1f;
        
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
