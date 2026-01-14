using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

    private readonly Dictionary<PlayerId, Tile> startCityByPlayer = new Dictionary<PlayerId, Tile>();

    public Tile GetStartCityTile(PlayerId player)
    {
        startCityByPlayer.TryGetValue(player, out var t);
        return t;
    }

    private void Awake()
    {
        Instance = this;
    }

    [Header("Размер карты")]
    public int width = 20;
    public int height = 20;
    public float tileSize = 1f;

    [Header("Плитки")]
    public Tile tilePrefab;

    [Header("Материалы по типу ландшафта (для Surface)")]
    public Material grassMaterial;
    public Material waterMaterial;
    public Material forestMaterial;
    public Material mountainMaterial;

    [Header("Префабы декораций")]
    public GameObject[] grassTreePrefabs;      // маленькие деревья на полях
    public GameObject[] grassPlantPrefabs;     // трава на полях
    public GameObject[] grassFlowerPrefabs;    // цветы на полях
    public GameObject[] forestTreePrefabs;     // деревья в лесу
    public GameObject[] forestGrassPrefabs;    // трава в лесу
    public GameObject[] mountainDecorPrefabs;  // скалы/горы сверху
    public GameObject[] waterDecorPrefabs;     // по желанию

    [Header("Количество декораций на тайл")]
    public Vector2Int grassTreesCount    = new Vector2Int(0, 2);  // 0–2 деревьев на полях
    public Vector2Int forestTreesCount   = new Vector2Int(3, 5);  // 3–5 деревьев в лесу
    public Vector2Int grassPlantsCount   = new Vector2Int(5, 7);  // 5–7 пучков травы на поле
    public Vector2Int grassFlowersCount  = new Vector2Int(3, 4);  // 3–4 цветка на поле
    public Vector2Int forestGrassPerTile = new Vector2Int(4, 7);  // 4–7 пучков травы в лесу

    [Header("Разброс декора по тайлу (по XZ)")]
    public float decorationSpread = 0.7f; // 0.7 тайла по ширине

    [Header("Шум для генерации карты")]
    public float noiseScale = 0.12f;
    public int seed = 12345;

    [Range(0f, 1f)]
    public float waterThreshold = 0.3f;
    [Range(0f, 1f)]
    public float mountainThreshold = 0.68f;
    [Range(0f, 1f)]
    public float forestThreshold = 0.55f;

    [Header("ВЫСОТА ТАЙЛОВ (ВЕРХА) ПО ТИПУ")]
    // Это высота ВЕРХА тайла (Surface) относительно низа земли (groundBottomY)
    public float grassHeight    = 0.25f;
    public float forestHeight   = 0.3f;
    public float mountainHeight = 0.8f;
    public float waterHeight    = 0.15f;

    [Header("Смещение декораций по высоте над тайлом")]
    public float grassDecorationOffset       = 0.006f; // трава/цветы на поле
    public float fieldTreeDecorationOffset   = 0.02f;  // деревья на полях
    public float forestTreeDecorationOffset  = 0.3f;   // деревья леса
    public float forestGrassDecorationOffset = 0.02f;  // трава в лесу
    public float mountainDecorationOffset    = 0.15f;
    public float waterDecorationOffset       = 0.02f;

    [Header("Нижний уровень земли (дно всех столбиков)")]
    public float groundBottomY = -1f;

    [Header("Города")]
    public GameObject cityPrefab;
    [Range(1, 5)]
    public int cityTerritoryRadius = 2;
    [Range(0f, 1f)]
    public float cityTerritoryAlpha = 0.55f;

    [Header("Игроки")]
    public PlayerId startingPlayer = PlayerId.Player1; // владелец стартового города

    public bool spawnSecondStartingCity = true;
    public PlayerId secondStartingPlayer = PlayerId.Player2; // владелец второго стартового города

    [Header("Resource deposits (поля/общие префабы)")]
    public GameObject[] goldDepositPrefabs;   // золото на равнинах/в лесу
    public GameObject[] coalDepositPrefabs;   // уголь на равнинах/в лесу

    [Header("Resource deposits для гор")]
    public GameObject[] goldMountainDepositPrefabs; // золото в горах
    public GameObject[] coalMountainDepositPrefabs; // уголь в горах

    public int maxGoldDeposits = 6;
    public int maxCoalDeposits = 6;
    private Tile[,] tiles;

    private void Start()
    {
        // При каждом запуске новой карты очищаем цвета игроков,
        // чтобы они распределялись заново с начала пула.
        PlayerColorManager.Reset();

        GenerateMap();
    }

    public void GenerateMap()
    {
        ClearOldMap();

        tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileTerrainType terrain = GetTerrainFromNoise(x, y);

                // все тайлы стоят на одном уровне по Y (0),
                // разница высот задаётся через Ground/Surface
                Vector3 worldPos = new Vector3(
                    x * tileSize,
                    0f,
                    y * tileSize
                );

                Tile tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                tile.Init(new Vector2Int(x, y), terrain);

                ApplyHeightToTile(tile, terrain);
                ApplyTerrainVisual(tile, terrain);
                SpawnDecorations(tile, terrain);

                tiles[x, y] = tile;
            }
        }

        // 1) создаём стартовый город и фокусируем камеру
        Tile firstCity = SpawnStartingCity(startingPlayer);

        if (spawnSecondStartingCity)
        {
            SpawnStartingCity(secondStartingPlayer);
        }

        // камера — на стартового игрока
        CameraController cam = FindObjectOfType<CameraController>();
        if (cam != null && firstCity != null)
        {
            cam.JumpToPosition(firstCity.transform.position);
        }


        // 2) генерируем месторождения
        GenerateResourceDeposits();
    }

    private void ClearOldMap()
    {
        if (tiles == null) return;

        foreach (Tile tile in tiles)
        {
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }
        }
    }

    // ====== ТИП ЛАНДШАФТА ПО ШУМУ ======
    private TileTerrainType GetTerrainFromNoise(int x, int y)
    {
        float nx = (x + seed) * noiseScale;
        float ny = (y + seed) * noiseScale;
        float baseNoise = Mathf.PerlinNoise(nx, ny);

        float detailNoise = Mathf.PerlinNoise(nx * 2f + 100f, ny * 2f + 100f);
        float randomFactor = Random.Range(-0.07f, 0.07f);

        float noise = baseNoise * 0.75f + detailNoise * 0.25f + randomFactor;
        noise = Mathf.Clamp01(noise);

        if (noise < waterThreshold)
            return TileTerrainType.Water;

        if (noise > mountainThreshold)
            return TileTerrainType.Mountain;

        if (detailNoise > forestThreshold)
            return TileTerrainType.Forest;

        return TileTerrainType.Grass;
    }

    // ====== ВЫСОТА ВЕРХА ТАЙЛА ПО ТИПУ ======
    private float GetTopHeightForTerrain(TileTerrainType terrain)
    {
        switch (terrain)
        {
            case TileTerrainType.Water:    return waterHeight;
            case TileTerrainType.Mountain: return mountainHeight;
            case TileTerrainType.Forest:   return forestHeight;
            case TileTerrainType.Grass:
            default:                       return grassHeight;
        }
    }

    // ====== ПРИМЕНЕНИЕ ВЫСОТЫ К Ground + Surface (+ TerritoryOverlay) ======
    private void ApplyHeightToTile(Tile tile, TileTerrainType terrain)
    {
        Transform ground = tile.transform.Find("Ground");
        Transform surface = tile.transform.Find("Surface");

        if (ground == null || surface == null)
        {
            Debug.LogWarning("Tile is missing Ground or Surface child!", tile);
            return;
        }

        // высота ВЕРХА тайла (где должна быть верхняя грань Surface) в мировых координатах
        float topHeight = Mathf.Max(0.05f, GetTopHeightForTerrain(terrain));

        // текущие масштабы
        Vector3 gScale = ground.localScale;
        Vector3 sScale = surface.localScale;

        float surfaceThickness = sScale.y; // толщина верхней плитки

        // высота коричневого столбика от дна до низа поверхности
        float groundHeight = Mathf.Max(0.1f, topHeight - surfaceThickness - groundBottomY);

        // масштаб Ground по Y
        gScale.y = groundHeight;
        ground.localScale = gScale;

        // центр Ground: от дна поднимаемся на половину высоты
        float groundCenterY = groundBottomY + groundHeight * 0.5f;
        ground.localPosition = new Vector3(0f, groundCenterY, 0f);

        // центр Surface: на topHeight минус половину толщины плитки
        float surfaceCenterY = topHeight - surfaceThickness * 0.5f;
        surface.localPosition = new Vector3(0f, surfaceCenterY, 0f);

        // двигаем TerritoryOverlay на ту же высоту, чуть выше поверхности
        if (tile.territoryRenderer != null)
        {
            Transform terr = tile.territoryRenderer.transform;
            Vector3 lp = terr.localPosition;
            lp.y = surfaceCenterY + 0.01f;
            terr.localPosition = lp;
        }

        tile.SetTopHeight(topHeight);
    }

    // ====== МАТЕРИАЛ ДЛЯ SURFACE ======
    private void ApplyTerrainVisual(Tile tile, TileTerrainType terrain)
    {
        Transform surfaceTransform = tile.transform.Find("Surface");
        if (surfaceTransform == null)
        {
            Debug.LogWarning("Tile doesn't have child 'Surface'!", tile);
            return;
        }

        Renderer surfaceRenderer = surfaceTransform.GetComponent<Renderer>();
        if (surfaceRenderer == null)
        {
            Debug.LogWarning("'Surface' has no Renderer!", surfaceTransform);
            return;
        }

        switch (terrain)
        {
            case TileTerrainType.Grass:
                surfaceRenderer.material = grassMaterial;
                break;
            case TileTerrainType.Water:
                surfaceRenderer.material = waterMaterial;
                break;
            case TileTerrainType.Forest:
                surfaceRenderer.material = forestMaterial;
                break;
            case TileTerrainType.Mountain:
                surfaceRenderer.material = mountainMaterial;
                break;
        }
    }

    // ====== СПАВН ДЕКОРАЦИЙ ПО ТИПУ ТАЙЛА ======
    private void SpawnDecorations(Tile tile, TileTerrainType terrain)
    {
        switch (terrain)
        {
            case TileTerrainType.Grass:
                // трава (5–7)
                if (grassPlantPrefabs != null && grassPlantPrefabs.Length > 0)
                {
                    int count = Random.Range(grassPlantsCount.x, grassPlantsCount.y + 1);
                    for (int i = 0; i < count; i++)
                    {
                        SpawnSingleDecoration(tile, grassPlantPrefabs, grassDecorationOffset, true);
                    }
                }

                // цветы (3–4)
                if (grassFlowerPrefabs != null && grassFlowerPrefabs.Length > 0)
                {
                    int count = Random.Range(grassFlowersCount.x, grassFlowersCount.y + 1);
                    for (int i = 0; i < count; i++)
                    {
                        SpawnSingleDecoration(tile, grassFlowerPrefabs, grassDecorationOffset, true);
                    }
                }

                // маленькие деревья на полях (0–2)
                if (grassTreePrefabs != null && grassTreePrefabs.Length > 0)
                {
                    int count = Random.Range(grassTreesCount.x, grassTreesCount.y + 1);
                    for (int i = 0; i < count; i++)
                    {
                        SpawnSingleDecoration(tile, grassTreePrefabs, fieldTreeDecorationOffset, true);
                    }
                }
                break;

            case TileTerrainType.Forest:
                // деревья в лесу (3–5)
                if (forestTreePrefabs != null && forestTreePrefabs.Length > 0)
                {
                    int count = Random.Range(forestTreesCount.x, forestTreesCount.y + 1);
                    for (int i = 0; i < count; i++)
                    {
                        SpawnSingleDecoration(tile, forestTreePrefabs, forestTreeDecorationOffset, true);
                    }
                }

                // трава в лесу
                if (forestGrassPrefabs != null && forestGrassPrefabs.Length > 0)
                {
                    int count = Random.Range(forestGrassPerTile.x, forestGrassPerTile.y + 1);
                    for (int i = 0; i < count; i++)
                    {
                        SpawnSingleDecoration(tile, forestGrassPrefabs, forestGrassDecorationOffset, true);
                    }
                }
                break;

            case TileTerrainType.Mountain:
                if (mountainDecorPrefabs != null && mountainDecorPrefabs.Length > 0)
                {
                    int count = Random.Range(1, 2); // пока одна "шапка"
                    for (int i = 0; i < count; i++)
                    {
                        SpawnSingleDecoration(tile, mountainDecorPrefabs, mountainDecorationOffset, false);
                    }
                }
                break;

            case TileTerrainType.Water:
                if (waterDecorPrefabs != null && waterDecorPrefabs.Length > 0)
                {
                    int count = Random.Range(0, 2); // редкий декор на воде
                    for (int i = 0; i < count; i++)
                    {
                        SpawnSingleDecoration(tile, waterDecorPrefabs, waterDecorationOffset, true);
                    }
                }
                break;
        }
    }

    // ====== СПАВН ОДНОЙ ДЕКОРАЦИИ НА ТАЙЛ ======
    private void SpawnSingleDecoration(Tile tile, GameObject[] prefabArray, float offset, bool randomizePosition)
    {
        if (prefabArray == null || prefabArray.Length == 0)
            return;

        GameObject prefab = prefabArray[Random.Range(0, prefabArray.Length)];
        if (prefab == null) return;

        Transform surface = tile.transform.Find("Surface");
        Vector3 spawnPos = tile.transform.position;

        if (surface != null)
        {
            Renderer surfaceRenderer = surface.GetComponent<Renderer>();
            if (surfaceRenderer != null)
            {
                float topY = surfaceRenderer.bounds.max.y;
                spawnPos = new Vector3(
                    surface.position.x,
                    topY + offset,
                    surface.position.z
                );
            }
            else
            {
                spawnPos = surface.position + Vector3.up * offset;
            }
        }
        else
        {
            spawnPos = tile.transform.position + Vector3.up * offset;
        }

        // случайный сдвиг по XZ внутри тайла
        if (randomizePosition)
        {
            float halfSpread = decorationSpread * 0.5f;
            float dx = Random.Range(-halfSpread, halfSpread);
            float dz = Random.Range(-halfSpread, halfSpread);
            spawnPos += new Vector3(dx, 0f, dz);
        }

        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity, tile.transform);

        // случайный поворот по Y
        instance.transform.Rotate(0f, Random.Range(0f, 360f), 0f);

        tile.RegisterDecoration(instance);
    }

    // ====== СОЗДАНИЕ СТАРТОВОГО ГОРОДА И ФОКУС КАМЕРЫ ======
    private Tile SpawnStartingCity(PlayerId owner)
    {
        if (cityPrefab == null)
        {
            Debug.LogWarning("City prefab is not assigned in MapGenerator!");
            return null;
        }

        // минимальная дистанция между стартовыми городами (в Чебышёвской метрике)
        // Радиус территории 2 => города должны быть минимум на 5 клеток "по сетке" друг от друга
        int minDistance = cityTerritoryRadius * 2 + 1;

        // 1) собираем кандидатов: не вода, не горы, не занято зданием
        List<Tile> candidates = new List<Tile>();
        foreach (Tile t in tiles)
        {
            if (t == null) continue;
            if (t.TerrainType == TileTerrainType.Water) continue;
            if (t.TerrainType == TileTerrainType.Mountain) continue;
            if (t.HasBuilding) continue;

            // 2) если уже есть стартовый город другого игрока — не ставим рядом
            bool tooClose = false;
            foreach (var kv in startCityByPlayer)
            {
                Tile other = kv.Value;
                if (other == null) continue;

                int dx = Mathf.Abs(other.GridPosition.x - t.GridPosition.x);
                int dy = Mathf.Abs(other.GridPosition.y - t.GridPosition.y);

                if (Mathf.Max(dx, dy) < minDistance) // строго ближе чем minDistance
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            candidates.Add(t);
        }

        // если кандидатов нет (карта маленькая/горы/вода) — делаем fallback без ограничения дистанции
        if (candidates.Count == 0)
        {
            foreach (Tile t in tiles)
            {
                if (t == null) continue;
                if (t.TerrainType == TileTerrainType.Water) continue;
                if (t.TerrainType == TileTerrainType.Mountain) continue;
                if (t.HasBuilding) continue;
                candidates.Add(t);
            }
        }

        if (candidates.Count == 0)
            return null;

        Tile cityTile = candidates[Random.Range(0, candidates.Count)];

        // позиция города — чуть над верхом тайла
        Vector3 pos = cityTile.transform.position;
        pos.y = cityTile.TopHeight + 0.01f;

        Instantiate(cityPrefab, pos, Quaternion.identity, cityTile.transform);

        // помечаем тайл
        cityTile.SetCityPresent(true);
        cityTile.SetOwner(owner);

        // цвет территории — цвет игрока
        Color territoryColor = PlayerColorManager.GetColor(owner);
        territoryColor.a = cityTerritoryAlpha;

        // красим тайлы в радиусе территории
        int cx = cityTile.GridPosition.x;
        int cy = cityTile.GridPosition.y;

        for (int dx = -cityTerritoryRadius; dx <= cityTerritoryRadius; dx++)
        {
            for (int dy = -cityTerritoryRadius; dy <= cityTerritoryRadius; dy++)
            {
                int nx = cx + dx;
                int ny = cy + dy;

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                Tile t = tiles[nx, ny];
                if (t == null) continue;

                t.SetTerritoryColor(territoryColor);
                t.SetOwner(owner);
            }
        }

        startCityByPlayer[owner] = cityTile;
        return cityTile;
    }


    

    // ====== Генерация ресурсных месторождений ======
    private void GenerateResourceDeposits()
    {
        bool hasGoldPrefabs =
            (goldDepositPrefabs != null && goldDepositPrefabs.Length > 0) ||
            (goldMountainDepositPrefabs != null && goldMountainDepositPrefabs.Length > 0);

        bool hasCoalPrefabs =
            (coalDepositPrefabs != null && coalDepositPrefabs.Length > 0) ||
            (coalMountainDepositPrefabs != null && coalMountainDepositPrefabs.Length > 0);

        if (!hasGoldPrefabs || !hasCoalPrefabs)
        {
            Debug.LogWarning("Deposit prefabs are not assigned for gold/coal!");
            return;
        }

        List<Tile> goldCandidates = new List<Tile>();
        List<Tile> coalCandidates = new List<Tile>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile t = tiles[x, y];
                if (t == null) continue;

                if (t.TerrainType == TileTerrainType.Water) continue; // не на воде
                if (t.HasBuilding) continue;                           // не под городом
                if (t.HasResourceDeposit) continue;                    // не дублировать

                // В горах — только на краю "грядки"
                if (t.TerrainType == TileTerrainType.Mountain &&
                    !IsMountainEdgeTile(x, y))
                {
                    continue;
                }

                // Под месторождения подходят и поля, и леса, и край гор
                goldCandidates.Add(t);
                coalCandidates.Add(t);
            }
        }

        // глобальный спавн золота
        int goldToSpawn = Mathf.Min(maxGoldDeposits, goldCandidates.Count);
        for (int i = 0; i < goldToSpawn; i++)
        {
            int index = Random.Range(0, goldCandidates.Count);
            Tile t = goldCandidates[index];
            PlaceResourceDeposit(t,
                goldDepositPrefabs,
                goldMountainDepositPrefabs,
                ResourceType.Gold);
            goldCandidates.RemoveAt(index);
        }

        // глобальный спавн угля
        int coalToSpawn = Mathf.Min(maxCoalDeposits, coalCandidates.Count);
        for (int i = 0; i < coalToSpawn; i++)
        {
            int index = Random.Range(0, coalCandidates.Count);
            Tile t = coalCandidates[index];
            PlaceResourceDeposit(t,
                coalDepositPrefabs,
                coalMountainDepositPrefabs,
                ResourceType.Coal);
            coalCandidates.RemoveAt(index);
        }

        // гарантируем, что на территории стартового игрока
        // есть хотя бы по одному месторождению каждого типа
        EnsureDepositInPlayerTerritory(
            ResourceType.Gold,
            goldDepositPrefabs,
            goldMountainDepositPrefabs,
            startingPlayer);

        EnsureDepositInPlayerTerritory(
            ResourceType.Coal,
            coalDepositPrefabs,
            coalMountainDepositPrefabs,
            startingPlayer);

            // ✅ То же самое для второго стартового игрока (если включён)
        if (spawnSecondStartingCity)
        {
            EnsureDepositInPlayerTerritory(
                ResourceType.Gold,
                goldDepositPrefabs,
                goldMountainDepositPrefabs,
                secondStartingPlayer);

            EnsureDepositInPlayerTerritory(
                ResourceType.Coal,
                coalDepositPrefabs,
                coalMountainDepositPrefabs,
                secondStartingPlayer);
        }

    }

    private bool IsMountainEdgeTile(int x, int y)
    {
        Tile t = tiles[x, y];
        if (t == null || t.TerrainType != TileTerrainType.Mountain)
            return false;

        // соседние клетки (4 направления)
        int[] dx = new int[] { -1, 1, 0, 0 };
        int[] dy = new int[] { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];

            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                continue;

            Tile n = tiles[nx, ny];
            if (n == null) continue;

            // если рядом не гора — значит, этот горный тайл на краю
            if (n.TerrainType != TileTerrainType.Mountain)
                return true;
        }

        return false;
    }

    private void EnsureDepositInPlayerTerritory(
        ResourceType type,
        GameObject[] genericPrefabs,
        GameObject[] mountainPrefabs,
        PlayerId player)
    {
        bool alreadyHas = false;
        List<Tile> candidates = new List<Tile>();

        foreach (Tile t in tiles)
        {
            if (t == null) continue;
            if (t.Owner != player) continue;

            if (t.HasResourceDeposit && t.ResourceDeposit.type == type)
            {
                alreadyHas = true;
                break;
            }

            if (t.HasBuilding) continue;
            if (t.TerrainType == TileTerrainType.Water) continue;
            if (t.HasResourceDeposit) continue;

            if (t.TerrainType == TileTerrainType.Mountain &&
                !IsMountainEdgeTile(t.GridPosition.x, t.GridPosition.y))
                continue;

            candidates.Add(t);
        }

        if (alreadyHas) return;
        if (candidates.Count == 0) return;

        Tile target = candidates[Random.Range(0, candidates.Count)];
        PlaceResourceDeposit(target, genericPrefabs, mountainPrefabs, type);
    }

    private void PlaceResourceDeposit(
        Tile tile,
        GameObject[] genericPrefabs,
        GameObject[] mountainPrefabs,
        ResourceType type)
    {
        if (tile == null) return;
        if (tile.HasResourceDeposit) return;

        // выбираем правильный пул префабов
        GameObject[] sourceArray = genericPrefabs;

        if (tile.TerrainType == TileTerrainType.Mountain &&
            mountainPrefabs != null && mountainPrefabs.Length > 0)
        {
            sourceArray = mountainPrefabs;
        }

        if (sourceArray == null || sourceArray.Length == 0) return;

        GameObject prefab = sourceArray[Random.Range(0, sourceArray.Length)];
        if (prefab == null) return;

        // Если это горный тайл — убираем горный декор,
        // чтобы месторождение не утонуло в скале.
        if (tile.TerrainType == TileTerrainType.Mountain)
        {
            foreach (var deco in tile.Decorations)
            {
                if (deco != null)
                    Object.Destroy(deco);
            }
            tile.Decorations.Clear();
        }

        // ставим месторождение по центру тайла, чуть над верхом тайла
        Vector3 pos = tile.transform.position;
        pos.y = tile.TopHeight + 0.02f;   // УЧЁТ ВЫСОТЫ ТАЙЛА ДЛЯ ЛЮБОГО БИОМА

        GameObject go = Instantiate(prefab, pos, Quaternion.identity, tile.transform);

        ResourceDeposit deposit = go.GetComponent<ResourceDeposit>();
        if (deposit == null)
        {
            deposit = go.AddComponent<ResourceDeposit>();
        }

        deposit.Init(tile, type);
        tile.SetResourceDeposit(deposit);

        // хотим, чтобы под юнитом/зданием месторождение скрывалось
        tile.RegisterDecoration(go);
    }
}
