using UnityEngine;

public class MapGenerator : MonoBehaviour
{
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
    public GameObject[] grassTreePrefabs;   // маленькие деревья на полях
    public GameObject[] grassPlantPrefabs;    // трава
    public GameObject[] grassFlowerPrefabs;   // цветы
    public GameObject[] forestTreePrefabs;    // деревья
    public GameObject[] mountainDecorPrefabs; // скалы/горы сверху
    public GameObject[] waterDecorPrefabs;    // по желанию

    [Header("Количество декораций на тайл")]
    public Vector2Int grassTreesCount = new Vector2Int(0, 2);  // новые маленькие деревья на полях
    public Vector2Int forestTreesCount = new Vector2Int(3, 5);     // 3–5 деревьев
    public Vector2Int grassPlantsCount = new Vector2Int(5, 7);     // 5–7 травы
    public Vector2Int grassFlowersCount = new Vector2Int(3, 4);    // 3–4 цветка

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
    // Это высота ВЕРХА тайла (Surface) относительно низа земли (0)
    public float grassHeight = 0.25f;
    public float forestHeight = 0.3f;
    public float mountainHeight = 0.8f;
    public float waterHeight = 0.15f;

    [Header("Смещение декораций по высоте над тайлом")]
    public float grassDecorationOffset = 0.03f;
    public float forestDecorationOffset = 0.04f;
    public float mountainDecorationOffset = 0.06f;
    public float waterDecorationOffset = 0.02f;

    [Header("Нижний уровень земли (дно всех столбиков)")]
    public float groundBottomY = -1f;

    private Tile[,] tiles;

    private void Start()
    {
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

                // ВСЕ ТАЙЛЫ СТОЯТ НА ОДНОМ УРОВНЕ ПО Y
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

    // ====== ПРИМЕНЕНИЕ ВЫСОТЫ К Ground + Surface ======
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
    // (насколько Ground "высокий")
    float groundHeight = Mathf.Max(0.1f, topHeight - surfaceThickness - groundBottomY);

    // масштаб Ground по Y
    gScale.y = groundHeight;
    ground.localScale = gScale;

    // центр Ground: от дна поднимаемся на половину высоты
    float groundCenterY = groundBottomY + groundHeight * 0.5f;
    ground.localPosition = new Vector3(0f, groundCenterY, 0f);

    // центр Surface: на topHeight минус половина толщины плитки
    float surfaceCenterY = topHeight - surfaceThickness * 0.5f;
    surface.localPosition = new Vector3(0f, surfaceCenterY, 0f);
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

    // ====== СПАВН ДЕКОРАЦИЙ (С УЧЁТОМ ТИПА И OFFSET) ======
        // ====== СПАВН НЕСКОЛЬКИХ ДЕКОРАЦИЙ НА ТАЙЛ ======
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
                    SpawnSingleDecoration(tile, grassTreePrefabs, forestDecorationOffset, true);
                }
            }
            break;

        case TileTerrainType.Forest:
            // лесные деревья (3–5)
            if (forestTreePrefabs != null && forestTreePrefabs.Length > 0)
            {
                int count = Random.Range(forestTreesCount.x, forestTreesCount.y + 1);
                for (int i = 0; i < count; i++)
                {
                    SpawnSingleDecoration(tile, forestTreePrefabs, forestDecorationOffset, true);
                }
            }
            break;

        case TileTerrainType.Mountain:
            if (mountainDecorPrefabs != null && mountainDecorPrefabs.Length > 0)
            {
                int count = Random.Range(1, 2); // одна "шапка"
                for (int i = 0; i < count; i++)
                {
                    SpawnSingleDecoration(tile, mountainDecorPrefabs, mountainDecorationOffset, false);
                }
            }
            break;

        case TileTerrainType.Water:
            if (waterDecorPrefabs != null && waterDecorPrefabs.Length > 0)
            {
                int count = Random.Range(0, 2); // редко
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

        // случайный поворот
        instance.transform.Rotate(0f, Random.Range(0f, 360f), 0f);

        tile.RegisterDecoration(instance);
    }

}
