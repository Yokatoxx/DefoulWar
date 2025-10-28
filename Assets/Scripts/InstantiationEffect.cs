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
    public UnityEvent<GameObject> OnDeathEvent,  OnHitEvent,OnDashedEvent;
    public GameObject onDeathGO, onHitGO, onDashGO;

    private void Awake()
    {
        InstantiateEffectEvent.AddListener(InstanceEffectBase);
        
        OnDeathEvent.AddListener(InstanceEffectPrefab);
        OnHitEvent.AddListener(InstanceEffectPrefab);
        OnDashedEvent.AddListener(InstanceEffectPrefab);

    }

    public void InstanceEffectPrefab(GameObject effect)
    {
        GameObject effectInstance = Instantiate(effect);
        effectInstance.transform.position = transform.position+Vector3.up;
    }

    public void InstanceEffectBase()
    {
    
        GameObject effectInstance = Instantiate(effectPrefab);
        effectInstance.transform.position = transform.position+Vector3.up;
       
    }

   
}