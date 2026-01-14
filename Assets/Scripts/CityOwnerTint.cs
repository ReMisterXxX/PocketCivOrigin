using UnityEngine;

public class CityOwnerTint : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Если false — город НЕ будет перекрашиваться цветом владельца (требование: сами города не красить).")]
    [SerializeField] private bool tintCity = false;

    private void Start()
    {
        if (!tintCity) return;

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
