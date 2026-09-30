// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The names the sprite sheets are registered under, for one of the two sets of artwork the
///     game can be drawn with. Ten of the sheets exist twice - once as the redrawn artwork and once
///     as the original 1985 graphics - and F8 swaps between them mid-game, so every draw call that
///     reaches for one of those ten has to ask which set is in play rather than name a sheet
///     outright.
///     <para>
///         There are only ever the two instances, <see cref="Redrawn" /> and
///         <see cref="Original" />, and they are immutable: switching artwork swaps which one is
///         being read, it does not edit either. The names are spelled out in full rather than
///         derived by appending a suffix, so a search for a sheet's name finds the place it is
///         asked for as well as the place it is loaded.
///     </para>
/// </summary>
public sealed class ResourceNames
{
    /// <summary>
    ///     The redrawn artwork, which is what the game starts in.
    /// </summary>
    public static readonly ResourceNames Redrawn = new()
    {
        LowerScreenTiles = "LowerScreenTiles",
        UpperScreenTiles = "UpperScreenTiles",
        BlobSprites = "BlobSprites",
        PowerUps = "PowerUps",
        BlobsShip = "BlobsShip",
        AccessCodesFailed = "AccessCodesFailed",
        AccessCodesPassed = "AccessCodesPassed",
        AccessCodesGrey = "AccessCodesGray",
        PickupsIlluminated = "PickupsIlluminated",
        Pickups = "Pickups"
    };

    /// <summary>
    ///     The original 1985 graphics.
    /// </summary>
    public static readonly ResourceNames Original = new()
    {
        LowerScreenTiles = "LowerScreenTilesOriginal",
        UpperScreenTiles = "UpperScreenTilesOriginal",
        BlobSprites = "BlobSpritesOriginal",
        PowerUps = "PowerUpsOriginal",
        BlobsShip = "BlobsShipOriginal",
        AccessCodesFailed = "AccessCodesFailedOriginal",
        AccessCodesPassed = "AccessCodesPassedOriginal",
        AccessCodesGrey = "AccessCodesGrayOriginal",
        PickupsIlluminated = "PickupsIlluminatedOriginal",
        Pickups = "PickupsOriginal"
    };

    private ResourceNames()
    {
    }

    /// <summary>
    ///     The first of a screen's two tile layers, which is also the one every screen coordinate is
    ///     measured in - a screen's width and height are counted in these tiles.
    /// </summary>
    public string LowerScreenTiles { get; private init; } = string.Empty;

    /// <summary>
    ///     The second of a screen's two tile layers, drawn over the first.
    /// </summary>
    public string UpperScreenTiles { get; private init; } = string.Empty;

    /// <summary>
    ///     The blob, in every frame of every direction it can face.
    /// </summary>
    public string BlobSprites { get; private init; } = string.Empty;

    /// <summary>
    ///     The power-ups the blob can be carrying.
    /// </summary>
    public string PowerUps { get; private init; } = string.Empty;

    /// <summary>
    ///     The ship the blob arrives in, which only the intro sequence draws.
    /// </summary>
    public string BlobsShip { get; private init; } = string.Empty;

    /// <summary>
    ///     The access code digits as they read once a code has been rejected.
    /// </summary>
    public string AccessCodesFailed { get; private init; } = string.Empty;

    /// <summary>
    ///     The access code digits as they read once a code has been accepted.
    /// </summary>
    public string AccessCodesPassed { get; private init; } = string.Empty;

    /// <summary>
    ///     The access code digits as they read before a code has been judged either way.
    /// </summary>
    public string AccessCodesGrey { get; private init; } = string.Empty;

    /// <summary>
    ///     The pickups lit from within, drawn additively over <see cref="Pickups" /> to make a core
    ///     element glow.
    /// </summary>
    public string PickupsIlluminated { get; private init; } = string.Empty;

    /// <summary>
    ///     Everything the blob can pick up and carry.
    /// </summary>
    public string Pickups { get; private init; } = string.Empty;
}
