using System;
using UnityEngine;

public class EffectStats : MonoBehaviour
{
    [SerializeField] private float duration=2f;

    [SerializeField] private bool grow;
    [SerializeField] private float growCoef=1;
    private Vector3 baseScale;
    private float tps;
    
    [SerializeField] private PositionSO positionS0;
    [SerializeField] private bool Offset;
    

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        Destroy(gameObject, duration);
        baseScale = transform.localScale;
        
    }

    private void Update()
    {
        tps+=Time.deltaTime;
        if (grow)
        {
            transform.localScale = baseScale * tps * growCoef;
        }
    }

    public void InstantiateThis()
    {
        GameObject _instance= Instantiate(gameObject);
        

        if (Offset)
        {
            _instance.transform.position=positionS0.positionRef+ positionS0.positionOffset;
        }
        else
        {
            _instance.transform.position = positionS0.positionRef;
        }
            
        
    }

   
}
