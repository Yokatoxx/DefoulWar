namespace Script
{
    using UnityEngine;

    /// <summary>
    /// Qui parle dans ce dialogue
    /// </summary>
    public enum Speaker
    {
        Player,
        NPC,
        Narrator
    }

    /// <summary>
    /// Container simple pour transmettre les données d'un dialogue au DialogueManager.
    /// </summary>
    [System.Serializable]
    public class DialogueData
    {
        [Header("Contenu")]
        public Speaker Speaker = Speaker.NPC;
        [Tooltip("Nom personnalisé du personnage qui parle. Laissez vide pour utiliser le nom par défaut du DialogueManager.")]
        public string SpeakerName;
        [TextArea(2, 5)]
        public string Text;
        public AudioClip Clip;
        public float TypewriterSpeed = 0.02f;

        [Header("Blendshape (optionnel, hérité de l'interactable si vide)")]
        public SkinnedMeshRenderer TargetRenderer;
        public string BlendshapeName;
        public float TargetValue = 100f;
        public float BlendDuration = 0.25f;
        public AnimationCurve BlendCurve;

        // Parameterless ctor needed for Unity serialization
        public DialogueData()
        {
            this.Text = "";
            this.SpeakerName = "";
            this.Clip = null;
            this.BlendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        public DialogueData(string text, AudioClip clip, Speaker speaker = Speaker.NPC, string speakerName = "")
        {
            this.Text = text;
            this.Clip = clip;
            this.Speaker = speaker;
            this.SpeakerName = speakerName;
            this.BlendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }
}
