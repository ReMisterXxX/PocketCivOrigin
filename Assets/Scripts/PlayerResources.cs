using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [Header("Owner")]
    public PlayerId owner = PlayerId.Player1;

    [Header("Starting stock")]
    [SerializeField] private int startGold = 10;
    [SerializeField] private int startCoal = 10;

    [Header("Current stock (read-only)")]
    [SerializeField] private int gold;
    [SerializeField] private int coal;

    [Header("Income per turn (read-only)")]
    [SerializeField] private int goldIncome;
    [SerializeField] private int coalIncome;

    public int Gold => gold;
    public int Coal => coal;
    public int GoldIncome => goldIncome;
    public int CoalIncome => coalIncome;

    private void Awake()
    {
        gold = startGold;
        coal = startCoal;
    }

    private void Start()
    {
        RecalculateIncomeFromDeposits();
    }

    // ✅ Чтобы TurnManager мог вызывать вариант с bool (и не было ошибок компиляции)
    public void RecalculateIncomeFromDeposits(bool _spawnPopups)
    {
        RecalculateIncomeFromDeposits();
    }

    public void RecalculateIncomeFromDeposits()
    {
        int newGoldIncome = 0;
        int newCoalIncome = 0;

        ResourceDeposit[] deposits = FindObjectsOfType<ResourceDeposit>(includeInactive: false);

        foreach (ResourceDeposit dep in deposits)
        {
            if (dep == null) continue;
            if (dep.Tile == null) continue;
            if (dep.Tile.Owner != owner) continue;

            int income = dep.GetIncomePerTurn();

            switch (dep.type)
            {
                case ResourceType.Gold: newGoldIncome += income; break;
                case ResourceType.Coal: newCoalIncome += income; break;
            }
        }

        goldIncome = newGoldIncome;
        coalIncome = newCoalIncome;
    }

    public void ApplyTurnIncome()
    {
        gold += goldIncome;
        coal += coalIncome;

        Color c = PlayerColorManager.GetColor(owner);

        ResourceDeposit[] deposits = FindObjectsOfType<ResourceDeposit>(includeInactive: false);
        foreach (ResourceDeposit dep in deposits)
        {
            if (dep == null) continue;
            if (dep.Tile == null) continue;
            if (dep.Tile.Owner != owner) continue;

            int income = dep.GetIncomePerTurn();
            dep.ShowIncomePopup(income, c);
        }
    }
}
