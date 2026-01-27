using UnityEngine;

[RequireComponent(typeof(Light))]
public class SmoothFlickeringLight : MonoBehaviour
{
    [Header("Flicker Settings")]
    public float flickerSpeed = 0.1f;
    public float transitionSpeed = 5f;
    public float minIntensity = 0.2f;
    public float maxIntensity = 1.2f;

    [Header("Outage Settings")]
    public float outageChance = 0.01f;           // Chance chaque frame de provoquer une panne
    public float minOutageDuration = 1f;         // Durée minimale de la panne
    public float maxOutageDuration = 4f;         // Durée maximale de la panne

    private Light spotlight;
    private float targetIntensity;
    private float flickerTimer;
    private float outageTimer;
    private bool isOutage;

    void Start()
    {
        spotlight = GetComponent<Light>();
        targetIntensity = spotlight.intensity;
        flickerTimer = flickerSpeed;
        outageTimer = 0f;
        isOutage = false;
    }

    void Update()
    {
        if (isOutage)
        {
            outageTimer -= Time.deltaTime;

            if (outageTimer <= 0f)
            {
                isOutage = false;
                targetIntensity = Random.Range(minIntensity, maxIntensity);
            }

            spotlight.intensity = Mathf.Lerp(spotlight.intensity, 0f, Time.deltaTime * transitionSpeed);
            return;
        }

        // Lancer une coupure aléatoire
        if (Random.value < outageChance * Time.deltaTime)
        {
            isOutage = true;
            outageTimer = Random.Range(minOutageDuration, maxOutageDuration);
            return;
        }

        // Changement de cible d’intensité régulier
        flickerTimer -= Time.deltaTime;

        if (flickerTimer <= 0f)
        {
            targetIntensity = Random.Range(minIntensity, maxIntensity);
            flickerTimer = flickerSpeed;
        }

        // Transition fluide
        spotlight.intensity = Mathf.Lerp(spotlight.intensity, targetIntensity, Time.deltaTime * transitionSpeed);
    }
}
