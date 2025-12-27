using System;
using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [Header("Start values")]
    [SerializeField] private int startGold = 10;
    [SerializeField] private int startCoal = 10;

    [Header("Fallback income (used only if there are ZERO deposits in scene)")]
    [SerializeField] private int fallbackGoldIncome = 5;
    [SerializeField] private int fallbackCoalIncome = 5;

    [Header("Runtime")]
    [SerializeField] private int gold;
    [SerializeField] private int coal;
    [SerializeField] private int goldIncome;
    [SerializeField] private int coalIncome;

    [Header("Owner / current player")]
    public PlayerId currentPlayer = PlayerId.Player1;
    public PlayerId CurrentPlayer => currentPlayer;

    public int Gold => gold;
    public int Coal => coal;

    public int GoldIncome => goldIncome;
    public int CoalIncome => coalIncome;

    public event Action OnChanged;

    private void Awake()
    {
        gold = startGold;
        coal = startCoal;

        RecalculateIncome();
        RaiseChanged();
    }

    /// <summary>
    /// Пересчитать доход:
    /// - +5 за каждую обычную жилу
    /// - +7 за горную жилу
    /// - с шахтой: обычная 7, горная 10
    /// Всё это уже зашито в ResourceDeposit.GetIncomePerTurn().
    /// Считаем только депозиты на тайлах текущего игрока (tile.Owner == currentPlayer).
    /// </summary>
    public void RecalculateIncome()
    {
        int g = 0;
        int c = 0;

        bool anyDepositExists = false;

        foreach (var d in ResourceDeposit.All)
        {
            if (d == null) continue;
            anyDepositExists = true;

            Tile tile = d.Tile;
            if (tile == null) continue;

            // Считаем только доход с тайлов текущего игрока
            if (tile.Owner != currentPlayer)
                continue;

            int income = d.GetIncomePerTurn();

            if (d.type == ResourceType.Gold) g += income;
            else if (d.type == ResourceType.Coal) c += income;
        }

        // Fallback нужен ТОЛЬКО если в сцене вообще нет депозитов (тестовая/пустая сцена)
        if (!anyDepositExists)
        {
            g = fallbackGoldIncome;
            c = fallbackCoalIncome;
        }

        goldIncome = g;
        coalIncome = c;
    }

    /// <summary>
    /// Начислить доход за ход.
    /// </summary>
    public void ApplyTurnIncome()
    {
        gold += goldIncome;
        coal += coalIncome;
        RaiseChanged();
    }

    public bool CanAfford(int goldCost, int coalCost)
        => gold >= goldCost && coal >= coalCost;

    public bool TrySpend(int goldCost, int coalCost)
    {
        if (!CanAfford(goldCost, coalCost))
            return false;

        gold -= goldCost;
        coal -= coalCost;
        RaiseChanged();
        return true;
    }

    public void AddGold(int amount)
    {
        gold += amount;
        RaiseChanged();
    }

    public void AddCoal(int amount)
    {
        coal += amount;
        RaiseChanged();
    }

    private void RaiseChanged() => OnChanged?.Invoke();
}
