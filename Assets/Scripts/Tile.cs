using System.Collections.Generic;
using UnityEngine;

public enum TileTerrainType
{
    Grass,
    Water,
    Forest,
    Mountain
}

public class Tile : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public TileTerrainType TerrainType { get; private set; }

    // список всех декораций на тайле
    public List<GameObject> Decorations { get; } = new List<GameObject>();

    public bool HasUnit { get; private set; }
    public bool HasBuilding { get; private set; }

    public void Init(Vector2Int gridPos, TileTerrainType terrain)
    {
        GridPosition = gridPos;
        TerrainType = terrain;

        gameObject.name = $"Tile_{gridPos.x}_{gridPos.y}";
    }

    public void RegisterDecoration(GameObject deco)
    {
        if (deco != null && !Decorations.Contains(deco))
        {
            Decorations.Add(deco);
        }
    }

    // ЮНИТ ЗАШЁЛ НА ТАЙЛ
    public void OnUnitEnter()
    {
        HasUnit = true;
        UpdateDecorationVisibility();
    }

    // ЮНИТ ПОКИНУЛ ТАЙЛ
    public void OnUnitLeave()
    {
        HasUnit = false;
        UpdateDecorationVisibility();
    }

    // ПОСТРОЙКА ПОЯВИЛАСЬ / ИСЧЕЗЛА
    public void SetBuildingPresent(bool hasBuilding)
    {
        HasBuilding = hasBuilding;
        UpdateDecorationVisibility();
    }

    private void UpdateDecorationVisibility()
    {
        bool shouldHide = HasUnit || HasBuilding;

        foreach (var deco in Decorations)
        {
            if (deco != null)
                deco.SetActive(!shouldHide);
        }
    }
}
