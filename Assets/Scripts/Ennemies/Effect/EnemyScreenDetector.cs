using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyScreenDetector : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Caméra utilisée pour orienter les indicateurs (par défaut: Camera.main).")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Transform du joueur (par défaut: ce GameObject).")]
    [SerializeField] private Transform player;

    [Header("HUD")]
    [SerializeField] private Image indicatorLeft;
    [SerializeField] private Image indicatorRight;
    [SerializeField] private Image indicatorBack;

    [Header("Intensité en fonction de la distance")]
    [Tooltip("Distance (m) à partir de laquelle l’alpha est maximum.")]
    [SerializeField] private float minDistanceForMaxAlpha = 3f;
    [Tooltip("Distance (m) au-delà de laquelle l’alpha tend vers 0.")]
    [SerializeField] private float maxDistanceForMinAlpha = 30f;

    [Header("Courbe de décroissance")]
    [Tooltip("Vitesse de fade des indicateurs (unités d’alpha par seconde).")]
    [SerializeField] private float fadeOutSpeed = 2.5f;
    [Tooltip("Gain d’alpha en fonction des dégâts reçus (alpha += damage * factor).")]
    [SerializeField] private float damageToAlphaFactor = 0.02f;
    [Tooltip("Alpha max autorisé par pulse.")]
    [SerializeField, Range(0f, 1f)] private float maxPulseAlpha = 1f;

    private float leftAlpha;
    private float rightAlpha;
    private float backAlpha;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (player == null) player = transform;

        SetImageAlpha(indicatorLeft, 0f);
        SetImageAlpha(indicatorRight, 0f);
        SetImageAlpha(indicatorBack, 0f);
    }

    private void Update()
    {
        // Fade vers 0
        if (leftAlpha > 0f) leftAlpha = Mathf.Max(0f, leftAlpha - fadeOutSpeed * Time.deltaTime);
        if (rightAlpha > 0f) rightAlpha = Mathf.Max(0f, rightAlpha - fadeOutSpeed * Time.deltaTime);
        if (backAlpha > 0f) backAlpha = Mathf.Max(0f, backAlpha - fadeOutSpeed * Time.deltaTime);

        ApplyAlphas();
    }

    // À appeler depuis les scripts d’attaque ennemie (melee, projectiles, etc.)
    public void RegisterHit(Transform attacker, float damage = 0f)
    {
        if (attacker == null) return;
        RegisterHit(attacker.position, damage);
    }

    // Variante avec position directe
    public void RegisterHit(Vector3 attackerWorldPosition, float damage = 0f)
    {
        if (targetCamera == null || player == null) return;

        // Direction depuis la caméra (plus fiable pour gauche/droite/derrière)
        Vector3 camPos = targetCamera.transform.position;
        Vector3 camFwd = targetCamera.transform.forward;
        Vector3 camRight = targetCamera.transform.right;
        Vector3 dir = (attackerWorldPosition - camPos).normalized;

        float forwardDot = Vector3.Dot(camFwd, dir);
        float rightDot = Vector3.Dot(camRight, dir);

        // Alpha basé sur distance + bonus dégâts
        float dist = Vector3.Distance(player.position, attackerWorldPosition);
        float distAlpha = 1f - Mathf.InverseLerp(minDistanceForMaxAlpha, maxDistanceForMinAlpha, dist);
        float pulse = Mathf.Clamp01(distAlpha + damage * damageToAlphaFactor);
        pulse = Mathf.Min(pulse, maxPulseAlpha);

        if (forwardDot < 0f)
        {
            // Derrière
            backAlpha = Mathf.Max(backAlpha, pulse);
        }
        else
        {
            // Devant ou latéral: gauche/droite selon le signe de rightDot
            if (rightDot < 0f)
                leftAlpha = Mathf.Max(leftAlpha, pulse);
            else
                rightAlpha = Mathf.Max(rightAlpha, pulse);
        }

        ApplyAlphas();
    }

    private void ApplyAlphas()
    {
        SetImageAlpha(indicatorLeft, leftAlpha);
        SetImageAlpha(indicatorRight, rightAlpha);
        SetImageAlpha(indicatorBack, backAlpha);
    }

    private static void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = Mathf.Clamp01(a);
        img.color = c;
        img.enabled = c.a > 0.01f;
    }

    // Outils de test dans l’éditeur
    [ContextMenu("Test Hit Gauche")]
    private void TestLeft()
    {
        RegisterHit(targetCamera.transform.position - targetCamera.transform.right * 5f, 10f);
    }
    [ContextMenu("Test Hit Droite")]
    private void TestRight()
    {
        RegisterHit(targetCamera.transform.position + targetCamera.transform.right * 5f, 10f);
    }
    [ContextMenu("Test Hit Derrière")]
    private void TestBack()
    {
        RegisterHit(targetCamera.transform.position - targetCamera.transform.forward * 5f, 10f);
    }
}