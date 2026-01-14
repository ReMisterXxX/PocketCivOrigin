using UnityEngine;
using UnityEngine.UI;

public class UnitHealthBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Unit unit;
    [SerializeField] private Image fillImage;

    [Header("Follow")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private Camera cam;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (unit == null) unit = GetComponentInParent<Unit>();
    }

    private void OnEnable()
    {
        if (unit != null) unit.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(unit);
    }

    private void OnDisable()
    {
        if (unit != null) unit.OnHealthChanged -= HandleHealthChanged;
    }

    private void LateUpdate()
    {
        if (unit == null || cam == null) return;

        transform.position = unit.transform.position + worldOffset;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }

    private void HandleHealthChanged(Unit u)
    {
        if (u == null || fillImage == null) return;

        float t = (u.MaxHP <= 0) ? 0f : (float)u.CurrentHP / u.MaxHP;
        fillImage.fillAmount = t;

        // ✅ Цвет HP должен соответствовать цвету территории/игрока, а не "мой/чужой".
        Color c = PlayerColorManager.GetColor(u.Owner);
        c.a = 1f;
        fillImage.color = c;
    }
}
