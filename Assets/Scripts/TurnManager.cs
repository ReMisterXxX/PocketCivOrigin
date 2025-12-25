using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public PlayerResources playerResources;
    public ResourceTopPanelUI topPanelUI;

    [Header("Units")]
    public UnitMovementSystem unitMovementSystem;

    [Header("Popup colors")]
    public Color goldPopupColor = new Color(1f, 0.85f, 0.15f, 1f);
    public Color coalPopupColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    public int currentTurn = 1;

    private IEnumerator Start()
    {
        if (playerResources == null)
            playerResources = FindObjectOfType<PlayerResources>();

        if (topPanelUI == null)
            topPanelUI = FindObjectOfType<ResourceTopPanelUI>();

        if (unitMovementSystem == null)
            unitMovementSystem = FindObjectOfType<UnitMovementSystem>();

        if (playerResources != null)
            playerResources.RecalculateIncome();

        if (topPanelUI != null && playerResources != null)
        {
            topPanelUI.UpdateAll(playerResources);
            topPanelUI.UpdateTurn(currentTurn);
        }

        yield return null;
    }

    public void NextTurn()
    {
        StartCoroutine(NextTurnRoutine());
    }

    private IEnumerator NextTurnRoutine()
    {
        // дождёмся конца кадра (на случай, если в этом кадре что-то ещё менялось)
        yield return null;

        if (playerResources != null)
        {
            // ✅ важно: сначала пересчитать доход, потом начислить
            playerResources.RecalculateIncome();
            playerResources.ApplyTurnIncome();

            // ✅ показать всплывающие доходы над жилами
            ShowIncomePopupsForCurrentPlayer();
        }

        currentTurn++;

        // ✅ сброс ходов юнитов
        if (unitMovementSystem != null)
            unitMovementSystem.ResetAllUnitsForNewTurn();

        if (topPanelUI != null && playerResources != null)
        {
            topPanelUI.UpdateAll(playerResources);
            topPanelUI.UpdateTurn(currentTurn);
        }
    }

    private void ShowIncomePopupsForCurrentPlayer()
    {
        // ResourceDeposit сам ведёт список всех жил
        var deposits = ResourceDeposit.All;
        if (deposits == null || deposits.Count == 0) return;
        if (playerResources == null) return;

        for (int i = 0; i < deposits.Count; i++)
        {
            var d = deposits[i];
            if (d == null) continue;

            // должна быть привязка к тайлу
            if (d.Tile == null) continue;

            // показываем только свои
            if (d.Tile.Owner != playerResources.CurrentPlayer) continue;

            int income = d.GetIncomePerTurn();
            if (income <= 0) continue;

            Color c = (d.type == ResourceType.Gold) ? goldPopupColor : coalPopupColor;
            d.ShowIncomePopup(income, c);
        }
    }
}
