using UnityEngine;
using System.Collections.Generic;

public class HelicopterPathMover : MonoBehaviour
{
    [Header("Path")]
    public List<Transform> waypoints;

    [Header("Movement")]
    public float speed = 5f;
    public float rotationSpeed = 2f;
    public float waypointThreshold = 0.5f;

    [Header("Rotation Offset")]
    [Tooltip("Offset de rotation pour corriger l'orientation du modèle")]
    public Vector3 rotationOffset = new Vector3(-90f, 0f, 0f);

    [Header("State")]
    public bool playOnStart = false;

    private int currentWaypointIndex = 0;
    private bool isMoving = false;

    void Start()
    {
        if (playOnStart)
            StartMovement();
    }

    void Update()
    {
        if (!isMoving || waypoints.Count == 0)
            return;

        MoveAlongPath();
    }

    void MoveAlongPath()
    {
        Transform target = waypoints[currentWaypointIndex];

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Quaternion offsetRotation = Quaternion.Euler(rotationOffset);

            Quaternion finalRotation = lookRotation * offsetRotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                finalRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance < waypointThreshold)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Count)
            {
                isMoving = false;
            }
        }
    }

    public void StartMovement()
    {
        if (waypoints.Count == 0)
            return;

        currentWaypointIndex = 0;
        isMoving = true;
    }
}
