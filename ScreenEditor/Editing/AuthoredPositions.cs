// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ScreenEditor.ViewModels;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     The handful of positions the author marks once for the whole planet - where Blob starts, where
///     his ship came down, where the core's exit is, and where the core itself sits - and what is wrong
///     with them.
/// </summary>
/// <remarks>
///     All four are looked up out of the map rather than held by it: a marker is a tile number painted on
///     a screen, so finding Blob's start means searching 512 screens for one. They are cached here
///     because the panel that shows them redraws far more often than the map changes, and
///     <see cref="Refresh" /> is what keeps the cache honest.
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
    ///     Where Blob is put down at the start of a game, or null if no screen says. The screen this
    ///     is on is the screen the game opens on.
    /// </summary>
    public MarkerPlacement? BlobStart { get; private set; }

    /// <inheritdoc cref="BlobStart" />
    public MarkerPlacement? ShipCrashSite { get; private set; }

    /// <summary>
    ///     Where Blob arrives having been thrown out of the planet core, or null if no screen says.
    /// </summary>
    public MarkerPlacement? CoreExit { get; private set; }

    /// <summary>
    ///     Where the planet core sits. Always answers somewhere - the original game's position stands in
    ///     for a map that has not placed one - so <see cref="CoreIsPlaced" /> says whether the map means it.
    /// </summary>
    public CoreSite Core { get; private set; } = CoreSite.Original;

    /// <summary>
    ///     Whether the map places the core itself, as opposed to reading back the fallback.
    /// </summary>
    public bool CoreIsPlaced => _document.CoreCount > 0;

    /// <summary>
    ///     What is wrong with the landing site, or null when there is nothing wrong with it. The game
    ///     opens on the screen the start position is on, so a missing marker or a wreck on another
    ///     screen means a map that does not begin the way its author meant it to.
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
    ///     The landing site as the panel shows it, which has to read as something even when half of it
    ///     has not been placed.
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
    ///     What the landing site box says underneath: what is wrong with it, or what it means when
    ///     nothing is.
    /// </summary>
    public string LandingSiteNote
        => LandingSiteProblem ?? "A new game opens here, with Blob stepping out of the wreck.";

    /// <summary>
    ///     Reads all four back out of the map. Called whenever something that could have moved one of them
    ///     has happened - a tile painted, the core placed, or a different map opened.
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
