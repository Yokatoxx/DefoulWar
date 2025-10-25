using UnityEngine;
using System.Collections.Generic;

namespace Proto3GD.FPS
{

    [RequireComponent(typeof(Rigidbody))]
    public class Bullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float damage = 25f;
        [SerializeField] private float speed = 50f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float gravity = 0.5f;
        
        [Header("Optional Trail")]
        [SerializeField] private TrailRenderer trailRenderer;
        
        // Multiplicateurs de dégâts par zone
        private bool useWeaponZoneMultipliers = false;
        private Dictionary<string, float> zoneMultiplierMap = null;
        
        private Rigidbody rb;
        private bool hasHit = false;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
                ConfigureTrail();
            }
        }
        
        private void Start()
        {
            rb.linearVelocity = transform.forward * speed;
            rb.useGravity = true;
            rb.linearDamping = 0;
            
            Destroy(gameObject, lifetime);
        }
        
        private void ConfigureTrail()
        {
            trailRenderer.time = 0.2f;
            trailRenderer.startWidth = 0.05f;
            trailRenderer.endWidth = 0.01f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.startColor = new Color(1f, 0.8f, 0.3f, 1f);
            trailRenderer.endColor = new Color(1f, 0.5f, 0f, 0f);
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;
            hasHit = true;
            
            var col = collision.collider;
            EnemyHealth enemyHealth = col.GetComponentInParent<EnemyHealth>();
            HitZone hitZone = col.GetComponent<HitZone>() ?? col.GetComponentInParent<HitZone>();
            
            if (enemyHealth != null)
            {
                string zoneName = hitZone != null ? hitZone.ZoneName : "Body";
                
                if (hitZone != null)
                {
                    hitZone.FlashOnHit();
                }
                
                // Calcul des dégâts: base damage  * multiplier zone * facteur armure
                float zoneMult = 1f;
                float armorFactor = 1f;
                if (hitZone != null)
                {
                    armorFactor = hitZone.ArmorFactor;
                    if (useWeaponZoneMultipliers && zoneMultiplierMap != null && zoneMultiplierMap.TryGetValue(zoneName, out float m))
                    {
                        zoneMult = m;
                    }
                    else
                    {
                        zoneMult = hitZone.BaseMultiplier;
                    }
                }
                
                float finalDamage = damage * zoneMult * armorFactor;
                enemyHealth.TakeDamage(finalDamage, zoneName);
            }
            
            Destroy(gameObject);
        }
        
        public void Initialize(float newDamage, float newSpeed)
        {
            damage = newDamage;
            speed = newSpeed;
            
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * speed;
            }
        }
        
        public void SetZoneMultipliers(Dictionary<string, float> map)
        {
            if (map != null && map.Count > 0)
            {
                zoneMultiplierMap = new Dictionary<string, float>(map);
                useWeaponZoneMultipliers = true;
            }
            else
            {
                zoneMultiplierMap = null;
                useWeaponZoneMultipliers = false;
            }
        }
        
        public void ClearZoneMultipliers()
        {
            zoneMultiplierMap = null;
            useWeaponZoneMultipliers = false;
        }
        
        public static GameObject CreateBulletPrefab()
        {
            GameObject bullet = new GameObject("Bullet");
            
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(bullet.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.05f;
            
            Renderer renderer = visual.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.9f, 0.3f);
            renderer.material = mat;
            
            SphereCollider collider = visual.GetComponent<SphereCollider>();
            
            Rigidbody rb = bullet.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            bullet.AddComponent<Bullet>();
            
            TrailRenderer trail = bullet.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.05f;
            trail.endWidth = 0.01f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 0.8f, 0.3f, 1f);
            trail.endColor = new Color(1f, 0.5f, 0f, 0f);
            
            return bullet;
        }
    }
}
