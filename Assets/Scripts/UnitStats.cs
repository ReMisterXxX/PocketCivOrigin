using UnityEngine;

[CreateAssetMenu(menuName = "PocketCiv/Units/Unit Stats", fileName = "UnitStats")]
public class UnitStats : ScriptableObject
{
    [Header("Movement")]
    [Min(1)] public int movePointsPerTurn = 3;

    [Header("Combat (later)")]
    [Min(0)] public int attack = 1;
    [Min(0)] public int defense = 1;
    [Min(1)] public int hp = 10;

    [Header("Cost (later)")]
    [Min(0)] public int goldCost = 15;
    [Min(0)] public int coalCost = 5;
}
