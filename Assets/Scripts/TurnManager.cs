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
    [SerializeField] private Button nextTurnButton;

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

        if (playerResources != null)
            playerResources.OnChanged += HandleResourcesChanged;

        // стартовый апдейт
        if (playerResources != null)
            playerResources.RecalculateIncome();

        if (topPanelUI != null && playerResources != null)
        {
            topPanelUI.UpdateAll(playerResources);
            topPanelUI.UpdateTurn(currentTurn);
        }

        // сброс ходов стартового игрока
        if (unitMovementSystem != null && playerResources != null)
            unitMovementSystem.ResetUnitsForNewTurn(playerResources.CurrentPlayer);

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
        if (isAdvancingTurn) return;
        StartCoroutine(NextTurnRoutine());
    }

    private IEnumerator NextTurnRoutine()
    {
        isAdvancingTurn = true;
        if (nextTurnButton != null) nextTurnButton.interactable = false;

        yield return null;

        if (playerResources != null)
        {
            // 1) переключаем игрока
            var next = GetNextPlayer(playerResources.CurrentPlayer);
            playerResources.SetCurrentPlayer(next);

            Tile startCity = MapGenerator.Instance?.GetStartCityTile(next);
            CameraController cam = FindObjectOfType<CameraController>();

            if (cam != null && startCity != null)
            {
                cam.JumpToPosition(startCity.transform.position);
            }


            // 2) доход для нового текущего игрока
            playerResources.RecalculateIncome();
            playerResources.ApplyTurnIncome();

            // 3) попапы дохода — только на его территории
            ShowIncomePopupsForCurrentPlayer();
        }

        currentTurn++;

        // 4) сброс ходов только юнитам активного игрока
        if (unitMovementSystem != null && playerResources != null)
        {
            unitMovementSystem.ClearSelection(); // чтобы нельзя было "тащить" чужое выделение между ходами
            unitMovementSystem.ResetUnitsForNewTurn(playerResources.CurrentPlayer);
        }

        if (topPanelUI != null)
            topPanelUI.UpdateTurn(currentTurn);

        yield return null;

        if (nextTurnButton != null) nextTurnButton.interactable = true;
        isAdvancingTurn = false;
    }

    private PlayerId GetNextPlayer(PlayerId current)
    {
        var list = playerResources.ActivePlayers;
        if (list == null || list.Count == 0) return current;

        int idx = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == current)
            {
                idx = i;
                break;
            }
        }

        int nextIdx = (idx + 1) % list.Count;
        return list[nextIdx];
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
