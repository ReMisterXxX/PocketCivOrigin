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

    [Header("City Recruit Panel (optional)")]
    public CityRecruitPanelUI cityRecruitPanelUI;

    [Header("Build Menu (optional)")]
    public BuildMenuUI buildMenuUI;

    [Header("Players")]
    public PlayerResources playerResources;

    private Tile currentTile;

    private CanvasGroup mainCg;
    private CanvasGroup detailsCg;

    private Coroutine mainAnimCoroutine;
    private Coroutine detailsAnimCoroutine;

    private void Awake()
    {
        if (panel == null) panel = gameObject;

        if (playerResources == null)
            playerResources = FindObjectOfType<PlayerResources>();

        mainCg = GetOrAddCanvasGroup(panel);
        detailsCg = GetOrAddCanvasGroup(detailsPanel);

        HideInstant(mainCg, deactivate: false);
        HideInstant(detailsCg, deactivate: true);

        if (infoButton != null) infoButton.onClick.AddListener(OnInfoClicked);
        if (buildButton != null) buildButton.onClick.AddListener(OnBuildClicked);
        if (mainCloseButton != null) mainCloseButton.onClick.AddListener(OnMainCloseClicked);
        if (detailsCloseButton != null) detailsCloseButton.onClick.AddListener(OnDetailsCloseClicked);
    }

    // чтобы TileSelector не ломался
    public void ShowForTile(Tile tile) => Show(tile);
    public void Hide() => OnMainCloseClicked();

    public void Show(Tile tile)
    {
        currentTile = tile;
        if (currentTile == null) return;

        bool isMine = playerResources == null || currentTile.Owner == playerResources.CurrentPlayer;

        // ✅ Build только на своём
        if (buildButton != null)
            buildButton.gameObject.SetActive(isMine);

        // ✅ Hire панель только на своём
        if (cityRecruitPanelUI != null)
        {
            if (isMine) cityRecruitPanelUI.ShowForTile(currentTile);
            else cityRecruitPanelUI.HideAll();
        }

        if (titleText != null)
        {
            var pos = currentTile.GridPosition;
            titleText.text = $"Tile ({pos.x}, {pos.y})";
        }

        if (infoText != null)
            infoText.text = "Press \"Info\" to view tile details.";

        if (mainCg != null)
        {
            if (mainAnimCoroutine != null) StopCoroutine(mainAnimCoroutine);
            mainAnimCoroutine = StartCoroutine(Animate(mainCg, true, false));
        }

        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null) StopCoroutine(detailsAnimCoroutine);
            detailsAnimCoroutine = StartCoroutine(Animate(detailsCg, false, true));
        }
    }

    private void OnInfoClicked()
    {
        if (currentTile == null) return;

        if (detailsText != null)
            detailsText.text = BuildDetailsText(currentTile);

        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null) StopCoroutine(detailsAnimCoroutine);
            detailsAnimCoroutine = StartCoroutine(Animate(detailsCg, true, true));
        }
    }

    private void OnBuildClicked()
    {
        if (currentTile == null) return;

        if (buildMenuUI != null)
        {
            buildMenuUI.ShowForTile(currentTile);
            return;
        }

        Debug.Log("[TileInfoUI] BuildMenuUI not assigned!");
    }

    private void OnMainCloseClicked()
    {
        if (mainCg != null)
        {
            if (mainAnimCoroutine != null) StopCoroutine(mainAnimCoroutine);
            mainAnimCoroutine = StartCoroutine(Animate(mainCg, false, false));
        }

        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null) StopCoroutine(detailsAnimCoroutine);
            detailsAnimCoroutine = StartCoroutine(Animate(detailsCg, false, true));
        }

        if (buildMenuUI != null)
            buildMenuUI.Hide();

        if (cityRecruitPanelUI != null)
            cityRecruitPanelUI.HideAll();
    }

    private void OnDetailsCloseClicked()
    {
        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null) StopCoroutine(detailsAnimCoroutine);
            detailsAnimCoroutine = StartCoroutine(Animate(detailsCg, false, true));
        }
    }

    private string BuildDetailsText(Tile t)
    {
        return
            $"Terrain: {t.TerrainType}\n" +
            $"Owner: {t.Owner}\n" +
            $"Has City: {t.HasCity}\n" +
            $"Has Building: {t.HasBuilding}";
    }

    private IEnumerator Animate(CanvasGroup cg, bool show, bool deactivateOnHide)
    {
        if (cg == null) yield break;

        if (show) cg.gameObject.SetActive(true);

        float start = cg.alpha;
        float end = show ? 1f : 0f;

        cg.blocksRaycasts = show;
        cg.interactable = show;

        float t = 0f;
        while (t < panelAnimDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / panelAnimDuration);
            yield return null;
        }

        cg.alpha = end;

        if (!show && deactivateOnHide)
            cg.gameObject.SetActive(false);
    }

    private void HideInstant(CanvasGroup cg, bool deactivate)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        if (deactivate) cg.gameObject.SetActive(false);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        var c = go.GetComponent<CanvasGroup>();
        if (c == null) c = go.AddComponent<CanvasGroup>();
        return c;
    }
}
