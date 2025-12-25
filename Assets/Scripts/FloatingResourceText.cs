using UnityEngine;
using TMPro;

public class FloatingResourceText : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro text;

    [Header("Animation")]
    public float lifetime = 0.7f;
    public float moveUpDistance = 0.22f;
    [Range(0f, 1f)] public float fadeStart = 0.2f;

    [Header("Billboard")]
    public bool faceCamera = true;
    public bool yAxisOnly = true;

    private float startTime;
    private Vector3 startPos;
    private Camera cam;

    private void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TextMeshPro>();

        cam = Camera.main;
    }

    private void OnEnable()
    {
        startTime = Time.time;
        startPos = transform.position;
    }

    public void SetText(string value, Color color)
    {
        if (text == null) return;
        text.text = value;
        text.color = color;
    }

    private void LateUpdate()
    {
        if (!faceCamera) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 dir = transform.position - cam.transform.position;
        if (yAxisOnly) dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void Update()
    {
        float t = (Time.time - startTime) / Mathf.Max(0.0001f, lifetime);

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = startPos + Vector3.up * moveUpDistance * t;

        if (text != null)
        {
            float ft = Mathf.InverseLerp(fadeStart, 1f, t);
            Color c = text.color;
            c.a = Mathf.Lerp(1f, 0f, ft);
            text.color = c;
        }
    }
}
