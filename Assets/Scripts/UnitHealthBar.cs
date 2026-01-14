using UnityEngine;
using UnityEngine.UI;

public class UnitHealthBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Unit unit;
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerResources playerResources;

    [Header("Colors")]
    [SerializeField] private Color myHpColor = new Color(0.15f, 0.95f, 0.25f, 1f);
    [SerializeField] private Color otherHpColor = new Color(0.95f, 0.20f, 0.20f, 1f);

    [Header("Follow")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private Camera cam;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (unit == null) unit = GetComponentInParent<Unit>();
        if (playerResources == null) playerResources = FindObjectOfType<PlayerResources>();
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

        // цвет по принадлежности
        if (playerResources != null && u.Owner == playerResources.CurrentPlayer)
            fillImage.color = myHpColor;
        else
            fillImage.color = otherHpColor;
    }
}
