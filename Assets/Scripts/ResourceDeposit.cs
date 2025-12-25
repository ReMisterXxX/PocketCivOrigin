using System.Collections.Generic;
using UnityEngine;

public enum ResourceType { Gold, Coal }

public class ResourceDeposit : MonoBehaviour
{
    private static readonly List<ResourceDeposit> allDeposits = new List<ResourceDeposit>();
    public static IReadOnlyList<ResourceDeposit> All => allDeposits;

    [Header("Основные настройки")]
    public ResourceType type = ResourceType.Gold;

    [Header("Доход с месторождения")]
    public int baseIncome = 5;
    public int mineIncome = 8;
    public int mountainBaseIncome = 7;
    public int mountainMineIncome = 10;

    [Header("FX")]
    public FloatingResourceText popupPrefab;

    [Header("Popup placement")]
    [Tooltip("Если указан — попап спавнится от этой точки. Если нет — берём верх визуала (Renderer bounds).")]
    public Transform popupAnchor;

    [Tooltip("Offset в МИРОВЫХ единицах (метры). НЕ зависит от масштаба рудника.")]
    public Vector3 popupOffsetWorld = new Vector3(0f, 0.05f, 0f);

    [Tooltip("Если включено — попап наследует масштаб рудника, но с безопасным клэмпом.")]
    public bool scalePopupWithDeposit = false;

    [Header("Runtime (read-only)")]
    [SerializeField] private bool hasMine;

    public Tile Tile { get; private set; }
    public bool HasMine => hasMine;

    private Renderer[] cachedRenderers;

    private void Awake()
    {
        // Кэшируем рендереры один раз (чтобы bounds считать быстро)
        cachedRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    private void OnEnable()
    {
        if (!allDeposits.Contains(this))
            allDeposits.Add(this);
    }

    private void OnDisable()
    {
        allDeposits.Remove(this);
    }

    public void Init(Tile tile, ResourceType t)
    {
        Tile = tile;
        type = t;
    }

    public void SetMineBuilt(bool built)
    {
        hasMine = built;
    }

    public int GetIncomePerTurn()
    {
        bool onMountain = Tile != null && Tile.TerrainType == TileTerrainType.Mountain;
        if (hasMine) return onMountain ? mountainMineIncome : mineIncome;
        return onMountain ? mountainBaseIncome : baseIncome;
    }

    public void ShowIncomePopup(int amount, Color color)
    {
        if (popupPrefab == null) return;

        Vector3 pos = GetPopupWorldPosition();
        FloatingResourceText popup = Instantiate(popupPrefab, pos, Quaternion.identity);

        popup.SetText($"+{amount}", color);

        if (scalePopupWithDeposit)
        {
            // Рудники у тебя часто 0.02 — поэтому без клэмпа попап исчезает.
            float s = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            s = Mathf.Clamp(s, 0.35f, 2.0f);
            popup.transform.localScale = Vector3.one * s;
        }
    }

    private Vector3 GetPopupWorldPosition()
    {
        Vector3 basePos;

        if (popupAnchor != null)
        {
            basePos = popupAnchor.position;
        }
        else
        {
            // Берём верх визуала (works даже если pivot у модели кривой)
            basePos = new Vector3(transform.position.x, GetVisualTopY(), transform.position.z);
        }

        return basePos + popupOffsetWorld;
    }

    private float GetVisualTopY()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            return transform.position.y;

        bool hasAny = false;
        float topY = transform.position.y;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            var r = cachedRenderers[i];
            if (r == null) continue;

            if (!hasAny)
            {
                topY = r.bounds.max.y;
                hasAny = true;
            }
            else
            {
                topY = Mathf.Max(topY, r.bounds.max.y);
            }
        }

        return topY;
    }
}
