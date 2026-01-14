using System;
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
    public UnitStats Stats => stats;
    private Tile currentTile;
    private int movesLeftThisTurn;

    // HP
    private int currentHp;

    // чтобы LateUpdate не мешал корутине движения
    private bool isMoving;

    // буфер для RaycastNonAlloc (без аллокаций)
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    public PlayerId Owner => owner;
    public Tile CurrentTile => currentTile;

    public int MovePointsPerTurn => (stats != null) ? stats.movePointsPerTurn : 3;
    public int MovesLeftThisTurn => movesLeftThisTurn;

    public int Attack => (stats != null) ? stats.attack : 1;
    public int Defense => (stats != null) ? stats.defense : 1;

    public int MaxHP => (stats != null) ? stats.hp : 10;
    public int CurrentHP => currentHp;
    public bool IsDead => currentHp <= 0;

    public event Action<Unit> OnHealthChanged;

    public void Initialize(PlayerId ownerId, Tile startTile)
    {
        owner = ownerId;

        // HP при спавне
        currentHp = MaxHP;

        SetTile(startTile, instant: true);
        ResetMoves();
        RaiseHealthChanged();
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

    public void ConsumeAllMoves()
    {
        movesLeftThisTurn = 0;
    }

    public void SetMoving(bool value)
    {
        isMoving = value;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHp = Mathf.Max(0, currentHp - amount);
        RaiseHealthChanged();
    }

    private void RaiseHealthChanged()
    {
        OnHealthChanged?.Invoke(this);
    }

    public void Die()
    {
        // аккуратно очистить ссылку на тайле
        if (currentTile != null)
        {
            // старое поведение
            currentTile.OnUnitLeave();

            // новое (если используешь AssignUnit/UnitOnTile)
            currentTile.ClearUnit(this);
        }

        Destroy(gameObject);
    }

    // ✅ выравнивание по текущему тайлу (использует UnitMovementSystem)
    public void SnapToCurrentTile()
    {
        if (currentTile == null) return;
        transform.position = GetWorldPositionOnTile(currentTile);
    }

    public void SetTile(Tile tile, bool instant)
    {
        // снять со старого тайла
        if (currentTile != null)
        {
            currentTile.OnUnitLeave();
            currentTile.ClearUnit(this);
        }

        currentTile = tile;

        // поставить на новый тайл
        if (currentTile != null)
        {
            currentTile.OnUnitEnter();
            currentTile.AssignUnit(this);
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
        // пока не двигаемся — всегда "липнем" к текущей поверхности тайла
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
