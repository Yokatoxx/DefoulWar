using System.Collections;
using UnityEngine;

public class WeaponShow : MonoBehaviour
{
    [Header("R�f�rence (laisser vide pour utiliser ce GameObject)")]
    [SerializeField] private Transform weaponRoot;

    [Header("Etat initial")]
    [SerializeField] private bool startHidden = true;

    [Header("Animation (effet 'sort de la poche')")]
    [Tooltip("D�calage local depuis la position 'affich�e' vers la position 'poche'")]
    [SerializeField] private Vector3 pocketLocalOffset = new Vector3(0f, -0.25f, -0.20f);
    [Tooltip("Rotation locale ajout�e lorsque l'arme est en 'poche'")]
    [SerializeField] private Vector3 pocketLocalEulerOffset = new Vector3(15f, 0f, 0f);
    [SerializeField, Min(0f)] private float showDuration = 0.25f;
    [SerializeField, Min(0f)] private float hideDuration = 0.18f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Options")]
    [Tooltip("D�sactive les renderers quand l'arme est cach�e (garde l'objet actif pour permettre l'animation)")]
    [SerializeField] private bool disableRenderersWhenHidden = true;

    [Header("Test Toggle (cacher/montrer)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.T;

    [Header("Mode Arme Baiss�e")]
    [Tooltip("D�calage local depuis la position 'affich�e' vers la position 'baiss�e'")]
    [SerializeField] private Vector3 loweredLocalOffset = new Vector3(0f, -0.15f, 0f);
    [Tooltip("Rotation locale ajout�e lorsque l'arme est 'baiss�e'")]
    [SerializeField] private Vector3 loweredLocalEulerOffset = new Vector3(10f, 0f, 0f);
    [SerializeField, Min(0f)] private float lowerDuration = 0.2f;
    [SerializeField, Min(0f)] private float raiseDuration = 0.2f;
    [SerializeField] private KeyCode lowerToggleKey = KeyCode.L;

    private Transform W => weaponRoot != null ? weaponRoot : transform;

    private Vector3 _shownLocalPos;
    private Quaternion _shownLocalRot;
    private Vector3 _shownLocalScale;

    private Renderer[] _renderers;
    private Coroutine _animRoutine;

    private bool _isVisible;
    private bool _isLowered;

    public bool IsVisible => _isVisible;
    public bool IsLowered => _isLowered;

    private void Awake()
    {
        if (weaponRoot == null) weaponRoot = transform;

        // La pose "affich�e" est la pose actuelle dans l'�diteur.
        _shownLocalPos = W.localPosition;
        _shownLocalRot = W.localRotation;
        _shownLocalScale = W.localScale;

        _renderers = weaponRoot.GetComponentsInChildren<Renderer>(true);

        if (startHidden)
        {
            SetHiddenInstant();
        }
        else
        {
            _isVisible = true;
            _isLowered = false;
            if (disableRenderersWhenHidden) SetRenderers(true);
            SetPoseShownInstant();
        }
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        // Toggle simple pour test cacher/montrer
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleWeapon();
        }

        // Toggle arme baiss�e (ne cache pas)
        if (Input.GetKeyDown(lowerToggleKey))
        {
            ToggleLowered();
        }
    }

    [ContextMenu("Show Weapon (animated)")]
    public void ShowWeapon() => ShowWeapon(false);

    [ContextMenu("Hide Weapon (animated)")]
    public void HideWeapon() => HideWeapon(false);

    public void ToggleWeapon(bool instant = false)
    {
        if (_isVisible) HideWeapon(instant);
        else ShowWeapon(instant);
    }

    public void ShowWeapon(bool instant)
    {
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(AnimateShowHide(show: true, instant: instant));
    }

    public void HideWeapon(bool instant)
    {
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(AnimateShowHide(show: false, instant: instant));
    }

    // Baisser / Remettre (sans cacher)
    public void LowerWeapon(bool instant = false)
    {
        if (!_isVisible)
        {
            // Si l'arme est cach�e, on l'affiche d'abord
            ShowWeapon(instant);
            _isLowered = true; // sera anim�e en position affich�e; on re-baissera juste apr�s si besoin
        }

        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(AnimateLowerRaise(lower: true, instant: instant));
    }

    public void RaiseWeapon(bool instant = false)
    {
        if (!_isVisible)
        {
            // Arme cach�e => rien � faire, on reste cach�
            return;
        }

        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(AnimateLowerRaise(lower: false, instant: instant));
    }

    public void ToggleLowered(bool instant = false)
    {
        if (!_isVisible)
        {
            // Ne cache pas/affiche automatiquement: si l'arme est cach�e, on l'affiche directement puis applique l'�tat souhait�
            ShowWeapon(instant);
            // On d�marre ensuite l'�tat bascul�
        }

        if (_isLowered) RaiseWeapon(instant);
        else LowerWeapon(instant);
    }

    private IEnumerator AnimateShowHide(bool show, bool instant)
    {
        // Si on cache, on part de la pose actuelle (qui peut �tre baiss�e ou non)
        Vector3 endPos = show ? _shownLocalPos : _shownLocalPos + pocketLocalOffset;
        Quaternion endRot = show ? _shownLocalRot : _shownLocalRot * Quaternion.Euler(pocketLocalEulerOffset);
        float duration = show ? showDuration : hideDuration;

        Vector3 startPos;
        Quaternion startRot;

        if (show)
        {
            // Si on affiche, on part de la poche
            startPos = _shownLocalPos + pocketLocalOffset;
            startRot = _shownLocalRot * Quaternion.Euler(pocketLocalEulerOffset);
        }
        else
        {
            // Si on cache, on part de la pose actuelle (baiss�e ou normale)
            var currentPose = GetCurrentTargetPose();
            startPos = currentPose.pos;
            startRot = currentPose.rot;
        }

        if (instant || duration <= 0f)
        {
            W.localPosition = endPos;
            W.localRotation = endRot;
            if (disableRenderersWhenHidden) SetRenderers(show);
            _isVisible = show;
            // Quand on cache, l'�tat lowered n'a plus de sens visuellement; on le conserve mais n'impacte pas.
            _animRoutine = null;
            yield break;
        }

        // Pour l'animation d'apparition, on rend visible avant de bouger.
        if (show && disableRenderersWhenHidden) SetRenderers(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = easing != null ? Mathf.Clamp01(easing.Evaluate(Mathf.Clamp01(t))) : Mathf.Clamp01(t);

            W.localPosition = Vector3.LerpUnclamped(startPos, endPos, k);
            W.localRotation = Quaternion.SlerpUnclamped(startRot, endRot, k);
            yield return null;
        }

        W.localPosition = endPos;
        W.localRotation = endRot;

        if (!show && disableRenderersWhenHidden) SetRenderers(false);

        _isVisible = show;
        _animRoutine = null;
    }

    private IEnumerator AnimateLowerRaise(bool lower, bool instant)
    {
        // Ne manipule pas les renderers; l'arme reste visible
        var shownPos = _shownLocalPos;
        var shownRot = _shownLocalRot;

        var loweredPos = shownPos + loweredLocalOffset;
        var loweredRot = shownRot * Quaternion.Euler(loweredLocalEulerOffset);

        Vector3 startPos = W.localPosition;
        Quaternion startRot = W.localRotation;

        Vector3 endPos = lower ? loweredPos : shownPos;
        Quaternion endRot = lower ? loweredRot : shownRot;

        float duration = lower ? lowerDuration : raiseDuration;

        if (instant || duration <= 0f)
        {
            W.localPosition = endPos;
            W.localRotation = endRot;
            _isLowered = lower;
            _animRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = easing != null ? Mathf.Clamp01(easing.Evaluate(Mathf.Clamp01(t))) : Mathf.Clamp01(t);

            W.localPosition = Vector3.LerpUnclamped(startPos, endPos, k);
            W.localRotation = Quaternion.SlerpUnclamped(startRot, endRot, k);
            yield return null;
        }

        W.localPosition = endPos;
        W.localRotation = endRot;
        _isLowered = lower;
        _animRoutine = null;
    }

    private (Vector3 pos, Quaternion rot) GetCurrentTargetPose()
    {
        if (_isLowered)
        {
            var loweredPos = _shownLocalPos + loweredLocalOffset;
            var loweredRot = _shownLocalRot * Quaternion.Euler(loweredLocalEulerOffset);
            return (loweredPos, loweredRot);
        }
        return (_shownLocalPos, _shownLocalRot);
    }

    private void SetHiddenInstant()
    {
        W.localPosition = _shownLocalPos + pocketLocalOffset;
        W.localRotation = _shownLocalRot * Quaternion.Euler(pocketLocalEulerOffset);
        if (disableRenderersWhenHidden) SetRenderers(false);
        _isVisible = false;
        _isLowered = false; // �tat neutre quand cach�e
    }

    private void SetPoseShownInstant()
    {
        W.localPosition = _shownLocalPos;
        W.localRotation = _shownLocalRot;
    }

    private void SetRenderers(bool enabled)
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null) _renderers[i].enabled = enabled;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var t = weaponRoot ? weaponRoot : transform;

        var posShown = Application.isPlaying ? _shownLocalPos : t.localPosition;
        var posHidden = posShown + pocketLocalOffset;
        var posLowered = posShown + loweredLocalOffset;

        Gizmos.color = Color.cyan;
        Gizmos.matrix = t.parent ? t.parent.localToWorldMatrix : Matrix4x4.identity;
        Gizmos.DrawWireSphere(posShown, 0.02f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(posHidden, 0.02f);
        Gizmos.DrawLine(posHidden, posShown);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(posLowered, 0.02f);
        Gizmos.DrawLine(posLowered, posShown);
    }
#endif
}