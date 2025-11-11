using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "IsInHorde",
    story: "Est-ce que l'ennemi fait partie d'une horde ?",
    category: "Conditions/Horde",
    id: "b6926c4e972713d96284923c0088142d"
)]
public partial class IsInHordeCondition : Condition
{
    [SerializeField, Tooltip("Optionnel : GameObject à tester (si null, on utilise le GameObject de l'agent).")]
    private GameObject targetObject;

    public override bool IsTrue()
    {
        var go = targetObject != null ? targetObject : this.GameObject;
        if (go == null)
            return false;

        var enemyAI = go.GetComponent<EnemyAI>();
        if (enemyAI == null)
            return false;

        var horde = enemyAI.currentHorde;
        if (horde == null)
            return false;

        return horde.members != null && horde.members.Count > 1;
    }

    public override void OnStart() { }
    public override void OnEnd() { }
}
