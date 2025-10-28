using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSettings", menuName = "FPS/Weapon/WeaponSettings", order = 1)]
public class WeaponSettings : ScriptableObject
{
    public float shotDelay = 0.2f;
    public float magazineSize = 30;
    public float reloadTime = 1.5f;
    public float shootingDistance = 100f;
    public float shootTrailWidth = 0.2f;
    public int bulletsPerShot = 1;
    public float bulletDammage = 10f;

    public bool addBulletSpread = true;
    public bool isAutomatic = true;

    public Vector3 bulletSpreadVaraiance = new Vector3(0.1f, 0.1f, 0.1f);

    public GameObject weaponModel;
    public ParticleSystem muzzleFlash;
    public ParticleSystem ImpactParticleSystem;
    public TrailRenderer bulletTrail;
}
