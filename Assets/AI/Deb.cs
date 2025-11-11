using System.Linq;
using UnityEngine;

public class RuntimeHordeInspector : MonoBehaviour
{
    public bool logOnStart = true;
    void Start()
    {
        if (!logOnStart) return;

        var mgrs = FindObjectsOfType<HordeManager>();
        Debug.Log($"[RuntimeHordeInspector] HordeManager instances: {mgrs.Length}");
        for (int i = 0; i < mgrs.Length; i++)
        {
            Debug.Log($"[RuntimeHordeInspector] HordeManager[{i}] = {mgrs[i].GetHashCode()} (gameObject: {mgrs[i].gameObject.name})");
            // expose via reflection activeHordes count
            var fi = typeof(HordeManager).GetField("activeHordes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fi != null)
            {
                var list = fi.GetValue(mgrs[i]) as System.Collections.ICollection;
                Debug.Log($"[RuntimeHordeInspector] activeHordes count on mgr[{i}] = {list?.Count ?? -1}");
            }
        }

        var enemies = FindObjectsOfType<EnemyAI>();
        Debug.Log($"[RuntimeHordeInspector] Found {enemies.Length} EnemyAI");
        foreach (var e in enemies.Take(5))
        {
            Debug.Log($"[RuntimeHordeInspector] Enemy {e.name} currentHorde null? {e.currentHorde == null}");
            if (e.currentHorde != null)
            {
                Debug.Log($"    members={e.currentHorde.members?.Count ?? -1} leader={(e.currentHorde.leader != null ? e.currentHorde.leader.name : "null")}");
            }
        }
    }
}