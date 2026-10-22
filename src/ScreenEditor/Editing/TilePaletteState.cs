// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using System.Collections.ObjectModel;
using Starquake.Constants;
using Starquake.ScreenEditor.Content;
using Starquake.ScreenEditor.ViewModels;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     Which of the sheet's tiles the palette offers, and the short row of recently used ones. A
///     sheet holds over 400 tiles, so finding one by eye needs narrowing down.
/// </summary>
/// <remarks>
///     The sheet and the map are passed in rather than reached for. Each is replaced in exactly one
///     place, which calls <see cref="Rebind" />, so this holds what it was given and needs no
///     reference back to the session.
/// </remarks>
internal sealed class TilePaletteState : ViewModelBase
{
    /// <summary>
    ///     How many tiles the recently-used row remembers. Enough for the handful in use, few enough
    ///     to stay one row.
    /// </summary>
    private const int RecentTileCount = 8;

    private readonly ObservableCollection<int> _recentTiles = [];

    private readonly Action _repaint;

    private EditorDocument _document;

    private string _filter = string.Empty;

    private TileSheet? _sheet;

    /// <summary>
    ///     Every tile number any screen uses, worked out once and kept until the map changes. Walking
    ///     512 screens on every palette repaint would be noticeable.
    /// </summary>
    private HashSet<int>? _tilesInUse;

    private bool _usedTilesOnly;

    private IReadOnlyList<int> _visibleTiles = [];

    public TilePaletteState(EditorDocument document, Action repaint)
    {
        _document = document;
        _repaint = repaint;
    }

    /// <summary>
    ///     The tiles most recently picked out of the sheet, newest first. Markers never appear here,
    ///     because each has its own toolbar button and so was never hunted down in the sheet.
    /// </summary>
    public IReadOnlyList<int> RecentTiles => _recentTiles;

    /// <summary>
    ///     Narrows the palette to tiles whose number or name matches. Typing "tunnel" finds the
    ///     tunnel mouths without scrolling to the bottom of the sheet and counting.
    /// </summary>
    public string TileFilter
    {
        get => _filter;
        set
        {
            SetField(ref _filter, value);
            RebuildVisibleTiles();
        }
    }

    /// <summary>
    ///     Hides tiles no screen uses. The sheet holds a fair number the finished map never touches,
    ///     and hiding them shows what the map is actually built from.
    /// </summary>
    public bool UsedTilesOnly
    {
        get => _usedTilesOnly;
        set
        {
            SetField(ref _usedTilesOnly, value);
            RebuildVisibleTiles();
        }
    }

    /// <summary>The tiles the palette draws, in sheet order.</summary>
    public IReadOnlyList<int> VisibleTiles
    {
        get => _visibleTiles;
        private set => SetField(ref _visibleTiles, value);
    }

    /// <summary>
    ///     Points the palette at a sheet and a map. Called when either is replaced, and once on
    ///     opening.
    /// </summary>
    public void Rebind(TileSheet? sheet, EditorDocument document)
    {
        _sheet = sheet;
        _document = document;
        _tilesInUse = null;

        RebuildVisibleTiles();
    }

    /// <summary>
    ///     Reports that the map's contents changed, so the tiles it uses must be counted again.
    ///     Separate from <see cref="Rebind" /> because painting a tile does not replace the map.
    /// </summary>
    /// <returns>
    ///     Whether the palette redrew. It only does so when showing the tiles the map uses, since
    ///     otherwise the map's contents make no difference to what is offered.
    /// </returns>
    public bool MapChanged()
    {
        _tilesInUse = null;

        if (!_usedTilesOnly)
        {
            return false;
        }

        RebuildVisibleTiles();

        return true;
    }

    /// <summary>Puts a tile at the head of the recently-used row.</summary>
    public void Remember(int tileImage)
    {
        // A marker has its own toolbar button, so it was never hunted down in the sheet.
        if (tileImage < 0 || MarkerTiles.Contains(tileImage))
        {
            return;
        }

        _recentTiles.Remove(tileImage);
        _recentTiles.Insert(0, tileImage);

        while (_recentTiles.Count > RecentTileCount)
        {
            _recentTiles.RemoveAt(_recentTiles.Count - 1);
        }

        RaisePropertyChanged(nameof(RecentTiles));
    }

    private void RebuildVisibleTiles()
    {
        if (_sheet is null)
        {
            VisibleTiles = [];

            return;
        }

        HashSet<int>? inUse = _usedTilesOnly ? TilesInUse() : null;
        string filter = _filter.Trim();
        var visible = new List<int>(_sheet.TileCount);

        for (int tile = 0; tile < _sheet.TileCount; tile++)
        {
            if (inUse is not null && !inUse.Contains(tile))
            {
                continue;
            }

            if (filter.Length > 0 && !Matches(tile, filter))
            {
                continue;
            }

            visible.Add(tile);
        }

        VisibleTiles = visible;
        _repaint();
    }

    private static bool Matches(int tileImage, string filter)
    {
        if (tileImage.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string? name = TileCatalogue.SpecialName(tileImage);

        return name is not null && name.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private HashSet<int> TilesInUse()
    {
        if (_tilesInUse is not null)
        {
            return _tilesInUse;
        }

        var tiles = new HashSet<int>();

        for (int screen = 0; screen < _document.Screens.Length; screen++)
        {
            for (int y = 0; y < TileMetrics.TilesDown; y++)
            {
                for (int x = 0; x < TileMetrics.TilesAcross; x++)
                {
                    tiles.Add(_document.GetTile(screen, x, y));
                }
            }
        }

        _tilesInUse = tiles;

        return tiles;
    }
}
