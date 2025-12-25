using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitMovementSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerResources playerResources;
    [SerializeField] private Camera mainCamera;

    [Header("Move markers (circles)")]
    [SerializeField] private GameObject moveMarkerPrefab;
    [SerializeField] private float markerYOffset = 0.05f;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.12f;
    [SerializeField] private bool blockWater = true;

    private Unit selectedUnit;
    private readonly List<GameObject> markers = new List<GameObject>();
    private Coroutine moveRoutine;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (playerResources == null) playerResources = FindObjectOfType<PlayerResources>();
    }

    // ✅ чтобы НЕ ломать твою старую схему, оставляем метод, который TileSelector может вызывать
    public void OnTileClicked(Tile tile)
    {
        if (tile == null) return;

        // если на тайле есть юнит — выбираем его
        Unit u = FindUnitOnTile(tile);
        if (u != null)
        {
            SelectUnit(u);
            return;
        }

        // если выбран юнит — пытаемся ходить
        if (selectedUnit != null)
        {
            TryMoveSelectedUnitTo(tile);
        }
    }

    // ✅ тоже оставляем публичным — чтобы TileSelector мог чистить
    public void ClearSelection()
    {
        selectedUnit = null;
        ClearMarkers();
    }

    // Если хочешь, можешь оставить “самостоятельные клики” тоже:
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Tile tile = hit.collider.GetComponentInParent<Tile>();
                if (tile != null)
                    OnTileClicked(tile);
            }
        }
    }

    public void ResetAllUnitsForNewTurn()
    {
        Unit[] all = FindObjectsOfType<Unit>();
        foreach (var u in all)
            u.ResetMoves();

        // если выбранный юнит был — обновим кружки
        if (selectedUnit != null)
            ShowMoveMarkers(selectedUnit);
    }

    private void SelectUnit(Unit unit)
    {
        selectedUnit = unit;
        ShowMoveMarkers(unit);
    }

    private void TryMoveSelectedUnitTo(Tile target)
    {
        if (selectedUnit == null || target == null) return;

        // нет ходов
        if (!selectedUnit.HasMoves())
            return;

        // запрет воды
        if (blockWater && target.TerrainType == TileTerrainType.Water)
            return;

        // нельзя в тайл, где уже юнит
        if (FindUnitOnTile(target) != null)
            return;

        // можно ходить ТОЛЬКО в подсвеченные (т.е. в радиус MovePoints)
        if (!IsTileInMoveRange(selectedUnit, target))
            return;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveUnitRoutine(selectedUnit, target));
    }

    private IEnumerator MoveUnitRoutine(Unit unit, Tile target)
    {
        // направление (для поворота)
        Vector3 from = unit.transform.position;
        Vector3 to = unit.GetWorldPositionOnTile(target);

        unit.FaceDirection(to - from);

        // снять с прошлого тайла + поставить на новый (флаги)
        Tile oldTile = unit.CurrentTile;

        // движение плавное
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, moveDuration);
            float tt = Mathf.SmoothStep(0f, 1f, t);
            unit.transform.position = Vector3.Lerp(from, to, tt);
            yield return null;
        }

        unit.transform.position = to;

        // закрепить тайл (и обновить флаги декора)
        unit.SetTile(target, instant: true);

        // списать 1 очко хода
        unit.SpendMovePoint(1);

        // обновить кружки
        ShowMoveMarkers(unit);

        moveRoutine = null;
    }

    private void ShowMoveMarkers(Unit unit)
    {
        ClearMarkers();

        if (unit == null) return;
        if (!unit.HasMoves()) return;

        int range = unit.MovesLeftThisTurn; // ✅ кружки показываем по оставшимся очкам

        // по твоему требованию: “может ходить по диагонали”
        // значит используем Chebyshev distance: max(|dx|,|dy|)
        Tile origin = unit.CurrentTile;
        if (origin == null) return;

        // простой способ: пробежаться по квадрату range и отфильтровать
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

                SpawnMarker(t);
            }
        }
    }

    private bool IsTileInMoveRange(Unit unit, Tile target)
    {
        if (unit == null || target == null || unit.CurrentTile == null) return false;

        Vector2Int a = unit.CurrentTile.GridPosition;
        Vector2Int b = target.GridPosition;

        int dist = Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        return dist > 0 && dist <= unit.MovesLeftThisTurn;
    }

    private void SpawnMarker(Tile tile)
    {
        if (moveMarkerPrefab == null) return;

        Vector3 p = tile.transform.position;
        p.y = tile.TopHeight + markerYOffset;

        GameObject m = Instantiate(moveMarkerPrefab, p, Quaternion.identity);
        m.name = $"MoveMarker_{tile.GridPosition.x}_{tile.GridPosition.y}";
        markers.Add(m);
    }

    private void ClearMarkers()
    {
        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] != null) Destroy(markers[i]);
        }
        markers.Clear();
    }

    private Tile FindTileAt(Vector2Int gridPos)
    {
        // Надёжно для текущего масштаба проекта: просто ищем в сцене.
        // Позже можно заменить на словарь из MapGenerator.
        Tile[] tiles = FindObjectsOfType<Tile>();
        foreach (var t in tiles)
        {
            if (t.GridPosition == gridPos) return t;
        }
        return null;
    }

    private Unit FindUnitOnTile(Tile tile)
    {
        if (tile == null) return null;

        Unit[] units = FindObjectsOfType<Unit>();
        foreach (var u in units)
        {
            if (u != null && u.CurrentTile == tile)
                return u;
        }
        return null;
    }
}
