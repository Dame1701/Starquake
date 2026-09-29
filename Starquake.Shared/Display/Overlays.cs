// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.Helpers;

namespace Starquake.Display;

public class Overlays
{
    /// <summary>
    ///     The pause banner, laid out as a bordered box: a bar across the top and bottom, a post down
    ///     each side, and the darkened panel the word sits on.
    /// </summary>
    private const int BannerWidth = 244;

    /// <inheritdoc cref="BannerWidth" />
    private const int BannerHeight = 82;

    /// <inheritdoc cref="BannerWidth" />
    private const int BorderThickness = 10;

    /// <inheritdoc cref="BannerWidth" />
    private const int PanelInset = 12;

    /// <inheritdoc cref="BannerWidth" />
    private const int PanelWidth = 220;

    /// <inheritdoc cref="BannerWidth" />
    private const int PanelHeight = 58;

    /// <inheritdoc cref="BannerWidth" />
    private const int PostHeight = 62;

    /// <inheritdoc cref="BannerWidth" />
    private const int RightPostXPos = BannerWidth - BorderThickness;

    /// <inheritdoc cref="BannerWidth" />
    private const int BottomBarYPos = BannerHeight - BorderThickness;

    /// <inheritdoc cref="BannerWidth" />
    private const int TextXPos = 42;

    /// <inheritdoc cref="BannerWidth" />
    private const int TextYPos = 22;

    /// <inheritdoc cref="BannerWidth" />
    private const int TextScale = 2;

    /// <summary>
    ///     The banner's colours: a dark red border over an almost black panel, both left slightly
    ///     see-through so the paused game still shows behind them.
    /// </summary>
    private const int BorderRed = 100;

    /// <inheritdoc cref="BorderRed" />
    private const int PanelGrey = 20;

    /// <inheritdoc cref="BorderRed" />
    private const int BannerAlpha = 200;

    public static void Paused(int xPos, int yPos, GameEngine gameEngine)
    {
        gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(0 + xPos, 0 + yPos, BannerWidth, BorderThickness), ColourHelper.GetXna4Colour(BorderRed, 0, 0, BannerAlpha));
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(0 + xPos, BorderThickness + yPos, BorderThickness, PostHeight), ColourHelper.GetXna4Colour(BorderRed, 0, 0, BannerAlpha));
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(PanelInset + xPos, PanelInset + yPos, PanelWidth, PanelHeight), ColourHelper.GetXna4Colour(PanelGrey, PanelGrey, PanelGrey, BannerAlpha));
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(0 + xPos, BottomBarYPos + yPos, BannerWidth, BorderThickness), ColourHelper.GetXna4Colour(BorderRed, 0, 0, BannerAlpha));
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(RightPostXPos + xPos, BorderThickness + yPos, BorderThickness, PostHeight), ColourHelper.GetXna4Colour(BorderRed, 0, 0, BannerAlpha));
        gameEngine.ShapeDrawer.End();

        gameEngine.Font2.Begin(BlendState.AlphaBlend);
        gameEngine.Font2.WriteText(TextXPos + xPos, TextYPos + yPos, "PAUSED...", TextScale, Color.White, ColourLevels.Full);
        gameEngine.Font2.End();
    }
}