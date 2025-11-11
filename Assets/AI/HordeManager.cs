using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Version debug/verbose du HordeManager.
/// - Logge les appels critiques pour diagnostiquer pourquoi aucune horde n'est créée / trouvée.
/// - Expose des propriétés publiques pour lire activeHordes / aloneEnemies depuis l'inspector ou d'autres scripts.
/// Utilisez temporairement en remplacement de votre HordeManager, puis remettez la version de production ensuite.
/// </summary>
public class HordeManager : MonoBehaviour
{
    public static HordeManager instance;

    [Header("Règles des Hordes (debug)")]
    public int maxHordes = 2;
    public int minHordeSize = 4;
    public float checkAloneInterval = 5.0f;

    // Rendre publiques pour debug
    [HideInInspector] public List<Horde> activeHordes = new List<Horde>();
    [HideInInspector] public List<EnemyAI> aloneEnemies = new List<EnemyAI>();

    [Header("Debug options")]
    public bool logCalls = true;
    public bool autoStartCheckCoroutine = true;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (logCalls) Debug.Log("[HordeManagerDebug] Instance assigned in Awake.");
        }
        else
        {
            Debug.LogWarning("[HordeManagerDebug] Duplicate instance detected, destroying this.");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (autoStartCheckCoroutine)
            StartCoroutine(CheckAloneEnemiesLogic());

        if (logCalls) Debug.Log($"[HordeManagerDebug] Start: maxHordes={maxHordes} minHordeSize={minHordeSize}");
    }

    // --- API public pour le BT / scripts (mêmes signatures que votre implémentation) ---

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (enemy == null) return;
        if (!aloneEnemies.Contains(enemy) && enemy.currentHorde == null)
        {
            aloneEnemies.Add(enemy);
            if (logCalls) Debug.Log($"[HordeManagerDebug] RegisterEnemy: {enemy.name} (aloneEnemies={aloneEnemies.Count})");
        }
    }

    public void UnregisterEnemy(EnemyAI enemy)
    {
        if (enemy == null) return;
        if (aloneEnemies.Contains(enemy))
        {
            aloneEnemies.Remove(enemy);
            if (logCalls) Debug.Log($"[HordeManagerDebug] UnregisterEnemy: {enemy.name} (aloneEnemies={aloneEnemies.Count})");
        }
    }

    public Horde FindNearestHorde(Vector3 position)
    {
        if (activeHordes == null || activeHordes.Count == 0)
        {
            if (logCalls) Debug.Log("[HordeManagerDebug] FindNearestHorde: no active hordes.");
            return null;
        }

        Horde nearest = null;
        float minDist = float.MaxValue;
        foreach (var h in activeHordes)
        {
            if (h == null) continue;
            h.UpdateRallyPoint();
            float d = Vector3.Distance(position, h.rallyPoint);
            if (d < minDist)
            {
                minDist = d;
                nearest = h;
            }
        }

        if (logCalls) Debug.Log($"[HordeManagerDebug] FindNearestHorde: nearest={(nearest != null ? "found" : "null")} dist={minDist}");
        return nearest;
    }

    public bool CanFormNewHorde()
    {
        bool can = activeHordes.Count < maxHordes;
        if (logCalls) Debug.Log($"[HordeManagerDebug] CanFormNewHorde => {can} (activeHordes={activeHordes.Count} / max={maxHordes})");
        return can;
    }

    public void CreateHorde(EnemyAI founder)
    {
        if (founder == null)
        {
            Debug.LogWarning("[HordeManagerDebug] CreateHorde called with null founder.");
            return;
        }

        if (!CanFormNewHorde())
        {
            if (logCalls) Debug.Log($"[HordeManagerDebug] CreateHorde: cannot form new horde (active={activeHordes.Count}).");
            return;
        }

        if (founder.currentHorde != null)
        {
            if (logCalls) Debug.Log($"[HordeManagerDebug] CreateHorde: founder {founder.name} already in a horde.");
            return;
        }

        Horde h = new Horde(founder);
        activeHordes.Add(h);
        if (aloneEnemies.Contains(founder)) aloneEnemies.Remove(founder);
        if (logCalls) Debug.Log($"[HordeManagerDebug] CreateHorde: created by {founder.name}. activeHordes={activeHordes.Count}");
    }

    public void JoinHorde(EnemyAI enemy, Horde horde)
    {
        if (enemy == null || horde == null)
        {
            Debug.LogWarning("[HordeManagerDebug] JoinHorde: null param.");
            return;
        }
        if (enemy.currentHorde != null)
        {
            if (logCalls) Debug.Log($"[HordeManagerDebug] JoinHorde: {enemy.name} already in a horde.");
            return;
        }

        horde.AddMember(enemy);
        if (aloneEnemies.Contains(enemy)) aloneEnemies.Remove(enemy);
        if (logCalls) Debug.Log($"[HordeManagerDebug] JoinHorde: {enemy.name} joined horde (size={horde.members.Count}).");
    }

    public void LeaveHorde(EnemyAI enemy)
    {
        if (enemy == null || enemy.currentHorde == null) return;
        var h = enemy.currentHorde;
        h.RemoveMember(enemy);
        if (enemy != null && enemy.gameObject.activeInHierarchy)
            RegisterEnemy(enemy);

        if (logCalls) Debug.Log($"[HordeManagerDebug] LeaveHorde: {enemy.name} left horde.");
    }

    public void DisbandHorde(Horde horde)
    {
        if (horde == null) return;
        if (activeHordes.Contains(horde))
        {
            activeHordes.Remove(horde);
            if (logCalls) Debug.Log("[HordeManagerDebug] DisbandHorde: removed a horde. activeHordes=" + activeHordes.Count);
        }
    }

    // Buffs
    public void ApplyBuff(EnemyAI enemy)
    {
        if (enemy == null) return;
        enemy.currentMoveSpeed = enemy.baseMoveSpeed * 1.5f;
        if (logCalls) Debug.Log($"[HordeManagerDebug] ApplyBuff: {enemy.name} speed -> {enemy.currentMoveSpeed}");
    }

    public void RemoveBuff(EnemyAI enemy)
    {
        if (enemy == null) return;
        enemy.currentMoveSpeed = enemy.baseMoveSpeed;
        if (logCalls) Debug.Log($"[HordeManagerDebug] RemoveBuff: {enemy.name} speed -> {enemy.currentMoveSpeed}");
    }

    // Coroutine advanced logic (same behaviour)
    private IEnumerator CheckAloneEnemiesLogic()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkAloneInterval);

            if (activeHordes.Count >= maxHordes && aloneEnemies.Count >= minHordeSize)
            {
                if (logCalls) Debug.Log("[HordeManagerDebug] CheckAloneEnemiesLogic: creating small horde.");
                EnemyAI founder = aloneEnemies[0];
                Horde small = new Horde(founder);
                activeHordes.Add(small);
                aloneEnemies.RemoveAt(0);

                int toJoin = minHordeSize - 1;
                for (int i = 0; i < toJoin && aloneEnemies.Count > 0; i++)
                    JoinHorde(aloneEnemies[0], small);
            }
        }
    }

    // Utilitaires d'inspection
    public int GetActiveHordesCount() => activeHordes?.Count ?? 0;
    public int GetAloneEnemiesCount() => aloneEnemies?.Count ?? 0;
}