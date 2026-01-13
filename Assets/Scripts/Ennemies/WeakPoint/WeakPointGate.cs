using System.Collections.Generic;
using UnityEngine;
using FPS; // DamageInfo, IDamageInterceptor

/// À mettre sur l'ennemi (même GameObject que EnemyHealth).
/// Tant qu'au moins un WeakPointTarget requis n'est pas détruit, bloque tous les dégâts.
public class WeakPointGate : MonoBehaviour, IDamageInterceptor
{
    [Header("Activation")]
    [SerializeField] private bool enabledGate = true;

    [Header("Points faibles requis")]
    [Tooltip("Liste explicite des points faibles. Si vide, cherche automatiquement dans les enfants.")]
    [SerializeField] private List<WeakPointTarget> requiredWeakPoints = new List<WeakPointTarget>();

    [Header("Comportement en blocage")]
    [Tooltip("Compter le tir comme hit statistique (OnDamageTaken) même si bloqué.")]
    [SerializeField] private bool countHitWhenBlocked = true;

    private void Awake()
    {
        if (requiredWeakPoints == null || requiredWeakPoints.Count == 0)
        {
            // Auto-discovery: chercher dans les enfants
            requiredWeakPoints = new List<WeakPointTarget>(GetComponentsInChildren<WeakPointTarget>(true));
        }
    }

    public bool OnBeforeDamage(ref DamageInfo info)
    {
        if (!enabledGate) return true;

        // Si pas de points faibles requis -> comportement classique
        if (requiredWeakPoints == null || requiredWeakPoints.Count == 0)
            return true;

        // Vérifier s'il reste des points faibles actifs
        bool anyRemaining = false;
        for (int i = 0; i < requiredWeakPoints.Count; i++)
        {
            var wp = requiredWeakPoints[i];
            if (wp == null) continue;

            if (!wp.IsDestroyed)
            {
                anyRemaining = true;
                break;
            }
        }

        if (!anyRemaining)
        {
            // Tous détruits -> autoriser les dégâts
            return true;
        }

        // Bloquer les dégâts tant que des points faibles restent
        info.amount = 0f;
        info.countAsHit = countHitWhenBlocked;
        return false;
    }

    /// Permet de définir/mettre à jour la liste explicitement en runtime.
    public void SetWeakPoints(IEnumerable<WeakPointTarget> points)
    {
        requiredWeakPoints = new List<WeakPointTarget>(points);
    }

    public void SetEnabled(bool enabled) => enabledGate = enabled;
}