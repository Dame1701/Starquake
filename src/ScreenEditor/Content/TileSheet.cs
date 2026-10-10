// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Starquake.Constants;

namespace Starquake.ScreenEditor.Content;

/// <summary>
///     A loaded tile sheet, cut into tiles the way the game cuts it.
/// </summary>
/// <remarks>
///     The sheet is one long image and screens refer to tiles by number, so most of this turns a
///     number into a rectangle. Each tile is also averaged down to a few colours up front, which
///     lets the world map draw five hundred screens without returning to the full size artwork.
/// </remarks>
internal sealed class TileSheet : IDisposable
{

    /// <summary>
    ///     How many colours a tile is reduced to for the world map. Reuses the collision grid's
    ///     subdivision, which is fine enough that a platform still reads as a platform at map scale
    ///     and leaves each tile a whole number of pixels wide.
    /// </summary>
    public const int ThumbnailCellsAcross = TileMetrics.CollisionCellsAcrossTile;

    /// <inheritdoc cref="ThumbnailCellsAcross" />
    public const int ThumbnailCellsDown = TileMetrics.CollisionCellsDownTile;

    private readonly WriteableBitmap _bitmap;

    private TileSheet(string path, WriteableBitmap bitmap, int tilesAcross, int tilesDown, uint[] thumbnailColours)
    {
        Path = path;
        _bitmap = bitmap;
        TilesAcross = tilesAcross;
        TilesDown = tilesDown;
        ThumbnailColours = thumbnailColours;
    }

    private string Path { get; }

    private IImage Image => _bitmap;

    private int TilesAcross { get; }

    private int TilesDown { get; }

    public int TileCount => TilesAcross * TilesDown;

    /// <summary>
    ///     Every tile averaged down to <see cref="ThumbnailCellsAcross" /> by
    ///     <see cref="ThumbnailCellsDown" /> premultiplied BGRA pixels, ordered tile by tile and then
    ///     in reading order within each tile. The world map's bitmap takes this form directly.
    /// </summary>
    public uint[] ThumbnailColours { get; }

    public void Dispose() => _bitmap.Dispose();

    public static TileSheet Load(string path)
    {
        using FileStream stream = File.OpenRead(path);
        WriteableBitmap bitmap = WriteableBitmap.Decode(stream);

        int tilesAcross = Math.Max(1, bitmap.PixelSize.Width / TileMetrics.TileWidth);
        int tilesDown = Math.Max(1, bitmap.PixelSize.Height / TileMetrics.TileHeight);

        uint[] thumbnailColours = BuildThumbnailColours(bitmap, tilesAcross, tilesDown);

        return new TileSheet(path, bitmap, tilesAcross, tilesDown, thumbnailColours);
    }

    /// <summary>Where a tile number sits on the sheet, in sheet pixels.</summary>
    private Rect SourceRect(int tileImage)
    {
        int row = tileImage / TilesAcross;
        int column = tileImage - row * TilesAcross;

        return new Rect(
            column * TileMetrics.TileWidth,
            row * TileMetrics.TileHeight,
            TileMetrics.TileWidth,
            TileMetrics.TileHeight);
    }

    public bool Contains(int tileImage) => tileImage >= 0 && tileImage < TileCount;

    /// <summary>
    ///     Draws one tile into the given rectangle. Tile sheets are pixel art, so the caller must
    ///     have turned interpolation off on the control doing the drawing.
    /// </summary>
    public void DrawTile(DrawingContext context, int tileImage, Rect destination)
    {
        if (!Contains(tileImage))
        {
            return;
        }

        context.DrawImage(Image, SourceRect(tileImage), destination);
    }

    private static uint[] BuildThumbnailColours(WriteableBitmap bitmap, int tilesAcross, int tilesDown)
    {
        int tileCount = tilesAcross * tilesDown;
        uint[] colours = new uint[tileCount * ThumbnailCellsAcross * ThumbnailCellsDown];

        int cellWidth = TileMetrics.TileWidth / ThumbnailCellsAcross;
        int cellHeight = TileMetrics.TileHeight / ThumbnailCellsDown;

        using ILockedFramebuffer frame = bitmap.Lock();

        // The platform's preferred format decides whether red or blue comes first.
        bool redFirst = frame.Format == PixelFormat.Rgba8888;

        unsafe
        {
            byte* pixels = (byte*)frame.Address;

            for (int tile = 0; tile < tileCount; tile++)
            {
                int tileRow = tile / tilesAcross;
                int tileColumn = tile - tileRow * tilesAcross;

                for (int cellY = 0; cellY < ThumbnailCellsDown; cellY++)
                {
                    for (int cellX = 0; cellX < ThumbnailCellsAcross; cellX++)
                    {
                        int left = tileColumn * TileMetrics.TileWidth + cellX * cellWidth;
                        int top = tileRow * TileMetrics.TileHeight + cellY * cellHeight;

                        long red = 0;
                        long green = 0;
                        long blue = 0;
                        long alpha = 0;

                        for (int y = top; y < top + cellHeight; y++)
                        {
                            byte* row = pixels + (long)y * frame.RowBytes;

                            for (int x = left; x < left + cellWidth; x++)
                            {
                                byte* pixel = row + (long)x * 4;

                                blue += redFirst ? pixel[2] : pixel[0];
                                green += pixel[1];
                                red += redFirst ? pixel[0] : pixel[2];
                                alpha += pixel[3];
                            }
                        }

                        int samples = cellWidth * cellHeight;

                        colours[(tile * ThumbnailCellsDown + cellY) * ThumbnailCellsAcross + cellX] =
                            (uint)(alpha / samples) << 24 |
                            (uint)(red / samples) << 16 |
                            (uint)(green / samples) << 8 |
                            (uint)(blue / samples);
                    }
                }
            }
        }

        return colours;
    }
}
