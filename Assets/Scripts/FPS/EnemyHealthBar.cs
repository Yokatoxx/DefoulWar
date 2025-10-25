// filepath: e:\Documents\Projet Unity\Proto3GD\Assets\Scripts\FPS\EnemyHealthBar.cs
using UnityEngine;
using UnityEngine.UI;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Barre de vie world-space au-dessus d'un ennemi.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 2.2f, 0);
        [SerializeField] private float width = 1.4f;
        [SerializeField] private float height = 0.15f;
        [SerializeField] private bool hideWhenFull = true;
        
        [Header("Colors")]
        [SerializeField] private Color fillColor = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
        
        private EnemyHealth enemyHealth;
        private Camera cam;
        private Canvas canvas;
        private Slider slider;
        private Image fill;
        private RectTransform rect;
        
        private void Awake()
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
            cam = Camera.main;
            
            // Créer l'UI si absente
            CreateUI();
        }
        
        private void LateUpdate()
        {
            if (enemyHealth == null || slider == null) return;
            
            // Mettre à jour la valeur
            float pct = Mathf.Clamp01(enemyHealth.CurrentHealth / Mathf.Max(1f, enemyHealth.MaxHealth));
            slider.value = pct;
            
            // Position & orientation face caméra
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                transform.position = (enemyHealth != null ? enemyHealth.transform.position : transform.position) + worldOffset;
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            }
            
            // Masquer si plein
            if (hideWhenFull)
            {
                canvas.enabled = pct < 1f && !enemyHealth.IsDead;
            }
        }
        
        private void CreateUI()
        {
            // Root: cet objet porte le Canvas
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            
            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            
            GraphicRaycaster raycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();
            
            // Container (BG centré)
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(transform, false);
            Image bg = bgObj.AddComponent<Image>();
            bg.color = backgroundColor;
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(width, height);
            bgRt.anchoredPosition = Vector2.zero;
            
            // Slider occupant le BG avec marges
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(bgObj.transform, false);
            slider = sliderObj.AddComponent<Slider>();
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            RectTransform srt = slider.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            // padding ~5% horizontal, 30% vertical
            float padX = width * 0.05f;
            float padY = height * 0.3f;
            srt.offsetMin = new Vector2(padX, padY);
            srt.offsetMax = new Vector2(-padX, -padY);
            
            // Zone de remplissage à l'intérieur du Slider
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fart = fillArea.AddComponent<RectTransform>();
            fart.anchorMin = new Vector2(0, 0);
            fart.anchorMax = new Vector2(1, 1);
            fart.offsetMin = Vector2.zero;
            fart.offsetMax = Vector2.zero;
            
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            fill = fillObj.AddComponent<Image>();
            fill.color = fillColor;
            RectTransform frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0);
            frt.anchorMax = new Vector2(1, 1);
            frt.pivot = new Vector2(0f, 0.5f); // pivot à gauche pour remplir vers la droite
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            
            slider.targetGraphic = fill;
            slider.fillRect = frt;
            
            rect = GetComponent<RectTransform>();
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
