using UnityEngine;

public class PooledObject : MonoBehaviour
{
    private SimplePool _owner;

    public void SetOwner(SimplePool owner)
    {
        _owner = owner;
    }

    // À appeler depuis l'ennemi (mort, sortie de zone, etc.)
    public void Despawn()
    {
        if (_owner != null)
        {
            _owner.Despawn(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}