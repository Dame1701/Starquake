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
        resources.AddSpriteResource(Assets.Graphics.Background1, "Graphics\\Background1", 1024, 576);
        resources.AddSpriteResource(Assets.Graphics.Background2, "Graphics\\Background2", 1024, 576);
        resources.AddSpriteResource(Assets.Graphics.ProportionalNumbers, "Graphics\\ProportionalNumbers", 27, 26);
    }

    private static void ScreenData(ResourceManager resources)
    {
        resources.AddDataResource(Assets.Data.GameScreens, "Data/SQScreenData.dat");
        resources.AddDataResource(Assets.Data.ScreenCollisionData, "Data/SQScreenData.dat");
    }

    /// <summary>
    ///     Both resources read the same file, and the reader takes them in the order they are named here,
    ///     so these two must stay in this order and in this method.
    /// </summary>
    private static void PresentationScreenGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.OptionScreenBackground, "Graphics\\OptionsScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource(Assets.Graphics.OptionScreenForeground, "Graphics\\OptionsScreenForeground", 821, 554);

        resources.AddSpriteResource(Assets.Graphics.ComputerReportScreenBackground, "Graphics\\ComputerReportScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource(Assets.Graphics.ComputerReportScreenForeground, "Graphics\\ComputerReportScreenForeground", 821, 554);

        resources.AddSpriteResource(Assets.Graphics.GameOverScreenBackground, "Graphics\\GameOverScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource(Assets.Graphics.GameOverScreenForeground, "Graphics\\GameOverScreenForeground", 821, 554);

        resources.AddSpriteResource(Assets.Graphics.HiScoreScreenBackground, "Graphics\\HiScoreScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource(Assets.Graphics.HiScoreScreenForeground, "Graphics\\HiScoreScreenForeground", 821, 554);

        resources.AddSpriteResource(Assets.Graphics.GameCompleteScreenBackground, "Graphics\\GameCompleteScreenBackground", CompositionSize.Width, CompositionSize.Height);
        resources.AddSpriteResource(Assets.Graphics.GameCompleteScreenForeground, "Graphics\\GameCompleteScreenForeground", 821, 554);
    }

    private static void GameScreenTiles(ResourceManager resources)
    {
        resources.AddSpriteResource(ResourceNames.Redrawn.UpperScreenTiles, "Graphics\\UpperScreenTiles", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource(ResourceNames.Original.UpperScreenTiles, "Graphics\\UpperScreenTilesOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);

        resources.AddSpriteResource(ResourceNames.Redrawn.LowerScreenTiles, "Graphics\\StartilesNew", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource(ResourceNames.Original.LowerScreenTiles, "Graphics\\StartilesOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);

        resources.AddSpriteResource(Assets.Graphics.ExitShadows, "Graphics\\ExitShadows", TileMetrics.TileWidth, TileMetrics.TileHeight);
    }

    private static void BlobGraphics(ResourceManager resources)
    {
        // The third of three redraws of Blob. blobNew and blobNewAlt are the earlier two; both are
        // still in Content and still built by Content.mgcb, so either can be put back by changing
        // this one line.
        resources.AddSpriteResource(ResourceNames.Redrawn.BlobSprites, "Graphics\\blobNewAlt2", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.BlobSprites, "Graphics\\BlobOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource(Assets.Graphics.BlobParts, "Graphics\\blobParts", 32, 32);
        resources.AddSpriteResource(Assets.Graphics.BlobShuttle, "Graphics\\BlobShuttle", 64, 32);
        resources.AddSpriteResource(Assets.Graphics.BlobShuttleOriginal, "Graphics\\BlobShuttleOriginal", 64, 32);

        resources.AddSpriteResource(Assets.Graphics.BlobFire1, "Graphics\\BlobFire1", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.BlobFire2, "Graphics\\BlobFireNew2", 32, 32);
        resources.AddSpriteResource(Assets.Graphics.BlobFire2Original, "Graphics\\BlobFire2Original", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource(Assets.Graphics.BlobFire2Overlay, "Graphics\\BlobFire2Overlay", 48, 48);
        resources.AddSpriteResource(Assets.Graphics.JumpPlatforms, "Graphics\\BlobJumpPlatform", 64, 32);
        resources.AddSpriteResource(Assets.Graphics.JumpPlatformsOriginal, "Graphics\\BlobJumpPlatformOriginal", 64, 32);
    }

    private static void PickUpAndNastyGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(ResourceNames.Redrawn.PowerUps, "Graphics\\PowerUps", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.PowerUps, "Graphics\\PowerUpsOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Redrawn.Pickups, "Graphics\\Pickups", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.Pickups, "Graphics\\PickupsOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.ConveyorNasty, "Graphics\\ConveyorNasty", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource(Assets.Graphics.Nasties, "Graphics\\Nasties", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.NastiesOriginal, "Graphics\\NastiesOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void AccessBoothGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.ScanLine, "Graphics\\ScanLine", 74, 9);

        resources.AddSpriteResource(ResourceNames.Redrawn.AccessCodesFailed, "Graphics\\AccessCodesFailed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Redrawn.AccessCodesPassed, "Graphics\\AccessCodesPassed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.AccessCodesGray, "Graphics\\AccessCodesGray", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Redrawn.PickupsIlluminated, "Graphics\\PickupsIlluminated", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource(ResourceNames.Original.AccessCodesFailed, "Graphics\\AccessCodesFailedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.AccessCodesPassed, "Graphics\\AccessCodesPassedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.AccessCodesGrayOriginal, "Graphics\\AccessCodesGrayOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.PickupsIlluminated, "Graphics\\PickupsIlluminatedOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void CheopsGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.CheopsPyramid, "Graphics\\CheopsPyramid", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.CheopsPyramidOriginal, "Graphics\\CheopsPyramidOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void TeleporterGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.TeleportInteriorLower, "Graphics\\TeleportInteriorLower", 1020, 543);
        resources.AddSpriteResource(Assets.Graphics.TeleportInteriorUpper, "Graphics\\TeleportInteriorUpper", 654, 460);
        resources.AddSpriteResource(Assets.Graphics.TeleporterKeyOverlays, "Graphics\\TeleporterKeyOverlays", 23, 23);
        resources.AddSpriteResource(Assets.Graphics.TeleporterPowerSupply, "Graphics\\TeleporterPowerSupply", 246, 402);
    }

    private static void ElectricityGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.ElectricityNodeElectricity1, "Graphics\\ElectricityAnim", 70, 64);
        resources.AddSpriteResource(Assets.Graphics.ElectricityNodeElectricity2, "Graphics\\ElectricityAnim", 70, 64);
        resources.AddSpriteResource(Assets.Graphics.NodeLights, "Graphics\\NodeLight", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.GateLights, "Graphics\\GateLight", 32, 32);
        resources.AddSpriteResource(Assets.Graphics.ElectricityOriginal, "Graphics\\ElectricityOriginal", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void OddSizedGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.OllyBackground, "Graphics\\OllyBackground", 256, 192);
        resources.AddSpriteResource(Assets.Graphics.OllyLower, "Graphics\\OllyLower", 256, 192);
        resources.AddSpriteResource(Assets.Graphics.OllyUpper, "Graphics\\OllyUpper", 125, 120);
        resources.AddSpriteResource(Assets.Graphics.OllyEyes, "Graphics\\OllyEyes", 44, 8);
        resources.AddSpriteResource(Assets.Graphics.OllyOriginal, "Graphics\\OllyOriginal", 256, 192);

        resources.AddSpriteResource(ResourceNames.Original.BlobsShip, "Graphics\\BlobsSpaceShipOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource(ResourceNames.Redrawn.BlobsShip, "Graphics\\BlobsSpaceShip", 156, 128);
        resources.AddSpriteResource(Assets.Graphics.BlobsShipHazardLights, "Graphics\\BlobsSpaceShipHazardLights", 156, 128);
        resources.AddSpriteResource(Assets.Graphics.BlobsShipHeadLights, "Graphics\\BlobsSpaceShipHeadLights", 156, 128);


        resources.AddSpriteResource(Assets.Graphics.BlankingTile, "Graphics\\BlankingTile", TileMetrics.TileWidth, TileMetrics.TileHeight);

        resources.AddSpriteResource(Assets.Graphics.BreakableBridge, "Graphics\\BreakableBridge", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource(Assets.Graphics.BreakableBridgeOriginal, "Graphics\\BreakableBridgeOriginal", TileMetrics.TileWidth, TileMetrics.TileHeight);
    }

    private static void StatusPanelGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.StatusPanelGraphic, "Graphics\\StatusPanelNew", 1020, 181);
        resources.AddSpriteResource(Assets.Graphics.EnergyBar, "Graphics\\EnergyBar", 256, 30);
        resources.AddSpriteResource(Assets.Graphics.JumpBar, "Graphics\\JumpBar", 240, 30);
        resources.AddSpriteResource(Assets.Graphics.AmmoBar, "Graphics\\AmmoBar", 240, 30);
        resources.AddSpriteResource(Assets.Graphics.PanelNumbers, "Graphics\\PanelNumbers", 29, 29);
        resources.AddSpriteResource(Assets.Graphics.StatusPanelShadow, "Graphics\\StatusPanelShadow", 1020, 50);
        resources.AddSpriteResource(Assets.Graphics.StatusPanelOverlay, "Graphics\\StatusPanelOverlay", 966, 135);

        resources.AddSpriteResource(Assets.Graphics.ParticleSprite, "Graphics\\BouncingParticle", 32, 32);
        resources.AddSpriteResource(Assets.Graphics.CollisionTile, "Graphics\\CollisionTile", TileMetrics.CollisionCellWidth, TileMetrics.CollisionCellHeight);
    }

    private static void ParticleEffects(ResourceManager resources)
    {
        resources.AddParticleResource(Assets.Effects.BeamMeUpSmall, "ParticleEffects\\BeamMeUp_Small");
        resources.AddParticleResource(Assets.Effects.FireTrail, "ParticleEffects\\FireTrail");

        resources.AddParticleResource(Assets.Effects.Fireball, "ParticleEffects\\Fireball");
        resources.AddParticleResource(Assets.Effects.NastyDies, "ParticleEffects\\NastyDies");
        resources.AddParticleResource(Assets.Effects.NastySplat, "ParticleEffects\\NastySplat");

        resources.AddParticleResource(Assets.Effects.BinaryEffect, "ParticleEffects\\BinaryEffect");
        resources.AddParticleResource(Assets.Effects.BasicSmokeSmall, "ParticleEffects\\BasicSmokeSmall");

        resources.AddParticleResource(Assets.Effects.BasicSmokePlume, "ParticleEffects\\BasicSmokePlume");
        resources.AddParticleResource(Assets.Effects.BasicSmokePlume2, "ParticleEffects\\BasicSmokePlume2");
        resources.AddParticleResource(Assets.Effects.Sparks, "ParticleEffects\\Sparks");

        resources.AddParticleResource(Assets.Effects.CoreSparks, "ParticleEffects\\CoreSparks");
        resources.AddParticleResource(Assets.Effects.CoreReplacementParticleEffect, "ParticleEffects\\CoreReplacementParticleEffect");
        resources.AddParticleResource(Assets.Effects.FireSparks, "ParticleEffects\\FireSparks");

        resources.AddParticleResource(Assets.Effects.ForceField, "ParticleEffects\\ForceField");
        resources.AddParticleResource(Assets.Effects.BlobFragmentExplosion, "ParticleEffects\\BlobFragmentExplosion");
    }

    private static void AnimationTiles(ResourceManager resources)
    {
        resources.AddAnimatedSpriteResource(Assets.Animations.BreakableBridges, "Graphics\\BreakableBridgeAnim", TileMetrics.TileWidth, 8, (int)ActionTiles.BreakableBridges, 4);
        resources.AddAnimatedSpriteResource(Assets.Animations.TeleporterPadsOccupied, "Graphics\\TeleporterPadAnimationOccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.ShuttlePlatform, 8);
        resources.AddAnimatedSpriteResource(Assets.Animations.TeleporterPadsUnoccupied, "Graphics\\TeleporterPadAnimationUnoccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.RemovedShuttlePlatform, 1);
    }

    private static void Sounds(ResourceManager resources)
    {
        resources.AddSoundResource(Assets.Sounds.ScanCode, "Sounds\\ScanCode", SoundEffectInstanceType.Multi, 4, false);
        resources.AddSoundResource(Assets.Sounds.AccessSuccess, "Sounds\\AccessSuccess", SoundEffectInstanceType.Multi, 2, false);
        resources.AddSoundResource(Assets.Sounds.AccessFailed, "Sounds\\AccessFailed", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource(Assets.Sounds.MachineryBackground, "Sounds\\MachineryBackground", SoundEffectInstanceType.Single, 0, true);

        resources.AddSoundResource(Assets.Sounds.Tunnel, "Sounds\\Tunnel", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource(Assets.Sounds.TunnelSwish1, "Sounds\\TunnelSwish1", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource(Assets.Sounds.TunnelSwish2, "Sounds\\TunnelSwish2", SoundEffectInstanceType.Single, 0, false);

        resources.AddSoundResource(Assets.Sounds.NastyDeath, "Sounds\\NastyDeath", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource(Assets.Sounds.BlobLazer, "Sounds\\BlobLazer", SoundEffectInstanceType.Multi, 2, true);
        resources.AddSoundResource(Assets.Sounds.ElectricityNodes, "Sounds\\ElectricityNodes", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource(Assets.Sounds.BridgeBreak, "Sounds\\BridgeBreak", SoundEffectInstanceType.Single, 0, true);
        resources.AddSoundResource(Assets.Sounds.BlobRun, "Sounds\\BlobRun", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource(Assets.Sounds.BlobFalls, "Sounds\\BlobFalls", SoundEffectInstanceType.Single, 0, true);
        resources.AddSoundResource(Assets.Sounds.TunnelLift, "Sounds\\TunnelLift", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource(Assets.Sounds.NastyAppear, "Sounds\\NastyAppear", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource(Assets.Sounds.Rumble, "Sounds\\Rumble", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource(Assets.Sounds.BlobJumps, "Sounds\\BlobJump", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource(Assets.Sounds.BlobJumpsNoPlatforms, "Sounds\\BlobJumpNoPlatforms", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource(Assets.Sounds.BlobPickUp, "Sounds\\BlobPickUp", SoundEffectInstanceType.Multi, 4, true);
        resources.AddSoundResource(Assets.Sounds.BlobOverloadAlarm, "Sounds\\BlobOverloadAlarm", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource(Assets.Sounds.BlobShipFall, "Sounds\\BlobShipFall", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource(Assets.Sounds.BlobShipExplodes, "Sounds\\BlobShipExplodes", SoundEffectInstanceType.Single, 0, false);

        resources.AddSoundResource(Assets.Sounds.BlobExplodes, "Sounds\\BlobExplodes", SoundEffectInstanceType.Multi, 4, false);
        resources.AddSoundResource(Assets.Sounds.BlobPartClang, "Sounds\\BlobPartClang", SoundEffectInstanceType.Multi, 8, false);

        resources.AddSoundResource(Assets.Sounds.NastyAppears, "Sounds\\BlobExplodes", SoundEffectInstanceType.Multi, 4, false);
        resources.AddSoundResource(Assets.Sounds.TeleportKeyClick, "Sounds\\TeleportKeyClick", SoundEffectInstanceType.Multi, 5, false);
        resources.AddSoundResource(Assets.Sounds.TeleportInvalidCode, "Sounds\\TeleportInvalidCode", SoundEffectInstanceType.Single, 0, false);
        resources.AddSoundResource(Assets.Sounds.Teleport, "Sounds\\Teleport", SoundEffectInstanceType.Multi, 3, false);

        resources.AddSoundResource(Assets.Sounds.PowerUp1, "Sounds\\PowerUp1", SoundEffectInstanceType.Multi, 2, false);
        resources.AddSoundResource(Assets.Sounds.PowerUp2, "Sounds\\PowerUp2", SoundEffectInstanceType.Multi, 2, false);
        resources.AddSoundResource(Assets.Sounds.PowerUp3, "Sounds\\PowerUp3", SoundEffectInstanceType.Multi, 2, false);
        resources.AddSoundResource(Assets.Sounds.LifePowerUp, "Sounds\\LifePowerUp", SoundEffectInstanceType.Multi, 2, false);

        resources.AddSoundResource(Assets.Sounds.BackgroundSound1, "Sounds\\BackgroundSound1", SoundEffectInstanceType.Single, 0, false);
    }
}
