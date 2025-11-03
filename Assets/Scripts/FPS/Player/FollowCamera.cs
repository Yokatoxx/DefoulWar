using FPS;
using UnityEngine;


public class FollowCamera : MonoBehaviour
{
    public Transform target;

    public DashSystem dashPlayer;

    public bool followPosition = true;
    public bool followRotation = true;

    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffsetEuler = Vector3.zero;

    public bool smoothPosition = false;
    public float positionSmoothSpeed = 10f;

    public bool smoothRotation = false;
    public float rotationSmoothSpeed = 10f;

    void LateUpdate()
    {
        if (target == null) return;

        if(dashPlayer != null)
        {
            if(dashPlayer.isDashing)
            {
                smoothPosition = false;
            }
            else
            {
                smoothPosition = true;
            }
        }

        // Position
        if (followPosition)
        {
            Vector3 desiredPos = target.position + positionOffset;
            if (smoothPosition)
            {
                transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * positionSmoothSpeed);
            }
            else
            {
                transform.position = desiredPos;
            }
        }

        // Rotation
        if (followRotation)
        {
            Quaternion targetRot = target.rotation * Quaternion.Euler(rotationOffsetEuler);

            if (smoothRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothSpeed);
            }
            else
            {
                transform.rotation = targetRot;
            }
        }
    }

    public void SnapToTarget()
    {
        if (target == null) return;

        if (followPosition)
        {
            transform.position =  target.position + positionOffset;
        }
        if (followRotation)
        {
            transform.rotation = target.rotation * Quaternion.Euler(rotationOffsetEuler);
        }
    }
}