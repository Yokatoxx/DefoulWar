using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CreateHorde", story: "CreateHorde", category: "Action", id: "c83a8c62b08d7b0092e69cfe45fd327a")]
public partial class Action_CreateHorde : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<int> hordeId;
    [SerializeReference] public BlackboardVariable<bool> isAlone;
    [SerializeReference] public BlackboardVariable<float> lastHordeCheckTime;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self == null || Self.Value == null)
        {
            Debug.LogWarning("[Action_CreateHorde] 'Self' n'est pas assigné ou est null.");
            return Status.Failure;
        }
        if (HordeManager.Instance == null)
        {
            Debug.LogWarning("[Action_CreateHorde] HordeManager.Instance est null.");
            return Status.Failure;
        }

        var agent = Self.Value.GetComponent<EnemyAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[Action_CreateHorde] EnemyAgent manquant sur 'Self'.");
            return Status.Failure;
        }

        var newH = HordeManager.Instance.CreateHorde(agent.transform.position, agent.hordeMax);
        newH.AddMember(agent);

        if (hordeId != null) hordeId.Value = newH.Id;
        if (isAlone != null) isAlone.Value = false;
        if (lastHordeCheckTime != null) lastHordeCheckTime.Value = Time.time;

        return Status.Success;
    }

    protected override void OnEnd() { }
}