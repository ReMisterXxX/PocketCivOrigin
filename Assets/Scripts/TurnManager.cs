using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public PlayerResources playerResources;
    public ResourceTopPanelUI topPanelUI;

    public int currentTurn = 1;

    // Делаем Start корутиной, чтобы можно было подождать один кадр
    private IEnumerator Start()
    {
        if (playerResources == null)
            playerResources = FindObjectOfType<PlayerResources>();

        if (topPanelUI == null)
            topPanelUI = FindObjectOfType<ResourceTopPanelUI>();

        // Ждём один кадр, чтобы MapGenerator успел создать карту и месторождения
        yield return null;

        if (playerResources != null)
        {
            // Считаем доход уже ПОСЛЕ появления всех жил
            playerResources.RecalculateIncomeFromDeposits();
        }

        if (topPanelUI != null && playerResources != null)
        {
            topPanelUI.UpdateAll(playerResources);
            topPanelUI.UpdateTurn(currentTurn);
        }
    }

    public void NextTurn()
    {
        if (playerResources != null)
        {
            // 1) Добавляем доход к запасам
            playerResources.ApplyTurnIncome();

            // 2) На случай, если за ход построили шахты/новые города
            playerResources.RecalculateIncomeFromDeposits();
        }

        currentTurn++;

        if (topPanelUI != null && playerResources != null)
        {
            topPanelUI.UpdateAll(playerResources);
            topPanelUI.UpdateTurn(currentTurn);
        }
    }
}
