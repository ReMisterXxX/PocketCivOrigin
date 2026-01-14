using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitMovementSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private UnitCombatSystem combatSystem;

    [Header("UI")]
    [SerializeField] private UnitInfoUI unitInfoUI;

    [Header("Input")]
    [SerializeField] private bool handleMouseInput = false;

    [Header("Move markers")]
    [SerializeField] private GameObject moveMarkerPrefab;
    [SerializeField] private float markerYOffset = 0.05f;

    [Header("Attack markers")]
    [SerializeField] private Color attackMarkerColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float attackMarkerYOffset = 0.08f;

    [Header("Attack VFX")]
    [SerializeField] private float attackAnimDuration = 0.70f;
    [SerializeField] private float attackLungeDistance = 0.75f;

    [Header("Death VFX")]
    [SerializeField] private float deathDuration = 0.55f;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.14f;
    [SerializeField] private bool blockWater = true;

    [Header("Attack Mode")]
    [SerializeField] private bool attackMode = false;
    public bool AttackMode => attackMode;
    public Unit SelectedUnit => selectedUnit;
    public bool IsAttackInProgress => attackRoutine != null;

    private Unit selectedUnit;

    private readonly List<GameObject> moveMarkers = new();
    private readonly List<GameObject> attackMarkers = new();

    private Coroutine moveRoutine;
    private Coroutine attackRoutine;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (combatSystem == null) combatSystem = FindObjectOfType<UnitCombatSystem>();
    }

    public void SelectUnitFromClick(Unit unit)
{
    if (unit == null) return;
    // используем твою приватную SelectUnit
    // (если SelectUnit у тебя private — просто оставь этот метод ВНУТРИ того же класса)
    SelectUnit(unit);
}


    public void ToggleAttackMode()
    {
        attackMode = !attackMode;
        RefreshMarkers();
    }

    // ✅ TileSelector вызывает это. Возвращает true только если был клик-атака.
    public bool HandleTileClick(Tile tile)
{
    if (tile == null) return false;

    Unit clickedUnit = FindUnitOnTile(tile);

    // 1) атака
    if (attackMode && selectedUnit != null && clickedUnit != null && clickedUnit != selectedUnit)
    {
        if (combatSystem != null && combatSystem.CanAttack(selectedUnit, clickedUnit))
        {
            TryAttackSelectedUnit(clickedUnit);
            return true;
        }
    }

    // 2) ✅ клик в атак-моде, но не атака -> сброс всего
    if (attackMode)
    {
        attackMode = false;
        ClearSelection();
        RefreshMarkers();
        return false;
    }

    // 3) обычная логика
    OnTileClicked(tile);
    return false;
}

        // ✅ TileSelector будет звать это при клике по юниту
    // Возвращает true, если клик "съеден" (атака началась)
    public bool HandleUnitClick(Unit clickedUnit)
    {
        if (clickedUnit == null) return false;

        // 1) Если AttackMode: пытаемся атаковать (и ничего не выделяем/не меняем)
        if (attackMode)
        {
            if (selectedUnit != null && clickedUnit != selectedUnit)
            {
                if (combatSystem != null && combatSystem.CanAttack(selectedUnit, clickedUnit))
                {
                    TryAttackSelectedUnit(clickedUnit);
                    return true; // ✅ атака началась
                }
            }

            // AttackMode включен, но атаки нет — НЕ меняем выделение и НЕ трогаем тайлы
            return false;
        }

        // 2) Обычный режим: просто выделяем юнита
        SelectUnitFromClick(clickedUnit);
        return false;
    }



    // совместимость (если где-то вызывается напрямую)
    public void OnTileClicked(Tile tile)
    {
        if (tile == null) return;

        Unit clickedUnit = FindUnitOnTile(tile);

        // AttackMode: по пустым тайлам не ходим
        if (attackMode)
        {
            if (clickedUnit != null)
                SelectUnit(clickedUnit);
            return;
        }

        // обычный режим
        if (clickedUnit != null)
        {
            SelectUnit(clickedUnit);
            return;
        }

        if (selectedUnit != null)
        {
            bool moved = TryMoveSelectedUnitTo(tile);
            if (!moved) ClearSelection();
        }
    }

    public void OnTileSelectionChanged()
    {
        if (selectedUnit == null) return;
        if (moveRoutine != null) return;
        if (attackRoutine != null) return;

        selectedUnit.SnapToCurrentTile();
    }

    public void ClearSelection()
    {
        selectedUnit = null;
        ClearMoveMarkers();
        ClearAttackMarkers();

        if (unitInfoUI != null)
            unitInfoUI.Hide();
    }

    // ✅ Вызывается TurnManager-ом в начале нового хода
    public void ResetAllUnitsForNewTurn()
    {
        Unit[] allUnits = FindObjectsOfType<Unit>();
        foreach (var u in allUnits)
        {
            if (u == null) continue;
            u.ResetMoves(); // должно быть в твоём Unit.cs
        }

        // Обновим маркеры для выбранного юнита
        RefreshMarkers();

        // Обновим панель, если выбранный юнит есть
        if (unitInfoUI != null && selectedUnit != null)
            unitInfoUI.Refresh(selectedUnit);
    }


    private void SelectUnit(Unit unit)
    {
        if (unit == null) return;

        selectedUnit = unit;
        selectedUnit.SetMoving(false);
        selectedUnit.SnapToCurrentTile();

        RefreshMarkers();

        if (unitInfoUI != null)
            unitInfoUI.ShowFor(selectedUnit);
    }

    private void RefreshMarkers()
    {
        ClearMoveMarkers();
        ClearAttackMarkers();

        if (selectedUnit == null) return;

        if (!attackMode)
        {
            ShowMoveMarkers(selectedUnit);
            return;
        }

        ShowAttackMarkers(selectedUnit);
    }

    private void Update()
    {
        if (!handleMouseInput) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Tile tile = hit.collider.GetComponentInParent<Tile>();
                if (tile != null) HandleTileClick(tile);
            }
        }
    }

    private bool TryAttackSelectedUnit(Unit targetUnit)
    {
        if (selectedUnit == null || targetUnit == null) return false;
        if (combatSystem == null) return false;

        if (!combatSystem.CanAttack(selectedUnit, targetUnit))
            return false;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(AttackSequence(selectedUnit, targetUnit));
        return true;
    }

    private IEnumerator AttackSequence(Unit attacker, Unit defender)
    {
        if (!attacker || !defender) yield break;

        yield return AttackVfxRoutine(attacker, defender);

        if (!attacker || !defender) yield break;

        var result = combatSystem.ResolveAttack(attacker, defender);

        if (unitInfoUI != null && selectedUnit != null)
            unitInfoUI.Refresh(selectedUnit);

        if (result.didCounter && !attacker.IsDead && !defender.IsDead)
        {
            yield return AttackVfxRoutine(defender, attacker);

            if (unitInfoUI != null && selectedUnit != null)
                unitInfoUI.Refresh(selectedUnit);
        }

        if (defender != null && defender.IsDead)
            yield return DeathRoutine(defender);

        if (attacker != null && attacker.IsDead)
        {
            yield return DeathRoutine(attacker);
            ClearSelection();
        }

        // ✅ выходим из AttackMode после атаки
        attackMode = false;

        attackRoutine = null;
        RefreshMarkers();
    }

    private IEnumerator AttackVfxRoutine(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null)
            yield break;

        // 🔒 Блокируем "прилипание" к тайлу во время анимации
        attacker.SetMoving(true);

        Vector3 startPos = attacker.transform.position;
        Vector3 targetPos = defender.transform.position;

        // направление удара (по плоскости)
        Vector3 dir = targetPos - startPos;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            attacker.SetMoving(false);
            yield break;
        }

        dir.Normalize();

        // 🔁 Поворот в сторону атаки
        attacker.FaceDirection(dir);

        // насколько выдвигаемся вперёд
        Vector3 lungePos = startPos + dir * attackLungeDistance;

        // ⏱️ Анимация: вперёд + назад
        float totalDuration = Mathf.Max(0.01f, attackAnimDuration);
        float halfDuration = totalDuration * 0.5f;

        // ▶️ движение ВПЕРЁД
        float t = 0f;
        while (t < 1f)
        {
            if (attacker == null)
                yield break;

            t += Time.deltaTime / halfDuration;
            float k = Mathf.SmoothStep(0f, 1f, t);
            attacker.transform.position = Vector3.Lerp(startPos, lungePos, k);
            yield return null;
        }

        // ◀️ возврат НАЗАД
        t = 0f;
        while (t < 1f)
        {
            if (attacker == null)
                yield break;

            t += Time.deltaTime / halfDuration;
            float k = Mathf.SmoothStep(0f, 1f, t);
            attacker.transform.position = Vector3.Lerp(lungePos, startPos, k);
            yield return null;
        }

        // 🔓 Возвращаем контроль юниту
        attacker.transform.position = startPos;
        attacker.SetMoving(false);
        attacker.SnapToCurrentTile();
    }



    private IEnumerator DeathRoutine(Unit unit)
    {
        if (!unit) yield break;

        Vector3 startPos = unit.transform.position;
        Quaternion startRot = unit.transform.rotation;

        Quaternion endRot = Quaternion.Euler(90f, startRot.eulerAngles.y, startRot.eulerAngles.z);
        Vector3 endPos = startPos + Vector3.down * 0.05f;

        float t = 0f;
        float dur = Mathf.Max(0.01f, deathDuration);

        while (t < 1f)
        {
            if (!unit) yield break;

            t += Time.deltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, t);

            unit.transform.position = Vector3.Lerp(startPos, endPos, k);
            unit.transform.rotation = Quaternion.Slerp(startRot, endRot, k);

            yield return null;
        }

        if (unit)
            unit.Die();
    }

    private bool TryMoveSelectedUnitTo(Tile target)
    {
        if (selectedUnit == null || target == null) return false;

        if (!selectedUnit.HasMoves())
            return false;

        if (blockWater && target.TerrainType == TileTerrainType.Water)
            return false;

        if (FindUnitOnTile(target) != null)
            return false;

        if (!IsTileInMoveRange(selectedUnit, target))
            return false;

        int cost = GetChebyshevDistance(selectedUnit.CurrentTile, target);
        if (cost <= 0) return false;

        if (cost > selectedUnit.MovesLeftThisTurn)
            return false;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveUnitRoutine(selectedUnit, target, cost));
        return true;
    }

    private IEnumerator MoveUnitRoutine(Unit unit, Tile target, int cost)
    {
        if (!unit) yield break;

        unit.SetMoving(true);

        Vector3 from = unit.transform.position;
        Vector3 to = unit.GetWorldPositionOnTile(target);

        unit.FaceDirection(to - from);

        float t = 0f;
        while (t < 1f)
        {
            if (!unit) yield break;

            t += Time.deltaTime / Mathf.Max(0.01f, moveDuration);
            float tt = Mathf.SmoothStep(0f, 1f, t);
            unit.transform.position = Vector3.Lerp(from, to, tt);
            yield return null;
        }

        if (!unit) yield break;

        unit.transform.position = to;
        unit.SetTile(target, instant: true);
        unit.SpendMovePoint(cost);

        unit.SetMoving(false);
        moveRoutine = null;

        RefreshMarkers();

        if (unitInfoUI != null && selectedUnit != null)
            unitInfoUI.Refresh(selectedUnit);
    }

    private void ShowMoveMarkers(Unit unit)
    {
        if (unit == null) return;
        if (!unit.HasMoves()) return;

        int range = unit.MovesLeftThisTurn;
        Tile origin = unit.CurrentTile;
        if (origin == null) return;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                if (dist == 0 || dist > range) continue;

                Vector2Int gp = origin.GridPosition + new Vector2Int(dx, dy);
                Tile t = FindTileAt(gp);
                if (t == null) continue;

                if (blockWater && t.TerrainType == TileTerrainType.Water) continue;
                if (FindUnitOnTile(t) != null) continue;

                SpawnMarker(t, moveMarkers, markerYOffset, null, "");
            }
        }
    }

    private void ShowAttackMarkers(Unit unit)
    {
        if (unit == null) return;
        if (!unit.HasMoves()) return;
        if (combatSystem == null) return;

        Tile origin = unit.CurrentTile;
        if (origin == null) return;

        int range = combatSystem.GetAttackRange(unit);

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                if (dist > range) continue;

                Vector2Int gp = origin.GridPosition + new Vector2Int(dx, dy);
                Tile t = FindTileAt(gp);
                if (t == null) continue;

                Unit target = FindUnitOnTile(t);
                if (target == null) continue;

                if (!combatSystem.CanAttack(unit, target))
                    continue;

                int dmgToDef = Mathf.Max(0, unit.Attack - (target.Defense + combatSystem.GetTerrainDefenseBonus(target.CurrentTile)));
                int rawCounter = Mathf.Max(0, target.Attack - (unit.Defense + combatSystem.GetTerrainDefenseBonus(unit.CurrentTile)));
                int dmgToAtk = combatSystem.EnableCounterAttack ? Mathf.Max(0, Mathf.RoundToInt(rawCounter * combatSystem.CounterAttackMultiplier)) : 0;

                string label = (dmgToAtk > 0) ? $"{dmgToDef}/{dmgToAtk}" : $"{dmgToDef}";
                SpawnMarker(t, attackMarkers, attackMarkerYOffset, attackMarkerColor, label);
            }
        }
    }

    private void SpawnMarker(Tile tile, List<GameObject> list, float yOffset, Color? color, string label)
    {
        if (moveMarkerPrefab == null) return;
        if (tile == null) return;

        Vector3 p = tile.transform.position;
        p.y = tile.TopHeight + yOffset;

        GameObject m = Instantiate(moveMarkerPrefab, p, Quaternion.identity);
        list.Add(m);

        MoveMarker mm = m.GetComponent<MoveMarker>();
        if (mm != null)
        {
            if (color.HasValue) mm.SetColor(color.Value);
            mm.SetLabel(label ?? "");
        }
    }

    private void ClearMoveMarkers()
    {
        foreach (var m in moveMarkers)
            if (m != null) Destroy(m);
        moveMarkers.Clear();
    }

    private void ClearAttackMarkers()
    {
        foreach (var m in attackMarkers)
            if (m != null) Destroy(m);
        attackMarkers.Clear();
    }

    private bool IsTileInMoveRange(Unit unit, Tile target)
    {
        if (unit == null || target == null || unit.CurrentTile == null) return false;
        Vector2Int a = unit.CurrentTile.GridPosition;
        Vector2Int b = target.GridPosition;
        int dist = Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        return dist > 0 && dist <= unit.MovesLeftThisTurn;
    }

    private int GetChebyshevDistance(Tile a, Tile b)
    {
        if (a == null || b == null) return 0;
        Vector2Int pa = a.GridPosition;
        Vector2Int pb = b.GridPosition;
        return Mathf.Max(Mathf.Abs(pa.x - pb.x), Mathf.Abs(pa.y - pb.y));
    }

    private Tile FindTileAt(Vector2Int gridPos)
    {
        if (Tile.TryGetTile(gridPos, out Tile tile) && tile != null)
            return tile;

        foreach (var t in FindObjectsOfType<Tile>())
            if (t.GridPosition == gridPos) return t;

        return null;
    }

    private Unit FindUnitOnTile(Tile tile)
    {
        if (tile == null) return null;

        if (tile.UnitOnTile != null) return tile.UnitOnTile;

        foreach (var u in FindObjectsOfType<Unit>())
            if (u != null && u.CurrentTile == tile) return u;

        return null;
    }
}
