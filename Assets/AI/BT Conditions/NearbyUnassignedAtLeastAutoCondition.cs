using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "NearbyUnassignedAtLeastAuto",
    story: "Returns true if there are enough unassigned agents near 'Self' to form a horde.",
    category: "Conditions",
    id: "5ae13cac328d3c7f190bf845780e4dcb"
)]
public partial class NearbyUnassignedAtLeastAutoCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self == null || Self.Value == null)
        {
            Debug.LogWarning("[NearbyUnassignedAtLeastAutoCondition] 'Self' n'est pas set ou est null dans le Blackboard.");
            return false;
        }

        var agentGO = Self.Value;
        var agent = agentGO.GetComponent<EnemyAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[NearbyUnassignedAtLeastAutoCondition] EnemyAgent manquant sur Self.");
            return false;
        }

        if (HordeManager.Instance == null)
        {
            Debug.LogWarning("[NearbyUnassignedAtLeastAutoCondition] HordeManager.Instance est null.");
            return false;
        }

        int nearbyCount = HordeManager.Instance.CountUnassignedNearby(
            agentGO.transform.position,
            agent.hordeJoinRadius
        );

        return nearbyCount >= agent.hordeMinSize;
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}