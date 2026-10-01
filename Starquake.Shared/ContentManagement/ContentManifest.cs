// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Structure;

namespace Starquake.ContentManagement;

/// <summary>
///     Everything the game loads, and the name each thing loads under. Nothing but a list: a sprite
///     sheet with its frame size, a sound with how many instances of it may play at once, a particle
///     effect, a screen's tile data.
///     <para>
///         This was a hundred and sixty lines of <c>GameEngine</c>, nineteen per cent of the file,
///         and the only thing any of it touched was the resource manager. It is declaration rather
///         than behaviour, so it reads better as a list on its own than as a chapter in the middle
///         of the class that runs the game.
///     </para>
///     <para>
///         The registration <em>order</em> is kept exactly as it was found. <c>ScreenData</c>
///         is the one place it is known to matter, and the rest was not worth risking to tidy.
///     </para>
/// </summary>
internal static class ContentManifest
{
    /// <summary>
    ///     Registers everything the game loads. The order is the order it was found in.
    /// </summary>
    public static void RegisterAll(ResourceManager resources)
    {
        SharedGraphics(resources);
        ScreenData(resources);
        PresentationScreenGraphics(resources);
        GameScreenTiles(resources);
        BlobGraphics(resources);
        PickUpAndNastyGraphics(resources);
        AccessBoothGraphics(resources);
        CheopsGraphics(resources);
        TeleporterGraphics(resources);
        ElectricityGraphics(resources);
        OddSizedGraphics(resources);
        StatusPanelGraphics(resources);
        ParticleEffects(resources);
        AnimationTiles(resources);
        Sounds(resources);
    }

    private static void SharedGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource("Background1", "Graphics\\Background1", 1024, 576);
        resources.AddSpriteResource("Background2", "Graphics\\Background2", 1024, 576);
        resources.AddSpriteResource("ProportionalNumbers", "Graphics\\ProportionalNumbers", 27, 26);
    }

    private static void ScreenData(ResourceManager resources)
    {
        resources.AddDataResource("GameScreens", "Data/SQScreenData.dat");
        resources.AddDataResource("ScreenCollisionData", "Data/SQScreenData.dat");
    }

    /// <summary>
    ///     Both resources read the same file, and the reader takes them in the order they are named here,
    ///     so these two must stay in this order and in this method.
    /// </summary>
    private static void PresentationScreenGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource("OptionScreenBackground", "Graphics\\OptionsScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource("OptionScreenForeground", "Graphics\\OptionsScreenForeground", 821, 554);

        resources.AddSpriteResource("ComputerReportScreenBackground", "Graphics\\ComputerReportScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource("ComputerReportScreenForeground", "Graphics\\ComputerReportScreenForeground", 821, 554);

        resources.AddSpriteResource("GameOverScreenBackground", "Graphics\\GameOverScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource("GameOverScreenForeground", "Graphics\\GameOverScreenForeground", 821, 554);

        resources.AddSpriteResource("HiScoreScreenBackground", "Graphics\\HiScoreScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource("HiScoreScreenForeground", "Graphics\\HiScoreScreenForeground", 821, 554);

        resources.AddSpriteResource("GameCompleteScreenBackground", "Graphics\\GameCompleteScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource("GameCompleteScreenForeground", "Graphics\\GameCompleteScreenForeground", 821, 554);
    }

    private static void GameScreenTiles(ResourceManager resources)
    {
        resources.AddSpriteResource("UpperScreenTiles", "Graphics\\UpperScreenTiles", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource("UpperScreenTilesOriginal", "Graphics\\UpperScreenTilesOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);

        resources.AddSpriteResource("LowerScreenTiles", "Graphics\\StartilesNew", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource("LowerScreenTilesOriginal", "Graphics\\StartilesOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);

        resources.AddSpriteResource("LowerScreenTilesDark", "Graphics\\startilesNew", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource("ExitShadows", "Graphics\\ExitShadows", TileMetrics.TileWidth, TileMetrics.TileHeight);
    }

    private static void BlobGraphics(ResourceManager resources)
    {
        //resources.AddSpriteResource(gameEngine.GraphicsSet.Names.BlobSprites, "Graphics\\blobNew", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        //resources.AddSpriteResource(gameEngine.GraphicsSet.Names.BlobSprites, "Graphics\\blobNewAlt", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("BlobSprites", "Graphics\\blobNewAlt2", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("BlobSpritesOriginal", "Graphics\\BlobOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource("BlobParts", "Graphics\\blobParts", 32, 32);
        resources.AddSpriteResource("BlobShuttle", "Graphics\\BlobShuttle", 64, 32);
        resources.AddSpriteResource("BlobShuttleOriginal", "Graphics\\BlobShuttleOriginal", 64, 32);

        resources.AddSpriteResource("BlobFire1", "Graphics\\BlobFire1", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("BlobFire2", "Graphics\\BlobFireNew2", 32, 32);
        resources.AddSpriteResource("BlobFire2Original", "Graphics\\BlobFire2Original", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource("BlobFire2Overlay", "Graphics\\BlobFire2Overlay", 48, 48);
        resources.AddSpriteResource("JumpPlatforms", "Graphics\\BlobJumpPlatform", 64, 32);
        resources.AddSpriteResource("JumpPlatformsOriginal", "Graphics\\BlobJumpPlatformOriginal", 64, 32);
    }

    private static void PickUpAndNastyGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource("PowerUps", "Graphics\\PowerUps", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("PowerUpsOriginal", "Graphics\\PowerUpsOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("Pickups", "Graphics\\Pickups", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("PickupsOriginal", "Graphics\\PickupsOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource("Nasties", "Graphics\\Nasties", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("NastiesOriginal", "Graphics\\NastiesOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void AccessBoothGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource("ScanLine", "Graphics\\ScanLine", 74, 9);

        resources.AddSpriteResource("AccessCodesFailed", "Graphics\\AccessCodesFailed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("AccessCodesPassed", "Graphics\\AccessCodesPassed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("AccessCodesGray", "Graphics\\AccessCodesGray", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("PickupsIlluminated", "Graphics\\PickupsIlluminated", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource("AccessCodesFailedOriginal", "Graphics\\AccessCodesFailedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("AccessCodesPassedOriginal", "Graphics\\AccessCodesPassedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("AccessCodesGrayOriginal", "Graphics\\AccessCodesGrayOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("PickupsIlluminatedOriginal", "Graphics\\PickupsIlluminatedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void CheopsGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource("CheopsPyramid", "Graphics\\CheopsPyramid", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("CheopsPyramidOriginal", "Graphics\\CheopsPyramidOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void TeleporterGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource("TeleportInteriorLower", "Graphics\\TeleportInteriorLower", 1020, 543);
        resources.AddSpriteResource("TeleportInteriorUpper", "Graphics\\TeleportInteriorUpper", 654, 460);
        resources.AddSpriteResource("TeleporterKeyOverlays", "Graphics\\TeleporterKeyOverlays", 23, 23);
        resources.AddSpriteResource("TeleporterPowerSupply", "Graphics\\TeleporterPowerSupply", 246, 402);
    }

    private static void ElectricityGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource("ElectricityNodeElectricity1", "Graphics\\ElectricityAnim", 70, 64);
        resources.AddSpriteResource("ElectricityNodeElectricity2", "Graphics\\ElectricityAnim", 70, 64);
        resources.AddSpriteResource("NodeLights", "Graphics\\NodeLight", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource("GateLights", "Graphics\\GateLight", 32, 32);
        resources.AddSpriteResource("ElectricityOriginal", "Graphics\\ElectricityOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void OddSizedGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource("OllyBackground", "Graphics\\OllyBackground", 256, 192);
        resources.AddSpriteResource("OllyLower", "Graphics\\OllyLower", 256, 192);
        resources.AddSpriteResource("OllyUpper", "Graphics\\OllyUpper", 125, 120);
        resources.AddSpriteResource("OllyEyes", "Graphics\\OllyEyes", 44, 8);
        resources.AddSpriteResource("OllyOriginal", "Graphics\\OllyOriginal", 256, 192);

        resources.AddSpriteResource("BlobsShipOriginal", "Graphics\\BlobsSpaceShipOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource("BlobsShip", "Graphics\\BlobsSpaceShip", 156, 128);
        resources.AddSpriteResource("BlobsShipHazardLights", "Graphics\\BlobsSpaceShipHazardLights", 156, 128);
        resources.AddSpriteResource("BlobsShipHeadLights", "Graphics\\BlobsSpaceShipHeadLights", 156, 128);


        resources.AddSpriteResource("BlankingTile", "Graphics\\BlankingTile", TileMetrics.TileWidth, TileMetrics.TileHeight);

        resources.AddSpriteResource("BreakableBridge", "Graphics\\BreakableBridge", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource("BreakableBridgeOriginal", "Graphics\\BreakableBridgeOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);
    }

    private static void StatusPanelGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource("StatusPanelGraphic", "Graphics\\StatusPanelNew", 1020, 181);
        resources.AddSpriteResource("EnerygyBar", "Graphics\\EnergyBar", 256, 30);
        resources.AddSpriteResource("JumpBar", "Graphics\\JumpBar", 240, 30);
        resources.AddSpriteResource("AmmoBar", "Graphics\\AmmoBar", 240, 30);
        resources.AddSpriteResource("PanelNumbers", "Graphics\\PanelNumbers", 29, 29);
        resources.AddSpriteResource("StatusPanelShadow", "Graphics\\StatusPanelShadow", 1020, 50);
        resources.AddSpriteResource("StatusPanelOverlay", "Graphics\\StatusPanelOverlay", 966, 135);

        resources.AddSpriteResource("ParticleSprite", "Graphics\\BouncingParticle", 32, 32);
        resources.AddSpriteResource("CollisionTile", "Graphics\\CollisionTile", TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight);
    }

    private static void ParticleEffects(ResourceManager resources)
    {
        resources.AddParticleResource("BeamMeUp_Small", "ParticleEffects\\BeamMeUp_Small");
        resources.AddParticleResource("FireTrail", "ParticleEffects\\FireTrail");

        resources.AddParticleResource("Fireball", "ParticleEffects\\Fireball");
        resources.AddParticleResource("NastyDies", "ParticleEffects\\NastyDies");
        resources.AddParticleResource("NastySplat", "ParticleEffects\\NastySplat");

        resources.AddParticleResource("BinaryEffect", "ParticleEffects\\BinaryEffect");
        resources.AddParticleResource("BasicSmokeSmall", "ParticleEffects\\BasicSmokeSmall");

        resources.AddParticleResource("BasicSmokePlume", "ParticleEffects\\BasicSmokePlume");
        resources.AddParticleResource("BasicSmokePlume2", "ParticleEffects\\BasicSmokePlume2");
        resources.AddParticleResource("Sparks", "ParticleEffects\\Sparks");

        resources.AddParticleResource("CoreSparks", "ParticleEffects\\CoreSparks");
        resources.AddParticleResource("CoreReplacementParticleEffect", "ParticleEffects\\CoreReplacementParticleEffect");
        resources.AddParticleResource("FireSparks", "ParticleEffects\\FireSparks");

        resources.AddParticleResource("ForceField", "ParticleEffects\\ForceField");
        resources.AddParticleResource("BlobFragmentExplosion", "ParticleEffects\\BlobFragmentExplosion");
    }

    private static void AnimationTiles(ResourceManager resources)
    {
        resources.AddAnimatedSpriteResource("BreakableBridges", "Graphics\\BreakableBridgeAnim", TileMetrics.TileWidth, 8, (int)ActionTiles.BreakableBridges, 4);
        resources.AddAnimatedSpriteResource("TeleporterPadsOccupied", "Graphics\\TeleporterPadAnimationOccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.ShuttlePlatform, 8);
        resources.AddAnimatedSpriteResource("TeleporterPadsUnoccupied", "Graphics\\TeleporterPadAnimationUnoccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.RemovedShuttlePlatform, 1);
    }

    private static void Sounds(ResourceManager resources)
    {
        resources.AddSoundResource("ScanCode", "Sounds\\ScanCode", SoundEffectInstanceType.Multi, 4, false);
        resources.AddSoundResource("AccessSuccess", "Sounds\\AccessSuccess", SoundEffectInstanceType.Multi, 2, false);
        resources.AddSoundResource("AccessFailed", "Sounds\\AccessFailed", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource("MachineryBackground", "Sounds\\MachineryBackground", SoundEffectInstanceType.Single, 0, true);

        resources.AddSoundResource("Tunnel", "Sounds\\Tunnel", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource("TunnelSwish1", "Sounds\\TunnelSwish1", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource("TunnelSwish2", "Sounds\\TunnelSwish2", SoundEffectInstanceType.Single, 0, false);

        resources.AddSoundResource("NastyDeath", "Sounds\\NastyDeath", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource("BlobLazer", "Sounds\\BlobLazer", SoundEffectInstanceType.Multi, 2, true);
        resources.AddSoundResource("ElectricityNodes", "Sounds\\ElectricityNodes", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource("BridgeBreak", "Sounds\\BridgeBreak", SoundEffectInstanceType.Single, 0, true);
        resources.AddSoundResource("BlobRun", "Sounds\\BlobRun", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource("BlobFalls", "Sounds\\BlobFalls", SoundEffectInstanceType.Single, 0, true);
        resources.AddSoundResource("TunnelLift", "Sounds\\TunnelLift", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource("NastyAppear", "Sounds\\NastyAppear", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource("Rumble", "Sounds\\Rumble", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource("BlobJumps", "Sounds\\BlobJump", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource("BlobJumpsNoPlatforms", "Sounds\\BlobJumpNoPlatforms", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource("BlobPickUp", "Sounds\\BlobPickUp", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource("BlobOverloadAlarm", "Sounds\\BlobOverloadAlarm", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource("BlobShipFall", "Sounds\\BlobShipFall", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource("BlobShipExplodes", "Sounds\\BlobShipExplodes", SoundEffectInstanceType.Single, 0, false);

        resources.AddSoundResource("BlobExplodes", "Sounds\\BlobExplodes", SoundEffectInstanceType.Multi, 4, false);
        resources.AddSoundResource("BlobPartClang", "Sounds\\BlobPartClang", SoundEffectInstanceType.Multi, 8, false);

        resources.AddSoundResource("NastyAppears", "Sounds\\BlobExplodes", SoundEffectInstanceType.Multi, 4, false);
        resources.AddSoundResource("TeleportKeyClick", "Sounds\\TeleportKeyClick", SoundEffectInstanceType.Multi, 5, false);
        resources.AddSoundResource("TeleportInvalidCode", "Sounds\\TeleportInvalidCode", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource("Teleport", "Sounds\\Teleport", SoundEffectInstanceType.Multi, 3, false);

        resources.AddSoundResource("PowerUp1", "Sounds\\PowerUp1", SoundEffectInstanceType.Multi, 2, false);
        resources.AddSoundResource("PowerUp2", "Sounds\\PowerUp2", SoundEffectInstanceType.Multi, 2, false);
        resources.AddSoundResource("PowerUp3", "Sounds\\PowerUp3", SoundEffectInstanceType.Multi, 2, false);
        resources.AddSoundResource("LifePowerUp", "Sounds\\LifePowerUp", SoundEffectInstanceType.Multi, 2, false);

        resources.AddSoundResource("BackgroundSound1", "Sounds\\BackgroundSound1", SoundEffectInstanceType.Single, 0, false);
    }
}
