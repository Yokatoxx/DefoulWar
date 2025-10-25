using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Proto3GD.FPS.Editor
{
    /// <summary>
    /// Outils d'éditeur pour créer rapidement l'UI du cooldown de dash
    /// </summary>
    public static class DashUICreator
    {
        [MenuItem("GameObject/UI/Dash Cooldown Gauge", false, 10)]
        public static void CreateDashCooldownUI(MenuCommand menuCommand)
        {
            // Trouver ou créer le Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            // Créer le GameObject principal
            GameObject dashUIObj = new GameObject("DashCooldownGauge");
            Undo.RegisterCreatedObjectUndo(dashUIObj, "Create Dash Cooldown Gauge");
            
            RectTransform rectTransform = dashUIObj.AddComponent<RectTransform>();
            GameObjectUtility.SetParentAndAlign(dashUIObj, canvas.gameObject);
            
            // Positionner en bas au centre de l'écran
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 80f);
            rectTransform.sizeDelta = new Vector2(200f, 30f);

            // Ajouter un CanvasGroup
            dashUIObj.AddComponent<CanvasGroup>();

            // Créer le fond
            GameObject backgroundObj = new GameObject("Background");
            GameObjectUtility.SetParentAndAlign(backgroundObj, dashUIObj);
            RectTransform bgRect = backgroundObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            Image bgImage = backgroundObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Créer la barre de remplissage
            GameObject fillObj = new GameObject("Fill");
            GameObjectUtility.SetParentAndAlign(fillObj, dashUIObj);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = new Vector2(-4f, -4f);
            fillRect.anchoredPosition = Vector2.zero;
            
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 1f, 0.3f, 0.8f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;

            // Créer le texte
            GameObject textObj = new GameObject("Text");
            GameObjectUtility.SetParentAndAlign(textObj, dashUIObj);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            TMPro.TextMeshProUGUI tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmpText.text = "100%";
            tmpText.alignment = TMPro.TextAlignmentOptions.Center;
            tmpText.fontSize = 18;
            tmpText.color = Color.white;
            tmpText.fontStyle = TMPro.FontStyles.Bold;

            // Créer un label au-dessus
            GameObject labelObj = new GameObject("Label");
            GameObjectUtility.SetParentAndAlign(labelObj, dashUIObj);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 20f);
            labelRect.anchoredPosition = new Vector2(0f, 5f);
            
            TMPro.TextMeshProUGUI labelText = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
            labelText.text = "DASH";
            labelText.alignment = TMPro.TextAlignmentOptions.Center;
            labelText.fontSize = 14;
            labelText.color = new Color(1f, 1f, 1f, 0.7f);
            labelText.fontStyle = TMPro.FontStyles.Bold;

            // Ajouter le composant DashCooldownUI
            DashCooldownUI dashUI = dashUIObj.AddComponent<DashCooldownUI>();
            
            // Assigner les références via réflexion
            SerializedObject serializedObject = new SerializedObject(dashUI);
            serializedObject.FindProperty("fillImage").objectReferenceValue = fillImage;
            serializedObject.FindProperty("cooldownText").objectReferenceValue = tmpText;
            serializedObject.ApplyModifiedProperties();

            // Trouver le système de dash automatiquement
            PillarDashSystem dashSystem = Object.FindFirstObjectByType<PillarDashSystem>();
            if (dashSystem != null)
            {
                serializedObject.FindProperty("dashSystem").objectReferenceValue = dashSystem;
                serializedObject.ApplyModifiedProperties();
                Debug.Log("DashCooldownUI créé et lié automatiquement au PillarDashSystem!");
            }
            else
            {
                Debug.LogWarning("DashCooldownUI créé mais aucun PillarDashSystem trouvé. Assignez-le manuellement dans l'Inspector.");
            }

            Selection.activeGameObject = dashUIObj;
        }
    }
}
