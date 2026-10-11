// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Core;
using Starquake.Structure;

namespace Starquake.ContentManagement;

/// <summary>
///     Everything the game loads, and the name each thing loads under. A list and nothing else: a
///     sprite sheet with its frame size, a sound with how many instances may play at once, a particle
///     effect, a screen's tile data.
///     <para>
///         The registration <em>order</em> is load-bearing for <c>ScreenData</c> and is otherwise
///         kept as found rather than tidied.
///     </para>
///     <para>
///         Four sheets sit in <c>Content\Graphics</c> and are deliberately absent from here and from
///         <c>Content.mgcb</c>: <c>Enhanced\BlobFire2</c>, <c>BlobFire2Overlay</c>,
///         <c>ExitShadows</c> and <c>CollisionTile</c>. Each was registered once and drawn by
///         nothing, so each was costing a texture on the graphics device for the whole run. The
///         artwork is kept; reviving one takes a line here and a block in <c>Content.mgcb</c>.
///     </para>
/// </summary>
internal static class ContentManifest
{
    /// <summary>Registers everything the game loads, in the order it was found in.</summary>
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
        Music(resources);
    }

    /// <summary>
    ///     The two parallax backdrops and the proportional digits. The backdrops are the redrawn
    ///     set's own - F8 turns them off along with the artwork, because the 1985 graphics have none
    ///     drawn for them - so only the digits here are shared.
    /// </summary>
    private static void SharedGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.Background1, "Graphics\\Enhanced\\Background1", 1024, 576);
        resources.AddSpriteResource(Assets.Graphics.Background2, "Graphics\\Enhanced\\Background2", 1024, 576);
        resources.AddSpriteResource(Assets.Graphics.ProportionalNumbers, "Graphics\\ProportionalNumbers", 27, 26);
    }

    private static void ScreenData(ResourceManager resources)
    {
        resources.AddDataResource(Assets.Data.GameScreens, "Data/SQScreenData.dat");
        resources.AddDataResource(Assets.Data.ScreenCollisionData, "Data/SQScreenData.dat");
    }

    /// <summary>
    ///     Both resources read the same file and the reader takes them in the order named here, so
    ///     these two must stay in this order and in this method.
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
        resources.AddSpriteResource(ResourceNames.Redrawn.UpperScreenTiles, "Graphics\\Enhanced\\UpperScreenTiles", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource(ResourceNames.Original.UpperScreenTiles, "Graphics\\Original\\UpperScreenTiles", TileMetrics.TileWidth, TileMetrics.TileHeight);

        resources.AddSpriteResource(ResourceNames.Redrawn.LowerScreenTiles, "Graphics\\Enhanced\\LowerScreenTiles", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource(ResourceNames.Original.LowerScreenTiles, "Graphics\\Original\\LowerScreenTiles", TileMetrics.TileWidth, TileMetrics.TileHeight);
    }

    private static void BlobGraphics(ResourceManager resources)
    {
        // The third of three redraws of Blob. Graphics\blobNew and Graphics\blobNewAlt are the
        // earlier two, left where they are rather than moved in beside this one because neither is a
        // second set of artwork. Both are still built, so either can be put back by changing this
        // one line.
        resources.AddSpriteResource(ResourceNames.Redrawn.BlobSprites, "Graphics\\Enhanced\\Blob", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.BlobSprites, "Graphics\\Original\\Blob", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource(Assets.Graphics.BlobParts, "Graphics\\Enhanced\\BlobParts", 32, 32);
        resources.AddSpriteResource(Assets.Graphics.BlobShuttle, "Graphics\\Enhanced\\BlobShuttle", 64, 32);
        resources.AddSpriteResource(Assets.Graphics.BlobShuttleOriginal, "Graphics\\Original\\BlobShuttle", 64, 32);

        // Both Original only. The redrawn set draws Blob's shot as a beam out of nothing and his
        // death and the nasties' as particles, so it wants no flame sheet at all: see
        // BlobWeapon.Draw, BlobDeath.Draw and Nasty, each of which reaches for these two under
        // IsOriginal and nothing under the other set. Graphics\Enhanced\BlobFire2 is a redraw of
        // the second that was left behind by that, which is why it sits on disk registered nowhere.
        resources.AddSpriteResource(Assets.Graphics.BlobFire1, "Graphics\\Original\\BlobFire1", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.BlobFire2Original, "Graphics\\Original\\BlobFire2", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource(Assets.Graphics.JumpPlatforms, "Graphics\\Enhanced\\BlobJumpPlatform", 64, 32);
        resources.AddSpriteResource(Assets.Graphics.JumpPlatformsOriginal, "Graphics\\Original\\BlobJumpPlatform", 64, 32);
    }

    private static void PickUpAndNastyGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(ResourceNames.Redrawn.PowerUps, "Graphics\\Enhanced\\PowerUps", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.PowerUps, "Graphics\\Original\\PowerUps", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Redrawn.Pickups, "Graphics\\Enhanced\\Pickups", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.Pickups, "Graphics\\Original\\Pickups", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.ConveyorNasty, "Graphics\\ConveyorNasty", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource(Assets.Graphics.Nasties, "Graphics\\Enhanced\\Nasties", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.NastiesOriginal, "Graphics\\Original\\Nasties", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void AccessBoothGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.ScanLine, "Graphics\\ScanLine", 74, 9);

        resources.AddSpriteResource(ResourceNames.Redrawn.AccessCodesFailed, "Graphics\\Enhanced\\AccessCodesFailed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Redrawn.AccessCodesPassed, "Graphics\\Enhanced\\AccessCodesPassed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.AccessCodesGray, "Graphics\\Enhanced\\AccessCodesGray", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Redrawn.PickupsIlluminated, "Graphics\\Enhanced\\PickupsIlluminated", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        resources.AddSpriteResource(ResourceNames.Original.AccessCodesFailed, "Graphics\\Original\\AccessCodesFailed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.AccessCodesPassed, "Graphics\\Original\\AccessCodesPassed", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.AccessCodesGrayOriginal, "Graphics\\Original\\AccessCodesGray", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(ResourceNames.Original.PickupsIlluminated, "Graphics\\Original\\PickupsIlluminated", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void CheopsGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.CheopsPyramid, "Graphics\\Enhanced\\CheopsPyramid", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.CheopsPyramidOriginal, "Graphics\\Original\\CheopsPyramid", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
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
        resources.AddSpriteResource(Assets.Graphics.ElectricityNodeElectricity1, "Graphics\\Enhanced\\Electricity", 70, 64);
        resources.AddSpriteResource(Assets.Graphics.ElectricityNodeElectricity2, "Graphics\\Enhanced\\Electricity", 70, 64);
        resources.AddSpriteResource(Assets.Graphics.NodeLights, "Graphics\\Enhanced\\NodeLight", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        resources.AddSpriteResource(Assets.Graphics.GateLights, "Graphics\\Enhanced\\GateLight", 32, 32);
        resources.AddSpriteResource(Assets.Graphics.ElectricityOriginal, "Graphics\\Original\\Electricity", SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
    }

    private static void OddSizedGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.OllyBackground, "Graphics\\Enhanced\\OllyBackground", 256, 192);
        resources.AddSpriteResource(Assets.Graphics.OllyLower, "Graphics\\Enhanced\\OllyLower", 256, 192);
        resources.AddSpriteResource(Assets.Graphics.OllyUpper, "Graphics\\Enhanced\\OllyUpper", 125, 120);
        resources.AddSpriteResource(Assets.Graphics.OllyEyes, "Graphics\\Enhanced\\OllyEyes", 44, 8);
        resources.AddSpriteResource(Assets.Graphics.OllyOriginal, "Graphics\\Original\\Olly", 256, 192);

        resources.AddSpriteResource(ResourceNames.Original.BlobsShip, "Graphics\\Original\\BlobsSpaceShip", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource(ResourceNames.Redrawn.BlobsShip, "Graphics\\Enhanced\\BlobsSpaceShip", 156, 128);
        resources.AddSpriteResource(Assets.Graphics.BlobsShipHazardLights, "Graphics\\Enhanced\\BlobsSpaceShipHazardLights", 156, 128);
        resources.AddSpriteResource(Assets.Graphics.BlobsShipHeadLights, "Graphics\\Enhanced\\BlobsSpaceShipHeadLights", 156, 128);


        resources.AddSpriteResource(Assets.Graphics.BlankingTile, "Graphics\\BlankingTile", TileMetrics.TileWidth, TileMetrics.TileHeight);

        resources.AddSpriteResource(Assets.Graphics.BreakableBridge, "Graphics\\Enhanced\\BreakableBridge", TileMetrics.TileWidth, TileMetrics.TileHeight);
        resources.AddSpriteResource(Assets.Graphics.BreakableBridgeOriginal, "Graphics\\Original\\BreakableBridge", TileMetrics.TileWidth, TileMetrics.TileHeight);
    }

    private static void StatusPanelGraphics(ResourceManager resources)
    {
        resources.AddSpriteResource(Assets.Graphics.StatusPanelGraphic, "Graphics\\StatusPanelNew", 1020, 181);
        resources.AddSpriteResource(Assets.Graphics.EnergyBar, "Graphics\\EnergyBar", 256, 30);
        resources.AddSpriteResource(Assets.Graphics.JumpBar, "Graphics\\JumpBar", 240, 30);
        resources.AddSpriteResource(Assets.Graphics.AmmoBar, "Graphics\\AmmoBar", 240, 30);
        resources.AddSpriteResource(Assets.Graphics.PanelNumbers, "Graphics\\PanelNumbers", 29, 29);
        resources.AddSpriteResource(Assets.Graphics.StatusPanelOverlay, "Graphics\\StatusPanelOverlay", 966, 135);

        resources.AddSpriteResource(Assets.Graphics.ParticleSprite, "Graphics\\BouncingParticle", 32, 32);    }

    private static void ParticleEffects(ResourceManager resources)
    {
        resources.AddParticleResource(Assets.Effects.BeamMeUpSmall, "ParticleEffects\\BeamMeUp_Small");
        resources.AddParticleResource(Assets.Effects.FireTrail, "ParticleEffects\\FireTrail");

        resources.AddParticleResource(Assets.Effects.NastyDies, "ParticleEffects\\NastyDies");
        resources.AddParticleResource(Assets.Effects.NastySplat, "ParticleEffects\\NastySplat");

        resources.AddParticleResource(Assets.Effects.BinaryEffect, "ParticleEffects\\BinaryEffect");

        resources.AddParticleResource(Assets.Effects.BasicSmokePlume, "ParticleEffects\\BasicSmokePlume");
        resources.AddParticleResource(Assets.Effects.BasicSmokePlume2, "ParticleEffects\\BasicSmokePlume2");

        resources.AddParticleResource(Assets.Effects.CoreSparks, "ParticleEffects\\CoreSparks");
        resources.AddParticleResource(Assets.Effects.CoreReplacementParticleEffect, "ParticleEffects\\CoreReplacementParticleEffect");
        resources.AddParticleResource(Assets.Effects.FireSparks, "ParticleEffects\\FireSparks");

        resources.AddParticleResource(Assets.Effects.ForceField, "ParticleEffects\\ForceField");
        resources.AddParticleResource(Assets.Effects.BlobFragmentExplosion, "ParticleEffects\\BlobFragmentExplosion");
    }

    /// <summary>
    ///     All three are the redrawn set's own: <c>AnimTile.Draw</c> draws nothing under the 1985
    ///     artwork, whose tiles do not animate.
    /// </summary>
    private static void AnimationTiles(ResourceManager resources)
    {
        resources.AddAnimatedSpriteResource(Assets.Animations.BreakableBridges, "Graphics\\Enhanced\\BreakableBridgeAnim", TileMetrics.TileWidth, 8, (int)ActionTiles.BreakableBridges, 4);
        resources.AddAnimatedSpriteResource(Assets.Animations.TeleporterPadsOccupied, "Graphics\\Enhanced\\TeleporterPadAnimationOccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.ShuttlePlatform, 8);
        resources.AddAnimatedSpriteResource(Assets.Animations.TeleporterPadsUnoccupied, "Graphics\\Enhanced\\TeleporterPadAnimationUnoccupied", TileMetrics.TileWidth, TileMetrics.TileHeight, (int)ActionTiles.RemovedShuttlePlatform, 1);
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
        resources.AddSoundResource(Assets.Sounds.CoreSounds, "Sounds\\CoreSounds", SoundEffectInstanceType.Single, 0, true);
        resources.AddSoundResource(Assets.Sounds.CoreSoundsSecondVoice, "Sounds\\CoreSounds", SoundEffectInstanceType.Single, 0, true);
    }

    /// <summary>
    ///     The music, named by the screen each piece plays on rather than by a registered name. The
    ///     five screens with a piece are the ones the game talks about itself on; the screens Blob is
    ///     played on have none, and a screen missing from here is a screen that plays in silence.
    ///     <para>In the order a player meets them, which is also the order the screens hand over in.</para>
    /// </summary>
    private static void Music(ResourceManager resources)
    {
        resources.AddMusicResource(GameScreenType.Options, "Music\\opening-game-song");
        // The one piece the game bends while it plays, which is why it is built as a sound effect
        // rather than a song. See MusicEngine.Distort and MalfunctionSettings.
        resources.AddMusicResource(GameScreenType.ComputerReport, "Music\\computer-report-song", true);
        resources.AddMusicResource(GameScreenType.GameComplete, "Music\\completed-game-song");
        resources.AddMusicResource(GameScreenType.GameOver, "Music\\gameover-song");
        resources.AddMusicResource(GameScreenType.HiScores, "Music\\hiscore-song");
    }
}
