using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsAlone", story: "Seul ?", category: "Conditions", id: "340532f0eb2a9b339a0a4b5388a0aa03")]
public partial class IsAloneCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> isAlone;

    public override bool IsTrue()
    {
        if (isAlone == null)
        {
            Debug.LogWarning("[IsAloneCondition] 'isAlone' n'est pas assigné.");
            return false;
        }
        return isAlone.Value;
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}