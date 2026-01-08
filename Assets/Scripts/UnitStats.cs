using UnityEngine;

[CreateAssetMenu(menuName = "PocketCiv/Units/Unit Stats", fileName = "UnitStats")]
public class UnitStats : ScriptableObject
{
    [Header("Movement")]
    [Min(1)] public int movePointsPerTurn = 3;

    [Header("Combat")]
    [Min(0)] public int attack = 1;
    [Min(0)] public int defense = 1;
    [Min(1)] public int hp = 10;

    [Tooltip("Дальность атаки (1 = ближний бой, 2+ = дальний).")]
    [Min(1)] public int attackRange = 1;

    [Header("Cost (later)")]
    [Min(0)] public int goldCost = 15;
    [Min(0)] public int coalCost = 5;

    // Удобные свойства (не ломают сериализацию, но помогают в коде)
    public int MovePointsPerTurn => movePointsPerTurn;
    public int Attack => attack;
    public int Defense => defense;
    public int MaxHP => hp;
    public int AttackRange => attackRange;
}
