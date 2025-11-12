using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "SeesPlayerCondition", story: "SeesPlayerCondition [Self] & [seesPlayer] & [playerPosition]", category: "Conditions", id: "c9e2a862740e0a0ec5f722c92d7b3f92")]
public partial class SeesPlayerCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<bool> seesPlayer;
    [SerializeReference] public BlackboardVariable<Vector3> playerPosition;

    public override bool IsTrue()
    {
        if (Self == null || Self.Value == null)
        {
            Debug.LogWarning("[SeesPlayerCondition] 'Self' n'est pas assigné ou est null.");
            return false;
        }

        var agentGO = Self.Value;
        var agent = agentGO.GetComponent<EnemyAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[SeesPlayerCondition] EnemyAgent manquant sur 'Self'.");
            return false;
        }

        bool result = agent.CheckSeesPlayer(out var p);

        if (seesPlayer != null) seesPlayer.Value = agent.seesPlayer;
        if (playerPosition != null) playerPosition.Value = agent.playerPosition;

        return result;
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}