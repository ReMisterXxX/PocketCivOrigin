using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TileInfoUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI infoText;

    public Button infoButton;
    public Button buildButton;
    public Button mainCloseButton;

    [Header("Details Panel")]
    public GameObject detailsPanel;
    public TextMeshProUGUI detailsText;
    public Button detailsCloseButton;

    [Header("Animation")]
    public float panelAnimDuration = 0.15f;

    // (опционально) Панель найма города — пока можешь не назначать
    [Header("City Recruit Panel (optional)")]
    public CityRecruitPanelUI cityRecruitPanelUI;

    private Tile currentTile;

    private CanvasGroup mainCg;
    private CanvasGroup detailsCg;

    private Coroutine mainAnimCoroutine;
    private Coroutine detailsAnimCoroutine;

    private void Awake()
    {
        if (panel == null) panel = gameObject;

        mainCg = GetOrAddCanvasGroup(panel);
        detailsCg = GetOrAddCanvasGroup(detailsPanel);

        // ВАЖНО: основная панель может не деактивироваться (как у тебя было),
        // но становиться невидимой/некликабельной — это ок.
        HideInstant(mainCg, deactivateGameObject: false);
        HideInstant(detailsCg, deactivateGameObject: true);

        if (infoButton != null) infoButton.onClick.AddListener(OnInfoClicked);
        if (buildButton != null) buildButton.onClick.AddListener(OnBuildClicked);
        if (mainCloseButton != null) mainCloseButton.onClick.AddListener(OnMainCloseClicked);
        if (detailsCloseButton != null) detailsCloseButton.onClick.AddListener(OnDetailsCloseClicked);
    }

    // ===== Совместимость с твоим TileSelector.cs =====
    public void ShowForTile(Tile tile)
    {
        Show(tile);
    }

    public void Hide()
    {
        OnMainCloseClicked();
    }
    // ===============================================

    public void Show(Tile tile)
    {
        currentTile = tile;

        if (titleText != null && currentTile != null)
        {
            var pos = currentTile.GridPosition;
            titleText.text = $"Tile ({pos.x}, {pos.y})";
        }

        if (infoText != null)
            infoText.text = "Press \"Info\" to view tile details.";

        if (mainCg != null)
        {
            if (mainAnimCoroutine != null) StopCoroutine(mainAnimCoroutine);
            mainAnimCoroutine = StartCoroutine(AnimatePanel(mainCg, show: true, deactivateOnHide: false));
        }

        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null) StopCoroutine(detailsAnimCoroutine);
            detailsAnimCoroutine = StartCoroutine(AnimatePanel(detailsCg, show: false, deactivateOnHide: true));
        }

        // Если панель найма не создана/не назначена — ничего не будет.
        if (cityRecruitPanelUI != null)
            cityRecruitPanelUI.ShowForTile(currentTile);
    }

    private void OnInfoClicked()
    {
        if (currentTile == null) return;

        if (detailsText != null)
            detailsText.text = BuildDetailsText(currentTile);

        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null) StopCoroutine(detailsAnimCoroutine);
            detailsAnimCoroutine = StartCoroutine(AnimatePanel(detailsCg, show: true, deactivateOnHide: true));
        }
    }

    private void OnBuildClicked()
    {
        if (currentTile == null) return;
        Debug.Log($"[BUILD] (reserved) Build menu for tile {currentTile.GridPosition}");
    }

    private void OnMainCloseClicked()
    {
        if (mainCg != null)
        {
            if (mainAnimCoroutine != null) StopCoroutine(mainAnimCoroutine);
            mainAnimCoroutine = StartCoroutine(AnimatePanel(mainCg, show: false, deactivateOnHide: false));
        }

        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null) StopCoroutine(detailsAnimCoroutine);
            detailsAnimCoroutine = StartCoroutine(AnimatePanel(detailsCg, show: false, deactivateOnHide: true));
        }

        // закрываем панель найма (если она есть)
        if (cityRecruitPanelUI != null)
            cityRecruitPanelUI.HideAll();
    }

    private void OnDetailsCloseClicked()
    {
        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null) StopCoroutine(detailsAnimCoroutine);
            detailsAnimCoroutine = StartCoroutine(AnimatePanel(detailsCg, show: false, deactivateOnHide: true));
        }
    }

    private string BuildDetailsText(Tile tile)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Terrain: {tile.TerrainType}");
        sb.AppendLine($"Owner: {tile.Owner}");
        sb.AppendLine($"Has Unit: {tile.HasUnit}");
        sb.AppendLine($"Has Building: {tile.HasBuilding}");
        sb.AppendLine($"Has City: {tile.HasCity}");
        sb.AppendLine($"Top Height: {tile.TopHeight:F2}");

        if (tile.HasResourceDeposit && tile.ResourceDeposit != null)
        {
            sb.AppendLine();
            sb.AppendLine($"Deposit: {tile.ResourceDeposit.type}");
            sb.AppendLine($"Income/Turn: {tile.ResourceDeposit.GetIncomePerTurn()}");
        }

        return sb.ToString();
    }

    // ===== Animation helpers =====

    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private void HideInstant(CanvasGroup cg, bool deactivateGameObject)
    {
        if (cg == null) return;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        if (deactivateGameObject && cg.gameObject != null)
            cg.gameObject.SetActive(false);
    }

    private IEnumerator AnimatePanel(CanvasGroup cg, bool show, bool deactivateOnHide)
    {
        if (cg == null) yield break;

        if (show && !cg.gameObject.activeSelf)
            cg.gameObject.SetActive(true);

        float start = cg.alpha;
        float end = show ? 1f : 0f;

        cg.interactable = false;
        cg.blocksRaycasts = false;

        float t = 0f;
        while (t < panelAnimDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / panelAnimDuration);
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
