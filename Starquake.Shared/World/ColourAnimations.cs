// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Sprites;
using Starquake.Tuning;

namespace Starquake.World;

internal class ColourAnimations
{

    private readonly List<ColourFader> _registered;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;

    public ColourAnimations(ResourceManager resources, GameScreenCollection gameScreens, GraphicsSet graphicsSet)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;

        _registered = new List<ColourFader>();
    }

    public void RegisterTile(ColourFader fader)
    {
        _registered.Add(fader);
    }

    public void Start(int tileNo, int loopCount)
    {
        foreach (ColourFader fader in _registered)
        {
            if (fader.TileNo == tileNo)
            {
                if (!fader.Running)
                {
                    Begin(fader, loopCount);
                }
            }
        }
    }

    public void Start(int tileNo, int loopCount, Color transitionColour, int intensity)
    {
        foreach (ColourFader fader in _registered)
        {
            if (fader.TileNo == tileNo)
            {
                if (!fader.Running)
                {
                    fader.TransitionColour = transitionColour;
                    fader.Intensity = intensity;

                    Begin(fader, loopCount);
                }
            }
        }
    }

    /// <summary>
    ///     Starts one registered animation from the top: invisible, fading up, with its loop count
    ///     reset. The transition colour's alpha is zeroed here rather than by the caller, so an
    ///     animation always begins invisible whichever overload asked for it.
    /// </summary>
    private static void Begin(ColourFader fader, int loopCount)
    {
        fader.TransitionColour.A = 0;
        fader.Running = true;
        fader.Direction = ColourFaderDirection.FadeUp;
        fader.Stopping = false;
        fader.LoopCount = loopCount;
        fader.LoopCounter = 0;
    }

    public void Stop(int tileNo)
    {
        for (int a = 0; a < _registered.Count; a++)
        {
            if (_registered[a].TileNo == tileNo)
            {
                if (_registered[a].Running &&
                    !_registered[a].Stopping)
                {
                    _registered[a].Stopping = true;
                }
            }
        }
    }

    public void Update()
    {
        foreach (ColourFader fader in _registered)
        {
            if (fader.Running)
            {
                if (fader.Direction == ColourFaderDirection.FadeUp)
                {
                    if (fader.TransitionColour.A < ColourLevels.Full)
                    {
                        int value = fader.TransitionColour.A;
                        value += fader.Speed;

                        if (value > ColourLevels.Full)
                        {
                            value = ColourLevels.Full;
                        }

                        fader.TransitionColour.A = (byte)value;
                    }
                    else
                    {
                        fader.Direction = ColourFaderDirection.FadeDown;
                    }
                }
                else if (fader.Direction == ColourFaderDirection.FadeDown)
                {
                    if (fader.TransitionColour.A > 0)
                    {
                        int value = fader.TransitionColour.A;
                        value -= fader.Speed;

                        if (value < 0)
                        {
                            value = 0;
                        }

                        fader.TransitionColour.A = (byte)value;
                    }
                    else
                    {
                        if (!fader.Stopping &&
                            fader.LoopCounter < fader.LoopCount - 1)
                        {
                            if (fader.LoopCount >= 0)
                            {
                                fader.LoopCounter++;
                            }

                            fader.Direction = ColourFaderDirection.FadeUp;
                        }
                        else
                        {
                            if (fader.Stopping ||
                                fader.LoopCount >= 0)
                            {
                                fader.Running = false;
                                fader.LoopCounter = 0;
                            }
                            else
                            {
                                fader.Direction = ColourFaderDirection.FadeUp;
                            }
                        }
                    }
                }
            }
        }
    }

    public void Draw() => DrawLayer(_graphicsSet.Names.LowerScreenTiles, false);

    public void DrawUpperLayer() => DrawLayer(_graphicsSet.Names.UpperScreenTiles, true);

    /// <summary>
    ///     Draws every pulsing tile on the screen, from one sheet, as one batch. The lower layer
    ///     repaints the tile that is already there; the upper one lays a second tile over it, and a
    ///     pulse that has no second tile is skipped - which is the only difference between the two
    ///     passes, and the whole of why they were written out twice.
    /// </summary>
    private void DrawLayer(string sheetName, bool upperLayer)
    {
        SpriteCollection tiles = _resources.GetSpriteCollection(sheetName);

        // Found before any of it is drawn, for two reasons. A halo needs the shader pointed at its
        // own tile and a batch holds one set of values, so the halos cannot share the batch the
        // plain pulses go down in - and the halo has to go *under* the light it belongs to, which
        // means it cannot be drawn as it is found either.
        List<(int Tile, int X, int Y, ColourFader Animation)> pulsing = [];

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                foreach (ColourFader fader in _registered)
                {
                    if (!fader.Running)
                    {
                        continue;
                    }

                    int tileToDraw = upperLayer ? fader.UpperTileToDraw : fader.TileNo;

                    if (upperLayer && tileToDraw < 0)
                    {
                        continue;
                    }

                    if (_gameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage != fader.TileNo)
                    {
                        continue;
                    }

                    pulsing.Add((tileToDraw, x, y, fader));
                }
            }
        }

        DrawHalos(tiles, pulsing);

        tiles.Begin(BlendState.Additive);

        foreach ((int tile, int x, int y, ColourFader animation) in pulsing)
        {
            DrawLayeredUp(tiles, tile, x, y, animation);
        }

        tiles.End();
    }

    /// <summary>
    ///     The halo under each tile that asked for one: a soft glow drawn at rather more than the
    ///     tile's own size, which is where the spreading comes from.
    /// </summary>
    /// <remarks>
    ///     One batch per tile, because the shader has to be told which square of the sheet the
    ///     tile occupies, and a batch holds one set of values. That is a batch apiece for at most a
    ///     handful of tiles on a screen, which is why it is affordable here and would not be for
    ///     the tiles at large.
    ///     <para>
    ///         The halo takes its colour from the pulse's own, because the shader multiplies by the
    ///         tint it is drawn with - so a tag that pulses red throws a red halo without being
    ///         told what colour it is.
    ///     </para>
    /// </remarks>
    private void DrawHalos(
        SpriteCollection tiles,
        List<(int Tile, int X, int Y, ColourFader Animation)> pulsing)
    {
        Effect halo = _resources.GetEffect(Assets.Shaders.Halo);

        halo.Parameters["OverReach"].SetValue(HaloSettings.OverReach);
        halo.Parameters["Falloff"].SetValue(HaloSettings.Falloff);

        halo.Parameters["YOverX"].SetValue(HaloSettings.YOverX);

        foreach ((int tile, int x, int y, ColourFader animation) in pulsing)
        {
            if (animation.HaloLights.Length == 0)
            {
                continue;
            }

            halo.Parameters["SpriteRect"].SetValue(tiles.SpriteRectIn01(tile));

            tiles.Begin(BlendState.Additive, null, halo);

            foreach (Point light in animation.HaloLights)
            {
                // The scaled draw turns about the tile's own middle, so the corner is stepped back
                // by half a tile to put that middle on the light instead.
                int haloX = x * TileMetrics.TileWidth + light.X - TileMetrics.TileWidth / 2;
                int haloY = y * TileMetrics.TileHeight + light.Y - TileMetrics.TileHeight / 2;

                // Over itself, because it is competing with a pulse that has already drawn the
                // tile over itself as many as six times - see HaloSettings.Passes.
                for (int pass = 0; pass < HaloSettings.Passes; pass++)
                {
                    tiles.DrawSprite(
                        tile, haloX, haloY, animation.TransitionColour, HaloSettings.Scale, 0f);
                }
            }

            tiles.End();
        }
    }

    /// <summary>
    ///     One pulsing tile, drawn over itself as many times as its intensity asks for. The blend is
    ///     additive, so each pass deepens the shade rather than replacing it.
    /// </summary>
    private static void DrawLayeredUp(
        SpriteCollection tiles,
        int tileToDraw,
        int x,
        int y,
        ColourFader fader)
    {
        for (int layer = 0; layer < fader.Intensity; layer++)
        {
            tiles.DrawSprite(
                tileToDraw,
                x * TileMetrics.TileWidth,
                y * TileMetrics.TileHeight,
                fader.TransitionColour);
        }
    }
}
