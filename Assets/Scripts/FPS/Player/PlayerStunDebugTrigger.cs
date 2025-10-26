using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Déclencheur de stun pour tests: appuyer sur la touche (par défaut K) pour étourdir le joueur.
    /// À n'activer qu'en debug.
    /// </summary>
    public class PlayerStunDebugTrigger : MonoBehaviour
    {
        [SerializeField] private bool enableDebugKey = true;
        [SerializeField] private KeyCode stunKey = KeyCode.K;
        [SerializeField] private float stunDuration = 2.5f;
        [SerializeField] private float autoFireInterval = 0.12f;

        private PlayerStunAutoFire stun;

        private void Awake()
        {
            stun = GetComponent<PlayerStunAutoFire>();
            if (stun == null)
            {
                stun = gameObject.AddComponent<PlayerStunAutoFire>();
            }
        }

        private void Update()
        {
            if (!enableDebugKey) return;
            if (Input.GetKeyDown(stunKey))
            {
                stun.ApplyStun(stunDuration, autoFireInterval);
            }
        }
    }
}

