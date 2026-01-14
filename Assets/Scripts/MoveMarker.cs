using UnityEngine;
using TMPro;

public class MoveMarker : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Label (TMP)")]
    [SerializeField] private TMP_Text label;

    [Tooltip("Насколько влево от центра кружка держать текст (в мировых единицах).")]
    [SerializeField] private float labelLeftOffset = 0.20f;

    [Tooltip("Насколько поднять текст над кружком (в мировых единицах).")]
    [SerializeField] private float labelUpOffset = 0.03f;

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock mpb;
    private Camera mainCamera;

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
        mainCamera = Camera.main;

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            label.text = "";

            // чтобы текст не исчезал из-за глубины / очереди
            var r = label.GetComponent<Renderer>();
            if (r != null && r.material != null)
            {
                r.material.renderQueue = 3100;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
    }

    private void Update()
    {
        // вращение кружка
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        if (label == null || mainCamera == null) return;
        if (!label.gameObject.activeSelf) return;

        // направление "вперёд" камеры в плоскости
        Vector3 camForward = mainCamera.transform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.001f) return;
        camForward.Normalize();

        // "влево" относительно камеры
        Vector3 camLeft = Vector3.Cross(Vector3.up, camForward).normalized;

        // центр кружка (мировой)
        Vector3 center = transform.position;

        // позиция текста: слева от центра + чуть вверх
        label.transform.position = center + camLeft * labelLeftOffset + Vector3.up * labelUpOffset;

        // поворот текста: лежит на земле (X=90) и смотрит к камере по Y
        Quaternion look = Quaternion.LookRotation(camForward);
        label.transform.rotation = Quaternion.Euler(90f, look.eulerAngles.y, 0f);
    }

    public void SetColor(Color c)
    {
        if (cachedRenderers == null) return;

        foreach (var r in cachedRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
        }
    }

    public void SetLabel(string text)
    {
        if (label == null) return;

        bool has = !string.IsNullOrWhiteSpace(text);
        label.gameObject.SetActive(has);
        label.text = has ? text : "";
    }
}
