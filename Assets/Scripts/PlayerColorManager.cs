using System.Collections.Generic;
using UnityEngine;

public enum PlayerId
{
    None   = -1,
    Player1 = 0,
    Player2 = 1,
    Player3 = 2,
    Player4 = 3
}

public static class PlayerColorManager
{
    private static readonly Color[] colorPool = new Color[]
    {
        new Color(0.95f, 0.10f, 0.10f), // Red
        new Color(1.00f, 0.50f, 0.00f), // Orange
        new Color(0.10f, 0.60f, 1.00f), // Blue
        new Color(0.60f, 0.20f, 1.00f), // Purple
        new Color(0.10f, 0.85f, 0.10f), // Green
        new Color(0.90f, 0.20f, 0.50f), // Pink
        new Color(0.20f, 0.90f, 0.80f), // Aqua
        new Color(0.95f, 0.85f, 0.10f), // Yellow
    };

    private static readonly Dictionary<PlayerId, Color> assignedColors = new Dictionary<PlayerId, Color>();

    public static void Reset()
    {
        assignedColors.Clear();
    }

    public static Color GetColor(PlayerId playerId)
    {
        if (assignedColors.TryGetValue(playerId, out var existing))
            return existing;

        foreach (var c in colorPool)
        {
            if (!assignedColors.ContainsValue(c))
            {
                assignedColors[playerId] = c;
                return c;
            }
        }

        // если игроков больше чем цветов — начнём повторять (на твой случай не критично)
        Color fallback = colorPool[0];
        assignedColors[playerId] = fallback;
        return fallback;
    }
}
