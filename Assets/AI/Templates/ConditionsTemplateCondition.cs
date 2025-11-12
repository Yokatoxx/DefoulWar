using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "ConditionsTemplate", story: "ConditionsTemplates", category: "Conditions", id: "0eb6dd9fd3f6f8571954abaec02c11b7")]
public partial class ConditionsTemplateCondition : Condition
{

    public override bool IsTrue()
    {
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
