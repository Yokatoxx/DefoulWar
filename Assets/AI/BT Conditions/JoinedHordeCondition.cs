using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "JoinedHorde", story: "Id de la horde à rejoindre [hordeId]", category: "Conditions", id: "91328be5e8f6d4d53229b2a5ac5c6301")]
public partial class JoinedHordeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<int> hordeId;

    public override bool IsTrue()
    {
        if (hordeId == null)
        {
            Debug.LogWarning("[JoinedHordeCondition] 'hordeId' n'est pas assigné.");
            return false;
        }
        return hordeId.Value != -1;
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}