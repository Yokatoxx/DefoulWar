using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "test", story: "[isAlone]", category: "Conditions", id: "b088bf002fadb58a3bac0d5556eac883")]
public partial class TestCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> isAlone;

    public override bool IsTrue()
    {
        if (isAlone == null)
        {
            Debug.LogWarning("[TestCondition] 'isAlone' n'est pas assigné.");
            return false;
        }
        return isAlone.Value;
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}