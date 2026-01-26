namespace Script
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Représente un choix de dialogue avec son texte affiché et la séquence de dialogues associée.
    /// </summary>
    [System.Serializable]
    public class DialogueChoiceData
    {
        [Tooltip("Texte affiché sur le bouton de choix")]
        public string ChoiceText;

        [Tooltip("Séquence de dialogues à jouer si ce choix est sélectionné")]
        public List<DialogueData> Dialogues = new List<DialogueData>();

        public DialogueChoiceData()
        {
            ChoiceText = "";
            Dialogues = new List<DialogueData>();
        }

        public DialogueChoiceData(string choiceText, List<DialogueData> dialogues)
        {
            ChoiceText = choiceText;
            Dialogues = dialogues ?? new List<DialogueData>();
        }
    }
}

