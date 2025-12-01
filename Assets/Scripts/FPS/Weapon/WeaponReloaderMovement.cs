using System.Collections;
using UnityEngine;

public class WeaponReloaderMovement : MonoBehaviour
{
    [Header("Rotation à vide")]
    [Tooltip("Axe local (masque) autour duquel effectuer la rotation. Composantes non nulles indiquent les axes affectés (ex: Z=1 pour tourner autour de Z).")]
    [SerializeField] private Vector3 localAxis = new Vector3(0f, 0f, 1f);
    [Tooltip("Angle total de rotation en degrés. 360, 720… sont supportés.")]
    [SerializeField] private float rotationAngle = 180f;
    [Tooltip("Durée de l’animation de rotation (secondes).")]
    [SerializeField] private float rotationDuration = 0.35f;
    [Tooltip("Courbe de progression (0→1).")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Délai avant de commencer (secondes).")]
    [SerializeField] private float startDelay = 0f;
    [Tooltip("Revenir à la rotation initiale à la fin (uniquement sur les axes indiqués par localAxis).")]
    [SerializeField] private bool returnToStart = true;
    [Tooltip("Durée du retour (secondes).")]
    [SerializeField] private float returnDuration = 0.25f;

    [Header("Sécurité")]
    [Tooltip("Ignorer si une rotation est déjà en cours.")]
    [SerializeField] private bool preventOverlap = true;

    private Quaternion initialLocalRotation;
    private Vector3 initialLocalEuler; // pour le retour par axe
    private Coroutine currentRoutine;

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;
        initialLocalEuler = transform.localEulerAngles;
    }

    public void TriggerRotateOnEmpty()
    {
        if (preventOverlap && currentRoutine != null) return;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(RotateSequence());
    }

    private IEnumerator RotateSequence()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        Quaternion startRot = transform.localRotation;
        Vector3 axis = localAxis.normalized;
        float totalAngle = rotationAngle;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, rotationDuration);

        while (t < 1f)
        {
            float k = easeCurve != null ? easeCurve.Evaluate(t) : t;
            float appliedAngle = totalAngle * k;

            transform.localRotation = startRot * Quaternion.AngleAxis(appliedAngle, axis);
            t += Time.deltaTime / dur;
            yield return null;
        }
        transform.localRotation = startRot * Quaternion.AngleAxis(totalAngle, axis);

        if (returnToStart)
        {
            yield return StartCoroutine(ReturnByAxis());
        }

        currentRoutine = null;
    }

    private IEnumerator ReturnByAxis()
    {
        float rt = 0f;
        float rdur = Mathf.Max(0.0001f, returnDuration);

        Vector3 currentEuler = transform.localEulerAngles;

        Vector3 targetEuler = currentEuler;
        if (Mathf.Abs(localAxis.x) > 0f) targetEuler.x = initialLocalEuler.x;
        if (Mathf.Abs(localAxis.y) > 0f) targetEuler.y = initialLocalEuler.y;
        if (Mathf.Abs(localAxis.z) > 0f) targetEuler.z = initialLocalEuler.z;

        while (rt < 1f)
        {
            float k = easeCurve != null ? easeCurve.Evaluate(rt) : rt;

            Vector3 e = new Vector3(
                Mathf.LerpAngle(currentEuler.x, targetEuler.x, k),
                Mathf.LerpAngle(currentEuler.y, targetEuler.y, k),
                Mathf.LerpAngle(currentEuler.z, targetEuler.z, k)
            );

            transform.localEulerAngles = e;
            rt += Time.deltaTime / rdur;
            yield return null;
        }
        transform.localEulerAngles = targetEuler;
    }

    private void OnDisable()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
        transform.localRotation = initialLocalRotation;
    }
}