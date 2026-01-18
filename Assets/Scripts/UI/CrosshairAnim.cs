using FPS;
using UnityEngine;

public class CrosshairAnim : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Animator animator;
    [SerializeField] private FPSMovement playerMovement;

    [Header("Paramètres Animator Mouvement")]
    [SerializeField] private string moveBool = "IsMoving";

    [Header("Tir")]
    [Tooltip("Utiliser un Trigger (recommande). Si désactivé, on utilisera un Bool (shootBoolName) qui sera auto-reset.")]
    [SerializeField] private bool useShootTrigger = true;
    [SerializeField] private string shootTrigger = "Shoot";
    [SerializeField] private string shootBoolName = "IsShooting";
    [Tooltip("Durée avant reset auto du bool de tir si useShootTrigger = false.")]
    [SerializeField] private float shootBoolAutoResetTime = 0.12f;
    [Tooltip("Forcer un reset du bool si l'état d'anim change (securité).")]
    [SerializeField] private bool forceResetOnStateChange = true;

    private float shootBoolResetAt = -1f;
    private int lastStateHash;
    private bool hasMoveBool;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = FindAnyObjectByType<FPSMovement>();
        
        // Verifier si le paramètre existe dans l'animator
        hasMoveBool = false;
        if (animator != null && !string.IsNullOrEmpty(moveBool))
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == moveBool && param.type == AnimatorControllerParameterType.Bool)
                {
                    hasMoveBool = true;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (animator == null) return;

        // Mouvement -> Idle/Walk (seulement si le paramètre existe)
        if (hasMoveBool)
        {
            bool isMoving = playerMovement != null && playerMovement.IsMoving;
            animator.SetBool(moveBool, isMoving);
        }

        // Gestion reset auto du bool de tir
        if (!useShootTrigger && !string.IsNullOrEmpty(shootBoolName))
        {
            if (shootBoolResetAt > 0f && Time.time >= shootBoolResetAt)
            {
                animator.SetBool(shootBoolName, false);
                shootBoolResetAt = -1f;
            }

            if (forceResetOnStateChange)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                int currentHash = state.fullPathHash;
                if (currentHash != lastStateHash)
                {
                    if (shootBoolResetAt > 0f)
                    {
                        animator.SetBool(shootBoolName, false);
                        shootBoolResetAt = -1f;
                    }
                    lastStateHash = currentHash;
                }
            }
        }
    }

    public void PlayShoot()
    {
        if (animator == null) return;

        if (useShootTrigger)
        {
            if (!string.IsNullOrEmpty(shootTrigger))
                animator.SetTrigger(shootTrigger);
        }
        else
        {
            if (!string.IsNullOrEmpty(shootBoolName))
            {
                animator.SetBool(shootBoolName, true);
                shootBoolResetAt = Time.time + shootBoolAutoResetTime;
            }
        }
    }

    public void EndShoot()
    {
        if (!useShootTrigger && !string.IsNullOrEmpty(shootBoolName))
        {
            animator.SetBool(shootBoolName, false);
            shootBoolResetAt = -1f;
        }
    }
}
