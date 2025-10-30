using System;
using Proto3GD.FPS;
using UnityEngine;
using UnityEngine.Events;

public class InstantiationEffect : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    


    public UnityEvent<Vector3> OnDeathEvent,  OnHitEvent,OnDashedEvent;


    // private void Awake()
    // {
    //     OnDeathEvent.AddListener(correctPos);
    // }

 
}