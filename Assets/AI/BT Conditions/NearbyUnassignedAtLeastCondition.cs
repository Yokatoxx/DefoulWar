using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "NearbyUnassignedAtLeast", story: "NearbyUnassignedAtLeast", category: "Conditions", id: "4b2787bd0090eb64cb0c4d601f4f4f6d")]
public partial class NearbyUnassignedAtLeastCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<int> Threshold;

    public override bool IsTrue()
    {
        if (Self == null || Self.Value == null)
        {
            Debug.LogWarning("[NearbyUnassignedAtLeastCondition] 'Self' n'est pas assigné ou est null.");
            return false;
        }
        if (Threshold == null)
        {
            Debug.LogWarning("[NearbyUnassignedAtLeastCondition] 'Threshold' n'est pas assigné.");
            return false;
        }
        if (HordeManager.Instance == null)
        {
            Debug.LogWarning("[NearbyUnassignedAtLeastCondition] HordeManager.Instance est null.");
            return false;
        }

        var agentGO = Self.Value;
        var agent = agentGO.GetComponent<EnemyAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[NearbyUnassignedAtLeastCondition] EnemyAgent manquant sur 'Self'.");
            return false;
        }

        int count = HordeManager.Instance.CountUnassignedNearby(agentGO.transform.position, agent.hordeJoinRadius);
        return count >= Threshold.Value;
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}