using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public PlayerResources playerResources;
    public ResourceTopPanelUI topPanelUI;

    public int currentTurn = 1;

    private IEnumerator Start()
    {
        if (playerResources == null)
            playerResources = FindObjectOfType<PlayerResources>();

        if (topPanelUI == null)
            topPanelUI = FindObjectOfType<ResourceTopPanelUI>();

        // Ждём один кадр, чтобы карта и жилы успели создаться
        yield return null;

        if (playerResources != null)
        {
            // На старте считаем доход (без попапов)
            playerResources.RecalculateIncomeFromDeposits(false);
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
            // 1. Пересчитываем доходы и показываем попапы над жилами
            playerResources.RecalculateIncomeFromDeposits(true);

            // 2. Начисляем этот доход в ресурсы
            playerResources.ApplyTurnIncome();
        }

        currentTurn++;

        if (topPanelUI != null && playerResources != null)
        {
            topPanelUI.UpdateAll(playerResources);
            topPanelUI.UpdateTurn(currentTurn);
        }
    }
}
