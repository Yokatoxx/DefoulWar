using System.Collections.Generic;
using UnityEngine;
using FPS; // Pour EnemyHealth

[RequireComponent(typeof(Collider))]
public class HealZone : MonoBehaviour
{
    [Header("Paramètres de soin")]
    [SerializeField] private float healPerSecond = 50f;
    [SerializeField] private float tickInterval = 0.2f; // intervalle de tick du soin

    [Header("Filtrage (optionnel)")]
    [SerializeField] private LayerMask targetLayers = ~0; // couches autorisées, ~0 = toutes
    [SerializeField] private bool onlyAlive = true;       // soigner uniquement les vivants

    // Cache des cibles dans la zone
    private readonly HashSet<EnemyHealth> targetsInZone = new HashSet<EnemyHealth>();
    private float nextTickTime;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; // s'assurer que le collider est un trigger
        }
        nextTickTime = Time.time + tickInterval;
    }

    private void Update()
    {
        // Tick de soin à intervalle fixe
        if (Time.time >= nextTickTime)
        {
            nextTickTime = Time.time + tickInterval;

            if (targetsInZone.Count > 0)
            {
                float amountPerTick = healPerSecond * tickInterval;

                // Itérer sur une copie pour éviter modification pendant l'énumération
                var snapshot = new List<EnemyHealth>(targetsInZone);
                foreach (var eh in snapshot)
                {
                    if (eh == null)
                    {
                        targetsInZone.Remove(eh);
                        continue;
                    }

                    if (onlyAlive && eh.IsDead)
                        continue;

                    // Appliquer le soin
                    eh.Heal(amountPerTick);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!LayerAllowed(other.gameObject.layer))
            return;

        var eh = other.GetComponentInParent<EnemyHealth>() ?? other.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            targetsInZone.Add(eh);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var eh = other.GetComponentInParent<EnemyHealth>() ?? other.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            targetsInZone.Remove(eh);
        }
    }

    // Nettoyage si un ennemi est détruit pendant qu'il est dans la zone
    private void OnDisable()
    {
        targetsInZone.Clear();
    }

    private bool LayerAllowed(int layer)
    {
        // Si tous les layers sont autorisés
        if (targetLayers == ~0) return true;
        return (targetLayers.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z)));
        }
        else if (col is CapsuleCollider capsule)
        {
            // Approximation visuelle
            Gizmos.DrawWireSphere(transform.position + capsule.center, capsule.radius);
        }
    }
}