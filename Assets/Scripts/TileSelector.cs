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
        {
            TrySelectTile();
        }
    }

    void TrySelectTile()
    {
        // если клик по UI — игнорируем
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Tile tile = hit.collider.GetComponentInParent<Tile>();

            if (tile != null)
            {
                SelectTile(tile);

                // ✅ если тайл поднялся/опустился — переснапаем выбранного юнита по высоте
                if (unitMovementSystem != null)
                    unitMovementSystem.OnTileSelectionChanged();

                // ✅ пробрасываем клик в систему юнитов (чтобы работало как раньше)
                if (unitMovementSystem != null)
                    unitMovementSystem.OnTileClicked(tile);

                return;
            }
        }

        // кликнули в пустоту
        DeselectCurrent();

        if (unitMovementSystem != null)
        {
            unitMovementSystem.ClearSelection();
            unitMovementSystem.OnTileSelectionChanged();
        }
    }

    void SelectTile(Tile tile)
    {
        if (selectedTile == tile)
            return;

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
