using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public enum TileTerrainType
{
    Grass,
    Water,
    Forest,
    Mountain
}

public class Tile : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public TileTerrainType TerrainType { get; private set; }

    // Владелец тайла (может быть None)
    public PlayerId Owner { get; private set; } = PlayerId.None;

    // список всех декораций на тайле
    public List<GameObject> Decorations { get; } = new List<GameObject>();

    public bool HasUnit { get; private set; }
    public bool HasBuilding { get; private set; }
    public float TopHeight { get; private set; }

    public void SetTopHeight(float value)
    {
        TopHeight = value;
    }

    public void SetOwner(PlayerId owner)
    {
        Owner = owner;
    }

    [Header("Territory")]
    public Renderer territoryRenderer;        // рендерер цветного "стекла" над тайлом
    [Range(0f, 1f)]
    public float territoryAlpha = 0.55f;      // запасной вариант прозрачности

    public Color? TerritoryColor { get; private set; }

    public void Init(Vector2Int gridPos, TileTerrainType terrain)
    {
        GridPosition = gridPos;
        TerrainType = terrain;

        gameObject.name = $"Tile_{gridPos.x}_{gridPos.y}";
    }

    public void RegisterDecoration(GameObject deco)
    {
        if (deco != null && !Decorations.Contains(deco))
        {
            Decorations.Add(deco);
        }
    }

    // ЮНИТ ЗАШЁЛ НА ТАЙЛ
    public void OnUnitEnter()
    {
        HasUnit = true;
        UpdateDecorationVisibility();
    }

    // ЮНИТ ПОКИНУЛ ТАЙЛ
    public void OnUnitLeave()
    {
        HasUnit = false;
        UpdateDecorationVisibility();
    }

    // ПОСТРОЙКА ПОЯВИЛАСЬ / ИСЧЕЗЛА
    public void SetBuildingPresent(bool hasBuilding)
    {
        HasBuilding = hasBuilding;
        UpdateDecorationVisibility();
    }

    private void UpdateDecorationVisibility()
    {
        bool shouldHide = HasUnit || HasBuilding;

        foreach (var deco in Decorations)
        {
            if (deco != null)
                deco.SetActive(!shouldHide);
        }
    }

    // ======== ВЫДЕЛЕНИЕ ТАЙЛА (С АНИМАЦИЕЙ) ========
    [Header("Selection")]
    public float selectionOffset = 0.3f;    // на сколько поднимать тайл
    public float selectionDuration = 0.12f; // длительность анимации

    private bool isSelected = false;
    private Vector3 baseTilePosition;
    private Color originalSurfaceColor;
    private bool hasOriginalColor = false;

    private Coroutine selectionCoroutine;

    public void SelectTile()
    {
        if (isSelected) return;
        isSelected = true;

        // запоминаем базовую позицию тайла (на земле)
        baseTilePosition = transform.position;

        // если уже шла анимация — останавливаем
        if (selectionCoroutine != null)
            StopCoroutine(selectionCoroutine);

        selectionCoroutine = StartCoroutine(AnimateSelection(true));

        // включаем Outline сразу (чтобы не мигал)
        Transform surface = transform.Find("Surface");
        if (surface != null)
        {
            Transform outline = surface.Find("Outline");
            if (outline != null)
            {
                outline.gameObject.SetActive(true);
            }
        }
    }

    public void DeselectTile()
    {
        if (!isSelected) return;
        isSelected = false;

        if (selectionCoroutine != null)
            StopCoroutine(selectionCoroutine);

        selectionCoroutine = StartCoroutine(AnimateSelection(false));

        // Outline выключаем в конце анимации (в корутине)
    }

    private IEnumerator AnimateSelection(bool select)
    {
        Transform surface = transform.Find("Surface");
        Renderer surfaceRenderer = null;

        if (surface != null)
        {
            surfaceRenderer = surface.GetComponent<Renderer>();
        }

        // запоминаем исходный цвет один раз
        if (surfaceRenderer != null && !hasOriginalColor)
        {
            originalSurfaceColor = surfaceRenderer.material.color;
            hasOriginalColor = true;
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = select
            ? baseTilePosition + Vector3.up * selectionOffset
            : baseTilePosition;

        Color startColor = originalSurfaceColor;
        Color targetColor = originalSurfaceColor;

        if (surfaceRenderer != null)
        {
            startColor = surfaceRenderer.material.color;
            targetColor = select
                ? originalSurfaceColor * 1.15f   // мягкая подсветка
                : originalSurfaceColor;
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / selectionDuration;
            float tt = Mathf.SmoothStep(0f, 1f, t);

            // позиция
            transform.position = Vector3.Lerp(startPos, targetPos, tt);

            // цвет
            if (surfaceRenderer != null)
            {
                surfaceRenderer.material.color = Color.Lerp(startColor, targetColor, tt);
            }

            yield return null;
        }

        // гарантирую финальные значения
        transform.position = targetPos;
        if (surfaceRenderer != null)
        {
            surfaceRenderer.material.color = targetColor;
        }

        // если это снятие выделения — выключаем Outline
        if (!select && surface != null)
        {
            Transform outline = surface.Find("Outline");
            if (outline != null)
            {
                outline.gameObject.SetActive(false);
            }
        }

        selectionCoroutine = null;
    }

    // ======== ТЕРРИТОРИЯ ГОРОДА ========
    public void SetTerritoryColor(Color color)
    {
        TerritoryColor = color;

        if (territoryRenderer == null)
            return;

        territoryRenderer.gameObject.SetActive(true);

        // усиливаем насыщенность и чуть яркость
        Color.RGBToHSV(color, out float h, out float s, out float v);
        s = Mathf.Clamp01(s * 1.3f);
        v = Mathf.Clamp01(v * 1.1f);
        Color col = Color.HSVToRGB(h, s, v);

        // если в color альфа > 0 — используем её, иначе берём territoryAlpha
        float alpha = color.a > 0f ? color.a : territoryAlpha;
        col.a = alpha;

        // отдельный экземпляр материала, чтобы цвет был уникальный
        Material mat = territoryRenderer.material;
        mat.color = col;
    }

    public void ClearTerritory()
    {
        TerritoryColor = null;

        if (territoryRenderer == null)
            return;

        territoryRenderer.gameObject.SetActive(false);
    }
}
