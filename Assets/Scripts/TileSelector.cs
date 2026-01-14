using UnityEngine;
using UnityEngine.EventSystems;

public class TileSelector : MonoBehaviour
{
    [Header("UI")]
    public TileInfoUI tileInfoUI;

    [Header("Units")]
    public UnitMovementSystem unitMovementSystem;

    [Header("Combat UI")]
    public AttackModeButtonUI attackModeButtonUI;
    public PlayerResources playerResources;

    private Tile selectedTile = null;

    void Start()
    {
        if (unitMovementSystem == null) unitMovementSystem = FindObjectOfType<UnitMovementSystem>();
        if (tileInfoUI == null) tileInfoUI = FindObjectOfType<TileInfoUI>();
        if (attackModeButtonUI == null) attackModeButtonUI = FindObjectOfType<AttackModeButtonUI>(true);
        if (playerResources == null) playerResources = FindObjectOfType<PlayerResources>();

        if (attackModeButtonUI != null)
            attackModeButtonUI.SetVisible(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectTile();
        }
    }

    void TrySelectTile()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Tile tile = hit.collider.GetComponentInParent<Tile>();
            if (tile != null)
            {
                SelectTile(tile);

                if (unitMovementSystem != null)
                {
                    unitMovementSystem.OnTileSelectionChanged();
                    unitMovementSystem.OnTileClicked(tile);
                }

                // ✅ После OnTileClicked() система могла выбрать юнита.
                RefreshAttackButton();

                return;
            }
        }

        DeselectCurrent();
        if (unitMovementSystem != null)
        {
            unitMovementSystem.ClearSelection();
            unitMovementSystem.OnTileSelectionChanged();
        }

        if (attackModeButtonUI != null)
            attackModeButtonUI.SetVisible(false);
    }

    private void RefreshAttackButton()
    {
        if (attackModeButtonUI == null || unitMovementSystem == null) return;

        Unit u = unitMovementSystem.SelectedUnit;
        if (u == null)
        {
            attackModeButtonUI.SetVisible(false);
            return;
        }

        bool isMine = playerResources == null || u.Owner == playerResources.CurrentPlayer;
        attackModeButtonUI.SetVisible(isMine);
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
