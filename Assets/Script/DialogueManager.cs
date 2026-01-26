namespace Script
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    /// <summary>
    /// DialogueManager: affiche un dialogue en typewriter, joue un clip audio,
    /// anime un blendshape sur un SkinnedMeshRenderer et bloque les contrôles du joueur
    /// sauf le clic gauche (OnAdvance) qui fait avancer/skip/fermer.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI refs (optionnel)")]
        public GameObject DialogueUI;
        public TextMeshProUGUI DialogueTextUI;
        public TextMeshProUGUI SpeakerNameUI;

        [Header("Audio")]
        public AudioSource AudioSource;

        [Header("Runtime options")]
        [SerializeField]
        private float defaultTypewriterSpeed = 0.02f;
        public float DefaultTypewriterSpeed => defaultTypewriterSpeed;

        // internals
        private DialogueData _currentData;
        private Coroutine _typewriterCoroutine;
        private Coroutine _blendCoroutine;
        private bool _isShowing;
        private bool _typewriterCompleted;

        // sequence support
        private List<DialogueData> _sequence;
        private int _sequenceIndex;

        private PlayerController _playerController;
        private PlayerInputHandler _inputHandler;

        // choice system
        private GameObject _choicesPanel;
        private List<GameObject> _choiceButtons = new List<GameObject>();
        private bool _isShowingChoices;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;

            if (AudioSource == null)
                AudioSource = GetComponent<AudioSource>();

            _playerController = FindFirstObjectByType<PlayerController>();
            _inputHandler = FindFirstObjectByType<PlayerInputHandler>();

            // Pré-créer le Canvas/TextMeshPro minimal dès le démarrage
            EnsureUIExists();
            if (DialogueUI != null)
                DialogueUI.SetActive(false);
        }

        /// <summary>
        /// Affiche un seul dialogue (pas de séquence)
        /// </summary>
        public void Show(DialogueData data)
        {
            if (data == null) return;
            // reset sequence
            _sequence = null;
            _sequenceIndex = 0;
            ShowInternal(data);
        }

        /// <summary>
        /// Démarre l'affichage d'une séquence de dialogues. Avance via clic gauche.
        /// </summary>
        public void ShowSequence(List<DialogueData> dialogues)
        {
            if (dialogues == null || dialogues.Count == 0) return;
            Debug.Log($"DialogueManager: ShowSequence avec {dialogues.Count} dialogue(s)");
            _sequence = dialogues;
            _sequenceIndex = 0;
            ShowInternal(_sequence[_sequenceIndex]);
        }

        /// <summary>
        /// Méthode interne pour afficher un dialogue sans toucher à _sequence
        /// </summary>
        void ShowInternal(DialogueData data)
        {
            if (data == null) return;

            // If already showing something, stop current display
            if (_isShowing)
            {
                StopCurrentDisplay();
            }

            _currentData = data;
            _isShowing = true;
            _typewriterCompleted = false;

            // Ensure UI exists
            EnsureUIExists();

            // Update speaker name - use custom name if provided, else use default
            if (SpeakerNameUI != null)
            {
                string displayName = GetSpeakerName(_currentData);
                SpeakerNameUI.text = displayName;
            }

            // Disable player controls (only on first dialogue of sequence)
            if (_sequenceIndex == 0 || _sequence == null)
            {
                if (_playerController == null)
                    _playerController = FindFirstObjectByType<PlayerController>();
                _playerController?.DisableControls();

                // Subscribe to advance input
                if (_inputHandler == null)
                    _inputHandler = FindFirstObjectByType<PlayerInputHandler>();
                if (_inputHandler != null)
                    _inputHandler.OnAdvance.AddListener(OnAdvancePressed);
            }

            // Play audio
            if (_currentData.Clip != null)
            {
                if (AudioSource == null)
                    AudioSource = gameObject.AddComponent<AudioSource>();
                AudioSource.clip = _currentData.Clip;
                AudioSource.Play();
            }

            // Start blendshape animation if requested
            if (_currentData.TargetRenderer != null && !string.IsNullOrEmpty(_currentData.BlendshapeName))
            {
                int idx = GetBlendshapeIndex(_currentData.TargetRenderer, _currentData.BlendshapeName);
                if (idx >= 0)
                {
                    _blendCoroutine = StartCoroutine(AnimateBlendshape(_currentData.TargetRenderer, idx, _currentData.TargetValue, _currentData.BlendDuration, _currentData.BlendCurve));
                }
                else
                {
                    Debug.LogWarning($"Blendshape '{_currentData.BlendshapeName}' not found on renderer {_currentData.TargetRenderer.name}");
                }
            }

            // Start typewriter
            float speed = _currentData.TypewriterSpeed > 0f ? _currentData.TypewriterSpeed : defaultTypewriterSpeed;
            string speakerName = GetSpeakerName(_currentData);
            string speakerPrefix = !string.IsNullOrEmpty(speakerName) ? $"<b>{speakerName}:</b> " : "";
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(speakerPrefix + _currentData.Text, speed));
        }

        /// <summary>
        /// Retourne le nom du speaker à afficher depuis SpeakerName.
        /// </summary>
        string GetSpeakerName(DialogueData data)
        {
            return data.SpeakerName ?? "";
        }

        void StopCurrentDisplay()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            if (_blendCoroutine != null)
            {
                StopCoroutine(_blendCoroutine);
                _blendCoroutine = null;
            }
            if (AudioSource != null && AudioSource.isPlaying)
                AudioSource.Stop();
        }

        void EnsureUIExists()
        {
            if (DialogueUI != null && DialogueTextUI != null)
            {
                DialogueUI.SetActive(true);
                DialogueTextUI.text = "";
                return;
            }

            // Create a minimal Canvas with TextMeshProUGUI
            if (DialogueUI == null)
            {
                var canvasGo = new GameObject("DialogueCanvas");
                canvasGo.layer = 5;
                var canvasComp = canvasGo.AddComponent<Canvas>();
                canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasComp.sortingOrder = 1000;
                canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                // Create a background panel
                var panelGo = new GameObject("DialoguePanel");
                panelGo.transform.SetParent(canvasGo.transform, false);
                var panelRect = panelGo.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.05f, 0.02f);
                panelRect.anchorMax = new Vector2(0.95f, 0.22f);
                panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

                var image = panelGo.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color(0f, 0f, 0f, 0.75f);

                DialogueUI = canvasGo;
            }

            if (DialogueTextUI == null)
            {
                Transform parent = DialogueUI.transform.Find("DialoguePanel") ?? DialogueUI.transform;
                var textGo = new GameObject("DialogueText");
                textGo.transform.SetParent(parent, false);
                var rect = textGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.02f, 0.1f);
                rect.anchorMax = new Vector2(0.98f, 0.9f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;

                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 22;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.text = "";
                tmp.color = Color.white;
                tmp.richText = true;

                DialogueTextUI = tmp;
            }

            // Ensure an EventSystem exists
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            DialogueUI.SetActive(true);
        }

        IEnumerator TypewriterCoroutine(string fullText, float speed)
        {
            DialogueTextUI.text = "";
            int len = fullText.Length;
            int i = 0;
            bool inTag = false;

            while (i < len)
            {
                char c = fullText[i];
                // Handle rich text tags
                if (c == '<') inTag = true;
                if (inTag)
                {
                    DialogueTextUI.text += c;
                    if (c == '>') inTag = false;
                    i++;
                    continue;
                }

                DialogueTextUI.text += c;
                i++;

                float waited = 0f;
                while (waited < speed)
                {
                    if (!_isShowing) yield break;
                    yield return null;
                    waited += Time.deltaTime;
                }
            }

            _typewriterCompleted = true;
        }

        void OnAdvancePressed()
        {
            if (!_isShowing) return;

            if (!_typewriterCompleted)
            {
                // Premier clic : terminer le texte immédiatement
                StopCurrentDisplay();
                string speakerName = GetSpeakerName(_currentData);
                string speakerPrefix = !string.IsNullOrEmpty(speakerName) ? $"<b>{speakerName}:</b> " : "";
                DialogueTextUI.text = speakerPrefix + _currentData.Text;
                _typewriterCompleted = true;
                // On s'arrête ici, il faudra un autre clic pour passer au suivant
                return;
            }

            // Deuxième clic (texte déjà complet) : passer au dialogue suivant ou fermer
            if (_sequence != null && _sequenceIndex < _sequence.Count - 1)
            {
                _sequenceIndex++;
                ShowInternal(_sequence[_sequenceIndex]);
            }
            else
            {
                CloseDialogue();
            }
        }

        /// <summary>
        /// Affiche une liste de choix au joueur. Quand un choix est sélectionné,
        /// la séquence de dialogues associée est jouée.
        /// </summary>
        public void ShowChoices(List<DialogueChoiceData> choices)
        {
            if (choices == null || choices.Count == 0) return;
            
            Debug.Log($"DialogueManager: ShowChoices avec {choices.Count} choix");
            
            _isShowingChoices = true;
            _isShowing = true;

            // Disable player controls
            if (_playerController == null)
                _playerController = FindFirstObjectByType<PlayerController>();
            _playerController?.DisableControls();

            // Ensure UI exists and show choices panel
            EnsureUIExists();
            EnsureChoicesUIExists();

            // Hide dialogue text, show choices
            if (DialogueTextUI != null)
                DialogueTextUI.gameObject.SetActive(false);
            if (SpeakerNameUI != null)
                SpeakerNameUI.gameObject.SetActive(false);

            // Clear existing choice buttons
            ClearChoiceButtons();

            // Create buttons for each choice
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                CreateChoiceButton(choice, i);
            }

            _choicesPanel.SetActive(true);

            // Enable cursor for clicking
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void EnsureChoicesUIExists()
        {
            if (_choicesPanel != null) return;

            Transform parent = DialogueUI.transform.Find("DialoguePanel") ?? DialogueUI.transform;

            // Create choices panel
            var panelGo = new GameObject("ChoicesPanel");
            panelGo.transform.SetParent(parent, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

            // Add vertical layout group
            var layoutGroup = panelGo.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new RectOffset(20, 20, 10, 10);

            _choicesPanel = panelGo;
            _choicesPanel.SetActive(false);
        }

        void CreateChoiceButton(DialogueChoiceData choice, int index)
        {
            var buttonGo = new GameObject($"ChoiceButton_{index}");
            buttonGo.transform.SetParent(_choicesPanel.transform, false);

            var rect = buttonGo.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);

            // Button background
            var image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            // Button component
            var button = buttonGo.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
            colors.pressedColor = new Color(0.4f, 0.4f, 0.6f, 1f);
            button.colors = colors;

            // Capture choice for lambda
            var capturedChoice = choice;
            button.onClick.AddListener(() => OnChoiceSelected(capturedChoice));

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(buttonGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = choice.ChoiceText;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            _choiceButtons.Add(buttonGo);
        }

        void ClearChoiceButtons()
        {
            foreach (var btn in _choiceButtons)
            {
                if (btn != null)
                    Destroy(btn);
            }
            _choiceButtons.Clear();
        }

        void OnChoiceSelected(DialogueChoiceData choice)
        {
            Debug.Log($"DialogueManager: Choix sélectionné - {choice.ChoiceText}");

            // Hide choices panel
            _isShowingChoices = false;
            ClearChoiceButtons();
            if (_choicesPanel != null)
                _choicesPanel.SetActive(false);

            // Restore dialogue text visibility
            if (DialogueTextUI != null)
                DialogueTextUI.gameObject.SetActive(true);
            if (SpeakerNameUI != null)
                SpeakerNameUI.gameObject.SetActive(true);

            // Re-lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Play the dialogue sequence for this choice
            if (choice.Dialogues != null && choice.Dialogues.Count > 0)
            {
                ShowSequence(choice.Dialogues);
            }
            else
            {
                CloseDialogue();
            }
        }

        void CloseDialogue()
        {
            StopCurrentDisplay();

            if (DialogueUI != null)
                DialogueUI.SetActive(false);

            if (_inputHandler != null)
                _inputHandler.OnAdvance.RemoveListener(OnAdvancePressed);

            if (_playerController == null)
                _playerController = FindFirstObjectByType<PlayerController>();
            _playerController?.EnableControls();

            _isShowing = false;
            _currentData = null;
            _typewriterCompleted = false;
            _sequence = null;
            _sequenceIndex = 0;
        }

        int GetBlendshapeIndex(SkinnedMeshRenderer smr, string blendshapeName)
        {
            if (smr == null || smr.sharedMesh == null) return -1;
            return smr.sharedMesh.GetBlendShapeIndex(blendshapeName);
        }

        IEnumerator AnimateBlendshape(SkinnedMeshRenderer smr, int index, float targetValue, float duration, AnimationCurve curve)
        {
            if (smr == null || index < 0)
                yield break;

            if (curve == null)
                curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            float start = smr.GetBlendShapeWeight(index);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!_isShowing) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float v = Mathf.Lerp(start, targetValue, curve.Evaluate(t));
                smr.SetBlendShapeWeight(index, v);
                yield return null;
            }
            smr.SetBlendShapeWeight(index, targetValue);
        }
    }
}
