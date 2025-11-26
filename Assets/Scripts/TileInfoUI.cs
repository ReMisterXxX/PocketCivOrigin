using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TileInfoUI : MonoBehaviour
{
    [Header("Ссылки на UI")]
    public GameObject panel;          // сам Panel
    public TextMeshProUGUI titleText; // заголовок, например "Тайл (x,y)"
    public TextMeshProUGUI infoText;  // текст с инфой (высота, биом)

    public Button infoButton;
    public Button buildButton;

    private Tile currentTile;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (infoButton != null)
            infoButton.onClick.AddListener(OnInfoClicked);

        if (buildButton != null)
            buildButton.onClick.AddListener(OnBuildClicked);
    }

    public void ShowForTile(Tile tile)
    {
        currentTile = tile;

        if (panel != null)
            panel.SetActive(true);

        if (titleText != null && currentTile != null)
        {
            var pos = currentTile.GridPosition;
            titleText.text = $"Тайл ({pos.x}, {pos.y})";
        }

        // по умолчанию можно очистить текст или сразу показать кратко
        if (infoText != null)
        {
            infoText.text = "Нажми «Инфо», чтобы увидеть данные о тайле";
        }
    }

    public void Hide()
    {
        currentTile = null;
        if (panel != null)
            panel.SetActive(false);
    }

    private void OnInfoClicked()
    {
        if (currentTile == null || infoText == null)
            return;

        string heightCategory = GetHeightCategory(currentTile.TopHeight);
        string biome = GetBiomeName(currentTile.TerrainType);

        infoText.text = 
            $"Высота: <b>{heightCategory}</b>\n" +
            $"Биом: <b>{biome}</b>";
    }

    private void OnBuildClicked()
    {
        if (currentTile == null)
            return;

        Debug.Log($"[BUILD] Построить на тайле {currentTile.GridPosition}");

        // сюда потом подвяжем окно строительства / меню
    }

    private string GetHeightCategory(float h)
    {
        // Можешь подогнать пороги под свой вкус
        if (h < 0.3f) return "Низкая";
        if (h < 0.7f) return "Средняя";
        return "Высокая";
    }

    private string GetBiomeName(TileTerrainType type)
    {
        switch (type)
        {
            case TileTerrainType.Grass:   return "Равнина";
            case TileTerrainType.Forest:  return "Лес";
            case TileTerrainType.Mountain:return "Горы";
            case TileTerrainType.Water:   return "Водоём";
            default:                      return type.ToString();
        }
    }
}
