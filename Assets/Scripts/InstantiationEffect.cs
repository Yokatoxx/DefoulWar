using System;
using Proto3GD.FPS;
using UnityEngine;
using UnityEngine.Events;

public class InstantiationEffect : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    

    [SerializeField] private GameObject effectPrefab;
    public UnityEvent InstantiateEffectEvent;
    public float effectDuration;


    private void Awake()
    {
        InstantiateEffectEvent.AddListener(InstanceEffectBase);
    }

    public void InstanceEffectPrefab(GameObject effect, float duration)
    {
        GameObject effectInstance = Instantiate(effect);
        Destroy(effectInstance, duration);
    }

    public void InstanceEffectBase()
    {
    
        GameObject effectInstance = Instantiate(effectPrefab);
        effectInstance.transform.position = transform.position+Vector3.up;
        Destroy(effectInstance, effectDuration);
    }

   
}