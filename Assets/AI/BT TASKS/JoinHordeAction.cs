using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "JoinHordeAction", story: "Rejoin la horde", category: "Action", id: "2e0f964b471eb8dda216991ee076becc")]
public partial class JoinHordeAction : Action
{
    // On reçoit ici le GameObject du leader (ou null)
    public BlackboardVariable<GameObject> HordeLeaderObject;

    protected override Status OnStart() => Status.Running;

    protected override Status OnUpdate()
    {
        if (HordeManager.instance == null)
            return Status.Failure;

        var go = this.GameObject;
        if (go == null)
            return Status.Failure;

        var enemy = go.GetComponent<EnemyAI>();
        if (enemy == null)
            return Status.Failure;

        if (HordeLeaderObject == null || HordeLeaderObject.Value == null)
            return Status.Failure;

        var leaderGO = HordeLeaderObject.Value;
        var leaderEnemy = leaderGO.GetComponent<EnemyAI>();
        if (leaderEnemy == null)
            return Status.Failure;

        var horde = leaderEnemy.currentHorde;
        if (horde == null)
            return Status.Failure;

        // Joins if possible
        HordeManager.instance.JoinHorde(enemy, horde);
        return Status.Success;
    }

    protected override void OnEnd() { }
}