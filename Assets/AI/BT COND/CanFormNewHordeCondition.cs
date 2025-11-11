using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CanFormNewHorde", story: "CanFormNewHorde", category: "Conditions", id: "ea7e8a506e8e0fa919019e4a75b1c614")]
public partial class CanFormNewHordeCondition : Condition
{
    public override bool IsTrue()
    {
        if (HordeManager.instance == null) return false;
        return HordeManager.instance.CanFormNewHorde();
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}