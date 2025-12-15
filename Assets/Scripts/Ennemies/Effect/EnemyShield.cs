// ...new file...
using UnityEngine;
using FPS;

namespace Ennemies.Effect
{
    /// <summary>
    /// Composant attaché au cube physique représentant le bouclier.
    /// Implémente IDamageInterceptor pour bloquer tous les dégâts lorsque le collider du shield est touché.
    /// </summary>
    public class EnemyShield : MonoBehaviour, IDamageInterceptor
    {
        [Header("Shield Settings")]
        [Tooltip("Si false, le shield n'interceptera pas les dégâts (utile pour debug)")]
        [SerializeField] private bool shieldActive = true;

        [Tooltip("Nom de la zone associée (doit correspondre au HitZone si utilisé)")]
        [SerializeField] private string shieldZoneName = "Shield";

        [Header("Dash Blocking")]
        [Tooltip("Si true, le shield bloquera les Dash qui proviennent d'un attaquant situé dans le cône frontal de l'ennemi.")]
        [SerializeField] private bool blockDashByFrontalCone = true;
        [Tooltip("Angle total (en degrés) du cône frontal centré sur l'avant de l'ennemi. Ex: 90 => +/-45°")]
        [SerializeField] private float frontalBlockAngle = 90f;

        public bool ShieldActive { get => shieldActive; set => shieldActive = value; }
        public string ShieldZoneName => shieldZoneName;

        // IDamageInterceptor: retourne false pour bloquer le dégât
        public bool OnBeforeDamage(ref DamageInfo damage)
        {
            if (!shieldActive) return true;

            // Seuls les Dashs peuvent être interceptés par ce composant ; les autres dégâts passent normalement
            if (damage.type != DamageType.Dash)
            {
                // Feedback visuel si la hitzone Shield est touchée
                if (damage.hitCollider != null)
                {
                    var hz = damage.hitCollider.GetComponent<HitZone>() ?? damage.hitCollider.GetComponentInParent<HitZone>();
                    if (hz != null && hz.ZoneName == shieldZoneName)
                        hz.FlashOnHit();
                }
                return true;
            }

            // === SIMPLE: blocage par cône frontal ===
            if (blockDashByFrontalCone)
            {
                // Récupérer transform racine de l'ennemi
                Transform enemyTransform = GetComponentInParent<EnnemiBehaviour>()?.transform ?? GetComponentInParent<EnemyHealth>()?.transform ?? transform.parent;
                if (enemyTransform != null)
                {
                    Vector3 attackerDir = Vector3.zero;
                    // Prioriser hitNormal (fourni par le DashCible) car il représente la direction depuis l'ennemi vers l'attaquant
                    if (damage.hitNormal != Vector3.zero)
                    {
                        attackerDir = damage.hitNormal.normalized;
                    }
                    else if (damage.attacker != null)
                    {
                        attackerDir = (damage.attacker.position - enemyTransform.position);
                        if (attackerDir.sqrMagnitude > 0.0001f)
                            attackerDir = attackerDir.normalized;
                        else
                            attackerDir = Vector3.zero;
                    }

                    if (attackerDir != Vector3.zero)
                    {
                        Vector3 forward = enemyTransform.forward;
                        float angle = Vector3.Angle(forward, attackerDir);
                        bool blocked = angle <= frontalBlockAngle * 0.5f;

#if UNITY_EDITOR
                        Debug.Log($"[EnemyShield] Dash angle={angle:F1} blocked={blocked} attackerDir={attackerDir} enemy={enemyTransform.name}");
#endif

                        if (blocked)
                        {
                            TryFlashHitZone(damage.hitCollider);
                            // Indiquer que le dash doit être compté comme un hit pour déclencher le rebond même si les dégâts sont bloqués
                            damage.countAsHit = true;
                            return false; // Bloquer le dash
                        }
                    }
                }
            }

            // Par défaut laisser passer
            return true;
        }

        private void TryFlashHitZone(Collider col)
        {
            if (col == null) return;
            var hz = col.GetComponent<HitZone>() ?? col.GetComponentInParent<HitZone>();
            if (hz != null)
            {
                hz.FlashOnHit();
            }
        }
    }
}