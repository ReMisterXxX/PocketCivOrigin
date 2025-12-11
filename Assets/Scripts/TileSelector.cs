using UnityEngine;
using UnityEngine.EventSystems;

public class TileSelector : MonoBehaviour
{
    public TileInfoUI tileInfoUI;

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
                Select(tile);
                return;
            }
        }

        // клик в пустоту — снимаем выделение
        DeselectCurrent();
    }

    void Select(Tile tile)
    {
        // если кликаем по уже выбранному тайлу —
        // не трогаем выделение, просто заново показываем панель
        if (selectedTile == tile)
        {
            if (tileInfoUI != null)
                tileInfoUI.ShowForTile(tile);

            return;
        }

        // снимаем выделение со старого тайла
        if (selectedTile != null)
        {
            selectedTile.DeselectTile();
        }

        // выделяем новый тайл
        selectedTile = tile;
        selectedTile.SelectTile();

        // показываем панель
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
