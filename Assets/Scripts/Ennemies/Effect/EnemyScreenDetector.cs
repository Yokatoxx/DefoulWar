using UnityEngine;
using UnityEngine.UI;

namespace Ennemies.Effect
{
    /// <summary>
    /// Affiche des indicateurs directionnels sur l'écran pour indiquer d'où viennent les attaques ennemies.
    /// Supporte 4 directions: gauche, droite, derrière et devant.
    /// </summary>
    public class EnemyScreenDetector : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Caméra utilisée pour orienter les indicateurs (par défaut: Camera.main)")]
        [SerializeField] private Camera targetCamera;

        [Header("Indicateurs UI")]
        [SerializeField] private Image leftIndicator;
        [SerializeField] private Image rightIndicator;
        [SerializeField] private Image backIndicator;
        [SerializeField] private Image frontIndicator;

        [Header("Configuration")]
        [Tooltip("Multiplicateur pour convertir les dégâts en alpha")]
        [SerializeField] private float damageToAlphaFactor = 0.05f;
        [Tooltip("Alpha maximum pour l'effet pulse")]
        [SerializeField] private float maxPulseAlpha = 0.8f;
        [Tooltip("Vitesse de fade out des indicateurs")]
        [SerializeField] private float fadeSpeed = 2f;

        [Header("Seuils d'angle (degrés)")]
        [Tooltip("Angle limite pour considérer une attaque comme venant de face/derrière")]
        [SerializeField] private float frontBackThreshold = 45f;

        // Alpha actuel de chaque direction
        private float leftAlpha;
        private float rightAlpha;
        private float backAlpha;
        private float frontAlpha;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            SetIndicatorAlpha(leftIndicator, 0f);
            SetIndicatorAlpha(rightIndicator, 0f);
            SetIndicatorAlpha(backIndicator, 0f);
            SetIndicatorAlpha(frontIndicator, 0f);
        }

        private void Update()
        {
            leftAlpha = FadeAlpha(leftAlpha);
            rightAlpha = FadeAlpha(rightAlpha);
            backAlpha = FadeAlpha(backAlpha);
            frontAlpha = FadeAlpha(frontAlpha);

            SetIndicatorAlpha(leftIndicator, leftAlpha);
            SetIndicatorAlpha(rightIndicator, rightAlpha);
            SetIndicatorAlpha(backIndicator, backAlpha);
            SetIndicatorAlpha(frontIndicator, frontAlpha);
        }

        /// <summary>
        /// Enregistre un hit venant d'une direction spécifique.
        /// </summary>
        public void RegisterHit(Transform enemyTransform, float damage)
        {
            if (targetCamera == null || enemyTransform == null) return;

            // Direction de l'ennemi par rapport à la caméra (plan horizontal)
            Vector3 toEnemy = enemyTransform.position - targetCamera.transform.position;
            toEnemy.y = 0f;
            toEnemy.Normalize();

            Vector3 forward = targetCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = targetCamera.transform.right;
            right.y = 0f;
            right.Normalize();

            float forwardDot = Vector3.Dot(toEnemy, forward);
            float rightDot = Vector3.Dot(toEnemy, right);

            // Seuil en cos (radians)
            float threshold = Mathf.Cos(frontBackThreshold * Mathf.Deg2Rad);
            float pulse = Mathf.Min(damage * damageToAlphaFactor, maxPulseAlpha);

            if (forwardDot > threshold)
            {
                frontAlpha = Mathf.Min(frontAlpha + pulse, maxPulseAlpha);
            }
            else if (forwardDot < -threshold)
            {
                backAlpha = Mathf.Min(backAlpha + pulse, maxPulseAlpha);
            }
            else
            {
                if (rightDot > 0f)
                {
                    rightAlpha = Mathf.Min(rightAlpha + pulse, maxPulseAlpha);
                }
                else
                {
                    leftAlpha = Mathf.Min(leftAlpha + pulse, maxPulseAlpha);
                }
            }
        }

        private float FadeAlpha(float alpha)
        {
            if (alpha <= 0f) return 0f;
            return Mathf.Max(0f, alpha - fadeSpeed * Time.deltaTime);
        }

        private void SetIndicatorAlpha(Image indicator, float alpha)
        {
            if (indicator == null) return;
            Color c = indicator.color;
            c.a = alpha;
            indicator.color = c;
        }

        [ContextMenu("Test Hit Gauche")]
        private void TestLeft()
        {
            if (targetCamera == null) return;
            RegisterHit(CreateTestTransform(-targetCamera.transform.right * 5f), 10f);
        }

        [ContextMenu("Test Hit Droite")]
        private void TestRight()
        {
            if (targetCamera == null) return;
            RegisterHit(CreateTestTransform(targetCamera.transform.right * 5f), 10f);
        }

        [ContextMenu("Test Hit Derrière")]
        private void TestBack()
        {
            if (targetCamera == null) return;
            RegisterHit(CreateTestTransform(-targetCamera.transform.forward * 5f), 10f);
        }

        [ContextMenu("Test Hit Devant")]
        private void TestFront()
        {
            if (targetCamera == null) return;
            RegisterHit(CreateTestTransform(targetCamera.transform.forward * 5f), 10f);
        }

        private Transform CreateTestTransform(Vector3 offset)
        {
            GameObject temp = new GameObject("TestHitPoint");
            temp.transform.position = targetCamera.transform.position + offset;
            Destroy(temp, 0.1f);
            return temp.transform;
        }
    }
}
