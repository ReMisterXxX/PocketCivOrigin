using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [Header("Start values")]
    [SerializeField] private int startGold = 10;
    [SerializeField] private int startCoal = 10;

    [Header("Fallback income (used if auto-income can't be detected)")]
    [SerializeField] private int fallbackGoldIncome = 5;
    [SerializeField] private int fallbackCoalIncome = 5;

    [Header("Runtime")]
    [SerializeField] private int gold;
    [SerializeField] private int coal;
    [SerializeField] private int goldIncome;
    [SerializeField] private int coalIncome;

    // Если у тебя есть PlayerId / текущий игрок — оставь.
    // Если в проекте PlayerId называется иначе — просто замени тип на твой.
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
    /// Пересчитать доход (пытаемся автоматически найти источники дохода через reflection,
    /// иначе используем fallback).
    /// </summary>
    public void RecalculateIncome()
    {
        int g = 0;
        int c = 0;

        bool foundAny = TryCalculateIncomeAuto(ref g, ref c);

        if (!foundAny)
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

    // =========================
    // AUTO INCOME (reflection)
    // =========================

    private bool TryCalculateIncomeAuto(ref int goldOut, ref int coalOut)
    {
        // Ищем любые компоненты в сцене, у которых имя типа похоже на источники дохода:
        // ResourceDeposit, Mine, IncomeSource и т.п.
        // Тебе не нужно менять код, если названия отличаются — он всё равно попробует.
        var all = FindObjectsOfType<MonoBehaviour>(true);

        bool found = false;

        foreach (var mb in all)
        {
            if (mb == null) continue;

            var t = mb.GetType();
            string typeName = t.Name;

            // фильтр — чтобы не рефлектить всё подряд
            if (!LooksLikeIncomeSource(typeName))
                continue;

            // пытаемся:
            // 1) понять ресурс (gold/coal)
            // 2) взять incomePerTurn / amount / value
            // 3) понять, принадлежит ли текущему игроку (через tile.owner / owner / playerId)
            if (!IsOwnedByCurrentPlayer(mb))
                continue;

            if (!TryGetIncomeValue(mb, out int income))
                continue;

            if (!TryGetResourceKind(mb, out string kind))
                continue;

            if (kind == "gold") goldOut += income;
            else if (kind == "coal") coalOut += income;
            else continue;

            found = true;
        }

        return found;
    }

    private bool LooksLikeIncomeSource(string typeName)
    {
        typeName = typeName.ToLowerInvariant();
        return typeName.Contains("deposit")
            || typeName.Contains("resource")
            || typeName.Contains("mine")
            || typeName.Contains("income");
    }

    private bool TryGetIncomeValue(object obj, out int income)
    {
        income = 0;
        var t = obj.GetType();

        // Частые имена полей/свойств
        string[] candidates =
        {
            "incomePerTurn", "IncomePerTurn",
            "income", "Income",
            "amountPerTurn", "AmountPerTurn",
            "perTurn", "PerTurn",
            "value", "Value",
            "yield", "Yield"
        };

        foreach (var name in candidates)
        {
            if (TryGetIntMember(t, obj, name, out income))
                return true;
        }

        return false;
    }

    private bool TryGetResourceKind(object obj, out string kind)
    {
        kind = null;
        var t = obj.GetType();

        // Частые имена: type / resourceType / kind
        string[] candidates =
        {
            "type", "Type",
            "resourceType", "ResourceType",
            "kind", "Kind",
            "resource", "Resource"
        };

        foreach (var name in candidates)
        {
            if (TryGetStringLikeEnum(t, obj, name, out kind))
            {
                kind = kind.ToLowerInvariant();
                if (kind.Contains("gold")) { kind = "gold"; return true; }
                if (kind.Contains("coal")) { kind = "coal"; return true; }
            }
        }

        return false;
    }

    private bool IsOwnedByCurrentPlayer(object obj)
    {
        var t = obj.GetType();

        // 1) Если источник сам хранит owner/playerId
        string[] ownerCandidates =
        {
            "owner", "Owner",
            "player", "Player",
            "playerId", "PlayerId"
        };

        foreach (var name in ownerCandidates)
        {
            if (TryGetPlayerId(t, obj, name, out var pid))
                return pid.Equals(currentPlayer);
        }

        // 2) Если у него есть Tile ссылка: tile / Tile / currentTile
        string[] tileCandidates = { "tile", "Tile", "currentTile", "CurrentTile" };

        foreach (var name in tileCandidates)
        {
            if (TryGetObjectMember(t, obj, name, out object tileObj) && tileObj != null)
            {
                // у Tile обычно есть Owner/owner/playerId
                var tileType = tileObj.GetType();
                if (TryGetPlayerId(tileType, tileObj, "Owner", out var pid)) return pid.Equals(currentPlayer);
                if (TryGetPlayerId(tileType, tileObj, "owner", out pid)) return pid.Equals(currentPlayer);
                if (TryGetPlayerId(tileType, tileObj, "PlayerId", out pid)) return pid.Equals(currentPlayer);
                if (TryGetPlayerId(tileType, tileObj, "playerId", out pid)) return pid.Equals(currentPlayer);
            }
        }

        // Если не смогли определить владельца — считаем НЕ принадлежащим (чтобы не накручивать чужое)
        return false;
    }

    // ===== Reflection helpers =====

    private bool TryGetIntMember(Type t, object obj, string name, out int value)
    {
        value = 0;

        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(int))
        {
            value = (int)f.GetValue(obj);
            return true;
        }

        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(int) && p.CanRead)
        {
            value = (int)p.GetValue(obj);
            return true;
        }

        return false;
    }

    private bool TryGetObjectMember(Type t, object obj, string name, out object value)
    {
        value = null;

        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null)
        {
            value = f.GetValue(obj);
            return true;
        }

        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanRead)
        {
            value = p.GetValue(obj);
            return true;
        }

        return false;
    }

    private bool TryGetStringLikeEnum(Type t, object obj, string name, out string str)
    {
        str = null;

        if (!TryGetObjectMember(t, obj, name, out object raw) || raw == null)
            return false;

        str = raw.ToString();
        return !string.IsNullOrEmpty(str);
    }

    private bool TryGetPlayerId(Type t, object obj, string name, out PlayerId pid)
    {
        pid = default;

        if (!TryGetObjectMember(t, obj, name, out object raw) || raw == null)
            return false;

        if (raw is PlayerId p)
        {
            pid = p;
            return true;
        }

        // если где-то owner хранится как int/string — попробуем распарсить по имени enum
        string s = raw.ToString();
        if (Enum.TryParse(s, out PlayerId parsed))
        {
            pid = parsed;
            return true;
        }

        return false;
    }
}
