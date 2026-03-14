using UnityEngine;

public class TileView2D : MonoBehaviour
{
    public int x;
    public int y;
    public int floor;
    public TileType tileType;
    public string contentId = "";

    [SerializeField] private SpriteRenderer spriteRenderer;

    public void SetData(TileData data)
    {
        // 把数据库格子数据同步到场景对象
        x = data.x;
        y = data.y;
        floor = data.floor;
        tileType = data.tileType;
        contentId = data.contentId;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // 用颜色区分不同格子类型，原型阶段代替美术资源
        spriteRenderer.color = GetColor(data);
        gameObject.name = $"Tile_{x}_{y}_{tileType}";
    }

    private Color GetColor(TileData tile)
    {
        return tile.tileType switch
        {
            TileType.Floor => Color.white,
            TileType.Wall => Color.gray,
            TileType.Door => Color.yellow,
            TileType.Key => Color.cyan,
            TileType.Monster => tile.contentId switch
            {
                "elite" => new Color(1f, 0.5f, 0f),
                "boss" => new Color(0.6f, 0f, 0.8f),
                _ => Color.red
            },
            TileType.Merchant => Color.green,
            TileType.TreasureItem => Color.magenta,
            TileType.StairsUp => Color.blue,
            TileType.StairsDown => new Color(0f, 0f, 0.5f),
            TileType.Spawn => new Color(0.2f, 0.4f, 1f),
            _ => Color.white
        };
    }
}