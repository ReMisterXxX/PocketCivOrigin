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

    private bool isMoving;

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

    public void SnapToCurrentTile()
    {
        if (currentTile == null) return;
        transform.position = GetWorldPositionOnTile(currentTile);
    }

    // ✅ ВАЖНО: тут фиксим консистентность UnitOnTile/HasUnit
    public void SetTile(Tile tile, bool instant)
    {
        // Снимаем себя с предыдущего тайла
        if (currentTile != null)
        {
            // Новая система (реальная ссылка UnitOnTile)
            currentTile.ClearUnit(this);

            // Старая система (флаг HasUnit) — оставляем для совместимости
            currentTile.OnUnitLeave();
        }

        currentTile = tile;

        // Назначаем себя на новый тайл
        if (currentTile != null)
        {
            // Новая система (UnitOnTile + HasUnit)
            currentTile.AssignUnit(this);

            // Старая система — для совместимости
            currentTile.OnUnitEnter();

            transform.position = GetWorldPositionOnTile(currentTile);
        }
    }

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

        float bestY = tile.transform.position.y + tile.TopHeight;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            var h = hitBuffer[i];
            if (h.collider == null) continue;

            // берём ближайшее попадание
            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                bestY = h.point.y;
            }
        }

        return bestY;
    }

    public void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (modelRoot != null)
            modelRoot.rotation = look * Quaternion.Euler(0f, modelYawOffset, 0f);
        else
            transform.rotation = look * Quaternion.Euler(0f, modelYawOffset, 0f);
    }

    private void LateUpdate()
    {
        if (isMoving) return;
        if (currentTile == null) return;

        // держим на поверхности тайла
        transform.position = GetWorldPositionOnTile(currentTile);
    }
}
