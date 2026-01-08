using UnityEngine;

public class UnitCombatSystem : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private bool enableCounterAttack = true;

    [Header("Testing")]
    [Tooltip("ВРЕМЕННО: разрешает бить своих для тестов. Потом выключим.")]
    [SerializeField] private bool allowFriendlyFire = true;

    public bool TryAttack(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null) return false;
        if (attacker == defender) return false;

        if (attacker.CurrentTile == null || defender.CurrentTile == null) return false;

        // ✅ ВРЕМЕННО: можно/нельзя бить своих
        if (!allowFriendlyFire && attacker.Owner == defender.Owner)
            return false;

        // атака только по соседней клетке (включая диагональ)
        if (!IsAdjacent(attacker.CurrentTile, defender.CurrentTile)) return false;

        // если у атакера нет ходов — запрещаем
        if (!attacker.HasMoves()) return false;

        // 1) атакер бьёт дефендера
        int dmgToDef = CalculateDamage(
            attack: attacker.Attack,
            targetDefense: defender.Defense,
            targetTile: defender.CurrentTile
        );

        defender.TakeDamage(dmgToDef);

        // если защитник умер — удаляем и всё
        if (defender.IsDead)
        {
            defender.Die();

            // атака заканчивает ход
            attacker.ConsumeAllMoves();
            return true;
        }

        // 2) контратака
        if (enableCounterAttack)
        {
            int dmgToAtk = CalculateDamage(
                attack: defender.Attack,
                targetDefense: attacker.Defense,
                targetTile: attacker.CurrentTile
            );

            attacker.TakeDamage(dmgToAtk);

            if (attacker.IsDead)
            {
                attacker.Die();
                return true;
            }
        }

        // атака заканчивает ход
        attacker.ConsumeAllMoves();
        return true;
    }

    private int CalculateDamage(int attack, int targetDefense, Tile targetTile)
    {
        int terrainBonus = GetTerrainDefenseBonus(targetTile);
        int reduced = targetDefense + terrainBonus;

        // твоя логика: защита уменьшает входящий урон на своё значение
        return Mathf.Max(0, attack - reduced);
    }

    private int GetTerrainDefenseBonus(Tile tile)
    {
        if (tile == null) return 0;

        switch (tile.TerrainType)
        {
            case TileTerrainType.Forest: return 1;
            case TileTerrainType.Mountain: return 3;
            default: return 0;
        }
    }

    private bool IsAdjacent(Tile a, Tile b)
    {
        Vector2Int pa = a.GridPosition;
        Vector2Int pb = b.GridPosition;

        int dx = Mathf.Abs(pa.x - pb.x);
        int dy = Mathf.Abs(pa.y - pb.y);

        return (dx <= 1 && dy <= 1) && !(dx == 0 && dy == 0);
    }
}
