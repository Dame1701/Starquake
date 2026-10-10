// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ScreenEditor.ViewModels;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     The four positions the author marks once for the whole planet, and what is wrong with them:
///     where Blob starts, where the ship came down, where the core's exit is, and where the core
///     sits.
/// </summary>
/// <remarks>
///     A marker is a tile number painted on a screen, so finding one means searching 512 screens.
///     They are cached here because the panel showing them redraws far more often than the map
///     changes. <see cref="Refresh" /> keeps the cache honest.
/// </remarks>
internal sealed class AuthoredPositions : ViewModelBase
{
    private EditorDocument _document;

    public AuthoredPositions(EditorDocument document)
    {
        _document = document;
        Refresh(document);
    }

    /// <summary>
    ///     Where Blob is put down at the start of a game, or null if no screen marks it. Its screen
    ///     is the screen the game opens on.
    /// </summary>
    public MarkerPlacement? BlobStart { get; private set; }

    /// <inheritdoc cref="BlobStart" />
    public MarkerPlacement? ShipCrashSite { get; private set; }

    /// <summary>
    ///     Where Blob arrives on leaving the planet core, or null if no screen marks it.
    /// </summary>
    public MarkerPlacement? CoreExit { get; private set; }

    /// <summary>
    ///     Where the planet core sits. Never null: a map that has not placed one reads back the
    ///     fallback position, so <see cref="CoreIsPlaced" /> says whether the map meant it.
    /// </summary>
    public CoreSite Core { get; private set; } = CoreSite.Original;

    /// <summary>Whether the map places the core itself, rather than reading back the fallback.</summary>
    public bool CoreIsPlaced => _document.CoreCount > 0;

    /// <summary>
    ///     What is wrong with the landing site, or null if nothing is. The game opens on the start
    ///     position's screen, so a missing marker or a wreck on another screen means the map does not
    ///     begin the way its author intended.
    /// </summary>
    public string? LandingSiteProblem
    {
        get
        {
            if (BlobStart is not { } start)
            {
                return ShipCrashSite is null
                    ? "No start position has been placed. Blob has nowhere to begin."
                    : "No start position has been placed, so the crash site is on its own.";
            }

            if (ShipCrashSite is not { } crash)
            {
                return "No crash site has been placed. There would be no wreck to climb out of.";
            }

            return crash.ScreenNumber == start.ScreenNumber
                ? null
                : "The crash site is on screen " + crash.ScreenNumber + ", away from the start on screen " +
                  start.ScreenNumber + ". Both belong on one screen.";
        }
    }

    /// <summary>
    ///     The landing site as the panel shows it. Has to read as something even when only half of it
    ///     has been placed.
    /// </summary>
    public string LandingSiteDisplay
    {
        get
        {
            if (BlobStart is not { } start)
            {
                return "NOT SET";
            }

            return "Screen " + start.ScreenNumber + "  ·  start " + Square(start) +
                   "  ·  crash " + (ShipCrashSite is { } crash ? Square(crash) : "not set");
        }
    }

    /// <summary>
    ///     The line under the landing site box: what is wrong with it, or what it means when nothing
    ///     is.
    /// </summary>
    public string LandingSiteNote
        => LandingSiteProblem ?? "A new game opens here, with Blob stepping out of the wreck.";

    /// <summary>
    ///     Reads all four back out of the map. Called after anything that could have moved one: a
    ///     tile painted, the core placed, or a different map opened.
    /// </summary>
    public void Refresh(EditorDocument document)
    {
        _document = document;

        BlobStart = document.FindMarker(MarkerTile.BlobStartPosition);
        ShipCrashSite = document.FindMarker(MarkerTile.ShipCrashSite);
        CoreExit = document.FindMarker(MarkerTile.CoreExitPosition);
        Core = document.FindCore();

        RaisePropertyChanged(nameof(BlobStart));
        RaisePropertyChanged(nameof(ShipCrashSite));
        RaisePropertyChanged(nameof(CoreExit));
        RaisePropertyChanged(nameof(Core));
        RaisePropertyChanged(nameof(CoreIsPlaced));
        RaisePropertyChanged(nameof(LandingSiteProblem));
        RaisePropertyChanged(nameof(LandingSiteDisplay));
        RaisePropertyChanged(nameof(LandingSiteNote));
    }

    private static string Square(MarkerPlacement placement)
        => placement.TileX + ", " + placement.TileY;
}
