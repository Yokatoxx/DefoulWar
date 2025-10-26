using UnityEngine;

namespace Ennemies.Effect
{
    public class ElectricEnnemis : MonoBehaviour
    {
        [Header("Effet appliqué au joueur si ce PNJ est touché par un dash")]
        [SerializeField] private float stunDuration = 2.5f;

        [Header("Auto-fire pendant le stun (override optionnel)")]
        [Tooltip("Si activé, remplace l'intervalle d'auto-fire du joueur pendant ce stun.")]
        [SerializeField] private bool overrideAutoFireInterval = false;
        [SerializeField, Min(0.01f)] private float stunAutoFireInterval = 0.12f;

        public float StunDuration => stunDuration;
        public bool OverrideAutoFireInterval => overrideAutoFireInterval;
        public float StunAutoFireInterval => stunAutoFireInterval;
    }
}
