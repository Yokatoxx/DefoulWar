using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Paramètres")]
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private Image fadeImage;
    private Canvas fadeCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Crée automatiquement le Canvas et l'Image de fade
    /// </summary>
    private void CreateFadeCanvas()
    {
        // Créer le Canvas
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);
        
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // Toujours au-dessus

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Créer l'Image noire
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f); // Noir transparent au départ
        fadeImage.raycastTarget = false; // Ne bloque pas les clics

        // Stretch pour remplir tout l'écran
        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Fade vers le noir
    /// </summary>
    public Coroutine FadeToBlack(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        return StartCoroutine(FadeCoroutine(0f, 1f, duration));
    }

    /// <summary>
    /// Fade depuis le noir vers transparent
    /// </summary>
    public Coroutine FadeFromBlack(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        return StartCoroutine(FadeCoroutine(1f, 0f, duration));
    }

    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = endAlpha;
        fadeImage.color = c;
    }

    /// <summary>
    /// Fade complet : noir -> action -> retour
    /// </summary>
    public Coroutine DoFadeSequence(System.Action onBlackScreen, float fadeDuration = -1f)
    {
        return StartCoroutine(FadeSequenceCoroutine(onBlackScreen, fadeDuration));
    }

    private IEnumerator FadeSequenceCoroutine(System.Action onBlackScreen, float fadeDuration)
    {
        if (fadeDuration < 0f) fadeDuration = defaultFadeDuration;

        yield return FadeCoroutine(0f, 1f, fadeDuration);

        onBlackScreen?.Invoke();

        yield return new WaitForSeconds(0.1f);

        yield return FadeCoroutine(1f, 0f, fadeDuration);
    }
}

