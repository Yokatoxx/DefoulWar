using UnityEngine;
using FPS; // EnemyHealth (pour évents/registry si besoin)

/// Composant à mettre sur un GameObject enfant représentant un point faible.
/// Il possède sa propre "vie" et se détruit lorsqu'il a reçu assez de dégâts.
/// Option: bloque les dégâts de l'ennemi parent tant qu'il n'est pas détruit (via WeakPointGate sur le parent).
public class WeakPointTarget : MonoBehaviour
{
    [Header("Santé du point faible")]
    [SerializeField] private float maxHealth = 25f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool destroyOnDepleted = true;

    [Header("Feedback")]
    [SerializeField] private GameObject destroyVfx;

    private bool isDestroyed;

    private void Awake()
    {
        currentHealth = Mathf.Max(1f, maxHealth);
    }

    /// Applique un dégât au point faible (appelé par votre système d’impact).
    public void TakeWeakPointDamage(float amount)
    {
        if (isDestroyed) return;
        currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));

        if (currentHealth <= 0f)
        {
            isDestroyed = true;
            if (destroyVfx != null)
            {
                Instantiate(destroyVfx, transform.position, transform.rotation);
            }

            if (destroyOnDepleted)
            {
                // On peut juste désactiver au lieu de Destroy pour garder les références
                gameObject.SetActive(false);
            }
        }
    }

    public bool IsDestroyed => isDestroyed || !gameObject.activeInHierarchy;
}