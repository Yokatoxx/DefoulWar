using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveToPlayer", story: "MoveToPlayer", category: "Action", id: "50a73acddc059bb272a61bf55fdebf8c")]
public partial class Action_MoveToPlayer : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<bool> seesPlayer;
    [SerializeReference] public BlackboardVariable<Vector3> playerPosition;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self == null || Self.Value == null)
        {
            Debug.LogWarning("[Action_MoveToPlayer] 'Self' n'est pas assigné ou est null.");
            return Status.Failure;
        }

        var agent = Self.Value.GetComponent<EnemyAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[Action_MoveToPlayer] EnemyAgent manquant sur 'Self'.");
            return Status.Failure;
        }

        // Effectue l'action locale (met à jour seesPlayer/playerPosition/nav)
        agent.Action_MoveToPlayer_Local();

        // Synchronise le Blackboard
        if (seesPlayer != null) seesPlayer.Value = agent.seesPlayer;
        if (playerPosition != null) playerPosition.Value = agent.playerPosition;

        return Status.Success;
    }

    protected override void OnEnd() { }
}