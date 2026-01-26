namespace Script
{
    using System.Collections.Generic;
    using UnityEngine;

    [RequireComponent(typeof(Collider))]
    public class InteractableDialogue : MonoBehaviour, IInteractable
    {
        [Header("Dialogues (séquence)")]
        public List<DialogueData> Dialogues = new List<DialogueData>();

        [Header("Blendshape partagé (appliqué à tous les dialogues)")]
        public SkinnedMeshRenderer TargetRenderer;
        public string BlendshapeName;
        public float TargetValue = 100f;
        public float BlendDuration = 0.25f;
        public AnimationCurve BlendCurve;

        [Header("Options")]
        public float TypewriterSpeed = 0.02f;

        public void Interact(GameObject interactor)
        {
            if (Dialogues == null || Dialogues.Count == 0)
            {
                Debug.LogWarning("InteractableDialogue: aucun dialogue configuré.");
                return;
            }

            // Appliquer le blendshape partagé et la vitesse à chaque dialogue
            foreach (var d in Dialogues)
            {
                // Blendshape partagé si non défini dans le dialogue
                if (d.TargetRenderer == null)
                    d.TargetRenderer = TargetRenderer;
                if (string.IsNullOrEmpty(d.BlendshapeName))
                    d.BlendshapeName = BlendshapeName;
                if (d.TargetValue == 0f && TargetValue != 0f)
                    d.TargetValue = TargetValue;
                if (d.BlendDuration == 0f && BlendDuration != 0f)
                    d.BlendDuration = BlendDuration;
                if (d.BlendCurve == null)
                    d.BlendCurve = BlendCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
                // Vitesse
                if (d.TypewriterSpeed <= 0f)
                    d.TypewriterSpeed = TypewriterSpeed;
            }

            if (DialogueManager.Instance != null)
            {
                Debug.Log($"InteractableDialogue: lancement de {Dialogues.Count} dialogue(s)");
                DialogueManager.Instance.ShowSequence(Dialogues);
            }
            else
            {
                Debug.LogWarning("DialogueManager instance not found in scene.");
            }
        }
    }
}
