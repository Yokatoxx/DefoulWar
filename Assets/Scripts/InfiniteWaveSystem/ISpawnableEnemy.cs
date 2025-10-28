using UnityEngine;

public struct SpawnContext
{
    public float Difficulty;
    public float ElapsedSeconds;
}

public interface ISpawnableEnemy
{
    // Appelé à chaque spawn: appliquez ici la montée en difficulté (HP, vitesse, dégâts…)
    void OnSpawn(SpawnContext context);

    // Appelé au retour dans le pool (nettoyage d'état)
    void OnDespawn();
}