using MessagePack;

namespace SharedLibs;

[MessagePackObject]
[Serializable]
public class ScreenTile
{
    private int tileImage;
        
    [Key(0)]
    public int TileImage
    {
        get { return tileImage; }
        set { tileImage = value; }
    }

    public ScreenTile()
    {
    }

    public ScreenTile(int tileImage, int tileType)
    {
        TileImage = tileImage;
    }
}