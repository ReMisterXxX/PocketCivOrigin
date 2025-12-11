using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TileInfoUI : MonoBehaviour
{
    [Header("Основная панель тайла")]
    public GameObject panel;                 // TileInfoPanel (объект с этим скриптом)
    public TextMeshProUGUI titleText;        // "Tile (x, y)"
    public TextMeshProUGUI infoText;         // подсказка под заголовком

    public Button infoButton;                // кнопка "Info"
    public Button buildButton;               // кнопка "Build"
    public Button mainCloseButton;           // кнопка "Close" на первой панели

    [Header("Панель подробной информации")]
    public GameObject detailsPanel;          // TileInfoDetailsPanel
    public TextMeshProUGUI detailsText;      // текст внутри details-панели
    public Button detailsCloseButton;        // кнопка "Close" на details-панели

    [Header("Параметры анимации")]
    public float panelAnimDuration = 0.15f;  // длительность анимации появления/скрытия

    private Tile currentTile;

    // CanvasGroup для анимации
    private CanvasGroup mainCg;
    private CanvasGroup detailsCg;

    private Coroutine mainAnimCoroutine;
    private Coroutine detailsAnimCoroutine;

    private void Awake()
    {
        // если panel не задана в инспекторе — считаем, что это текущий объект
        if (panel == null)
            panel = gameObject;

        mainCg = GetOrAddCanvasGroup(panel);
        detailsCg = GetOrAddCanvasGroup(detailsPanel);

        // прячем панели при старте (главную — только по alpha, details — ещё и SetActive)
        HideInstant(mainCg, deactivateGameObject: false);
        HideInstant(detailsCg, deactivateGameObject: true);

        if (infoButton != null)
            infoButton.onClick.AddListener(OnInfoClicked);

        if (buildButton != null)
            buildButton.onClick.AddListener(OnBuildClicked);

        if (mainCloseButton != null)
            mainCloseButton.onClick.AddListener(OnMainCloseClicked);

        if (detailsCloseButton != null)
            detailsCloseButton.onClick.AddListener(OnDetailsCloseClicked);
    }

    // ================= ПЕРВАЯ ПАНЕЛЬ (СВОДКА О ТАЙЛЕ) =================

    public void ShowForTile(Tile tile)
    {
        currentTile = tile;

        // обновляем заголовок
        if (titleText != null && currentTile != null)
        {
            var pos = currentTile.GridPosition;
            titleText.text = $"Tile ({pos.x}, {pos.y})";
        }

        // обновляем подсказку
        if (infoText != null)
        {
            infoText.text = "Press \"Info\" to view tile details.";
        }

        // показываем основную панель
        if (mainCg != null)
        {
            if (mainAnimCoroutine != null)
                StopCoroutine(mainAnimCoroutine);

            mainAnimCoroutine = StartCoroutine(AnimatePanel(mainCg, show: true, deactivateOnHide: false));
        }

        // и на всякий случай прячем details-панель
        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null)
                StopCoroutine(detailsAnimCoroutine);

            detailsAnimCoroutine = StartCoroutine(AnimatePanel(detailsCg, show: false, deactivateOnHide: true));
        }
    }

    /// <summary>
    /// Полное скрытие UI, используется TileSelector при снятии выделения.
    /// </summary>
    public void Hide()
    {
        currentTile = null;

        if (mainCg != null)
        {
            if (mainAnimCoroutine != null)
                StopCoroutine(mainAnimCoroutine);

            mainAnimCoroutine = StartCoroutine(AnimatePanel(mainCg, show: false, deactivateOnHide: false));
        }

        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null)
                StopCoroutine(detailsAnimCoroutine);

            detailsAnimCoroutine = StartCoroutine(AnimatePanel(detailsCg, show: false, deactivateOnHide: true));
        }
    }

    /// <summary>
    /// Закрытие только основной панели по кнопке "Close".
    /// Тайл при этом остаётся выделенным.
    /// </summary>
    private void OnMainCloseClicked()
    {
        if (mainCg != null)
        {
            if (mainAnimCoroutine != null)
                StopCoroutine(mainAnimCoroutine);

            mainAnimCoroutine = StartCoroutine(AnimatePanel(mainCg, show: false, deactivateOnHide: false));
        }

        if (detailsCg != null)
        {
            if (detailsAnimCoroutine != null)
                StopCoroutine(detailsAnimCoroutine);

            detailsAnimCoroutine = StartCoroutine(AnimatePanel(detailsCg, show: false, deactivateOnHide: true));
        }
        // currentTile НЕ обнуляем — чтобы можно было заново открыть панель по клику
    }

    // ================= ПАНЕЛЬ DETAILS (ПОДРОБНАЯ ИНФА) =================

    private void OnInfoClicked()
    {
        if (currentTile == null || detailsCg == null || detailsText == null)
            return;

        string heightCategory = GetHeightCategory(currentTile.TopHeight);
        string biome = GetBiomeName(currentTile.TerrainType);

        detailsText.text =
            $"<b>Coordinates:</b> {currentTile.GridPosition.x}, {currentTile.GridPosition.y}\n\n" +
            $"<b>Biome:</b> {biome}\n" +
            $"<b>Elevation:</b> {heightCategory}\n\n" +
            $"<b>Contents:</b>\n" +
            $"- Decorations: {currentTile.Decorations.Count}\n" +
            $"- Unit: {(currentTile.HasUnit ? "Present" : "None")}\n" +
            $"- Building: {(currentTile.HasBuilding ? "Present" : "None")}";

        if (detailsAnimCoroutine != null)
            StopCoroutine(detailsAnimCoroutine);

        detailsAnimCoroutine = StartCoroutine(AnimatePanel(detailsCg, show: true, deactivateOnHide: true));
    }

    private void OnDetailsCloseClicked()
    {
        if (detailsCg == null)
            return;

        if (detailsAnimCoroutine != null)
            StopCoroutine(detailsAnimCoroutine);

        detailsAnimCoroutine = StartCoroutine(AnimatePanel(detailsCg, show: false, deactivateOnHide: true));
    }

    // ================= КНОПКА BUILD (ПОКА ЗАГЛУШКА) =================

    private void OnBuildClicked()
    {
        if (currentTile == null)
            return;

        Debug.Log($"[BUILD] Build menu for tile {currentTile.GridPosition}");
        // здесь позже будет открываться меню построек
    }

    // ================= СЛУЖЕБНЫЕ МЕТОДЫ ДЛЯ АНИМАЦИИ =================

    /// <summary>
    /// Гарантированно добавляет CanvasGroup к объекту.
    /// </summary>
    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;

        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = go.AddComponent<CanvasGroup>();

        return cg;
    }

    /// <summary>
    /// Мгновенно прячет панель без анимации.
    /// </summary>
    private void HideInstant(CanvasGroup cg, bool deactivateGameObject)
    {
        if (cg == null) return;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        if (deactivateGameObject && cg.gameObject != null)
            cg.gameObject.SetActive(false);
    }

    /// <summary>
    /// Универсальная анимация появления/исчезновения панели.
    /// </summary>
    private IEnumerator AnimatePanel(CanvasGroup cg, bool show, bool deactivateOnHide)
    {
        if (cg == null) yield break;

        GameObject go = cg.gameObject;

        if (show && !go.activeSelf)
            go.SetActive(true);

        float startAlpha = cg.alpha;
        float targetAlpha = show ? 1f : 0f;

        Vector3 startScale = go.transform.localScale;
        Vector3 targetScale = show ? Vector3.one : Vector3.one * 0.9f;

        float t = 0f;

        while (t < panelAnimDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / panelAnimDuration);
            float ease = Mathf.SmoothStep(0f, 1f, k);

            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, ease);
            go.transform.localScale = Vector3.Lerp(startScale, targetScale, ease);

            yield return null;
        }

        cg.alpha = targetAlpha;
        go.transform.localScale = targetScale;

        if (show)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        else
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;

            if (deactivateOnHide)
                go.SetActive(false);
        }
    }

    // ================= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ТЕКСТА =================

    private string GetHeightCategory(float h)
    {
        // здесь можно подогнать пороги под реальные высоты
        if (h < 0.3f) return "Low";
        if (h < 0.7f) return "Medium";
        return "High";
    }

    private string GetBiomeName(TileTerrainType type)
    {
        switch (type)
        {
            case TileTerrainType.Grass:    return "Grassland";
            case TileTerrainType.Forest:   return "Forest";
            case TileTerrainType.Mountain: return "Mountain";
            case TileTerrainType.Water:    return "Water";
            default:                       return type.ToString();
        }
    }
}
