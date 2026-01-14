using System.Collections;
using System.Collections.Generic;
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

    // ✅ кто уже нажал "Next Turn" в текущем раунде
    private HashSet<PlayerId> endedThisRound = new HashSet<PlayerId>();

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

        // стартовый апдейт (без начисления, просто пересчёт)
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

    // ✅ Теперь NextTurn = "я закончил"
    public void NextTurn()
    {
        if (isAdvancingTurn) return;
        StartCoroutine(EndTurnRequestRoutine());
    }

    private IEnumerator EndTurnRequestRoutine()
    {
        isAdvancingTurn = true;
        if (nextTurnButton != null) nextTurnButton.interactable = false;

        yield return null;

        if (playerResources == null)
        {
            if (nextTurnButton != null) nextTurnButton.interactable = true;
            isAdvancingTurn = false;
            yield break;
        }

        PlayerId current = playerResources.CurrentPlayer;

        // защита от дабл-клика
        endedThisRound.Add(current);

        // ✅ Проверяем: все активные игроки нажали?
        bool allEnded = true;
        var active = playerResources.ActivePlayers;

        if (active != null && active.Count > 0)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (!endedThisRound.Contains(active[i]))
                {
                    allEnded = false;
                    break;
                }
            }
        }
        else
        {
            allEnded = true;
        }

        if (allEnded)
        {
            // ✅ Раунд завершён — начинается НОВЫЙ ХОД (вот тут начисляем доход)
            endedThisRound.Clear();
            currentTurn++;

            // 1) начислить доход ВСЕМ игрокам один раз в начале нового хода
            ApplyIncomeForAllPlayersAtNewTurn();

            // 2) начать новый ход с первого активного игрока (обычно Player1)
            PlayerId first = (active != null && active.Count > 0) ? active[0] : current;

            // активируем игрока БЕЗ повторного начисления
            ActivatePlayer_NoIncome(first);

            // попапы дохода показываем только для активного игрока (начало нового хода)
            ShowIncomePopupsForCurrentPlayer();

            if (topPanelUI != null)
                topPanelUI.UpdateTurn(currentTurn);
        }
        else
        {
            // ✅ Раунд НЕ завершён — просто передаём управление следующему, БЕЗ начисления дохода
            PlayerId next = GetNextNotEndedPlayer(current);
            ActivatePlayer_NoIncome(next);
        }

        yield return null;

        if (nextTurnButton != null) nextTurnButton.interactable = true;
        isAdvancingTurn = false;
    }

    // ✅ Переключить активного игрока: камера + ресет юнитов + UI
    // ❌ Никаких ApplyTurnIncome/попапов тут нет
    private void ActivatePlayer_NoIncome(PlayerId who)
    {
        if (playerResources == null) return;

        playerResources.SetCurrentPlayer(who);

        Tile startCity = MapGenerator.Instance?.GetStartCityTile(who);
        CameraController cam = FindObjectOfType<CameraController>();
        if (cam != null && startCity != null)
            cam.JumpToPosition(startCity.transform.position);

        // сброс ходов юнитам активного игрока
        if (unitMovementSystem != null)
        {
            unitMovementSystem.ClearSelection();
            unitMovementSystem.ResetUnitsForNewTurn(playerResources.CurrentPlayer);
        }

        // пересчитаем доход (цифры на панели), но НЕ начисляем
        playerResources.RecalculateIncome();

        // верхняя панель ресурсов (цифры)
        if (topPanelUI != null)
            topPanelUI.UpdateAll(playerResources);
    }

    // ✅ Начисление дохода всем игрокам (один раз на новый ход)
    private void ApplyIncomeForAllPlayersAtNewTurn()
    {
        if (playerResources == null) return;

        var active = playerResources.ActivePlayers;
        if (active == null || active.Count == 0) return;

        // сохраним текущего, чтобы аккуратно вернуть после начислений (на всякий случай)
        PlayerId saved = playerResources.CurrentPlayer;

        for (int i = 0; i < active.Count; i++)
        {
            PlayerId p = active[i];
            playerResources.SetCurrentPlayer(p);

            playerResources.RecalculateIncome();
            playerResources.ApplyTurnIncome();
        }

        // вернуть как было
        playerResources.SetCurrentPlayer(saved);

        // после начисления можно обновить UI (цифры) — но окончательно обновим при ActivatePlayer_NoIncome(first)
        if (topPanelUI != null)
            topPanelUI.UpdateAll(playerResources);
    }

    // ✅ следующий игрок, который ещё не нажал "Next Turn"
    private PlayerId GetNextNotEndedPlayer(PlayerId current)
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

        for (int step = 1; step <= list.Count; step++)
        {
            int nextIdx = (idx + step) % list.Count;
            PlayerId p = list[nextIdx];
            if (!endedThisRound.Contains(p))
                return p;
        }

        return GetNextPlayer(current);
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
