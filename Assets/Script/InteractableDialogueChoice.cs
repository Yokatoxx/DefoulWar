namespace Script
{
    using System.Collections.Generic;
    using UnityEngine;

    [RequireComponent(typeof(Collider))]
    public class InteractableDialogueChoice : MonoBehaviour, IInteractable
    {
        [Header("Choix de dialogues")]
        [Tooltip("Liste des choix proposés au joueur")]
        public List<DialogueChoiceData> Choices = new List<DialogueChoiceData>();

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
            if (Choices == null || Choices.Count == 0)
            {
                Debug.LogWarning("InteractableDialogueChoice: aucun choix configuré.");
                return;
            }

            // Appliquer le blendshape partagé et la vitesse à chaque dialogue de chaque choix
            foreach (var choice in Choices)
            {
                if (choice.Dialogues == null) continue;
                
                foreach (var d in choice.Dialogues)
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
            }

            if (DialogueManager.Instance != null)
            {
                Debug.Log($"InteractableDialogueChoice: affichage de {Choices.Count} choix");
                DialogueManager.Instance.ShowChoices(Choices);
            }
            else
            {
                Debug.LogWarning("DialogueManager instance not found in scene.");
            }
        }
    }
}

