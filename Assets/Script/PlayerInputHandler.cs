using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gère les inputs du joueur et émet des événements pour l'interaction (E) et l'avancement du dialogue (clic gauche).
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnInteract = new UnityEvent();    // Touche E
    public UnityEvent OnAdvance = new UnityEvent();     // Clic gauche

    void Update()
    {
        // Touche E pour interagir
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("PlayerInputHandler: E pressé -> OnInteract");
            OnInteract.Invoke();
        }
        
        // Clic gauche pour avancer le dialogue
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("PlayerInputHandler: Clic gauche -> OnAdvance");
            OnAdvance.Invoke();
        }
    }
}
