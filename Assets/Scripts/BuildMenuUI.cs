using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BuildMenuUI : MonoBehaviour
{
    [Header("Refs")]
    public BuildSystem buildSystem;

    [Header("UI Root")]
    public GameObject panel; // весь BuildMenu
    public Button closeButton;

    [Header("Title/Info")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI hintText;

    [Header("Buttons")]
    public Button buildCityButton;
    public Button buildMineButton;

    [Header("Animation")]
    public float animDuration = 0.15f;

    private Tile currentTile;
    private CanvasGroup cg;
    private Coroutine animRoutine;

    private void Awake()
    {
        if (panel == null) panel = gameObject;

        cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        HideInstant();

        if (buildSystem == null)
            buildSystem = FindObjectOfType<BuildSystem>();

        if (closeButton != null) closeButton.onClick.AddListener(Hide);

        if (buildCityButton != null) buildCityButton.onClick.AddListener(OnBuildCity);
        if (buildMineButton != null) buildMineButton.onClick.AddListener(OnBuildMine);
    }

    public void ShowForTile(Tile tile)
    {
        currentTile = tile;

        if (titleText != null && currentTile != null)
        {
            var p = currentTile.GridPosition;
            titleText.text = $"Build on Tile ({p.x}, {p.y})";
        }

        RefreshButtons();

        Show();
    }

    public void Hide()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(Animate(show: false));
    }

    private void Show()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(Animate(show: true));
    }

    private void RefreshButtons()
    {
        if (buildSystem == null || currentTile == null)
        {
            SetButton(buildCityButton, false, "City (n/a)");
            SetButton(buildMineButton, false, "Mine (n/a)");
            if (hintText != null) hintText.text = "";
            return;
        }

        // City
        bool canCity = buildSystem.CanBuildCity(currentTile, out string cityReason);
        SetButton(buildCityButton, canCity, canCity ? "Build City" : "Build City (locked)");

        // Mine
        bool canMine = buildSystem.CanBuildMine(currentTile, out string mineReason);
        SetButton(buildMineButton, canMine, canMine ? "Build Mine" : "Build Mine (locked)");

        // Hint
        if (hintText != null)
        {
            string msg = "";

            if (!canCity && !string.IsNullOrEmpty(cityReason))
                msg += $"City: {cityReason}\n";

            if (!canMine && !string.IsNullOrEmpty(mineReason))
                msg += $"Mine: {mineReason}\n";

            hintText.text = msg.Trim();
        }
    }

    private void SetButton(Button b, bool enabled, string label)
    {
        if (b == null) return;

        b.interactable = enabled;

        var t = b.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.text = label;
    }

    private void OnBuildCity()
    {
        if (buildSystem == null || currentTile == null) return;

        bool ok = buildSystem.TryBuildCity(currentTile, out string reason);
        if (!ok)
        {
            if (hintText != null) hintText.text = reason;
            RefreshButtons();
            return;
        }

        RefreshButtons();
        Hide();
    }

    private void OnBuildMine()
    {
        if (buildSystem == null || currentTile == null) return;

        bool ok = buildSystem.TryBuildMine(currentTile, out string reason);
        if (!ok)
        {
            if (hintText != null) hintText.text = reason;
            RefreshButtons();
            return;
        }

        RefreshButtons();
        Hide();
    }

    private void HideInstant()
    {
        if (panel != null && !panel.activeSelf)
            panel.SetActive(true);

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    private IEnumerator Animate(bool show)
    {
        if (panel != null && !panel.activeSelf)
            panel.SetActive(true);

        float start = cg.alpha;
        float end = show ? 1f : 0f;

        cg.interactable = false;
        cg.blocksRaycasts = false;

        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / animDuration);
            cg.alpha = Mathf.Lerp(start, end, k);
            yield return null;
        }

        cg.alpha = end;

        bool visible = end > 0.5f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }
}
