using UnityEngine;

public class UnitCombatSystem : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private bool enableCounterAttack = true;

    [Range(0f, 1f)]
    [SerializeField] private float counterAttackMultiplier = 0.5f;

    [Header("Testing")]
    [SerializeField] private bool allowFriendlyFire = true;

    public bool EnableCounterAttack => enableCounterAttack;
    public float CounterAttackMultiplier => counterAttackMultiplier;

    public struct CombatResult
    {
        public bool didAttack;
        public bool didCounter;
        public int damageToDefender;
        public int damageToAttacker;
        public bool defenderDied;
        public bool attackerDied;
    }

    public bool CanAttack(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null) return false;
        if (attacker == defender) return false;

        if (attacker.CurrentTile == null || defender.CurrentTile == null) return false;

        if (!allowFriendlyFire && attacker.Owner == defender.Owner)
            return false;

        int range = GetAttackRange(attacker);
        if (!IsInRange(attacker.CurrentTile, defender.CurrentTile, range))
            return false;

        if (!attacker.HasMoves()) return false;

        return true;
    }

    public int GetAttackRange(Unit attacker)
    {
        if (attacker != null && attacker.Stats != null)
            return Mathf.Max(1, attacker.Stats.AttackRange);
        return 1;
    }

    public CombatResult ResolveAttack(Unit attacker, Unit defender)
    {
        CombatResult r = new CombatResult();

        if (!CanAttack(attacker, defender))
            return r;

        r.didAttack = true;

        r.damageToDefender = CalculateDamage(attacker.Attack, defender.Defense, defender.CurrentTile);
        defender.TakeDamage(r.damageToDefender);
        r.defenderDied = defender.IsDead;

        if (enableCounterAttack && !r.defenderDied)
        {
            r.didCounter = true;

            int raw = CalculateDamage(defender.Attack, attacker.Defense, attacker.CurrentTile);
            r.damageToAttacker = Mathf.Max(0, Mathf.RoundToInt(raw * counterAttackMultiplier));

            attacker.TakeDamage(r.damageToAttacker);
            r.attackerDied = attacker.IsDead;
        }

        attacker.ConsumeAllMoves();
        return r;
    }

    public int GetTerrainDefenseBonus(Tile tile)
    {
        if (tile == null) return 0;

        switch (tile.TerrainType)
        {
            case TileTerrainType.Forest: return 1;
            case TileTerrainType.Mountain: return 3;
            default: return 0;
        }
    }

    private int CalculateDamage(int attack, int targetDefense, Tile targetTile)
    {
        int terrainBonus = GetTerrainDefenseBonus(targetTile);
        int reduced = targetDefense + terrainBonus;
        return Mathf.Max(0, attack - reduced);
    }

    private bool IsInRange(Tile a, Tile b, int range)
    {
        Vector2Int pa = a.GridPosition;
        Vector2Int pb = b.GridPosition;

        int dx = Mathf.Abs(pa.x - pb.x);
        int dy = Mathf.Abs(pa.y - pb.y);

        int cheb = Mathf.Max(dx, dy);
        return cheb >= 1 && cheb <= range;
    }
}
