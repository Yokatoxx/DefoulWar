namespace Script
{
    using UnityEngine;

    /// <summary>
    /// Interface simple pour objets pouvant être "interactés" par le joueur.
    /// Le composant implémentant cette interface doit définir le comportement dans Interact.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Appelé lorsque le joueur interacte avec l'objet (par ex. appuie sur E).
        /// </summary>
        /// <param name="interactor">Le GameObject qui a initié l'interaction (généralement le joueur).</param>
        void Interact(GameObject interactor);
    }
}
