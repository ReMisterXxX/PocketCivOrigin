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

    // список всех декораций на тайле
    public List<GameObject> Decorations { get; } = new List<GameObject>();

    public bool HasUnit { get; private set; }
    public bool HasBuilding { get; private set; }
    public float TopHeight { get; private set; }

    public void SetTopHeight(float value)
    {
        TopHeight = value;
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

    private void Awake()
    {
        // Базовая позиция тайла запоминается один раз
        baseTilePosition = transform.position;
    }

    public void SelectTile()
    {
        if (isSelected) return;
        isSelected = true;

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
                ? originalSurfaceColor * 1.3f   // делаем ярче
                : originalSurfaceColor;        // возвращаем исходный
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / selectionDuration;
            float tt = Mathf.SmoothStep(0f, 1f, t); // плавное ускорение/замедление

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
}
