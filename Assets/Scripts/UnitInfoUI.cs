using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UnitInfoUI : MonoBehaviour
{
    [Header("Root (panel object that may be inactive)")]
    [SerializeField] private GameObject panelRoot;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    [Header("Anim")]
    [SerializeField] private float fadeDuration = 0.12f;

    private CanvasGroup panelCanvasGroup;
    private Coroutine anim;
    private UnitCombatSystem combat;

    private void Awake()
    {
        combat = FindObjectOfType<UnitCombatSystem>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        EnsurePanelSetup();
        SetVisibleInstant(false);
    }

    private void EnsurePanelSetup()
    {
        if (panelRoot == null)
        {
            // Если не назначили — считаем, что панель это этот объект
            panelRoot = gameObject;
        }

        panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
    }

    public void ShowFor(Unit unit)
    {
        if (unit == null) return;

        EnsurePanelSetup();
        Refresh(unit);

        if (!panelRoot.activeSelf)
            panelRoot.SetActive(true);

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(FadeTo(1f));
    }

    public void Refresh(Unit unit)
    {
        if (unit == null) return;

        string uName = (unit.Stats != null) ? unit.Stats.name : "Unit";

        int hpCur = unit.CurrentHP;
        int hpMax = (unit.Stats != null) ? unit.Stats.MaxHP : unit.MaxHP;

        int atk = unit.Attack;
        int defBase = unit.Defense;

        int terrainBonus = 0;
        string terrainName = "None";
        if (unit.CurrentTile != null)
        {
            terrainName = unit.CurrentTile.TerrainType.ToString();
            if (combat != null) terrainBonus = combat.GetTerrainDefenseBonus(unit.CurrentTile);
        }

        int range = (unit.Stats != null) ? Mathf.Max(1, unit.Stats.AttackRange) : 1;

        int movesCur = unit.MovesLeftThisTurn;
        int movesMax = unit.MovePointsPerTurn;

        if (titleText != null)
            titleText.text = uName;

        if (bodyText != null)
        {
            bodyText.text =
                $"HP: {hpCur}/{hpMax}\n" +
                $"Move: {movesCur}/{movesMax}\n" +
                $"Attack: {atk}\n" +
                $"Defense: {defBase} (+{terrainBonus}) = {defBase + terrainBonus}\n" +
                $"Range: {range}\n" +
                $"Terrain: {terrainName}";
        }
    }

    public void Hide()
    {
        EnsurePanelSetup();

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(FadeTo(0f));
    }

    private IEnumerator FadeTo(float target)
    {
        float start = panelCanvasGroup.alpha;
        float t = 0f;

        if (target > 0.5f && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            panelCanvasGroup.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }

        panelCanvasGroup.alpha = target;

        bool visible = target > 0.5f;
        panelCanvasGroup.interactable = visible;
        panelCanvasGroup.blocksRaycasts = visible;

        if (!visible)
            panelRoot.SetActive(false);

        anim = null;
    }

    private void SetVisibleInstant(bool visible)
    {
        EnsurePanelSetup();

        if (!visible)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            panelRoot.SetActive(false);
        }
        else
        {
            panelRoot.SetActive(true);
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }
    }
}
