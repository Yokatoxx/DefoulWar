using UnityEngine;
using FPS;

namespace Ennemies.Effect
{
    [RequireComponent(typeof(Collider))]
    public class SlamDamageZone : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 30f;
        [SerializeField] private float lifetime = 0.6f;
        [SerializeField] private bool destroyOnEnd = true;

        private float nextTickTime;
        private const float tickInterval = 0.1f;
        private float endTime;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true; // garantir trigger
        }

        public void Init(float damagePerSecond, float lifetime)
        {
            this.damagePerSecond = Mathf.Max(0f, damagePerSecond);
            this.lifetime = Mathf.Max(0.05f, lifetime);
            endTime = Time.time + this.lifetime;
            nextTickTime = Time.time + tickInterval;
        }

        private void Update()
        {
            if (Time.time >= endTime)
            {
                if (destroyOnEnd) Destroy(gameObject);
                else gameObject.SetActive(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (Time.time < nextTickTime) return;
            nextTickTime = Time.time + tickInterval;

            var player = other.GetComponentInParent<PlayerHealth>() ?? other.GetComponent<PlayerHealth>();
            if (player != null && !player.IsDead)
            {
                player.TakeDamage(damagePerSecond * tickInterval);
            }
        }
    }
}