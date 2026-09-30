// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.IO;
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
///     The sheet is one long image and a screen refers to its tiles by number, so everything here is
///     about turning a number into a rectangle. It also averages each tile down to a handful of
///     colours up front, which is what lets the world map draw five hundred screens at once without
///     five hundred trips back to the full size artwork.
/// </remarks>
public sealed class TileSheet : IDisposable
{

    /// <summary>
    ///     How many colours a tile is boiled down to for the world map. Reusing the collision grid's
    ///     subdivision is not just convenience: it is fine enough that a platform still reads as a
    ///     platform at map scale, and it means one tile occupies a whole number of pixels there.
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

    public string Path { get; }

    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

    /// <summary>
    ///     The sheet itself, for drawing tiles from.
    /// </summary>
    public IImage Image => _bitmap;

    public int TilesAcross { get; }

    public int TilesDown { get; }

    public int TileCount => TilesAcross * TilesDown;

    /// <summary>
    ///     Every tile averaged down to <see cref="ThumbnailCellsAcross" /> by
    ///     <see cref="ThumbnailCellsDown" /> premultiplied BGRA pixels, laid out tile by tile and then
    ///     in reading order within a tile. This is the form the world map's bitmap wants, so it can be
    ///     blitted straight in.
    /// </summary>
    public uint[] ThumbnailColours { get; }

    public void Dispose() => _bitmap.Dispose();

    public static TileSheet Load(string path)
    {
        using var stream = File.OpenRead(path);
        var bitmap = WriteableBitmap.Decode(stream);

        var tilesAcross = Math.Max(1, bitmap.PixelSize.Width / TileMetrics.TileWidth);
        var tilesDown = Math.Max(1, bitmap.PixelSize.Height / TileMetrics.TileHeight);

        var thumbnailColours = BuildThumbnailColours(bitmap, tilesAcross, tilesDown);

        return new TileSheet(path, bitmap, tilesAcross, tilesDown, thumbnailColours);
    }

    /// <summary>
    ///     Where a tile number sits on the sheet, in sheet pixels.
    /// </summary>
    public Rect SourceRect(int tileImage)
    {
        var row = tileImage / TilesAcross;
        var column = tileImage - row * TilesAcross;

        return new Rect(
            column * TileMetrics.TileWidth,
            row * TileMetrics.TileHeight,
            TileMetrics.TileWidth,
            TileMetrics.TileHeight);
    }

    public bool Contains(int tileImage) => tileImage >= 0 && tileImage < TileCount;

    /// <summary>
    ///     Draws one tile into the given rectangle. Tile sheets are pixel art, so callers are expected
    ///     to have turned interpolation off on the control doing the drawing.
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
        var tileCount = tilesAcross * tilesDown;
        var colours = new uint[tileCount * ThumbnailCellsAcross * ThumbnailCellsDown];

        var cellWidth = TileMetrics.TileWidth / ThumbnailCellsAcross;
        var cellHeight = TileMetrics.TileHeight / ThumbnailCellsDown;

        using var frame = bitmap.Lock();

        // Both byte orders turn up depending on the platform's preferred format, and the only
        // difference that makes here is whether red or blue comes first.
        var redFirst = frame.Format == PixelFormat.Rgba8888;

        unsafe
        {
            var pixels = (byte*)frame.Address;

            for (var tile = 0; tile < tileCount; tile++)
            {
                var tileRow = tile / tilesAcross;
                var tileColumn = tile - tileRow * tilesAcross;

                for (var cellY = 0; cellY < ThumbnailCellsDown; cellY++)
                {
                    for (var cellX = 0; cellX < ThumbnailCellsAcross; cellX++)
                    {
                        var left = tileColumn * TileMetrics.TileWidth + cellX * cellWidth;
                        var top = tileRow * TileMetrics.TileHeight + cellY * cellHeight;

                        long red = 0;
                        long green = 0;
                        long blue = 0;
                        long alpha = 0;

                        for (var y = top; y < top + cellHeight; y++)
                        {
                            var row = pixels + (long)y * frame.RowBytes;

                            for (var x = left; x < left + cellWidth; x++)
                            {
                                var pixel = row + (long)x * 4;

                                blue += redFirst ? pixel[2] : pixel[0];
                                green += pixel[1];
                                red += redFirst ? pixel[0] : pixel[2];
                                alpha += pixel[3];
                            }
                        }

                        var samples = cellWidth * cellHeight;

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
