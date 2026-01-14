using UnityEngine;

public class CityOwnerTint : MonoBehaviour
{
    private void Start()
    {
        Tile tile = GetComponentInParent<Tile>();
        if (tile == null) return;

        Color c = PlayerColorManager.GetColor(tile.Owner);

        // красим все рендереры в городе
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null || r.material == null) continue;
            r.material.color = c;
        }
    }
}
