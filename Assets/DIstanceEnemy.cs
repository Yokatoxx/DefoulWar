using System;
using FPS;
using UnityEngine;

public class DistanceEnemy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   
    
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPosition;

    [SerializeField] private float fireRate;
    [SerializeField] private float speed;
    [SerializeField] private float bulletDuration;
    

    private float timer;

    private void Awake()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= fireRate)
        {
            timer -= fireRate;
            ShootToPlayer();
        }
    }

    private void ShootToPlayer()
    {
        Transform _playerTransform = FindFirstObjectByType<PlayerHealth>().gameObject.transform;
        GameObject _bullet = Instantiate(projectilePrefab);
        _bullet.transform.position = shootPosition.position;
        Vector3 dir= _playerTransform.position - transform.position;
        Rigidbody _rb = _bullet.GetComponent<Rigidbody>();
        _rb.AddForce(dir.normalized * speed, ForceMode.Impulse);
        Destroy(_bullet,bulletDuration);
    }
}