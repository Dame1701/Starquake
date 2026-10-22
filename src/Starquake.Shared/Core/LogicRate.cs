// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Core;

/// <summary>
///     How many logic frames <c>GameHost</c> runs in a real second, and so how fast the game plays.
///     F11 switches it between the PAL rate the original was written for and the NTSC one.
/// </summary>
/// <remarks>
///     Only the pacing changes. Everything in the game counts logic frames, and a second of game
///     time stays <see cref="Constants.LogicClock.FramesPerSecond" /> of them, so at sixty hertz the
///     whole game runs 20% fast, waits and fades included, the way it did on an NTSC machine.
/// </remarks>
internal sealed class LogicRate
{
    /// <summary>The PAL rate, and the one the game starts at.</summary>
    public const int Pal = 50;

    /// <summary>The NTSC rate.</summary>
    public const int Ntsc = 60;

    /// <summary>
    ///     A rate at <paramref name="hertz" />, or at <see cref="Pal" /> for anything that is not one
    ///     of the two rates, which is what a hand-edited preferences file can hand over.
    /// </summary>
    public LogicRate(int hertz)
    {
        Hertz = hertz == Ntsc ? Ntsc : Pal;
    }

    public int Hertz { get; private set; }

    /// <summary>How much real time one logic frame takes.</summary>
    public TimeSpan FrameLength => TimeSpan.FromTicks(TimeSpan.TicksPerSecond / Hertz);

    /// <summary>What the mode announcement says the rate now is.</summary>
    public string Name => Hertz == Ntsc ? "60HZ NTSC" : "50HZ PAL";

    /// <summary>Swaps to the other rate.</summary>
    public void Switch() => Hertz = Hertz == Pal ? Ntsc : Pal;
}
