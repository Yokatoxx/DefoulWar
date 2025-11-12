using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "TryJoinNearestHorde", story: "TryJoinNearestHorde", category: "Action", id: "878fc5586a81250b4f2ab52375f1609f")]
public partial class TryJoinNearestHorde : Action
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
            Debug.LogWarning("[TryJoinNearestHorde] 'Self' n'est pas assigné ou est null.");
            return Status.Failure;
        }
        if (HordeManager.Instance == null)
        {
            Debug.LogWarning("[TryJoinNearestHorde] HordeManager.Instance est null.");
            return Status.Failure;
        }

        var agentGO = Self.Value;
        var agent = agentGO.GetComponent<EnemyAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[TryJoinNearestHorde] EnemyAgent manquant sur 'Self'.");
            return Status.Failure;
        }

        // Si l'agent a déjà une horde locale => succès (synchroniser le blackboard)
        if (agent.hordeId != -1)
        {
            if (hordeId != null) hordeId.Value = agent.hordeId;
            if (isAlone != null) isAlone.Value = false;
            if (lastHordeCheckTime != null) lastHordeCheckTime.Value = agent.lastHordeCheckTime;
            return Status.Success;
        }

        // Tente de rejoindre la horde non pleine la plus proche
        var nearest = HordeManager.Instance.GetNearestJoinableHorde(agentGO.transform.position, agent.hordeJoinRadius);
        if (nearest != null)
        {
            nearest.AddMember(agent); // met à jour agent.hordeId via OnJoinedHorde
            if (hordeId != null) hordeId.Value = nearest.Id;
            if (isAlone != null) isAlone.Value = false;
            if (lastHordeCheckTime != null) lastHordeCheckTime.Value = Time.time;
            return Status.Success;
        }

        // Créer une nouvelle horde si suffisamment de non assignés à proximité
        int nearbyUnassigned = HordeManager.Instance.CountUnassignedNearby(agentGO.transform.position, agent.hordeJoinRadius);
        if (nearbyUnassigned >= agent.hordeMinSize)
        {
            var newH = HordeManager.Instance.CreateHorde(agentGO.transform.position, agent.hordeMax);
            newH.AddMember(agent);
            if (hordeId != null) hordeId.Value = newH.Id;
            if (isAlone != null) isAlone.Value = false;
            if (lastHordeCheckTime != null) lastHordeCheckTime.Value = Time.time;
            return Status.Success;
        }

        // Sinon rester seul
        if (isAlone != null) isAlone.Value = true;
        if (lastHordeCheckTime != null) lastHordeCheckTime.Value = Time.time;
        return Status.Failure;
    }

    protected override void OnEnd() { }
}