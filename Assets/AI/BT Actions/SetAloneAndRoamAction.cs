using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "SetAloneAndRoam",
    story: "Sets the agent as alone and assigns a roaming target position.",
    category: "Actions",
    id: "bf4b0be1923026c35739d12b78e86856"
)]
public partial class Action_SetAloneAndRoam : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<bool> isAlone;
    [SerializeReference] public BlackboardVariable<Vector3> targetPosition;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self == null || Self.Value == null)
        {
            Debug.LogWarning("[Action_SetAloneAndRoam] 'Self' n'est pas set ou est null dans le Blackboard.");
            return Status.Failure;
        }

        var agentGO = Self.Value;
        var agent = agentGO.GetComponent<EnemyAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[Action_SetAloneAndRoam] EnemyAgent manquant sur Self.");
            return Status.Failure;
        }

        agent.Action_SetAloneAndRoam_Local();

        if (isAlone != null)
            isAlone.Value = true;

        if (targetPosition != null)
            targetPosition.Value = agent.targetPosition;

        return Status.Success;
    }

    protected override void OnEnd() { }
}