// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Starquake.Core;

namespace Starquake.Particles;

/// <summary>
///     One Project Mercury effect and the two rules the game wraps every one of them in. The three
///     effect classes differ in when they emit and when they consider themselves finished, but not in
///     either of these.
/// </summary>
/// <remarks>
///     Held rather than inherited. A base class would have to own <c>Update</c> to share the step
///     below, and <c>Update</c> is the one method where the three genuinely differ. As a collaborator,
///     each effect class keeps its own <c>Update</c> whole.
/// </remarks>
internal sealed class MercuryEffect(GraphicsSet graphicsSet, ParticleEffect effect, SpriteBatchRenderer renderer)
{
    public ParticleEffect Effect { get; } = effect;

    public int ActiveParticles => Effect.ActiveParticlesCount;

    /// <summary>
    ///     Steps the simulation by however long the frame took. Must happen every frame, including
    ///     frames where the effect emits nothing: particles already in flight still have to travel and
    ///     die, and an effect that stops being stepped freezes them on screen rather than clearing
    ///     them.
    /// </summary>
    public void Advance(GameTime gameTime) =>
        Effect.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

    /// <summary>Starts particles at a point.</summary>
    public void Trigger(int xPos, int yPos)
    {
        var position = new Vector2(xPos, yPos);

        Effect.Trigger(ref position);
    }

    public void Terminate() => Effect.Terminate();

    /// <summary>
    ///     Draws whatever is in flight, but only for the redrawn artwork. The original 1985 graphics
    ///     have no particles at all, so the whole system is silent under that set, which is what the
    ///     check below is for.
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
