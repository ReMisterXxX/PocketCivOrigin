using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [Header("Owner")]
    public PlayerId owner = PlayerId.Player1;

    [Header("Current stock")]
    public int Gold = 10;
    public int Coal = 10;

    [Header("Income per turn")]
    public int GoldIncome;
    public int CoalIncome;

    /// <summary>
    /// Пересчитать доход с месторождений, принадлежащих этому игроку.
    /// Вызывается:
    /// - в Start() у TurnManager, чтобы доход был виден сразу
    /// - после каждого хода (на случай новых шахт/городов).
    /// </summary>
    public void RecalculateIncomeFromDeposits()
    {
        int newGoldIncome = 0;
        int newCoalIncome = 0;

        ResourceDeposit[] allDeposits = FindObjectsOfType<ResourceDeposit>();

        foreach (ResourceDeposit dep in allDeposits)
        {
            if (dep == null) continue;
            if (dep.Tile == null) continue;

            // считаем только месторождения на тайлах этого игрока
            if (dep.Tile.Owner != owner)
                continue;

            // обычный тайл: 5 / 8
            // гора: 7 / 10
            bool isMountain = dep.Tile.TerrainType == TileTerrainType.Mountain;

            int income;
            if (isMountain)
            {
                income = dep.HasMine ? 10 : 7;
            }
            else
            {
                income = dep.HasMine ? 8 : 5;
            }

            switch (dep.type)
            {
                case ResourceType.Gold:
                    newGoldIncome += income;
                    break;

                case ResourceType.Coal:
                    newCoalIncome += income;
                    break;
            }
        }

        GoldIncome = newGoldIncome;
        CoalIncome = newCoalIncome;
    }

    /// <summary>
    /// Добавить доход за ход к текущим запасам.
    /// Вызывается один раз при нажатии Next Turn.
    /// </summary>
    public void ApplyTurnIncome()
    {
        Gold += GoldIncome;
        Coal += CoalIncome;
    }
}
