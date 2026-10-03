// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.ContentManagement;

namespace Starquake.Particles;

/// <summary>
///     One Project Mercury effect and the two rules the game wraps every one of them in. The three
///     effect classes differ in when they emit and when they consider themselves finished; none of
///     them differs in either of these, and all three wrote both out by hand.
/// </summary>
/// <remarks>
///     Held rather than inherited. A base class would have to own <c>Update</c> to share the step
///     below, and <c>Update</c> is the one method where the three genuinely differ - splitting that
///     story across two files to save two lines is a bad trade. As a collaborator each effect class
///     keeps its own <c>Update</c> whole and reads top to bottom.
/// </remarks>
internal sealed class MercuryEffect(GraphicsSet graphicsSet, ParticleEffect effect, SpriteBatchRenderer renderer)
{
    public ParticleEffect Effect { get; } = effect;

    public int ActiveParticles => Effect.ActiveParticlesCount;

    /// <summary>
    ///     Steps the simulation by however long the frame took. This has to happen every frame,
    ///     including frames where the effect is emitting nothing - the particles already in flight
    ///     still have to travel and die, and an effect that stops being stepped freezes them on
    ///     screen rather than clearing them.
    /// </summary>
    public void Advance(GameTime gameTime) =>
        Effect.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

    /// <summary>
    ///     Starts particles at a point.
    /// </summary>
    public void Trigger(int xPos, int yPos)
    {
        var position = new Vector2(xPos, yPos);

        Effect.Trigger(ref position);
    }

    public void Terminate() => Effect.Terminate();

    /// <summary>
    ///     Draws whatever is in flight - but only for the redrawn artwork. The original 1985
    ///     graphics have no particles at all, so the whole system is silent under that art set,
    ///     which is what the check below is and what all six draw methods wrote out in full.
    /// </summary>
    public void Render()
    {
        if (graphicsSet.IsRedrawn)
        {
            renderer.RenderEffect(Effect);
        }
    }

    /// <inheritdoc cref="Render()" />
    /// <param name="screenFade">How far up the screen the effect is drawn with it.</param>
    public void Render(int screenFade)
    {
        if (graphicsSet.IsRedrawn)
        {
            renderer.RenderEffect(Effect, screenFade);
        }
    }
}
