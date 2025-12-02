using UnityEngine;

namespace FPS
{
    [CreateAssetMenu(fileName = "BounceDefinition", menuName = "FPS/Bounce Definition")]
    public class BounceDefinition : ScriptableObject
    {
        [Header("Direction")]
        public Vector3 direction = new Vector3(0f, 0.85f, -0.35f);
        public bool directionIsLocal = true;

        [Header("Force")]
        [Min(0f)] public float force = 18f;
    }
}

