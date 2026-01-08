using UnityEngine;

public class MoveMarker : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void SetColor(Color c)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;

        foreach (var r in cachedRenderers)
        {
            if (r == null) continue;

            r.GetPropertyBlock(mpb);
            // большинство шейдеров понимают _Color
            mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
        }
    }
}
