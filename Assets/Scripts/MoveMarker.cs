using UnityEngine;

public class MoveMarker : MonoBehaviour
{
    public Tile TargetTile { get; private set; }

    public void Init(Tile tile)
    {
        TargetTile = tile;
        gameObject.name = $"MoveMarker_{tile.GridPosition.x}_{tile.GridPosition.y}";
    }
}
