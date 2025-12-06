using UnityEngine;

public class DoorArena : MonoBehaviour
{
    public GameObject doorObject;
    public float doorSpeed = 2f;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 currentPosition; 
    public bool isOpen = false;

    [SerializeField] Vector3 openOffset = new Vector3(0, 5, 0);

    private void Start()
    {
        closedPosition = doorObject.transform.position;
        openPosition = closedPosition + openOffset;
    }

    private void Update()
    {
        if (isOpen)
        {
            currentPosition = Vector3.MoveTowards(doorObject.transform.position, openPosition, doorSpeed * Time.deltaTime);
        }
        else
        {
            currentPosition = Vector3.MoveTowards(doorObject.transform.position, closedPosition, doorSpeed * Time.deltaTime);
        }
        doorObject.transform.position = currentPosition;
    }

}
