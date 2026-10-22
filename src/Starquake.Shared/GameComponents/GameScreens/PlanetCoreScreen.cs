// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ActiveSprites;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Sounds;
using Starquake.Sprites;
using Starquake.Tuning;
using Starquake.World;

namespace Starquake.GameComponents.GameScreens;

internal class PlanetCoreScreen : IGameScreen, IDynamicCollisionCells
{
    /// <summary>
    ///     The most aliens the core room ever holds, which is also how many it holds once Blob has
    ///     nearly finished repairing it. The fuller the core, the busier its guards.
    /// </summary>
    private const int MaxCoreAliens = 4;

    /// <summary>How many core elements there are to replace. The last one finishes the game.</summary>
    private const int CoreElementsToReplace = 9;

    /// <summary>
    ///     How busy the core room gets as it is repaired. A guard arrives as the number of elements
    ///     still missing falls past each of these, so an untouched core is unguarded and a nearly
    ///     finished one has all <see cref="MaxCoreAliens" />.
    /// </summary>
    private const int OneAlienFrom = 8;

    /// <inheritdoc cref="OneAlienFrom" />
    private const int TwoAliensFrom = 6;

    /// <inheritdoc cref="OneAlienFrom" />
    private const int ThreeAliensFrom = 4;

    /// <inheritdoc cref="OneAlienFrom" />
    private const int FourAliensFrom = 2;

    /// <summary>
    ///     How long Blob is held in the core room before being put back outside, in logic frames.
    /// </summary>
    private const int CoreVisitFrames = 240;

    /// <summary>How fast the tiles blanking out the room fade as the core is revealed.</summary>
    private const int BlankingTilesFadeStep = 16;

    /// <summary>
    ///     The blanking tiles cover the room from the second column onwards, and run
    ///     <see cref="BlankingTilesAcross" /> by <see cref="BlankingTilesDown" />.
    /// </summary>
    private const int BlankingTilesAcross = 6;

    /// <inheritdoc cref="BlankingTilesAcross" />
    private const int BlankingTilesDown = 6;

    /// <inheritdoc cref="BlankingTilesAcross" />
    private const int BlankingTilesFirstColumn = TileMetrics.TileWidth;

    /// <summary>
    ///     Where the core's own particle effects are emitted from: the middle of the playfield.
    /// </summary>
    private const int CoreCentreX = TileMetrics.PlayAreaWidth / 2;

    /// <inheritdoc cref="CoreCentreX" />
    private const int CoreCentreY = TileMetrics.PlayAreaHeight / 2;

    /// <summary>How many frames the electricity artwork has, so the beams crawl as well as move.</summary>
    private const int CoreArcFrames = 10;

    /// <summary>What the status panel is told when no part is being drawn out of Blob's hands.</summary>
    private const int NoSlotIdentified = -1;

    /// <summary>
    ///     The two copies of the core room's loop, each on its own instance so they can be tuned and
    ///     placed apart. See <see cref="CoreSoundSettings" />.
    /// </summary>
    private static readonly string[] CoreSoundVoices =
        [Assets.Sounds.CoreSounds, Assets.Sounds.CoreSoundsSecondVoice];
    private readonly Blob _blob;

    private readonly List<Nasty> _coreAliens;
    private readonly IStandardParticleEffect _coreComputationEffect;

    /// <summary>The copies of the core room's loop, held so they can be bent and silenced.</summary>
    private readonly Guid[] _coreSoundIds = new Guid[CoreSoundSettings.Voices];

    private readonly IStandardParticleEffect _coreSparks;

    private readonly CoreElements _elements;

    /// <summary>
    ///     What the core is taking and which of Blob's slots it comes out of. The slot is carried
    ///     because it is not emptied when Blob walks in: it must still hold the part while the core
    ///     makes a show of picking it out, so the match can be seen glowing at both ends.
    /// </summary>
    private readonly Queue<(int Element, int Slot)> _elementsToReplace = new();
    private readonly GameScreenCollection _gameScreens;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;

    /// <summary>Held only to wring the room when a part lands in the core.</summary>
    private readonly ScreenEffectEngine _screenEffects;

    /// <summary>
    ///     The quad the funnel is drawn over. A <c>SpriteBatch</c> with a one by one white texture,
    ///     which is all a shader that works every pixel out for itself needs.
    /// </summary>
    private readonly Shapes _shapes;

    /// <summary>Held to glow the inventory slot a part is being drawn out of.</summary>
    private readonly StatusPanel _statusPanel;
    private int _blankingTilesOpacity;
    private int _coreAlienCount = MaxCoreAliens;

    /// <summary>
    ///     Logic frames the core has been running, which turns the funnel and moves the beams. Counts
    ///     while Blob is elsewhere too, because the core is the one room that carries on off screen.
    /// </summary>
    private int _coreFrame;
    private int _entryTime;

    /// <summary>How long the core has been making a show of the part it picked out.</summary>
    private int _framesIdentifying;

    /// <summary>
    ///     How many frames the core has been winding up towards bearing down on a replacement, or
    ///     winding back out of one.
    /// </summary>
    private int _framesWinding;

    /// <summary>
    ///     How far the funnel has turned. Accumulated rather than derived from
    ///     <see cref="_coreFrame" />, because the rate changes while a slot is being filled and a new
    ///     rate applied to the whole history would jump the funnel round.
    /// </summary>
    private float _funnelTurned;

    private bool _justEnteredCore = true;

    private bool _replacingElements;


    public PlanetCoreScreen(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        ScreenDirectory screenDirectory,
        Blob blob,
        Random randomNumberGenerator,
        CoreElements elements,
        Shapes shapes,
        ScreenEffectEngine screenEffects,
        StatusPanel statusPanel)
    {
        _shapes = shapes;
        _screenEffects = screenEffects;
        _statusPanel = statusPanel;

        _resources = resources;
        _gameScreens = gameScreens;
        _screenDirectory = screenDirectory;
        _blob = blob;

        _elements = elements;
        _elements.OnCoreElementReplaced += Elements_OnCoreElementReplaced;
        _elements.OnCoreElementLanded += Elements_OnCoreElementLanded;

        // The elements standing in the core are collision the screen's tiles say nothing about, so
        // the collision map asks for them on every rebuild. See IDynamicCollisionCells.
        _gameScreens.Collisions.RegisterDynamicCells(this);

        _coreAliens = new List<Nasty>();
        for (int a = 0; a < _coreAlienCount; a++)
        {
            var coreAlien = new Nasty(
                _resources,
                _gameScreens,
                graphicsSet,
                _blob,
                randomNumberGenerator,
                // A guardian is never mechanical, and only mechanical nasties explode this way, but
                // it is wired truthfully rather than with a no-op so one ever made mechanical would
                // shake the room like anything else.
                () => screenDirectory.MainGame.JoltScreen(),
                0,
                0,
                CoreElementsToReplace)
            {
                NastyType = NastyType.CoreGuardian
            };

            _coreAliens.Add(coreAlien);
        }

        _coreComputationEffect = (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BinaryEffect, ParticleEffectType.Continual, null);
        _coreSparks = (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.CoreSparks, ParticleEffectType.Continual, null);
    }


    /// <summary>
    ///     How hard the core is bearing down on the slot being filled: one at rest, rising towards
    ///     <see cref="CoreEnergySettings.FillingBrightness" /> while a part goes in.
    /// </summary>
    private float BearingDown => 1f + (CoreEnergySettings.FillingBrightness - 1f)
        * CoreEnergySettings.WoundUpBy(_framesWinding);

    /// <summary>
    ///     The cells of the nine elements standing in the core room, present only while the core room
    ///     is the screen being shown. The core sits on one of the planet's own screens, which Blob
    ///     walks normally once he has been put back outside.
    /// </summary>
    public IEnumerable<CollisionTile> CellsOnScreen(int screenNumber)
    {
        if (screenNumber != _gameScreens.CoreSite.ScreenNumber ||
            _screenDirectory.Current != GameScreenType.PlanetCore)
        {
            return [];
        }

        Point core = _gameScreens.CorePosition;

        return _elements.GetCollisionMap(core.X, core.Y);
    }

    public void Update(GameTime gameTime)
    {
        // The core room steps the screen effects itself, because the surge is triggered here and
        // MainGameScreen only steps the ones triggered from the planet. Without this the surge stays
        // pinned at full strength and never finishes, so each part going in re-centres a twist that
        // has not unwound.
        //
        // Here and not in UpdateOffScreen, which the engine also calls while Blob is out on the
        // planet. MainGameScreen is stepping them then, so the two together would run every effect
        // at twice its proper speed.
        _screenEffects.Update();

        if (_justEnteredCore)
        {
            _blankingTilesOpacity = ColourLevels.Full;
            _replacingElements = false;

            // The count must be settled before EnterCore, which places and clamps that many
            // guardians. The other way round places the previous visit's number and leaves the rest
            // wherever they last stood.
            CalculateCoreAlienCount();
            EnterCore();
            _justEnteredCore = false;

            TakeElementsTheCoreWants();

            if (!_replacingElements)
            {
                _entryTime = 0;
            }
            else
            {
                StartNextReplacement();
            }
        }

        if (!_replacingElements)
        {
            _blankingTilesOpacity = Ramp.Down(_blankingTilesOpacity, BlankingTilesFadeStep, ColourLevels.None);
        }

        PlayCoreSoundsUnlessReplacing();

        if (!_replacingElements && _entryTime > CoreVisitFrames)
        {
            LeaveTheCoreRoom();
        }
        else if (!_replacingElements && _elements.GetElementsReplacedCount() == CoreElementsToReplace)
        {
            CompleteTheGame();
        }
        else
        {
            UpdateOffScreen(gameTime);
        }

        _entryTime++;
    }

    public void Draw()
    {
        // Under everything else in the room, so the elements and guardians sit in the funnel rather
        // than behind it. Drawn through a replacement as well as around one: the room should be at
        // its most alive at that moment, not dark.
        DrawFunnel();

        _coreComputationEffect.Draw();
        _coreSparks.Draw();

        if (!_replacingElements)
        {
            _resources.GetSpriteCollection(Assets.Graphics.BlankingTile).Begin(BlendState.AlphaBlend);
            for (int a = 0; a < BlankingTilesDown; a++)
            {
                for (int b = 0; b < BlankingTilesAcross; b++)
                {
                    _resources.GetSpriteCollection(Assets.Graphics.BlankingTile).DrawSprite(0, b * TileMetrics.TileWidth + BlankingTilesFirstColumn, a * TileMetrics.TileHeight, Colours.WhiteFadedTo((byte)_blankingTilesOpacity));
                }
            }
            _resources.GetSpriteCollection(Assets.Graphics.BlankingTile).End();
        }

        _gameScreens.Renderer.Draw(Color.White);

        DrawCoreGuardians();

        // Where the core sits comes off the map, so it is asked for rather than known here.
        Point core = _gameScreens.CorePosition;

        _elements.Draw(core.X, core.Y);

        DrawBeamsAcrossTheElements(core);
    }

    /// <summary>
    ///     Loops the core room's sound while the room is idling, its elements flashing and its
    ///     guardians bounding about, and silences it while parts are going in. Asked for every frame,
    ///     which a looped sound shrugs off, so it comes back by itself once the last part has landed.
    ///     Leaving the room takes it down with the rest of the screen's sounds.
    /// </summary>
    /// <remarks>
    ///     Placed, tuned and bent afresh each frame, off the same frame count the funnel turns by and
    ///     the same faders the elements flash with. See <see cref="CoreSoundSettings" />.
    /// </remarks>
    private void PlayCoreSoundsUnlessReplacing()
    {
        SoundEffectEngine sounds = _resources.GetSoundFxEngine();

        if (_replacingElements)
        {
            foreach (Guid id in _coreSoundIds)
            {
                sounds.StopSound(id, SoundFade.Fast);
            }
            return;
        }

        float flashSwing = CoreSoundSettings.FlashSwing(
            _elements.GetActiveElements().Select(element => element.Flash).ToList());

        for (int voice = 0; voice < CoreSoundSettings.Voices; voice++)
        {
            _coreSoundIds[voice] = sounds.PlaySound(
                CoreSoundVoices[voice], CoreSoundSettings.VolumeOf(voice, _coreFrame), true);

            sounds.Bend(
                _coreSoundIds[voice],
                CoreSoundSettings.PitchOf(voice, flashSwing),
                CoreSoundSettings.PanOf(voice, _coreFrame));
        }
    }

    /// <summary>
    ///     The funnel of energy turning about the core, drawn over a plain filled rectangle rather
    ///     than artwork. The shader works out every pixel, so there is nothing to sample and
    ///     <c>Shapes</c>' one by one white texture is the whole quad.
    /// </summary>
    private void DrawFunnel()
    {
        Effect funnel = _resources.GetEffect(Assets.Shaders.CoreFunnel);

        funnel.Parameters["Turned"].SetValue(_funnelTurned);
        funnel.Parameters["Arms"].SetValue(CoreEnergySettings.Arms);
        funnel.Parameters["Twist"].SetValue(CoreEnergySettings.Twist);
        float brightness = CoreEnergySettings.Brightness * BearingDown;

        funnel.Parameters["Brightness"].SetValue(brightness);
        funnel.Parameters["ThroatBrightness"].SetValue(
            brightness * CoreEnergySettings.ThroatShare);
        funnel.Parameters["ThroatColour"].SetValue(CoreEnergySettings.ThroatColour);
        funnel.Parameters["ArmColour"].SetValue(CoreEnergySettings.ArmColour);

        // The three spans are constants, so their reciprocals are worked out here rather than
        // costing a divide on every pixel. The shader has no slots to spare.
        funnel.Parameters["OuterEdge"].SetValue(CoreEnergySettings.OuterEdge);
        funnel.Parameters["OverOuterEdge"].SetValue(1f / CoreEnergySettings.OuterEdge);
        funnel.Parameters["OverThroatEdge"].SetValue(1f / CoreEnergySettings.ThroatEdge);
        funnel.Parameters["OverSpan"].SetValue(
            1f / (CoreEnergySettings.OuterEdge - CoreEnergySettings.ThroatEdge));

        var over = new Rectangle(
            CoreCentreX - CoreEnergySettings.FunnelSize / 2,
            CoreCentreY - CoreEnergySettings.FunnelSize / 2,
            CoreEnergySettings.FunnelSize,
            CoreEnergySettings.FunnelSize);

        _shapes.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, funnel);
        _shapes.FillRectangle(over, Color.White);
        _shapes.End();
    }

    /// <summary>
    ///     Electricity crawling between the elements set into the core, through the same burning
    ///     shader the node and gate arcs use, because it is the same electricity.
    /// </summary>
    private void DrawBeamsAcrossTheElements(Point core)
    {
        SpriteCollection arc = _resources.GetSpriteCollection(Assets.Graphics.ElectricityNodeElectricity1);

        int frame = _coreFrame % CoreArcFrames;

        arc.Begin(BlendState.Additive, null, ElectricSettings.PointAt(
            _resources.GetEffect(Assets.Shaders.Electric),
            arc.SpriteRectIn01(frame),
            arc.SpriteWidth,
            arc.SpriteHeight,
            _coreFrame));

        for (int beam = 0; beam < CoreEnergySettings.BeamCount; beam++)
        {
            (int from, int to) = CoreEnergySettings.BeamEndsAt(
                _coreFrame, beam, _elements.SlotBeingFilled);

            Point start = CoreEnergySettings.ElementCentre(core, from);
            Point end = CoreEnergySettings.ElementCentre(core, to);

            var along = new Vector2(end.X - start.X, end.Y - start.Y);

            arc.DrawSprite(
                frame,
                (start.X + end.X) / 2f - arc.SpriteWidth / 2f,
                (start.Y + end.Y) / 2f - arc.SpriteHeight / 2f,
                Color.White,
                CoreEnergySettings.BeamScale,
                (float)Math.Atan2(along.Y, along.X));
        }

        arc.End();
    }


    /// <summary>
    ///     Keeps the core room's events moving while Blob is away. The engine calls this directly
    ///     rather than through <see cref="IGameScreen" />, which holds only the frame loop's two
    ///     members, because the core is the only screen that carries on off screen.
    /// </summary>
    public void UpdateOffScreen(GameTime gameTime)
    {
        for (int a = 0; a < _coreAlienCount; a++)
        {
            _coreAliens[a].Update(gameTime);
        }

        _elements.Update(gameTime, _gameScreens.CorePosition);

        // Thrown from a point going round the middle rather than the middle itself, so they spiral
        // the way the funnel over them turns instead of spraying evenly outward.
        Point emitter = CoreEnergySettings.EmitterAt(_funnelTurned, CoreCentreX, CoreCentreY);

        _coreSparks.Start();
        _coreSparks.Update(emitter.X, emitter.Y, gameTime);

        _coreComputationEffect.Start();
        _coreComputationEffect.Update(emitter.X, emitter.Y, gameTime);

        UpdateIdentifying();

        _coreFrame++;

        // Wound towards bearing down while a slot is being filled and back out afterwards, so the
        // room leans into a replacement rather than snapping into one.
        _framesWinding = _elements.SlotBeingFilled is null
            ? Math.Max(0, _framesWinding - 1)
            : Math.Min(CoreEnergySettings.WindFrames, _framesWinding + 1);

        // Turned by this frame's own rate. The funnel winds up with everything else.
        _funnelTurned += (CoreEnergySettings.TurnedBy(1) - CoreEnergySettings.TurnedBy(0))
                         * (1f + (CoreEnergySettings.FillingTurnFaster - 1f)
                             * CoreEnergySettings.WoundUpBy(_framesWinding));
    }

    /// <summary>
    ///     A part has reached full size in its slot. The sound and the wring belong here rather than
    ///     on <see cref="Elements_OnCoreElementReplaced" />, which comes a fifth of a second later:
    ///     it waits on the burst at this moment, so hanging the room's answer off it leaves a gap
    ///     between the part landing and the room noticing.
    /// </summary>
    private void Elements_OnCoreElementLanded()
    {
        // Placeholder - see the remarks on Elements_OnCoreElementReplaced.
        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.AccessSuccess, 1f, false);

        WringTheRoomAboutTheSlotJustFilled();
    }

    /// <summary>
    ///     Wrings the room about the slot a part has just landed in. The slot is found before the
    ///     element's state moves on, so where it went in is read rather than guessed.
    /// </summary>
    /// <remarks>
    ///     The landing is raised while the part is still going in, so the element itself still says
    ///     which slot it is. Latching it every frame instead is only needed if the room is told after
    ///     the part has finished and stopped saying so.
    /// </remarks>
    private void WringTheRoomAboutTheSlotJustFilled()
    {
        // Read rather than remembered. See the remarks above.
        if (_elements.SlotBeingFilled is not { } slot)
        {
            return;
        }

        Point core = _gameScreens.CorePosition;
        Point centre = CoreEnergySettings.ElementCentre(core, slot);

        _screenEffects.WringCoreSurgeAbout(new Vector2(
            centre.X / (float)TileMetrics.PlayAreaWidth,
            centre.Y / (float)TileMetrics.PlayAreaHeight));

        _screenEffects.ExecuteScreenEffect(
            ScreenEffectType.CoreSurge, -1, CoreReplacementSettings.SurgeFrames, false);
    }

    /// <summary>
    ///     Starts the next element going in, and says whether there was one. Reached both when Blob
    ///     walks in carrying something the core wants and when one has just finished seating itself.
    /// </summary>
    private bool StartNextReplacement()
    {
        if (_elementsToReplace.Count == 0)
        {
            return false;
        }

        // Picked out and left to glow and shake first, in Blob's hands as well as in the core. The
        // countdown below is what drops it in, once the core has made a show of finding it.
        (int element, int slot) = _elementsToReplace.Peek();

        _elements.IdentifyElement(element);
        _statusPanel.IdentifySlot(slot);
        _framesIdentifying = 0;

        // The overload alarm, borrowed: a part being drawn out of Blob is the same kind of event as
        // his energy failing. Placeholder, see the remarks on Elements_OnCoreElementReplaced.
        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobOverloadAlarm, 1f, false);

        return true;
    }

    /// <summary>
    ///     Counts out the beat the core spends picking a part out, then drops it in. The part is
    ///     dequeued only here, so the identified one and the queued one cannot come apart.
    /// </summary>
    private void UpdateIdentifying()
    {
        if (_elements.SlotIdentified is null)
        {
            return;
        }

        _framesIdentifying++;

        if (_framesIdentifying < CoreReplacementSettings.IdentifyFrames)
        {
            return;
        }

        (int element, int slot) = _elementsToReplace.Dequeue();

        // Taken out of Blob's hands at the moment it goes in, not when he walked through the door.
        _blob.Carrying[slot] = Collectables.None;
        _statusPanel.IdentifySlot(NoSlotIdentified);

        _elements.ReplaceElement(element);

        // Placeholder - see the remarks on Elements_OnCoreElementReplaced.
        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.PowerUp3, 1f, false);
    }

    /// <summary>
    ///     Takes from Blob anything the core is still missing and queues it to go in. Every slot is
    ///     checked against every element the core wants, so one visit can hand over several.
    /// </summary>
    private void TakeElementsTheCoreWants()
    {
        _elementsToReplace.Clear();

        List<CoreElement> activeElements = _elements.GetActiveElements();

        for (int slot = 0; slot < _blob.Carrying.Length; slot++)
        {
            foreach (CoreElement activeElement in activeElements)
            {
                if ((int)_blob.Carrying[slot] != activeElement.CurrentElement)
                {
                    continue;
                }

                _elementsToReplace.Enqueue((activeElement.CurrentElement, slot));
                _replacingElements = true;
            }
        }
    }

    /// <summary>
    ///     Puts Blob out of the core and back on the planet, on the square the author marked rather
    ///     than beside the core against its right-hand wall.
    /// </summary>
    /// <remarks>
    ///     The Y is set as well as the X. Leaving it at whatever height Blob was at inside the core
    ///     room can put him mid air on any other screen.
    /// </remarks>
    private void LeaveTheCoreRoom()
    {
        _statusPanel.IdentifySlot(NoSlotIdentified);

        Point exit = _gameScreens.CoreExitPosition;

        _gameScreens.ScreenNumber = _gameScreens.CoreExit.ScreenNumber;
        _screenDirectory.Current = GameScreenType.MainGame;
        _blob.XPos = exit.X;
        _blob.YPos = exit.Y;
        _screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.PlanetCore, new ScreenChange.FadeIn());

        _justEnteredCore = true;
    }

    /// <summary>The core is whole again, which is the end of the game.</summary>
    private void CompleteTheGame()
    {
        // The game complete screen is not a change of screen as far as the sounds are concerned,
        // so the core's loop would carry on under its music.
        foreach (Guid id in _coreSoundIds)
        {
            _resources.GetSoundFxEngine().StopSound(id, SoundFade.None);
        }

        _screenDirectory.Current = GameScreenType.GameComplete;
        _screenDirectory.GameComplete.PerformPostScreenChangeActions();
    }

    /// <summary>
    ///     The guardians circling inside the core room. They fade up with the room, so they are drawn
    ///     against what is left of the blanking tiles rather than at full strength.
    /// </summary>
    private void DrawCoreGuardians()
    {
        for (int a = 0; a < _coreAlienCount; a++)
        {
            _coreAliens[a].Draw((byte)(ColourLevels.Full - _blankingTilesOpacity));
        }
    }

    /// <summary>
    ///     Puts the core screen and its guardians in place for an arrival. Places
    ///     <see cref="_coreAlienCount" /> of them, so <see cref="CalculateCoreAlienCount" /> must run
    ///     first.
    /// </summary>
    private void EnterCore()
    {
        // Setting the screen number rebuilds the collision map, and that rebuild is what puts the
        // core's elements into it. See CellsOnScreen. Adding them here instead loses them the moment
        // anything else on this screen rebuilds the map.
        _gameScreens.ScreenNumber = _gameScreens.CoreSite.ScreenNumber;

        PlaceCoreGuardians(0);
    }

    /// <summary>
    ///     Stands the guardians from <paramref name="firstToPlace" /> up to
    ///     <see cref="_coreAlienCount" /> somewhere the room allows and brings them to life. Clamps
    ///     the count if the room offers fewer places.
    /// </summary>
    /// <param name="firstToPlace">
    ///     The first guardian needing placement. Nought on arriving in the room, so all are put down.
    ///     The old count when a replacement has just raised it, so the ones already walking about stay
    ///     where they are rather than jumping to fresh squares.
    /// </param>
    private void PlaceCoreGuardians(int firstToPlace)
    {
        List<Point> potentialLocations = _gameScreens.GetNastyPotentialLocations();
        if (potentialLocations.Count < _coreAlienCount)
        {
            _coreAlienCount = potentialLocations.Count;
        }

        for (int a = firstToPlace; a < _coreAlienCount; a++)
        {
            _coreAliens[a].XPos = potentialLocations[a].X;
            _coreAliens[a].YPos = potentialLocations[a].Y;
            _coreAliens[a].NastyType = NastyType.CoreGuardian;
            _coreAliens[a].NastyState = NastyState.Normal;
        }
    }

    /// <summary>
    ///     One element has gone in, so either the next starts or the room settles back.
    /// </summary>
    /// <remarks>
    ///     <b>The two sounds here are placeholders.</b> A power-up effect stands in for a part
    ///     dropping in and the access-granted chime for it locking, because the core has no sound of
    ///     its own. Replacing them is a one-word change at each of the two call sites; adding a sound
    ///     of its own needs a wav in <c>Content/Sounds</c>, a block in <c>Content.mgcb</c>, a name in
    ///     <c>Assets.Sounds</c> and a line in <c>ContentManifest</c>.
    /// </remarks>
    private void Elements_OnCoreElementReplaced()
    {
        if (StartNextReplacement())
        {
            return;
        }

        _entryTime = 0;
        _replacingElements = false;

        // Replacing an element can earn another guardian, and one never placed is still at the
        // origin with nothing to move it. A core guardian is drawn whatever its state, so it would
        // show stuck in the top left corner until the room was left and entered again.
        int guardiansAlreadyPlaced = _coreAlienCount;

        CalculateCoreAlienCount();
        PlaceCoreGuardians(guardiansAlreadyPlaced);
    }

    private void CalculateCoreAlienCount()
    {
        int elementsLeftToReplace = _elements.GetActiveElements().Count;
        if (elementsLeftToReplace > OneAlienFrom)
        {
            _coreAlienCount = 0;
        }
        else if (elementsLeftToReplace > TwoAliensFrom)
        {
            _coreAlienCount = 1;
        }
        else if (elementsLeftToReplace > ThreeAliensFrom)
        {
            _coreAlienCount = 2;
        }
        else if (elementsLeftToReplace > FourAliensFrom)
        {
            _coreAlienCount = 3;
        }
        else
        {
            _coreAlienCount = MaxCoreAliens;
        }
    }
}
