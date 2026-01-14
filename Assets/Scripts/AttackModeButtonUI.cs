using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttackModeButtonUI : MonoBehaviour
{
    [SerializeField] private UnitMovementSystem unitMovementSystem;

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    private void Awake()
    {
        if (unitMovementSystem == null)
            unitMovementSystem = FindObjectOfType<UnitMovementSystem>();

        if (button == null)
            button = GetComponent<Button>();

        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        Refresh();
    }

    private void OnClick()
    {
        if (unitMovementSystem == null) return;
        unitMovementSystem.ToggleAttackMode();
        Refresh();
    }

    public void Refresh()
    {
        if (unitMovementSystem == null || label == null) return;
        label.text = unitMovementSystem.AttackMode ? "ATTACK: ON" : "ATTACK";
    }
}
