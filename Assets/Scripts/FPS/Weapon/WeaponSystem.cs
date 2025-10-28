using System.Collections;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    [SerializeField] private float shotDelay = 0.2f;
    private float lastShootTime;
    [SerializeField] private float magazineSize = 30;
    [SerializeField] private float reloadTime = 1.5f;
    [SerializeField] private float shootingDistance = 100f;
    [SerializeField] private float projectileRandomness = 0.1f;
    [SerializeField] private int bulletsPerShot = 1;

    private float currentAmmo;
    [SerializeField] private float bulletDammage = 10f;

    [SerializeField] private bool addBulletSpread = true;
    [SerializeField] private bool isAutomatic = true;

    [SerializeField] private Vector3 bulletSpreadVaraiance = new Vector3(0.1f, 0.1f, 0.1f);

    [SerializeField] private GameObject weaponModel;
    [SerializeField] private ParticleSystem shootingSystem;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private ParticleSystem ImpactParticleSystem;
    [SerializeField] private TrailRenderer bulletTrail;

    private Animator animator;

    private void Awake()
    {
        animator = weaponModel.GetComponent<Animator>();
    }

    private void Update()
    {
        if (isAutomatic)
        {
            if (Input.GetMouseButton(0))
            {
                Shoot();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
        }
    }

    public void Shoot()
    {
        if(lastShootTime + shotDelay < Time.time)
        {
            animator.SetBool("isShooting", true);
            for (int i = 0; i < bulletsPerShot; i++)
            {
                shootingSystem.Play();
                Vector3 direction = GetDirection();

                if (Physics.Raycast(bulletSpawnPoint.position, direction, out RaycastHit hit, float.MaxValue))
                {
                    TrailRenderer trail = Instantiate(bulletTrail, bulletSpawnPoint.position, Quaternion.identity);

                    StartCoroutine(SpawnTrail(trail, hit));

                    lastShootTime = Time.time;
                }
            }
        }
    }

    private Vector3 GetDirection()
    {
        Vector3 direction = bulletSpawnPoint.forward;
        if (addBulletSpread)
        {
            direction += new Vector3(
                Random.Range(-bulletSpreadVaraiance.x, bulletSpreadVaraiance.x),
                Random.Range(-bulletSpreadVaraiance.y, bulletSpreadVaraiance.y),
                Random.Range(-bulletSpreadVaraiance.z, bulletSpreadVaraiance.z)
            );

            direction.Normalize();
        }
        return direction;
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hit.point, time);
            time += Time.deltaTime / trail.time;

            yield return null;
        }
        animator.SetBool("IsShooting", false);
        trail.transform.position = hit.point;
        Instantiate(ImpactParticleSystem, hit.point, Quaternion.LookRotation(hit.normal));

        Destroy(trail.gameObject, trail.time);
    }

}
