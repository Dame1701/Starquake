using MessagePack;

namespace SharedLibs;

[MessagePackObject]
public class ScreenTile
{
    [Key(0)]
    public int TileImage { get; set; }

    public ScreenTile()
    {
    }

    public ScreenTile(int tileImage)
    {
        TileImage = tileImage;
    }
}