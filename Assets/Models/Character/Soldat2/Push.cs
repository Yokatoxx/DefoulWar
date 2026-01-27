using UnityEngine;

public class NPCPushTrigger : MonoBehaviour
{
    private Animator animator;
    private bool canPush = true;

    [Header("Pousse")]
    public float pushDistance = 3f;  // distance de recul
    public float pushSpeed = 5f;     // vitesse de recul
    public float animationCooldown = 2.5f; // durée après l'animation avant de pouvoir repousser à nouveau

    // Target stockée quand le joueur entre dans le trigger
    private CharacterController pendingCC;
    private Rigidbody pendingRB;
    private bool hasPendingTarget = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canPush) return;

        if (other.CompareTag("Player"))
        {
            // Ne pas pousser immédiatement : on stocke la cible et on déclenche l'anim.
            // L'animation doit appeler OnPushAnimationEvent via un Animation Event au moment voulu.
            pendingCC = other.GetComponent<CharacterController>();
            pendingRB = other.GetComponent<Rigidbody>();
            hasPendingTarget = true;

            animator.SetTrigger("Push");

            // Note: ResetPush se fera après la poussée, déclenchée par l'event d'animation.
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Si le joueur quitte la zone avant que l'event d'anim se produise, on annule la cible.
        if (!hasPendingTarget) return;
        if (other.CompareTag("Player"))
        {
            // Si c'est la même entité, clear
            if (pendingCC == other.GetComponent<CharacterController>() || pendingRB == other.GetComponent<Rigidbody>())
            {
                ClearPending();
            }
        }
    }

    // Méthode publique appelée par un Animation Event sur l'animation "Push"
    public void OnPushAnimationEvent()
    {
        if (!canPush) return;
        if (!hasPendingTarget) return;

        canPush = false; // Bloquer nouvelles poussées tant que la cooldown n'est pas finie

        // Recalculer la direction au moment de l'event (plus précis si le joueur a bougé)
        Transform targetTransform = null;
        if (pendingCC != null) targetTransform = pendingCC.transform;
        else if (pendingRB != null) targetTransform = pendingRB.transform;

        if (targetTransform == null)
        {
            Invoke(nameof(ResetPush), 0.1f);
            ClearPending();
            return;
        }

        Vector3 direction = (targetTransform.position - transform.position).normalized;

        // Si on a un CharacterController, utiliser la coroutine existante
        if (pendingCC != null)
        {
            StartCoroutine(PushPlayer(pendingCC, direction));
        }
        else if (pendingRB != null)
        {
            // Fallback : appliquer une impulsion si le joueur utilise un Rigidbody
            Vector3 impulse = direction * pushSpeed * pushDistance; // approximation
            pendingRB.AddForce(impulse, ForceMode.VelocityChange);
            // On planifie le reset de la possibilité de pousser après la durée de l'anim
            Invoke(nameof(ResetPush), animationCooldown);
            ClearPending();
        }
        else
        {
            // Pas de composant contrôlable trouvé, on réarme la capacité de pousser
            Invoke(nameof(ResetPush), 0.1f);
            ClearPending();
        }
    }

    private System.Collections.IEnumerator PushPlayer(CharacterController cc, Vector3 direction)
    {
        float elapsed = 0f;
        float duration = pushDistance / pushSpeed;

        while (elapsed < duration)
        {
            cc.Move(direction * pushSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Après la poussée, on reset la possibilité après la durée de l'anim
        Invoke(nameof(ResetPush), animationCooldown); // durée de l'anim
        ClearPending();
    }

    void ResetPush()
    {
        canPush = true;
    }

    private void ClearPending()
    {
        pendingCC = null;
        pendingRB = null;
        hasPendingTarget = false;
    }
}
