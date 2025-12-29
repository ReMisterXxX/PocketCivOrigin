using System.Collections.Generic;
using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerResources playerResources;

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
    }

    // =========================
    // Public API
    // =========================

    public bool CanBuildCity(Tile tile, out string reason)
    {
        reason = "";
        if (tile == null) { reason = "No tile."; return false; }

        // нельзя строить на территории кого-либо (своей/вражеской)
        if (tile.Owner != PlayerId.None)
        {
            reason = "Tile already belongs to a territory.";
            return false;
        }

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

        if (playerResources != null && !playerResources.TrySpend(cityGoldCost, cityCoalCost))
        {
            reason = "Can't spend resources.";
            return false;
        }

        PlayerId p = playerResources != null ? playerResources.CurrentPlayer : PlayerId.Player1;

        // базовый цвет игрока
        Color col = PlayerColorManager.GetColor(p);

        // ✅ приводим alpha к тайловому (чтобы выглядело как стартовый город)
        col.a = tile != null ? tile.territoryAlpha : 0.55f;

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

        reason = "";
        return true;
    }

    public bool CanBuildMine(Tile tile, out string reason)
    {
        reason = "";
        if (tile == null) { reason = "No tile."; return false; }

        PlayerId p = playerResources != null ? playerResources.CurrentPlayer : PlayerId.Player1;
        if (tile.Owner != p)
        {
            reason = "Mine can be built only on your territory.";
            return false;
        }

        if (!tile.HasResourceDeposit || tile.ResourceDeposit == null)
        {
            reason = "No deposit here.";
            return false;
        }

        if (tile.HasBuilding || tile.HasCity)
        {
            reason = "Tile already has a building.";
            return false;
        }

        if (tile.ResourceDeposit.HasMine)
        {
            reason = "Mine already built.";
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

        GameObject prefab = null;
        if (tile.ResourceDeposit.type == ResourceType.Gold) prefab = goldMinePrefab;
        if (tile.ResourceDeposit.type == ResourceType.Coal) prefab = coalMinePrefab;

        if (prefab == null)
        {
            reason = "Mine prefab not set.";
            return false;
        }

        Vector3 pos = GetBuildWorldPos(tile);

        GameObject mine = Instantiate(prefab, pos, Quaternion.identity, tile.transform);
        spawnedBuildings[tile] = mine;

        tile.SetBuildingPresent(true);

        // ✅ депозит НЕ должен отключаться (Tile.cs теперь это гарантирует),
        // а доход должен вырасти
        tile.ResourceDeposit.SetMineBuilt(true);

        if (playerResources != null)
            playerResources.RecalculateIncome();

        reason = "";
        return true;
    }

    // =========================
    // Internal
    // =========================

    private Vector3 GetBuildWorldPos(Tile tile)
    {
        Vector3 p = tile.transform.position;
        p.y = tile.TopHeight + buildingYOffset;
        return p;
    }

    private void CaptureTerritory(Tile center, PlayerId owner, Color colorWithAlpha)
    {
        if (center == null) return;

        // центр тоже захватываем
        if (center.Owner == PlayerId.None)
        {
            center.SetOwner(owner);
            center.SetTerritoryColor(colorWithAlpha);
        }

        Vector2Int o = center.GridPosition;

        for (int dx = -cityCaptureRadius; dx <= cityCaptureRadius; dx++)
        {
            for (int dy = -cityCaptureRadius; dy <= cityCaptureRadius; dy++)
            {
                int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                if (dist == 0 || dist > cityCaptureRadius) continue;

                Vector2Int gp = o + new Vector2Int(dx, dy);

                if (!Tile.TryGetTile(gp, out Tile t) || t == null)
                    continue;

                // "обтекание": не трогаем уже занятую территорию
                if (t.Owner != PlayerId.None)
                    continue;

                t.SetOwner(owner);

                // ✅ гарантируем одинаковую прозрачность на всех тайлах
                Color c = colorWithAlpha;
                c.a = t.territoryAlpha;
                t.SetTerritoryColor(c);
            }
        }
    }
}
