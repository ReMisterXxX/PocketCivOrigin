using UnityEngine;
using TMPro;

/// UI верхней панели: золото, уголь, доход и номер хода
public class ResourceTopPanelUI : MonoBehaviour
{
    [Header("Gold UI")]
    public TextMeshProUGUI goldValueText;    // GoldValueText
    public TextMeshProUGUI goldIncomeText;   // GoldIncomeText

    [Header("Coal UI")]
    public TextMeshProUGUI coalValueText;    // CoalValueText
    public TextMeshProUGUI coalIncomeText;   // CoalIncomeText

    [Header("Turn UI")]
    public TextMeshProUGUI turnText;         // TurnLabel

    /// <summary>
    /// Обновить все показатели ресурсов по данным PlayerResources.
    /// </summary>
    public void UpdateAll(PlayerResources pr)
    {
        if (pr == null) return;

        if (goldValueText != null)
            goldValueText.text = pr.Gold.ToString();

        if (goldIncomeText != null)
            goldIncomeText.text = $"+{pr.GoldIncome}";

        if (coalValueText != null)
            coalValueText.text = pr.Coal.ToString();

        if (coalIncomeText != null)
            coalIncomeText.text = $"+{pr.CoalIncome}";
    }

    /// <summary>
    /// Обновить текст хода.
    /// </summary>
    public void UpdateTurn(int turn)
    {
        if (turnText != null)
            turnText.text = $"Turn {turn}";
    }
}
