// filepath: e:\Documents\Projet Unity\DefoulWar\Assets\Scripts\FPS\DamageInfo.cs
using UnityEngine;

namespace FPS
{
    public enum DamageType
    {
        Bullet,
        Dash,
        Electric,
        Explosion,
        Melee,
        Other
    }

    public struct DamageInfo
    {
        public float amount;
        public string zoneName;
        public DamageType type;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public Transform attacker;
        public Collider hitCollider;
        public bool countAsHit; // Si true, même si un intercepteur bloque le dégât, on considère que l'attaque a "touché" pour les effets (ex: rebond)

        public DamageInfo(float amount, string zoneName = "Body", DamageType type = DamageType.Bullet,
            Vector3 hitPoint = default, Vector3 hitNormal = default, Transform attacker = null, Collider hitCollider = null, bool countAsHit = false)
        {
            this.amount = amount;
            this.zoneName = string.IsNullOrWhiteSpace(zoneName) ? "Body" : zoneName;
            this.type = type;
            this.hitPoint = hitPoint;
            this.hitNormal = hitNormal;
            this.attacker = attacker;
            this.hitCollider = hitCollider;
            this.countAsHit = countAsHit;
        }
    }
}
