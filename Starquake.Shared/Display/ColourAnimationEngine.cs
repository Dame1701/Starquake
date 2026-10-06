// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Display;

internal class ColourAnimationEngine
{

    private readonly List<ColourFaderAnimation> _colourAnimationsRegistered;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;

    public ColourAnimationEngine(ResourceManager resources, GameScreenCollection gameScreens, GraphicsSet graphicsSet)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;

        _colourAnimationsRegistered = new List<ColourFaderAnimation>();
    }

    public void RegisterTile(ColourFaderAnimation faderAnimation)
    {
        _colourAnimationsRegistered.Add(faderAnimation);
    }

    public void Start(int tileNo, int loopCount)
    {
        foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
        {
            if (colourAnimation.TileNo == tileNo)
            {
                if (!colourAnimation.Running)
                {
                    Begin(colourAnimation, loopCount);
                }
            }
        }
    }

    public void Start(int tileNo, int loopCount, Color transitionColour, int intensity)
    {
        foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
        {
            if (colourAnimation.TileNo == tileNo)
            {
                if (!colourAnimation.Running)
                {
                    colourAnimation.TransitionColour = transitionColour;
                    colourAnimation.Intensity = intensity;

                    Begin(colourAnimation, loopCount);
                }
            }
        }
    }

    /// <summary>
    ///     Starts one registered animation from the top: invisible, fading up, with its loop count
    ///     reset. The transition colour's alpha is zeroed here rather than by the caller, so an
    ///     animation always begins invisible whichever overload asked for it.
    /// </summary>
    private static void Begin(ColourFaderAnimation colourAnimation, int loopCount)
    {
        colourAnimation.TransitionColour.A = 0;
        colourAnimation.Running = true;
        colourAnimation.Direction = ColourAnimationDirection.FadeUp;
        colourAnimation.Stopping = false;
        colourAnimation.LoopCount = loopCount;
        colourAnimation.LoopCounter = 0;
    }

    public void Stop(int tileNo)
    {
        for (int a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].TileNo == tileNo)
            {
                if (_colourAnimationsRegistered[a].Running &&
                    !_colourAnimationsRegistered[a].Stopping)
                {
                    _colourAnimationsRegistered[a].Stopping = true;
                }
            }
        }
    }

    public void Update()
    {
        foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
        {
            if (colourAnimation.Running)
            {
                if (colourAnimation.Direction == ColourAnimationDirection.FadeUp)
                {
                    if (colourAnimation.TransitionColour.A < ColourLevels.Full)
                    {
                        int value = colourAnimation.TransitionColour.A;
                        value += colourAnimation.Speed;

                        if (value > ColourLevels.Full)
                        {
                            value = ColourLevels.Full;
                        }

                        colourAnimation.TransitionColour.A = (byte)value;
                    }
                    else
                    {
                        colourAnimation.Direction = ColourAnimationDirection.FadeDown;
                    }
                }
                else if (colourAnimation.Direction == ColourAnimationDirection.FadeDown)
                {
                    if (colourAnimation.TransitionColour.A > 0)
                    {
                        int value = colourAnimation.TransitionColour.A;
                        value -= colourAnimation.Speed;

                        if (value < 0)
                        {
                            value = 0;
                        }

                        colourAnimation.TransitionColour.A = (byte)value;
                    }
                    else
                    {
                        if (!colourAnimation.Stopping &&
                            colourAnimation.LoopCounter < colourAnimation.LoopCount - 1)
                        {
                            if (colourAnimation.LoopCount >= 0)
                            {
                                colourAnimation.LoopCounter++;
                            }

                            colourAnimation.Direction = ColourAnimationDirection.FadeUp;
                        }
                        else
                        {
                            if (colourAnimation.Stopping ||
                                colourAnimation.LoopCount >= 0)
                            {
                                colourAnimation.Running = false;
                                colourAnimation.LoopCounter = 0;
                            }
                            else
                            {
                                colourAnimation.Direction = ColourAnimationDirection.FadeUp;
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
        List<(int Tile, int X, int Y, ColourFaderAnimation Animation)> pulsing = [];

        for (int y = 0; y < _gameScreens.TilesDown; y++)
        {
            for (int x = 0; x < _gameScreens.TilesAcross; x++)
            {
                foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
                {
                    if (!colourAnimation.Running)
                    {
                        continue;
                    }

                    int tileToDraw = upperLayer ? colourAnimation.UpperTileToDraw : colourAnimation.TileNo;

                    if (upperLayer && tileToDraw < 0)
                    {
                        continue;
                    }

                    if (_gameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage != colourAnimation.TileNo)
                    {
                        continue;
                    }

                    pulsing.Add((tileToDraw, x, y, colourAnimation));
                }
            }
        }

        DrawHalos(tiles, pulsing);

        tiles.Begin(BlendState.Additive);

        foreach ((int tile, int x, int y, ColourFaderAnimation animation) in pulsing)
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
        List<(int Tile, int X, int Y, ColourFaderAnimation Animation)> pulsing)
    {
        Effect halo = _resources.GetEffect(Assets.Shaders.Halo);

        halo.Parameters["OverReach"].SetValue(HaloSettings.OverReach);
        halo.Parameters["Falloff"].SetValue(HaloSettings.Falloff);

        halo.Parameters["YOverX"].SetValue(HaloSettings.YOverX);

        foreach ((int tile, int x, int y, ColourFaderAnimation animation) in pulsing)
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
        ColourFaderAnimation colourAnimation)
    {
        for (int layer = 0; layer < colourAnimation.Intensity; layer++)
        {
            tiles.DrawSprite(
                tileToDraw,
                x * TileMetrics.TileWidth,
                y * TileMetrics.TileHeight,
                colourAnimation.TransitionColour);
        }
    }
}
