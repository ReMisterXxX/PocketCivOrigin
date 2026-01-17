using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BuildMenuUI : MonoBehaviour
{
    [Header("Refs")]
    public BuildSystem buildSystem;
    public PlayerResources playerResources;

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

        if (buildSystem == null) buildSystem = FindObjectOfType<BuildSystem>();
        if (playerResources == null) playerResources = FindObjectOfType<PlayerResources>();

        if (closeButton != null) closeButton.onClick.AddListener(Hide);

        if (buildCityButton != null) buildCityButton.onClick.AddListener(OnBuildCity);
        if (buildMineButton != null) buildMineButton.onClick.AddListener(OnBuildMine);

        // Названия кнопок (чтобы не было "Button")
        var cityLabel = buildCityButton != null
            ? buildCityButton.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;
        if (cityLabel != null) cityLabel.text = "Build City";

        var mineLabel = buildMineButton != null
            ? buildMineButton.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;
        if (mineLabel != null) mineLabel.text = "Build Mine";
    }

    public void ShowForTile(Tile tile)
    {
        currentTile = tile;
        if (currentTile == null) return;

        PlayerId currentPlayer = playerResources != null ? playerResources.CurrentPlayer : PlayerId.Player1;

        // ✅ ТОЛЬКО своё или ничейное
        bool canBuildHere = (currentTile.Owner == currentPlayer) || (currentTile.Owner == PlayerId.None);

        if (buildCityButton != null) buildCityButton.interactable = canBuildHere;
        if (buildMineButton != null) buildMineButton.interactable = canBuildHere;

        if (titleText != null)
        {
            var p = currentTile.GridPosition;
            titleText.text = $"Build ({p.x}, {p.y})";
        }

        if (hintText != null)
        {
            if (canBuildHere) hintText.text = "";
            else hintText.text = "Not your tile";
        }

        Show();
    }

    public void Show()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(Animate(true));
    }

    public void Hide()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(Animate(false));
    }

    private void OnBuildCity()
    {
        if (buildSystem == null || currentTile == null) return;

        if (buildSystem.TryBuildCity(currentTile, out string reason))
        {
            Hide();
        }
        else
        {
            if (hintText != null) hintText.text = reason;
        }
    }

    private void OnBuildMine()
    {
        if (buildSystem == null || currentTile == null) return;

        if (buildSystem.TryBuildMine(currentTile, out string reason))
        {
            Hide();
        }
        else
        {
            if (hintText != null) hintText.text = reason;
        }
    }

    private IEnumerator Animate(bool show)
    {
        float start = cg.alpha;
        float end = show ? 1f : 0f;

        cg.blocksRaycasts = show;
        cg.interactable = show;

        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / Mathf.Max(0.0001f, animDuration));
            yield return null;
        }

        cg.alpha = end;
    }

    private void HideInstant()
    {
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }
}
