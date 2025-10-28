using System;
using UnityEngine;

public class EffectStats : MonoBehaviour
{
    [SerializeField] private float duration=2f;

    [SerializeField] private bool grow;
    [SerializeField] private float growCoef=1;
    private Vector3 baseScale;
    private float tps;

    
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
}
