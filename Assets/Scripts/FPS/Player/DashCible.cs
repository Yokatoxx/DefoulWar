using System.Collections;
using UnityEngine;

namespace FPS
{
public class DashCible : MonoBehaviour
{
    [Header("Ciblage")]
    [SerializeField]
    private Camera aimCamera;
    [SerializeField] private LayerMask enemyMask = ~0;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float maxAimAngle = 30f;

    [Header("Dash Ciblé Settings")]
    [SerializeField] private int countDash = 3;
    [SerializeField] private float slowMoTime = 0.75f;
    [SerializeField] private float slowMoScale = 0.2f;
    [SerializeField] private float distanceDash = 25f;
    [SerializeField] private float cooldown = 1.5f;
    [SerializeField]
    private float dashDamage = 9999f;

    [Header("Déplacement")]
    [SerializeField] private float dashTravelTime = 0.08f;
    [SerializeField] private float capsuleRadius = 0.4f;
    [SerializeField] private float stopOffset = 1.0f;

    [Header("Input")]
    [SerializeField] private KeyCode activationKey = KeyCode.Q;

    [Header("Références Joueur")]
    [SerializeField] private FPSPlayerController playerController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private FPSMovement fpsMovement;

    private bool isDashing;
    private bool chainActive;
    private int remainingChains;
    private float nextAvailableTime;
    private float slowMoEndUnscaled;
    private bool slowMoApplied;
    private float previousTimeScale = 1f;
    private bool pathElectricStunned;

    private static readonly Collider[] OverlapBuffer = new Collider[16];

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<FPSPlayerController>();
        if (characterController == null && playerController != null)
            characterController = playerController.Controller;
        if (fpsMovement == null)
            fpsMovement = GetComponent<FPSMovement>();
        if (aimCamera == null)
            aimCamera = Camera.main;
    }

    private void Update()
    {
        if (slowMoApplied && Time.unscaledTime >= slowMoEndUnscaled)
        {
            ClearSlowMo();
            if (!isDashing && chainActive)
            {
                EndChain();
            }
        }

        if (Input.GetKeyDown(activationKey))
        {
            TryTriggerOrChain();
        }
    }

    private void TryTriggerOrChain()
    {
        if (isDashing) return;

        if (!chainActive)
        {
            if (Time.time < nextAvailableTime) return;
            remainingChains = Mathf.Max(1, countDash);
            chainActive = true;
        }
        else
        {
            if (!slowMoApplied || remainingChains <= 0)
            {
                return;
            }
        }

        var target = AcquireTarget();
        if (target == null)
        {
            if (chainActive)
            {
                ClearSlowMo();
                EndChain();
            }
            return;
        }

        remainingChains = Mathf.Max(0, remainingChains - 1);
        StartCoroutine(DoTargetDash(target));
    }

    private EnemyHealth AcquireTarget()
    {
        if (aimCamera == null) return null;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, distanceDash, enemyMask, QueryTriggerInteraction.Ignore))
        {
            var eh = hit.collider.GetComponentInParent<EnemyHealth>() ?? hit.collider.GetComponent<EnemyHealth>();
            if (eh != null && !eh.IsDead)
            {
                if (!IsObstructed(aimCamera.transform.position, eh.transform.position))
                    return eh;
            }
        }

        var all = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        EnemyHealth best = null;
        float bestScore = float.MaxValue;
        Vector3 camPos = aimCamera.transform.position;
        Vector3 camFwd = aimCamera.transform.forward;

        foreach (var eh in all)
        {
            if (eh == null || eh.IsDead) continue;
            Vector3 to = eh.transform.position - camPos;
            float dist = to.magnitude;
            if (dist > distanceDash) continue;
            Vector3 dir = to / (dist + 1e-5f);
            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(camFwd, dir), -1f, 1f)) * Mathf.Rad2Deg;
            if (angle > maxAimAngle) continue;
            if (IsObstructed(camPos, eh.transform.position)) continue;
            float score = angle * 2f + dist * 0.2f;
            if (score < bestScore)
            {
                bestScore = score;
                best = eh;
            }
        }

        return best;
    }

    private bool IsObstructed(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return false;
        return Physics.Raycast(from, dir.normalized, dist - 0.1f, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private IEnumerator DoTargetDash(EnemyHealth target)
    {
        isDashing = true;
        pathElectricStunned = false;

        Vector3 start = transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 dirToTarget = (targetPos - start).normalized;
        float distToTarget = Vector3.Distance(start, targetPos);

        float stopDist = Mathf.Clamp(stopOffset, 0f, Mathf.Max(0f, distToTarget - 0.1f));
        Vector3 end = targetPos - dirToTarget * stopDist;

        Vector3 top = start + Vector3.up * 1.5f;
        Vector3 bottom = start + Vector3.up * 0.2f;
        Vector3 moveDir = (end - start);
        float moveLen = moveDir.magnitude;
        if (moveLen > 0.01f)
        {
            if (Physics.CapsuleCast(top, bottom, capsuleRadius, moveDir.normalized, out RaycastHit hit, moveLen, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point - moveDir.normalized * 0.2f;
            }
        }

        if (fpsMovement != null)
        {
            fpsMovement.SetSpeedToMax();
        }

        float t0 = Time.unscaledTime;
        float dur = Mathf.Max(0.01f, dashTravelTime);
        Vector3 prev = start;

        while (Time.unscaledTime - t0 < dur)
        {
            float u = (Time.unscaledTime - t0) / dur;
            Vector3 pos = Vector3.Lerp(start, end, u);
            Vector3 delta = pos - prev;
            if (characterController != null)
                characterController.Move(delta);
            else
                transform.position = pos;
            prev = pos;

            TryStunElectricOnPath(pos);

            yield return null;
        }
        Vector3 finalDelta = end - prev;
        if (characterController != null)
            characterController.Move(finalDelta);
        else
            transform.position = end;

        var electric = target.GetComponent<Ennemies.Effect.ElectricEnnemis>();
        if (electric != null)
        {
            var playerStun = GetComponent<PlayerStunAutoFire>();
            if (playerStun == null) playerStun = gameObject.AddComponent<PlayerStunAutoFire>();
            if (electric.OverrideAutoFireInterval)
                playerStun.ApplyStun(electric.StunDuration, electric.StunAutoFireInterval);
            else
                playerStun.ApplyStun(electric.StunDuration);

            if (electric.ResistToDash)
            {
                isDashing = false;
                ClearSlowMo();
                EndChain();
                yield break;
            }
        }

        var hitCol = target.GetComponentInChildren<Collider>();
        var dmg = new DamageInfo(amount: dashDamage, zoneName: "Dash", type: DamageType.Dash, hitPoint: target.transform.position, hitNormal: -dirToTarget, attacker: transform, hitCollider: hitCol);
        bool applied = target.TryApplyDamage(dmg);

        if (applied)
        {
            ApplyOrRefreshSlowMo();
        }

        isDashing = false;

        if (remainingChains <= 0)
        {
            if (!slowMoApplied)
            {
                EndChain();
            }
        }
    }

    private void ApplyOrRefreshSlowMo()
    {
        slowMoEndUnscaled = Time.unscaledTime + Mathf.Max(0.01f, slowMoTime);
        if (!slowMoApplied)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = Mathf.Clamp(slowMoScale, 0.01f, 1f);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            slowMoApplied = true;
        }
    }

    private void ClearSlowMo()
    {
        if (!slowMoApplied) return;
        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        slowMoApplied = false;
    }

    private void EndChain()
    {
        chainActive = false;
        remainingChains = 0;
        nextAvailableTime = Time.time + Mathf.Max(0f, cooldown);
    }

    private void OnDisable()
    {
        if (slowMoApplied) ClearSlowMo();
    }

    private void OnDestroy()
    {
        if (slowMoApplied) ClearSlowMo();
    }

    public int CountDash { get => countDash; set => countDash = Mathf.Max(1, value); }
    public float SlowMoTime { get => slowMoTime; set => slowMoTime = Mathf.Max(0.01f, value); }
    public float DistanceDash { get => distanceDash; set => distanceDash = Mathf.Max(0.5f, value); }
    public float Cooldown { get => cooldown; set => cooldown = Mathf.Max(0f, value); }

    private void TryStunElectricOnPath(Vector3 currentPos)
    {
        if (pathElectricStunned) return;
        Vector3 top = currentPos + Vector3.up * 1.5f;
        Vector3 bottom = currentPos + Vector3.up * 0.2f;
        int count = Physics.OverlapCapsuleNonAlloc(top, bottom, capsuleRadius, OverlapBuffer, enemyMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var col = OverlapBuffer[i];
            if (col == null) continue;
            var electric = col.GetComponentInParent<Ennemies.Effect.ElectricEnnemis>() ?? col.GetComponent<Ennemies.Effect.ElectricEnnemis>();
            if (electric != null)
            {
                var playerStun = GetComponent<PlayerStunAutoFire>() ?? gameObject.AddComponent<PlayerStunAutoFire>();
                if (electric.OverrideAutoFireInterval)
                    playerStun.ApplyStun(electric.StunDuration, electric.StunAutoFireInterval);
                else
                    playerStun.ApplyStun(electric.StunDuration);
                pathElectricStunned = true;
                break;
            }
        }
    }
}
}
