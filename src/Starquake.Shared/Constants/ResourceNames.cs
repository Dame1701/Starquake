// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The names the sprite sheets are registered under, for one of the two sets of artwork. Ten
///     sheets exist twice, redrawn and as the original 1985 graphics, and F8 swaps between them
///     mid-game, so a draw call reaching for one of those ten must ask which set is in play.
///     <para>
///         There are only ever two instances, <see cref="Redrawn" /> and <see cref="Original" />,
///         and both are immutable: switching artwork changes which is read, not what either holds.
///         Names are spelled out in full rather than built by appending a suffix, so searching for a
///         sheet's name finds where it is asked for as well as where it is loaded.
///     </para>
/// </summary>
internal sealed class ResourceNames
{
    /// <summary>The redrawn artwork, which is what the game starts in.</summary>
    public static readonly ResourceNames Redrawn = new()
    {
        LowerScreenTiles = "LowerScreenTiles",
        UpperScreenTiles = "UpperScreenTiles",
        BlobSprites = "BlobSprites",
        PowerUps = "PowerUps",
        BlobsShip = "BlobsShip",
        AccessCodesFailed = "AccessCodesFailed",
        AccessCodesPassed = "AccessCodesPassed",
        PickupsIlluminated = "PickupsIlluminated",
        Pickups = "Pickups"
    };

    /// <summary>The original 1985 graphics.</summary>
    public static readonly ResourceNames Original = new()
    {
        LowerScreenTiles = "LowerScreenTilesOriginal",
        UpperScreenTiles = "UpperScreenTilesOriginal",
        BlobSprites = "BlobSpritesOriginal",
        PowerUps = "PowerUpsOriginal",
        BlobsShip = "BlobsShipOriginal",
        AccessCodesFailed = "AccessCodesFailedOriginal",
        AccessCodesPassed = "AccessCodesPassedOriginal",
        PickupsIlluminated = "PickupsIlluminatedOriginal",
        Pickups = "PickupsOriginal"
    };

    private ResourceNames()
    {
    }

    /// <summary>
    ///     The first of a screen's two tile layers, and the one every screen coordinate is measured
    ///     in: a screen's width and height are counted in these tiles.
    /// </summary>
    public string LowerScreenTiles { get; private init; } = string.Empty;

    /// <summary>The second of a screen's two tile layers, drawn over the first.</summary>
    public string UpperScreenTiles { get; private init; } = string.Empty;

    /// <summary>Blob, in every frame of every direction he can face.</summary>
    public string BlobSprites { get; private init; } = string.Empty;

    /// <summary>The power-ups Blob can be carrying.</summary>
    public string PowerUps { get; private init; } = string.Empty;

    /// <summary>The ship Blob arrives in, which only the intro sequence draws.</summary>
    public string BlobsShip { get; private init; } = string.Empty;

    /// <summary>The access code digits as they read once a code has been rejected.</summary>
    public string AccessCodesFailed { get; private init; } = string.Empty;

    /// <summary>The access code digits as they read once a code has been accepted.</summary>
    public string AccessCodesPassed { get; private init; } = string.Empty;

    /// <summary>
    ///     The pickups lit from within, drawn additively over <see cref="Pickups" /> to make a core
    ///     element glow.
    /// </summary>
    public string PickupsIlluminated { get; private init; } = string.Empty;

    /// <summary>Everything Blob can pick up and carry.</summary>
    public string Pickups { get; private init; } = string.Empty;
}
