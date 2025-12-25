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

    [Header("Runtime")]
    [SerializeField] private PlayerId owner = PlayerId.None;
    [SerializeField] private Tile currentTile;

    [SerializeField] private int movesLeftThisTurn = 0;

    public UnitStats Stats => stats;
    public PlayerId Owner => owner;
    public Tile CurrentTile => currentTile;
    public int MovesLeftThisTurn => movesLeftThisTurn;

    public int MovePointsPerTurn => (stats != null && stats.movePointsPerTurn > 0) ? stats.movePointsPerTurn : 3;

    private void Awake()
    {
        if (modelRoot == null) modelRoot = transform;
    }

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

    public void SetTile(Tile tile, bool instant)
    {
        // снять флаг с прошлого тайла
        if (currentTile != null)
            currentTile.OnUnitLeave();

        currentTile = tile;

        // поставить флаг на новый тайл
        if (currentTile != null)
            currentTile.OnUnitEnter();

        if (currentTile != null)
        {
            Vector3 target = GetWorldPositionOnTile(currentTile);
            if (instant) transform.position = target;
            else transform.position = target; // плавное движение делает UnitMovementSystem
        }
    }

    public Vector3 GetWorldPositionOnTile(Tile tile)
    {
        // центр тайла = позиция тайла (у тебя тайлы поднимаются при селекте, но их transform.position — норм)
        // чтобы всегда быть над поверхностью — используем tile.TopHeight + yOffset
        float y = (tile != null) ? (tile.TopHeight + yOffset) : yOffset;
        Vector3 p = (tile != null) ? tile.transform.position : transform.position;
        return new Vector3(p.x, y, p.z);
    }

    public void FaceDirection(Vector3 worldDir)
    {
        if (worldDir.sqrMagnitude < 0.0001f) return;

        // Смотрим туда, куда идём (по XZ)
        Vector3 flat = new Vector3(worldDir.x, 0f, worldDir.z).normalized;

        Quaternion look = Quaternion.LookRotation(flat, Vector3.up);
        Quaternion yawFix = Quaternion.Euler(0f, modelYawOffset, 0f);

        // крутим именно визуальную модель
        if (modelRoot != null)
            modelRoot.rotation = look * yawFix;
        else
            transform.rotation = look * yawFix;
    }
}
