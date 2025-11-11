using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FindNearestHorde", story: "Trouve une horde", category: "Action", id: "b2b2fca2a4edceaaab11c9ca796dd520")]
public partial class FindNearestHordeAction : Action
{
    [SerializeReference, SerializeField]
    public BlackboardVariable<GameObject> NearestHorde; // GameObject du leader
    [SerializeReference, SerializeField]
    public BlackboardVariable<Vector3> TargetPos;

    protected override Status OnStart()
    {
        Debug.Log("[FindNearestHorde] OnStart");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (HordeManager.instance == null)
        {
            Debug.LogWarning("[FindNearestHorde] HordeManager.instance is null");
            return Status.Failure;
        }

        var agentGO = this.GameObject;
        if (agentGO == null)
        {
            Debug.LogWarning("[FindNearestHorde] GameObject agent is null");
            return Status.Failure;
        }

        var pos = agentGO.transform.position;
        Horde nearest = HordeManager.instance.FindNearestHorde(pos);
        if (nearest == null)
        {
            Debug.Log("[FindNearestHorde] Aucune horde trouvée pour " + agentGO.name);
            return Status.Failure;
        }

        // leader peut être null => on prend rallyPoint mais on veut un leader GameObject pour blackboard
        GameObject leaderGO = nearest.leader != null ? nearest.leader.gameObject : null;
        if (NearestHorde != null)
        {
            NearestHorde.Value = leaderGO;
        }
        if (TargetPos != null)
        {
            TargetPos.Value = nearest.rallyPoint;
        }

        Debug.Log($"[FindNearestHorde] Pour {agentGO.name} trouvée horde (leader={(leaderGO ? leaderGO.name : "null")}) rallyPoint={nearest.rallyPoint} members={nearest.members?.Count ?? 0}");
        return Status.Success;
    }

    protected override void OnEnd()
    {
        Debug.Log("[FindNearestHorde] OnEnd");
    }
}