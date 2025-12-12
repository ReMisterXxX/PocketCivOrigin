using UnityEngine;

/// Тип месторождения
public enum ResourceType
{
    Gold,
    Coal
}

/// Компонент на префабе месторождения (руда, уголь и т.п.)
public class ResourceDeposit : MonoBehaviour
{
    // На каком тайле стоит месторождение
    public Tile Tile { get; private set; }

    // Тип ресурса (золото / уголь)
    public ResourceType type;

    // Есть ли построенная шахта
    public bool HasMine { get; private set; } = false;

    /// <summary>
    /// Инициализация месторождения при спавне из MapGenerator.
    /// </summary>
    public void Init(Tile tile, ResourceType resourceType)
    {
        Tile = tile;
        type = resourceType;
        HasMine = false;
    }

    /// <summary>
    /// Вызывай, когда игрок построит шахту.
    /// </summary>
    public void BuildMine()
    {
        HasMine = true;
    }
}
