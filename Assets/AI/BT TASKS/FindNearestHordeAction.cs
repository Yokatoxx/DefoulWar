using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FindNearestHorde", story: "Trouve une horde", category: "Action", id: "b2b2fca2a4edceaaab11c9ca796dd520")]
public partial class FindNearestHordeAction : Action
{
    public BlackboardVariable<Horde> NearestHorde;
    public BlackboardVariable<Vector3> TargetPosition;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (HordeManager.instance == null)
        {
            return Status.Failure;
        }
        var agent = this.GameObject;

        if (agent == null)
            return Status.Failure;

        Horde nearest = HordeManager.instance.FindNearestHorde(agent.transform.position);
        if (nearest != null)
        {
            if (NearestHorde != null)
                NearestHorde.Value = nearest;

            if (TargetPosition != null)
                TargetPosition.Value = nearest.rallyPoint;
            Debug.LogWarning("TEST (Sucess ici)");
            return Status.Success;
        }

        Debug.LogWarning("AUCUNE HORDE A PROXIMITE");
        return Status.Failure;
    }

    protected override void OnEnd()
    {
    }
}

