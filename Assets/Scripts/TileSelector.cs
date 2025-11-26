using UnityEngine;

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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Tile tile = hit.collider.GetComponentInParent<Tile>();

            if (tile != null)
            {
                Select(tile);
                return; // <-- ключевое, панель не скрывается если попали по тайлу
            }
        }

        // сюда попадаем только если НЕ попали по тайлу

        DeselectCurrent();
    }

    void Select(Tile tile)
{
    // Если кликаем на уже выбранный тайл — НИЧЕГО не делаем
    if (selectedTile == tile)
        return;

    // Снимаем выделение со старого тайла
    if (selectedTile != null)
    {
        selectedTile.DeselectTile();
    }

    // Выбираем новый тайл
    selectedTile = tile;
    selectedTile.SelectTile();

    // Показываем панель
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
