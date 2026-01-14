using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerStartResources
{
    public PlayerId playerId = PlayerId.Player1;
    public int startGold = 10;
    public int startCoal = 10;
}

public class PlayerResources : MonoBehaviour
{
    [Header("Active players (turn order)")]
    [SerializeField] private List<PlayerId> activePlayers = new List<PlayerId> { PlayerId.Player1, PlayerId.Player2 };

    [Header("Start values per player")]
    [SerializeField] private List<PlayerStartResources> startPerPlayer = new List<PlayerStartResources>();

    [Header("Fallback income (used only if there are ZERO deposits in scene)")]
    [SerializeField] private int fallbackGoldIncome = 5;
    [SerializeField] private int fallbackCoalIncome = 5;

    [Header("Current player")]
    [SerializeField] private PlayerId currentPlayer = PlayerId.Player1;
    public PlayerId CurrentPlayer => currentPlayer;

    private class Wallet
    {
        public int gold;
        public int coal;
        public int goldIncome;
        public int coalIncome;
    }

    private readonly Dictionary<PlayerId, Wallet> wallets = new Dictionary<PlayerId, Wallet>();

    public int Gold => GetWallet(currentPlayer).gold;
    public int Coal => GetWallet(currentPlayer).coal;
    public int GoldIncome => GetWallet(currentPlayer).goldIncome;
    public int CoalIncome => GetWallet(currentPlayer).coalIncome;

    public event Action OnChanged;

    private void Awake()
    {
        EnsureWallets();
        ApplyStartValuesOnce();
        RecalculateIncome();
    }

    private void EnsureWallets()
    {
        for (int i = 0; i < activePlayers.Count; i++)
        {
            var id = activePlayers[i];
            if (id == PlayerId.None) continue;
            if (!wallets.ContainsKey(id))
                wallets[id] = new Wallet();
        }

        if (!wallets.ContainsKey(currentPlayer))
            wallets[currentPlayer] = new Wallet();
    }

    private void ApplyStartValuesOnce()
    {
        if (startPerPlayer == null || startPerPlayer.Count == 0)
        {
            foreach (var p in activePlayers)
            {
                var w = GetWallet(p);
                if (w.gold == 0 && w.coal == 0)
                {
                    w.gold = 10;
                    w.coal = 10;
                }
            }
            return;
        }

        for (int i = 0; i < startPerPlayer.Count; i++)
        {
            var cfg = startPerPlayer[i];
            if (cfg == null) continue;

            var w = GetWallet(cfg.playerId);
            if (w.gold == 0 && w.coal == 0)
            {
                w.gold = cfg.startGold;
                w.coal = cfg.startCoal;
            }
        }
    }

    private Wallet GetWallet(PlayerId id)
    {
        if (!wallets.TryGetValue(id, out var w))
        {
            w = new Wallet();
            wallets[id] = w;
        }
        return w;
    }

    public IReadOnlyList<PlayerId> ActivePlayers => activePlayers;

    public void SetCurrentPlayer(PlayerId playerId)
    {
        if (playerId == currentPlayer) return;

        currentPlayer = playerId;
        EnsureWallets();

        RecalculateIncome();
        RaiseChanged();
    }

    public void RecalculateIncome()
    {
        var w = GetWallet(currentPlayer);

        int g = 0;
        int c = 0;
        bool anyDepositExists = false;

        foreach (var d in ResourceDeposit.All)
        {
            if (d == null) continue;
            anyDepositExists = true;

            Tile tile = d.Tile;
            if (tile == null) continue;

            if (tile.Owner != currentPlayer)
                continue;

            int income = d.GetIncomePerTurn();
            if (d.type == ResourceType.Gold) g += income;
            else if (d.type == ResourceType.Coal) c += income;
        }

        if (!anyDepositExists)
        {
            g = fallbackGoldIncome;
            c = fallbackCoalIncome;
        }

        w.goldIncome = g;
        w.coalIncome = c;

        RaiseChanged();
    }

    public void ApplyTurnIncome()
    {
        var w = GetWallet(currentPlayer);
        w.gold += w.goldIncome;
        w.coal += w.coalIncome;
        RaiseChanged();
    }

    public bool CanAfford(int goldCost, int coalCost)
    {
        var w = GetWallet(currentPlayer);
        return w.gold >= goldCost && w.coal >= coalCost;
    }

    public bool TrySpend(int goldCost, int coalCost)
    {
        var w = GetWallet(currentPlayer);

        if (w.gold < goldCost || w.coal < coalCost)
            return false;

        w.gold -= goldCost;
        w.coal -= coalCost;
        RaiseChanged();
        return true;
    }

    public void AddGold(int amount)
    {
        var w = GetWallet(currentPlayer);
        w.gold += amount;
        RaiseChanged();
    }

    public void AddCoal(int amount)
    {
        var w = GetWallet(currentPlayer);
        w.coal += amount;
        RaiseChanged();
    }

    public int GetGold(PlayerId player) => GetWallet(player).gold;
    public int GetCoal(PlayerId player) => GetWallet(player).coal;

    private void RaiseChanged() => OnChanged?.Invoke();
}
