using FPS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyScreenDetector : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Caméra utilisée pour déterminer le champ de vision (par défaut: Camera.main).")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Transform du joueur (par défaut: ce GameObject).")]
    [SerializeField] private Transform player;

    [Header("Détection")]
    [Tooltip("Rayon de détection des ennemis (m).")]
    [SerializeField] private float detectionRadius = 30f;
    [Tooltip("Distance à partir de laquelle l’icône est au max d’opacité.")]
    [SerializeField] private float minDistanceForMaxAlpha = 3f;
    [Tooltip("Fréquence de scan des ennemis (s).")]
    [SerializeField] private float scanInterval = 0.5f;
    [Tooltip("Calque(s) à considérer pour les raycasts éventuels (facultatif).")]
    [SerializeField] private LayerMask enemyMask = ~0;

    [Header("HUD")]
    [SerializeField] private Image indicatorLeft;
    [SerializeField] private Image indicatorRight;
    [SerializeField] private Image indicatorBack;

    [SerializeField] private float alphaSmoothSpeed = 10f;

    private readonly List<EnemyController> enemies = new List<EnemyController>();
    private float leftAlpha, rightAlpha, backAlpha;
    private float targetLeftAlpha, targetRightAlpha, targetBackAlpha;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (player == null) player = transform;

        SetImageAlpha(indicatorLeft, 0f);
        SetImageAlpha(indicatorRight, 0f);
        SetImageAlpha(indicatorBack, 0f);
    }

    private void OnEnable()
    {
        StartCoroutine(ScanLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator ScanLoop()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.05f, scanInterval));
        while (true)
        {
            RefreshEnemyCache();
            yield return wait;
        }
    }

    private void RefreshEnemyCache()
    {
        enemies.Clear();
        // Récupère tous les EnemyController actifs
        foreach (var e in FindObjectsOfType<EnemyController>())
        {
            if (e != null && e.isActiveAndEnabled)
                enemies.Add(e);
        }
    }

    private void Update()
    {
        // Réinitialiser cibles
        targetLeftAlpha = 0f;
        targetRightAlpha = 0f;
        targetBackAlpha = 0f;

        if (targetCamera == null || player == null)
        {
            ApplyAlphas();
            return;
        }

        var cam = targetCamera;
        Vector3 camPos = cam.transform.position;
        Vector3 camFwd = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            Vector3 toEnemy = enemy.transform.position - player.position;
            float dist = toEnemy.magnitude;
            if (dist > detectionRadius) continue;

            // Visible à l’écran ou non
            Vector3 viewport = cam.WorldToViewportPoint(enemy.transform.position);
            bool inFront = viewport.z > 0f;
            bool onScreen = inFront && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;

            if (onScreen)
                continue;

            // Direction normalisée depuis la caméra (meilleure référence pour gauche/droite/derrière)
            Vector3 dir = (enemy.transform.position - camPos).normalized;

            float forwardDot = Vector3.Dot(camFwd, dir);
            float rightDot = Vector3.Dot(camRight, dir);

            // Calcul alpha cible par proximité (plus proche => plus opaque)
            float alpha = 1f - Mathf.InverseLerp(minDistanceForMaxAlpha, detectionRadius, dist);
            alpha = Mathf.Clamp01(alpha);

            if (forwardDot < 0f)
            {
                // Derrière
                targetBackAlpha = Mathf.Max(targetBackAlpha, alpha);
            }
            else
            {
                if (rightDot < 0f)
                    targetLeftAlpha = Mathf.Max(targetLeftAlpha, alpha);
                else
                    targetRightAlpha = Mathf.Max(targetRightAlpha, alpha);
            }
        }

        ApplyAlphas();
    }

    private void ApplyAlphas()
    {
        // Lissage
        leftAlpha = Mathf.Lerp(leftAlpha, targetLeftAlpha, Time.deltaTime * alphaSmoothSpeed);
        rightAlpha = Mathf.Lerp(rightAlpha, targetRightAlpha, Time.deltaTime * alphaSmoothSpeed);
        backAlpha = Mathf.Lerp(backAlpha, targetBackAlpha, Time.deltaTime * alphaSmoothSpeed);

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
}