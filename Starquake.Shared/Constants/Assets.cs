// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The name every asset is registered under, in one place. <c>ContentManifest</c> registers by
///     these names and everything that draws or plays asks by them, so the two cannot drift: a
///     mistyped name now fails to compile instead of throwing out of a dictionary lookup at the
///     moment the asset is first wanted.
///     <para>
///         Nested by the kind of thing, because the lookup already says which kind it wants -
///         <c>GetSpriteCollection</c> takes a <see cref="Graphics" />, <c>GetDataResource</c> a
///         <see cref="Data" /> - so the grouping is what the call site is choosing between.
///     </para>
///     <para>
///         The ten sheets that exist twice, once redrawn and once as the 1985 artwork, are not here.
///         They live in <see cref="ResourceNames" />, which is a different problem: picking between
///         two sets at run time rather than naming one thing. Nothing should name those by literal
///         either, and nothing does - they are reached through <c>GraphicsSet.Names</c>.
///     </para>
///     <para>
///         A constant here that nothing reads is an asset the game loads and never uses, which
///         until now was invisible. <c>jb inspectcode</c> reports each one as an unused member, so
///         the list of them is something that can be looked at rather than guessed at.
///     </para>
/// </summary>
internal static class Assets
{
    /// <summary>
    ///     The sprite sheets, by the name each is registered under.
    /// </summary>
    internal static class Graphics
    {
        // Shared
        public const string Background1                    = "Background1";
        public const string Background2                    = "Background2";
        public const string ProportionalNumbers            = "ProportionalNumbers";

        // Presentation screens
        public const string OptionScreenBackground         = "OptionScreenBackground";
        public const string OptionScreenForeground         = "OptionScreenForeground";
        public const string ComputerReportScreenBackground = "ComputerReportScreenBackground";
        public const string ComputerReportScreenForeground = "ComputerReportScreenForeground";
        public const string GameOverScreenBackground       = "GameOverScreenBackground";
        public const string GameOverScreenForeground       = "GameOverScreenForeground";
        public const string HiScoreScreenBackground        = "HiScoreScreenBackground";
        public const string HiScoreScreenForeground        = "HiScoreScreenForeground";
        public const string GameCompleteScreenBackground   = "GameCompleteScreenBackground";
        public const string GameCompleteScreenForeground   = "GameCompleteScreenForeground";

        // Screen tiles
        public const string ExitShadows                    = "ExitShadows";

        // Blob
        public const string BlobParts                      = "BlobParts";
        public const string BlobShuttle                    = "BlobShuttle";
        public const string BlobShuttleOriginal            = "BlobShuttleOriginal";
        public const string BlobFire1                      = "BlobFire1";
        public const string BlobFire2                      = "BlobFire2";
        public const string BlobFire2Original              = "BlobFire2Original";
        public const string BlobFire2Overlay               = "BlobFire2Overlay";
        public const string JumpPlatforms                  = "JumpPlatforms";
        public const string JumpPlatformsOriginal          = "JumpPlatformsOriginal";

        // Pick-ups and nasties
        public const string Nasties                        = "Nasties";
        public const string NastiesOriginal                = "NastiesOriginal";

        // Access booths
        public const string ScanLine                       = "ScanLine";
        public const string AccessCodesGray                = "AccessCodesGray";
        public const string AccessCodesGrayOriginal        = "AccessCodesGrayOriginal";

        // Cheops
        public const string CheopsPyramid                  = "CheopsPyramid";
        public const string CheopsPyramidOriginal          = "CheopsPyramidOriginal";

        // Teleporters
        public const string TeleportInteriorLower          = "TeleportInteriorLower";
        public const string TeleportInteriorUpper          = "TeleportInteriorUpper";
        public const string TeleporterKeyOverlays          = "TeleporterKeyOverlays";
        public const string TeleporterPowerSupply          = "TeleporterPowerSupply";

        // Electricity
        public const string ElectricityNodeElectricity1    = "ElectricityNodeElectricity1";
        public const string ElectricityNodeElectricity2    = "ElectricityNodeElectricity2";
        public const string NodeLights                     = "NodeLights";
        public const string GateLights                     = "GateLights";
        public const string ElectricityOriginal            = "ElectricityOriginal";

        // Odd sizes
        public const string OllyBackground                 = "OllyBackground";
        public const string OllyLower                      = "OllyLower";
        public const string OllyUpper                      = "OllyUpper";
        public const string OllyEyes                       = "OllyEyes";
        public const string OllyOriginal                   = "OllyOriginal";
        public const string BlobsShipHazardLights          = "BlobsShipHazardLights";
        public const string BlobsShipHeadLights            = "BlobsShipHeadLights";
        public const string BlankingTile                   = "BlankingTile";
        public const string BreakableBridge                = "BreakableBridge";
        public const string BreakableBridgeOriginal        = "BreakableBridgeOriginal";

        // Status panel
        public const string StatusPanelGraphic             = "StatusPanelGraphic";
        public const string EnergyBar                      = "EnergyBar";
        public const string JumpBar                        = "JumpBar";
        public const string AmmoBar                        = "AmmoBar";
        public const string PanelNumbers                   = "PanelNumbers";
        public const string StatusPanelShadow              = "StatusPanelShadow";
        public const string StatusPanelOverlay             = "StatusPanelOverlay";
        public const string ParticleSprite                 = "ParticleSprite";
        public const string CollisionTile                  = "CollisionTile";
    }

    /// <summary>
    ///     The animated tile sheets. Each is bound to the tile number it animates.
    /// </summary>
    internal static class Animations
    {
        public const string BreakableBridges         = "BreakableBridges";
        public const string TeleporterPadsOccupied   = "TeleporterPadsOccupied";
        public const string TeleporterPadsUnoccupied = "TeleporterPadsUnoccupied";
    }

    /// <summary>
    ///     The particle effects. A name here is a registered effect, not an instance of one.
    /// </summary>
    internal static class Effects
    {
        public const string BeamMeUpSmall                 = "BeamMeUp_Small";
        public const string FireTrail                     = "FireTrail";
        public const string Fireball                      = "Fireball";
        public const string NastyDies                     = "NastyDies";
        public const string NastySplat                    = "NastySplat";
        public const string BinaryEffect                  = "BinaryEffect";
        public const string BasicSmokeSmall               = "BasicSmokeSmall";
        public const string BasicSmokePlume               = "BasicSmokePlume";
        public const string BasicSmokePlume2              = "BasicSmokePlume2";
        public const string Sparks                        = "Sparks";
        public const string CoreSparks                    = "CoreSparks";
        public const string CoreReplacementParticleEffect = "CoreReplacementParticleEffect";
        public const string FireSparks                    = "FireSparks";
        public const string ForceField                    = "ForceField";
        public const string BlobFragmentExplosion         = "BlobFragmentExplosion";
    }

    /// <summary>
    ///     The sound effects, by the name each is registered under.
    /// </summary>
    internal static class Sounds
    {
        public const string ScanCode             = "ScanCode";
        public const string AccessSuccess        = "AccessSuccess";
        public const string AccessFailed         = "AccessFailed";
        public const string MachineryBackground  = "MachineryBackground";
        public const string Tunnel               = "Tunnel";
        public const string TunnelSwish1         = "TunnelSwish1";
        public const string TunnelSwish2         = "TunnelSwish2";
        public const string NastyDeath           = "NastyDeath";
        public const string BlobLazer            = "BlobLazer";
        public const string ElectricityNodes     = "ElectricityNodes";
        public const string BridgeBreak          = "BridgeBreak";
        public const string BlobRun              = "BlobRun";
        public const string BlobFalls            = "BlobFalls";
        public const string TunnelLift           = "TunnelLift";
        public const string NastyAppear          = "NastyAppear";
        public const string Rumble               = "Rumble";
        public const string BlobJumps            = "BlobJumps";
        public const string BlobJumpsNoPlatforms = "BlobJumpsNoPlatforms";
        public const string BlobPickUp           = "BlobPickUp";
        public const string BlobOverloadAlarm    = "BlobOverloadAlarm";
        public const string BlobShipFall         = "BlobShipFall";
        public const string BlobShipExplodes     = "BlobShipExplodes";
        public const string BlobExplodes         = "BlobExplodes";
        public const string BlobPartClang        = "BlobPartClang";
        public const string NastyAppears         = "NastyAppears";
        public const string TeleportKeyClick     = "TeleportKeyClick";
        public const string TeleportInvalidCode  = "TeleportInvalidCode";
        public const string Teleport             = "Teleport";
        public const string PowerUp1             = "PowerUp1";
        public const string PowerUp2             = "PowerUp2";
        public const string PowerUp3             = "PowerUp3";
        public const string LifePowerUp          = "LifePowerUp";
        public const string BackgroundSound1     = "BackgroundSound1";
    }

    /// <summary>
    ///     The screen data resources. Both read the same file, in the order the manifest names them.
    /// </summary>
    internal static class Data
    {
        public const string GameScreens         = "GameScreens";
        public const string ScreenCollisionData = "ScreenCollisionData";
    }
}
