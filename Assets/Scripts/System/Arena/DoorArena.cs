using UnityEngine;

public class DoorArena : MonoBehaviour
{
    public GameObject doorObject;
    public float doorSpeed = 2f;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 currentPosition; 
    public bool isClosed = false;

    [SerializeField] Vector3 closeOffset = new Vector3(0, -5, 0);

    private void Start()
    {
        // La position dans la scene = position ouverte
        openPosition = doorObject.transform.position;
        closedPosition = openPosition + closeOffset;
    }

    private void Update()
    {
        if (isClosed)
        {
            currentPosition = Vector3.MoveTowards(doorObject.transform.position, closedPosition, doorSpeed * Time.deltaTime);
        }
        else
        {
            currentPosition = Vector3.MoveTowards(doorObject.transform.position, openPosition, doorSpeed * Time.deltaTime);
        }
        doorObject.transform.position = currentPosition;
    }

}
