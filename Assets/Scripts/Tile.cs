using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Владелец тайла
    public PlayerId Owner { get; private set; } = PlayerId.None;

    // Месторождение на тайле (если есть)
    public ResourceDeposit ResourceDeposit { get; private set; }
    public bool HasResourceDeposit => ResourceDeposit != null;

    // Список всех декораций на тайле
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

    public void SetResourceDeposit(ResourceDeposit deposit)
    {
        ResourceDeposit = deposit;
    }

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

    // ===== ЮНИТЫ / ПОСТРОЙКИ =====

    // Юнит зашёл на тайл
    public void OnUnitEnter()
    {
        HasUnit = true;
        UpdateDecorationVisibility();
    }

    // Юнит покинул тайл
    public void OnUnitLeave()
    {
        HasUnit = false;
        UpdateDecorationVisibility();
    }

    // На тайле появилась/исчезла постройка
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

    // ====== ВЫДЕЛЕНИЕ ТАЙЛА (АНИМАЦИЯ ПОДСКОКА + ПОДСВЕТКА) ======

    [Header("Selection")]
    public float selectionOffset = 0.3f;    // Насколько поднимать тайл при выделении
    public float selectionDuration = 0.12f; // Длительность анимации

    private bool isSelected = false;
    private Vector3 baseTilePosition;
    private Color originalSurfaceColor;
    private bool hasOriginalColor = false;

    private Coroutine selectionCoroutine;

    public void SelectTile()
    {
        if (isSelected) return;
        isSelected = true;

        baseTilePosition = transform.position;

        if (selectionCoroutine != null)
            StopCoroutine(selectionCoroutine);

        selectionCoroutine = StartCoroutine(AnimateSelection(true));

        // Включаем Outline сразу
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
    }

    private IEnumerator AnimateSelection(bool select)
    {
        Transform surface = transform.Find("Surface");
        Renderer surfaceRenderer = null;

        if (surface != null)
        {
            surfaceRenderer = surface.GetComponent<Renderer>();
        }

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
                ? originalSurfaceColor * 1.15f  // немного ярче при выделении
                : originalSurfaceColor;
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / selectionDuration;
            float tt = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPos, tt);

            if (surfaceRenderer != null)
            {
                surfaceRenderer.material.color = Color.Lerp(startColor, targetColor, tt);
            }

            yield return null;
        }

        transform.position = targetPos;

        if (surfaceRenderer != null)
        {
            surfaceRenderer.material.color = targetColor;
        }

        // При снятии выделения выключаем Outline
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

    // ====== ТЕРРИТОРИЯ (ЦВЕТНОЕ "СТЕКЛО") ======

    [Header("Territory")]
    public Renderer territoryRenderer;
    [Range(0f, 1f)]
    public float territoryAlpha = 0.55f;

    public Color? TerritoryColor { get; private set; }

    public void SetTerritoryColor(Color color)
    {
        TerritoryColor = color;

        if (territoryRenderer == null)
            return;

        territoryRenderer.gameObject.SetActive(true);

        // Делаем цвет чуть более насыщенным и ярким
        Color.RGBToHSV(color, out float h, out float s, out float v);
        s = Mathf.Clamp01(s * 1.3f);
        v = Mathf.Clamp01(v * 1.1f);
        Color col = Color.HSVToRGB(h, s, v);

        float alpha = color.a > 0f ? color.a : territoryAlpha;
        col.a = alpha;

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
