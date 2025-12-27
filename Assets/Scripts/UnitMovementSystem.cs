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

    public void OnTileClicked(Tile tile)
    {
        if (tile == null) return;

        Unit u = FindUnitOnTile(tile);
        if (u != null)
        {
            SelectUnit(u);
            return;
        }

        if (selectedUnit != null)
        {
            bool moved = TryMoveSelectedUnitTo(tile);

            // клик вне радиуса/нельзя ходить -> снять выделение
            if (!moved)
                ClearSelection();
        }
    }

    public void ClearSelection()
    {
        selectedUnit = null;
        ClearMarkers();
    }

    /// <summary>
    /// Вызывать при смене выделенного тайла (select/deselect),
    /// чтобы выбранный юнит не "висел" в воздухе, когда тайл поднялся/опустился.
    /// </summary>
    public void OnTileSelectionChanged()
    {
        if (selectedUnit == null) return;
        if (moveRoutine != null) return;

        selectedUnit.SnapToCurrentTile();
    }

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

        if (selectedUnit != null)
            ShowMoveMarkers(selectedUnit);
    }

    private void SelectUnit(Unit unit)
    {
        selectedUnit = unit;

        // ✅ на всякий случай сразу выровняем по тайлу
        selectedUnit.SetMoving(false);
        selectedUnit.SnapToCurrentTile();

        ShowMoveMarkers(unit);
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
        unit.SetMoving(true);

        Vector3 from = unit.transform.position;
        Vector3 to = unit.GetWorldPositionOnTile(target);

        unit.FaceDirection(to - from);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, moveDuration);
            float tt = Mathf.SmoothStep(0f, 1f, t);
            unit.transform.position = Vector3.Lerp(from, to, tt);
            yield return null;
        }

        unit.transform.position = to;

        unit.SetTile(target, instant: true);
        unit.SpendMovePoint(cost);

        unit.SetMoving(false);
        unit.SnapToCurrentTile();

        ShowMoveMarkers(unit);

        moveRoutine = null;
    }

    private void ShowMoveMarkers(Unit unit)
    {
        ClearMarkers();

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

    private int GetChebyshevDistance(Tile a, Tile b)
    {
        if (a == null || b == null) return 0;
        Vector2Int pa = a.GridPosition;
        Vector2Int pb = b.GridPosition;
        return Mathf.Max(Mathf.Abs(pa.x - pb.x), Mathf.Abs(pa.y - pb.y));
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
