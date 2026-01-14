using UnityEngine;
using UnityEngine.EventSystems;

public class TileSelector : MonoBehaviour
{
    [Header("UI")]
    public TileInfoUI tileInfoUI;

    [Header("Units")]
    public UnitMovementSystem unitMovementSystem;

    private Tile selectedTile = null;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TrySelect();
    }

    void TrySelect()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            DeselectCurrent();

            if (unitMovementSystem != null)
            {
                unitMovementSystem.ClearSelection();
                unitMovementSystem.OnTileSelectionChanged();
            }
            return;
        }

        // =========================
        // 1) КЛИК ПО ЮНИТУ
        // =========================
        Unit clickedUnit = hit.collider.GetComponentInParent<Unit>();
        if (clickedUnit != null && unitMovementSystem != null)
        {
            // В AttackMode (или если атака уже крутится) — НИЧЕГО НЕ ПОДНИМАЕМ.
            // Просто отдаём клик системе юнитов.
            if (unitMovementSystem.AttackMode || unitMovementSystem.IsAttackInProgress)
            {
                unitMovementSystem.HandleUnitClick(clickedUnit);
                unitMovementSystem.OnTileSelectionChanged();
                return;
            }

            // Обычный режим: выделяем юнита и поднимаем тайл ПОД НИМ
            unitMovementSystem.SelectUnitFromClick(clickedUnit);
            unitMovementSystem.OnTileSelectionChanged();

            if (clickedUnit.CurrentTile != null)
                SelectTile(clickedUnit.CurrentTile);

            return;
        }

        // =========================
        // 2) КЛИК ПО ТАЙЛУ
        // =========================
        Tile tile = hit.collider.GetComponentInParent<Tile>();
        if (tile != null)
        {
            if (unitMovementSystem != null)
            {
                bool consumed = unitMovementSystem.HandleTileClick(tile);
                unitMovementSystem.OnTileSelectionChanged();

                if (consumed)
                {
                    // ✅ фикс: после атаки возвращаем поднятие на атакующего
                    ForceTileToSelectedUnit();
                    return;
                }

                // ✅ Если AttackMode включен — не меняем поднятый тайл вообще
                if (unitMovementSystem.AttackMode || unitMovementSystem.IsAttackInProgress)
                    return;
            }

            // обычное выделение тайла
            SelectTile(tile);
            return;
        }

        // =========================
        // 3) КЛИК В ПУСТОТУ
        // =========================
        DeselectCurrent();

        if (unitMovementSystem != null)
        {
            unitMovementSystem.ClearSelection();
            unitMovementSystem.OnTileSelectionChanged();
        }
    }

    private void ForceTileToSelectedUnit()
    {
        if (unitMovementSystem == null) return;

        Unit su = unitMovementSystem.SelectedUnit;
        if (su == null) return;

        Tile t = su.CurrentTile;
        if (t == null) return;

        // поднимаем тайл под выбранным юнитом обратно
        SelectTile(t);
    }


    void SelectTile(Tile tile)
    {
        if (tile == null) return;
        if (selectedTile == tile) return;

        if (selectedTile != null)
            selectedTile.DeselectTile();

        selectedTile = tile;
        selectedTile.SelectTile();

        if (tileInfoUI != null)
            tileInfoUI.ShowForTile(selectedTile);
    }

    void DeselectCurrent()
    {
        if (selectedTile != null)
        {
            selectedTile.DeselectTile();
            selectedTile = null;
        }

        if (tileInfoUI != null)
            tileInfoUI.Hide();
    }
}
