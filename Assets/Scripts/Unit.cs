using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private UnitStats stats;

    [Tooltip("Поднять юнита над поверхностью тайла.")]
    [SerializeField] private float yOffset = 0.08f;

    [Tooltip("Поворот модели, если она изначально смотрит не по +Z (обычно 90 или 180).")]
    [SerializeField] private float modelYawOffset = 0f;

    [Tooltip("Какая часть является визуальной моделью (если null — будет использован transform).")]
    [SerializeField] private Transform modelRoot;

    [Header("Ground snap")]
    [Tooltip("С какой высоты делаем луч вниз для поиска поверхности тайла.")]
    [SerializeField] private float groundRayHeight = 5f;

    [Tooltip("Длина луча вниз.")]
    [SerializeField] private float groundRayLength = 20f;

    private PlayerId owner;
    private Tile currentTile;
    private int movesLeftThisTurn;

    // чтобы LateUpdate не мешал корутине движения
    private bool isMoving;

    // буфер для RaycastNonAlloc (без аллокаций)
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    public PlayerId Owner => owner;
    public Tile CurrentTile => currentTile;

    public int MovePointsPerTurn => (stats != null) ? stats.movePointsPerTurn : 3;
    public int MovesLeftThisTurn => movesLeftThisTurn;

    public void Initialize(PlayerId ownerId, Tile startTile)
    {
        owner = ownerId;
        SetTile(startTile, instant: true);
        ResetMoves();
    }

    public void ResetMoves()
    {
        movesLeftThisTurn = MovePointsPerTurn;
    }

    public bool HasMoves()
    {
        return movesLeftThisTurn > 0;
    }

    public void SpendMovePoint(int cost = 1)
    {
        movesLeftThisTurn = Mathf.Max(0, movesLeftThisTurn - Mathf.Max(1, cost));
    }

    public void SetMoving(bool value)
    {
        isMoving = value;
    }

    // ✅ ВОТ ЭТОГО НЕ ХВАТАЛО (его вызывает UnitMovementSystem)
    public void SnapToCurrentTile()
    {
        if (currentTile == null) return;
        transform.position = GetWorldPositionOnTile(currentTile);
    }

    public void SetTile(Tile tile, bool instant)
    {
        if (currentTile != null)
            currentTile.OnUnitLeave();

        currentTile = tile;

        if (currentTile != null)
            currentTile.OnUnitEnter();

        if (currentTile != null)
        {
            transform.position = GetWorldPositionOnTile(currentTile);
        }
    }

    /// <summary>
    /// Мировая позиция на тайле: XZ = центр тайла, Y = поверхность тайла (raycast) + yOffset.
    /// </summary>
    public Vector3 GetWorldPositionOnTile(Tile tile)
    {
        if (tile == null) return transform.position;

        Vector3 center = tile.transform.position;
        float surfaceY = GetTileSurfaceY(tile, center);
        return new Vector3(center.x, surfaceY + yOffset, center.z);
    }

    /// <summary>
    /// Возвращает текущую высоту поверхности тайла через Raycast в его коллайдер.
    /// Учитывает AnimateSelection(), потому что коллайдер двигается вместе с transform.
    /// </summary>
    private float GetTileSurfaceY(Tile tile, Vector3 tileCenter)
    {
        Vector3 origin = tileCenter + Vector3.up * groundRayHeight;
        Ray ray = new Ray(origin, Vector3.down);

        int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, groundRayLength);
        if (hitCount <= 0)
        {
            // fallback
            return tile.TopHeight;
        }

        float bestY = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitBuffer[i].collider;
            if (col == null) continue;

            // только коллайдеры этого тайла (не деревья/камни)
            Tile hitTile = col.GetComponentInParent<Tile>();
            if (hitTile != tile) continue;

            float y = hitBuffer[i].point.y;
            if (!found || y > bestY)
            {
                bestY = y;
                found = true;
            }
        }

        if (found)
            return bestY;

        return tile.TopHeight;
    }

    private void LateUpdate()
    {
        // ✅ пока не двигаемся — всегда "липнем" к текущей поверхности тайла
        if (isMoving) return;
        if (currentTile == null) return;

        transform.position = GetWorldPositionOnTile(currentTile);
    }

    public void FaceDirection(Vector3 worldDir)
    {
        if (worldDir.sqrMagnitude < 0.0001f) return;

        Vector3 flat = new Vector3(worldDir.x, 0f, worldDir.z).normalized;

        Quaternion look = Quaternion.LookRotation(flat, Vector3.up);
        Quaternion yawFix = Quaternion.Euler(0f, modelYawOffset, 0f);

        if (modelRoot != null)
            modelRoot.rotation = look * yawFix;
        else
            transform.rotation = look * yawFix;
    }
}
