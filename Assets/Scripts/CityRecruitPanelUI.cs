using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CityRecruitPanelUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject cityPanelRoot;
    public GameObject unitListRoot;

    [Header("City panel UI")]
    public TextMeshProUGUI cityTitleText;
    public Button hireButton;
    public Button closeButton;

    [Header("Unit list UI")]
    public TextMeshProUGUI unitListTitleText;
    public Button recruitBasicUnitButton;
    public Button backButton;

    [Header("Gameplay refs")]
    public PlayerResources playerResources;
    public ResourceTopPanelUI topPanelUI;

    [Header("Unit prefab")]
    public GameObject basicUnitPrefab;
    public Vector3 unitSpawnOffset = new Vector3(0f, 0.08f, 0f);
    public Vector3 unitSpawnEuler = Vector3.zero;

    [Header("Costs")]
    public int basicUnitGoldCost = 15;
    public int basicUnitCoalCost = 5;

    [Header("Animation")]
    public float animDuration = 0.15f;

    private CanvasGroup cityCg;
    private CanvasGroup listCg;

    private Tile currentTile;

    private void Awake()
    {
        if (cityPanelRoot == null)
            cityPanelRoot = gameObject;

        if (playerResources == null)
            playerResources = FindObjectOfType<PlayerResources>();

        if (topPanelUI == null)
            topPanelUI = FindObjectOfType<ResourceTopPanelUI>();

        cityCg = GetOrAddCanvasGroup(cityPanelRoot);
        listCg = GetOrAddCanvasGroup(unitListRoot);

        HideInstant(cityCg, false);
        HideInstant(listCg, true);

        hireButton?.onClick.AddListener(OpenUnitList);
        closeButton?.onClick.AddListener(HideAll);

        backButton?.onClick.AddListener(BackToCity);
        recruitBasicUnitButton?.onClick.AddListener(RecruitBasicUnit);

        if (unitListTitleText != null)
            unitListTitleText.text = "Units";
    }

    public void ShowForTile(Tile tile)
    {
        currentTile = tile;

        if (currentTile == null || !currentTile.HasCity)
        {
            HideAll();
            return;
        }

        if (cityTitleText != null)
        {
            var p = currentTile.GridPosition;
            cityTitleText.text = $"City ({p.x}, {p.y})";
        }

        ShowCityPanel();
    }

    public void HideAll()
    {
        currentTile = null;
        StopAllCoroutines();
        StartCoroutine(Animate(cityCg, false, false));
        StartCoroutine(Animate(listCg, false, true));
    }

    private void ShowCityPanel()
    {
        StopAllCoroutines();
        StartCoroutine(Animate(cityCg, true, false));
        StartCoroutine(Animate(listCg, false, true));
    }

    private void OpenUnitList()
    {
        if (currentTile == null || !currentTile.HasCity) return;

        StopAllCoroutines();
        StartCoroutine(Animate(cityCg, false, false));
        StartCoroutine(Animate(listCg, true, false));

        if (recruitBasicUnitButton != null)
        {
            string label = $"Recruit Basic ({basicUnitGoldCost}G / {basicUnitCoalCost}C)";
            var tmp = recruitBasicUnitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = label;
        }
    }

    private void BackToCity()
    {
        ShowCityPanel();
    }

    private void RecruitBasicUnit()
    {
        if (currentTile == null || !currentTile.HasCity) return;

        if (currentTile.HasUnit)
        {
            Debug.Log("[Recruit] Tile already has a unit.");
            return;
        }

        if (playerResources == null)
        {
            Debug.LogWarning("[Recruit] PlayerResources not found.");
            return;
        }

        // ✅ FIX: owner -> currentPlayer
        if (currentTile.Owner != playerResources.currentPlayer)
        {
            Debug.Log("[Recruit] Can't recruit in чужом городе.");
            return;
        }

        if (!playerResources.TrySpend(basicUnitGoldCost, basicUnitCoalCost))
        {
            Debug.Log("[Recruit] Not enough resources.");
            return;
        }

        // на всякий: доход мог меняться — пересчитаем и обновим UI
        playerResources.RecalculateIncome();
        topPanelUI?.UpdateAll(playerResources);

        if (basicUnitPrefab == null)
        {
            Debug.LogWarning("[Recruit] basicUnitPrefab not assigned.");
            return;
        }

        Vector3 anchor = currentTile.transform.position;
        anchor.y = currentTile.TopHeight;

        Vector3 spawnPos = anchor + unitSpawnOffset;
        Quaternion spawnRot = Quaternion.Euler(unitSpawnEuler);

        GameObject go = Instantiate(basicUnitPrefab, spawnPos, spawnRot);
        go.name = $"{basicUnitPrefab.name}_({currentTile.GridPosition.x},{currentTile.GridPosition.y})";

        Unit unit = go.GetComponent<Unit>();
        if (unit == null) unit = go.AddComponent<Unit>();

        // ✅ FIX: owner -> currentPlayer
        unit.Initialize(playerResources.currentPlayer, currentTile);

        // ✅ КЛЮЧЕВОЕ: записываем юнита в тайл, чтобы система движения его видела
        currentTile.AssignUnit(unit);

        ShowCityPanel();
    }

    // ===== helpers =====

    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private void HideInstant(CanvasGroup cg, bool deactivate)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (deactivate) cg.gameObject.SetActive(false);
    }

    private IEnumerator Animate(CanvasGroup cg, bool show, bool deactivateOnHide)
    {
        if (cg == null) yield break;

        if (show && !cg.gameObject.activeSelf)
            cg.gameObject.SetActive(true);

        float start = cg.alpha;
        float end = show ? 1f : 0f;

        cg.interactable = false;
        cg.blocksRaycasts = false;

        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime / animDuration;
            float k = Mathf.Clamp01(t);
            cg.alpha = Mathf.Lerp(start, end, k);
            yield return null;
        }

        cg.alpha = end;

        bool visible = end > 0.5f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;

        if (!show && deactivateOnHide)
            cg.gameObject.SetActive(false);
    }
}
