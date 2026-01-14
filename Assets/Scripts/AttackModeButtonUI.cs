using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttackModeButtonUI : MonoBehaviour
{
    [SerializeField] private UnitMovementSystem unitMovementSystem;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);

        Refresh();
    }

    private void OnClick()
    {
        if (unitMovementSystem == null) return;
        unitMovementSystem.ToggleAttackMode();
        Refresh();
    }

    private void Refresh()
    {
        if (unitMovementSystem == null || label == null) return;
        label.text = unitMovementSystem.AttackMode ? "ATTACK: ON" : "ATTACK: OFF";
    }
}
