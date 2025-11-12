using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HordeCheckDue", story: "HordeCheckDue [Self] & [lastHordeCheckTime]", category: "Conditions", id: "9bb2d24b67ad82d11a9b2fe86e4125fe")]
public partial class HordeCheckDueCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> lastHordeCheckTime;

    public override bool IsTrue()
    {
        if (Self == null || Self.Value == null)
        {
            Debug.LogWarning("[HordeCheckDueCondition] 'Self' non assigné ou null dans le Blackboard.");
            return false;
        }

        var agentGO = Self.Value;
        var agent = agentGO.GetComponent<EnemyAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[HordeCheckDueCondition] EnemyAgent manquant sur Self.");
            return false;
        }

        float last = lastHordeCheckTime != null ? lastHordeCheckTime.Value : 0f;
        float interval = Mathf.Max(0.1f, agent.hordeCheckInterval);

        return (Time.time - last) >= interval;
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}