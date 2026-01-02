using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public PlayerResources playerResources;
    public ResourceTopPanelUI topPanelUI;

    [Header("Units")]
    public UnitMovementSystem unitMovementSystem;

    [Header("Optional UI")]
    [SerializeField] private Button nextTurnButton; // назначь NextTurnButton сюда (если хочешь блокировку)

    [Header("Popup colors")]
    public Color goldPopupColor = new Color(1f, 0.85f, 0.15f, 1f);
    public Color coalPopupColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    public int currentTurn = 1;

    private bool isAdvancingTurn;

    private IEnumerator Start()
    {
        if (playerResources == null)
            playerResources = FindObjectOfType<PlayerResources>();

        if (topPanelUI == null)
            topPanelUI = FindObjectOfType<ResourceTopPanelUI>();

        if (unitMovementSystem == null)
            unitMovementSystem = FindObjectOfType<UnitMovementSystem>();

        // ✅ ВАЖНО: чтобы UI обновлялся сразу при любом изменении ресурсов (TrySpend/ApplyTurnIncome/etc)
        if (playerResources != null)
            playerResources.OnChanged += HandleResourcesChanged;

        if (playerResources != null)
            playerResources.RecalculateIncome(); // внутри будет RaiseChanged()

        if (topPanelUI != null && playerResources != null)
        {
            topPanelUI.UpdateAll(playerResources);
            topPanelUI.UpdateTurn(currentTurn);
        }

        yield return null;
    }

    private void OnDestroy()
    {
        if (playerResources != null)
            playerResources.OnChanged -= HandleResourcesChanged;
    }

    private void HandleResourcesChanged()
    {
        if (topPanelUI != null && playerResources != null)
            topPanelUI.UpdateAll(playerResources);
    }

    public void NextTurn()
    {
        if (isAdvancingTurn) return; // ✅ анти-спам
        StartCoroutine(NextTurnRoutine());
    }

    private IEnumerator NextTurnRoutine()
    {
        isAdvancingTurn = true;
        if (nextTurnButton != null) nextTurnButton.interactable = false;

        // дождёмся конца кадра (на случай если в этом кадре что-то ещё менялось)
        yield return null;

        if (playerResources != null)
        {
            // ✅ сначала пересчитать доход, потом начислить
            playerResources.RecalculateIncome();     // RaiseChanged() внутри
            playerResources.ApplyTurnIncome();       // RaiseChanged() внутри

            // ✅ показать всплывающие доходы над жилами
            ShowIncomePopupsForCurrentPlayer();
        }

        currentTurn++;

        // ✅ сброс ходов юнитов
        if (unitMovementSystem != null)
            unitMovementSystem.ResetAllUnitsForNewTurn();

        // ✅ обновим текст хода (и пульс) ОДИН раз
        if (topPanelUI != null)
            topPanelUI.UpdateTurn(currentTurn);

        // маленькая пауза, чтобы исключить двойной клик в тот же кадр
        yield return null;

        if (nextTurnButton != null) nextTurnButton.interactable = true;
        isAdvancingTurn = false;
    }

    private void ShowIncomePopupsForCurrentPlayer()
    {
        var deposits = ResourceDeposit.All;
        if (deposits == null || deposits.Count == 0) return;
        if (playerResources == null) return;

        for (int i = 0; i < deposits.Count; i++)
        {
            var d = deposits[i];
            if (d == null) continue;
            if (d.Tile == null) continue;

            if (d.Tile.Owner != playerResources.CurrentPlayer) continue;

            int income = d.GetIncomePerTurn();
            if (income <= 0) continue;

            Color c = (d.type == ResourceType.Gold) ? goldPopupColor : coalPopupColor;
            d.ShowIncomePopup(income, c);
        }
    }
}
