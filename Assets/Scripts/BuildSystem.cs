using System.Collections.Generic;
using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerResources playerResources;
    [SerializeField] private UnitMovementSystem unitMovementSystem;

    [Header("Prefabs")]
    [SerializeField] private GameObject cityPrefab;
    [SerializeField] private GameObject goldMinePrefab;
    [SerializeField] private GameObject coalMinePrefab;

    [Header("Costs")]
    [SerializeField] private int cityGoldCost = 20;
    [SerializeField] private int cityCoalCost = 0;

    [SerializeField] private int mineGoldCost = 0;
    [SerializeField] private int mineCoalCost = 5;

    [Header("City Territory")]
    [SerializeField] private int cityCaptureRadius = 2;

    [Header("Placement")]
    [SerializeField] private float buildingYOffset = 0f;

    private readonly Dictionary<Tile, GameObject> spawnedBuildings = new Dictionary<Tile, GameObject>();

    private void Awake()
    {
        if (playerResources == null)
            playerResources = FindObjectOfType<PlayerResources>();

        if (unitMovementSystem == null)
            unitMovementSystem = FindObjectOfType<UnitMovementSystem>();
    }

    public bool CanBuildCity(Tile tile, out string reason)
    {
        reason = "";
        if (tile == null) { reason = "No tile."; return false; }

        // ✅ город только если на тайле есть юнит
        Unit unit = FindUnitOnTile(tile);
        if (unit == null)
        {
            reason = "You need a unit on this tile to found a city.";
            return false;
        }

        // ✅ только юнит текущего игрока (мягкая проверка — не ломает если owner у юнита нет)
        if (!IsUnitOwnedByCurrentPlayer(unit))
        {
            reason = "Only your unit can found a city.";
            return false;
        }

        // нельзя на чужой/своей территории
        if (tile.Owner != PlayerId.None)
        {
            reason = "Tile already belongs to a territory.";
            return false;
        }

        // нельзя на тайл, где уже есть город/здание
        if (tile.HasCity || tile.HasBuilding)
        {
            reason = "Tile already has a building.";
            return false;
        }

        if (tile.TerrainType == TileTerrainType.Water)
        {
            reason = "Can't build city on water.";
            return false;
        }

        if (playerResources != null && !playerResources.CanAfford(cityGoldCost, cityCoalCost))
        {
            reason = $"Not enough resources (City costs {cityGoldCost}G / {cityCoalCost}C).";
            return false;
        }

        return true;
    }

    public bool TryBuildCity(Tile tile, out string reason)
    {
        if (!CanBuildCity(tile, out reason))
            return false;

        // ✅ Сначала тратим ресурсы
        if (playerResources != null && !playerResources.TrySpend(cityGoldCost, cityCoalCost))
        {
            reason = "Can't spend resources.";
            return false;
        }

        // ✅ форсим обновление UI (не меняя ресурсы)
        if (playerResources != null)
        {
            playerResources.AddGold(0);
            playerResources.AddCoal(0);
        }

        // ✅ чистим выбор (на всякий)
        if (unitMovementSystem != null)
            unitMovementSystem.ClearSelection();

        PlayerId p = playerResources != null ? playerResources.CurrentPlayer : PlayerId.Player1;

        // базовый цвет игрока + альфа тайла
        Color col = PlayerColorManager.GetColor(p);
        col.a = tile != null ? tile.territoryAlpha : 0.55f;

        // ✅ убираем (уничтожаем) юнита-основателя
        Unit founder = FindUnitOnTile(tile);
        if (founder != null)
        {
            // 1) снять с тайла “по новой системе”
            try { tile.ClearUnit(founder); } catch { }

            // 2) снять флаг “по старой системе”
            try { tile.OnUnitLeave(); } catch { }

            Destroy(founder.gameObject);
        }

        // 1) ставим город
        if (cityPrefab != null)
        {
            Vector3 pos = GetBuildWorldPos(tile);
            GameObject city = Instantiate(cityPrefab, pos, Quaternion.identity, tile.transform);
            spawnedBuildings[tile] = city;
        }

        // 2) отмечаем тайл как город/постройка
        tile.SetCityPresent(true);

        // 3) захват территории (обтекаем уже занятые тайлы)
        CaptureTerritory(tile, p, col);

        // 4) пересчёт дохода
        if (playerResources != null)
            playerResources.RecalculateIncome();

        // ✅ форсим апдейт доходов на UI (без изменения значений)
        if (playerResources != null)
        {
            playerResources.AddGold(0);
            playerResources.AddCoal(0);
        }

        reason = "";
        return true;
    }

    public bool CanBuildMine(Tile tile, out string reason)
    {
        reason = "";
        if (tile == null) { reason = "No tile."; return false; }

        if (tile.Owner == PlayerId.None)
        {
            reason = "Mine must be built on captured territory.";
            return false;
        }

        if (playerResources != null && tile.Owner != playerResources.CurrentPlayer)
        {
            reason = "You can build only on your territory.";
            return false;
        }

        if (tile.HasCity || tile.HasBuilding)
        {
            reason = "Tile already has a building.";
            return false;
        }

        if (!tile.HasResourceDeposit || tile.ResourceDeposit == null)
        {
            reason = "Mine can be built only on a resource deposit.";
            return false;
        }

        if (playerResources != null && !playerResources.CanAfford(mineGoldCost, mineCoalCost))
        {
            reason = $"Not enough resources (Mine costs {mineGoldCost}G / {mineCoalCost}C).";
            return false;
        }

        return true;
    }

    public bool TryBuildMine(Tile tile, out string reason)
    {
        if (!CanBuildMine(tile, out reason))
            return false;

        if (playerResources != null && !playerResources.TrySpend(mineGoldCost, mineCoalCost))
        {
            reason = "Can't spend resources.";
            return false;
        }

        // ✅ форсим обновление UI (не меняя ресурсы)
        if (playerResources != null)
        {
            playerResources.AddGold(0);
            playerResources.AddCoal(0);
        }

        var dep = tile.ResourceDeposit;
        if (dep == null)
        {
            reason = "No deposit.";
            return false;
        }

        GameObject prefab = null;
        if (dep.type == ResourceType.Gold) prefab = goldMinePrefab;
        if (dep.type == ResourceType.Coal) prefab = coalMinePrefab;

        if (prefab == null)
        {
            reason = "Mine prefab not assigned for this deposit type.";
            return false;
        }

        Vector3 pos = GetBuildWorldPos(tile);
        GameObject mine = Instantiate(prefab, pos, Quaternion.identity, tile.transform);
        spawnedBuildings[tile] = mine;

        tile.SetBuildingPresent(true);

        tile.ResourceDeposit.SetMineBuilt(true);

        if (playerResources != null)
            playerResources.RecalculateIncome();

        // ✅ форсим апдейт доходов на UI
        if (playerResources != null)
        {
            playerResources.AddGold(0);
            playerResources.AddCoal(0);
        }

        reason = "";
        return true;
    }

    private Vector3 GetBuildWorldPos(Tile tile)
    {
        Vector3 anchor = tile.transform.position;
        anchor.y = tile.TopHeight + buildingYOffset;
        return anchor;
    }

    private Unit FindUnitOnTile(Tile tile)
    {
        if (tile == null) return null;

        if (tile.UnitOnTile != null) return tile.UnitOnTile;

        Unit[] units = FindObjectsOfType<Unit>();
        foreach (var u in units)
        {
            if (u != null && u.CurrentTile == tile)
                return u;
        }

        return null;
    }

    private bool IsUnitOwnedByCurrentPlayer(Unit unit)
    {
        if (unit == null) return false;
        if (playerResources == null) return true;

        PlayerId current = playerResources.CurrentPlayer;

        var t = unit.GetType();

        // пытаемся найти поле/свойство Owner (мягко, чтобы не ломать)
        var prop = t.GetProperty("Owner");
        if (prop != null && prop.PropertyType == typeof(PlayerId))
        {
            PlayerId val = (PlayerId)prop.GetValue(unit);
            return val.Equals(current);
        }

        var field = t.GetField("owner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(PlayerId))
        {
            PlayerId val = (PlayerId)field.GetValue(unit);
            return val.Equals(current);
        }

        // если owner не найден — считаем что свой (чтобы не сломать игру)
        return true;
    }

    private void CaptureTerritory(Tile center, PlayerId owner, Color color)
    {
        if (center == null) return;

        float a = center.territoryAlpha;
        Color colorWithAlpha = color;
        colorWithAlpha.a = a;

        Vector2Int c = center.GridPosition;

        for (int dx = -cityCaptureRadius; dx <= cityCaptureRadius; dx++)
        {
            for (int dy = -cityCaptureRadius; dy <= cityCaptureRadius; dy++)
            {
                int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                if (dist > cityCaptureRadius) continue;

                Vector2Int gp = c + new Vector2Int(dx, dy);

                if (!Tile.TryGetTile(gp, out Tile t) || t == null) continue;

                if (t.TerrainType == TileTerrainType.Water) continue;

                // обтекание — не трогаем уже занятую территорию
                if (t.Owner != PlayerId.None) continue;

                t.SetOwner(owner);

                Color cc = colorWithAlpha;
                cc.a = t.territoryAlpha;
                t.SetTerritoryColor(cc);
            }
        }
    }
}
