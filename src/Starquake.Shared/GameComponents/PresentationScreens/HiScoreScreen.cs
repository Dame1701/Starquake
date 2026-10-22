// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.Input;
using Starquake.Scoring;
using Starquake.Sprites;

namespace Starquake.GameComponents.PresentationScreens;

internal class HiScoreScreen : GameScreenBase, IGameScreen
{
    /// <summary>The most letters a name in the table can have, each shown as a slot while it is typed.</summary>
    private const int NameLength = 6;

    /// <summary>
    ///     How far apart the name's slots sit, the same pitch as the teleporter's code so a letter
    ///     lands in place over its underscore rather than pushing the rest along.
    /// </summary>
    private const int NameSlotPitch = 50;

    private readonly SpriteCollection _backgroundImage;
    private readonly SpriteCollection _foregroundImage;

    private readonly HiScores _hiScoreList;
    private readonly PlayerStats _stats;
    private bool _gotHighScore;
    private HiScore _playersHighScore = new();

    public HiScoreScreen(
        ResourceManager resources,
        BitmapFont font,
        GraphicsDeviceManager graphics,
        PresentationSettings presentation,
        ScreenEffectEngine screenEffects,
        ScreenDirectory screenDirectory,
        PlayerStats stats,
        ToggleKeys input)
        : base(
            resources,
            font,
            graphics,
            presentation,
            screenEffects,
            screenDirectory,
            input)
    {
        _stats = stats;

        ScreenFade = 0;
        ScreenFadeSpeed = 5;

        _backgroundImage = Resources.GetSpriteCollection(Assets.Graphics.HiScoreScreenBackground);
        _foregroundImage = Resources.GetSpriteCollection(Assets.Graphics.HiScoreScreenForeground);
        _hiScoreList = new HiScores(new HiScoreStore());
    }

    public override void Update(GameTime gameTime)
    {
        if (!_gotHighScore)
        {
            // On a key or on the end of this screen's music, so a player who walks away is returned
            // to the title screen. Only once the table is the thing on show: the branch below has
            // the player mid-word and is theirs to finish.
            if (Input.AnyKeyPressed || Resources.GetMusicEngine().HasPlayedThrough)
            {
                GraphicsDeviceManager.GraphicsDevice.Clear(Presentation.ScreenClearColour);
                ScreenDirectory.Current = GameScreenType.Options;
                ScreenDirectory.Options.PerformPostScreenChangeActions();
            }
        }
        else
        {
            if (Input.GetPressedKey() != Keys.None)
            {
                string keyPressedString = Input.GetPressedKey().ToString().ToUpper();
                int keyPressed = AsciiCodes.GetAsciiCode(keyPressedString);

                if (_playersHighScore.Name.Length < NameLength)
                {
                    if (keyPressed is > 64 and < 91)
                    {
                        TypeIntoName(keyPressedString);
                    }

                    if (Input.GetPressedKey() == Keys.Space)
                    {
                        TypeIntoName(" ");
                    }
                }

                if (Input.GetPressedKey() == Keys.Enter)
                {
                    // The entry has been in the table since PerformPostScreenChangeActions and the
                    // letters typed into it in place, so this is the first moment there is a finished
                    // table worth keeping.
                    _gotHighScore = false;
                    _hiScoreList.Save();

                    // The table is what this screen's music is for, and this is the first frame it
                    // is the thing on show.
                    Resources.GetMusicEngine().StopHoldingBack();
                }


                if (Input.GetPressedKey() == Keys.Back)
                {
                    if (_playersHighScore.Name.Length > 0)
                    {
                        _playersHighScore.Name = _playersHighScore.Name.Substring(0, _playersHighScore.Name.Length - 1);
                        PlayKeyClick();
                    }
                }
            }
        }


        base.Update(gameTime);
    }

    public void Draw()
    {
        ScreenEffects.SelectRenderViewPort(ViewPorts.EntireScreen);
        _backgroundImage.Begin(BlendState.AlphaBlend);

        _backgroundImage.DrawSprite(0, 0, 0, Colours.WhiteFadedTo((byte)ScreenFade));

        _backgroundImage.End();

        if (_gotHighScore)
        {
            Font.Begin(BlendState.AlphaBlend);
            Font.WriteTextCentred(1280, 180, "ENCODE YOUR NAME", 2, Colours.GetXna4Colour(9, 132, ColourLevels.Full), (byte)ScreenFade);
            Font.WriteTextCentred(1280, 240, "INTO THE CORE", 2, Colours.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)ScreenFade);
            Font.WriteTextCentred(1280, 300, "FOR YOU HAVE ACHIEVED", 2, Colours.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)ScreenFade);
            Font.WriteTextCentred(1280, 360, "A HERO'S SCORE", 2, Colours.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)ScreenFade);

            DrawEnteredName();
            Font.End();
        }
        else
        {
            Font.Begin(BlendState.AlphaBlend);
            Font.WriteTextCentred(1280, 180, "CORE OF HEROES", 2, Colours.GetXna4Colour(9, 132, ColourLevels.Full), (byte)ScreenFade);
            Font.End();

            _hiScoreList.Draw(Resources, Font, 380, 250, ScreenFade);
        }

        _foregroundImage.Begin(BlendState.AlphaBlend);

        _foregroundImage.DrawSprite(0, 230, 120, Colours.WhiteFadedTo((byte)ScreenFade));

        _foregroundImage.End();
    }

    private void TypeIntoName(string letter)
    {
        _playersHighScore.Name += letter;
        PlayKeyClick();
    }

    private void PlayKeyClick()
    {
        Resources.GetSoundFxEngine().PlaySound(Assets.Sounds.TeleportKeyClick, 0.5f, false);
    }

    /// <summary>
    ///     The name as typed so far, one underscore per letter still free, laid out in fixed slots
    ///     like the teleporter's code. Drawn between the font's Begin and End.
    /// </summary>
    private void DrawEnteredName()
    {
        int slotX = (1280 - NameLength * NameSlotPitch) / 2;
        string paddedName = _playersHighScore.Name.PadRight(NameLength, '_');

        foreach (char letter in paddedName)
        {
            Font.WriteText(slotX, 420, letter.ToString(), 2, Colours.GetXna4Colour(9, 132, ColourLevels.Full), (byte)ScreenFade);
            slotX += NameSlotPitch;
        }
    }

    /// <summary>
    ///     Called once this screen has been handed control, with the score it needs. Typed, so there
    ///     is no case to reject: the game over screen is the only way onto this one and always has a
    ///     score to pass.
    /// </summary>
    /// <param name="playersScore">
    ///     The score the player has just finished with, for a name to be typed against it.
    /// </param>
    public void PerformPostScreenChangeActions(HiScore playersScore)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
        _playersHighScore = new HiScore();

        _gotHighScore = _hiScoreList.GotHighScore(_stats.PlayerScore);

        if (_gotHighScore)
        {
            // Silent while the name is being typed. A player sitting over the keyboard working out
            // six letters is not being played out to, and the music belongs to the table they get
            // afterwards.
            Resources.GetMusicEngine().HoldBack(GameScreenType.HiScores);

            _playersHighScore = playersScore;
            _playersHighScore.Name = string.Empty;

            _hiScoreList.AddScoreToTable(_playersHighScore);
        }
    }
}
