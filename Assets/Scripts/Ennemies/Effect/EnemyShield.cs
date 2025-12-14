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

        public bool ShieldActive { get => shieldActive; set => shieldActive = value; }
        public string ShieldZoneName => shieldZoneName;

        // IDamageInterceptor: retourne false pour bloquer le dégât
        public bool OnBeforeDamage(ref DamageInfo damage)
        {
            if (!shieldActive) return true;

            // Si la zone explicitement indique Shield, bloquer
            if (!string.IsNullOrWhiteSpace(damage.zoneName) && damage.zoneName == shieldZoneName)
            {
                TryFlashHitZone(damage.hitCollider);
                return false;
            }

            // Si le collider touché appartient au GameObject du shield (ou à un parent ayant ce component), bloquer
            if (damage.hitCollider != null)
            {
                // Vérifier si le collider a un EnemyShield dans ses parents
                var shieldComp = damage.hitCollider.GetComponentInParent<EnemyShield>();
                if (shieldComp != null)
                {
                    TryFlashHitZone(damage.hitCollider);
                    return false;
                }

                // Vérifier le HitZone associé au collider
                var hz = damage.hitCollider.GetComponent<HitZone>() ?? damage.hitCollider.GetComponentInParent<HitZone>();
                if (hz != null && hz.ZoneName == shieldZoneName)
                {
                    hz.FlashOnHit();
                    return false;
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

