// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;

namespace ProjectMercury.Controllers;

/// <summary>
///     Defines the abstract base class for a controller.
/// </summary>
public abstract class Controller
{
    /// <summary>
    ///     Gets or sets which particle effect the controller is assigned to.
    /// </summary>
    public ParticleEffect ParticleEffect { get; internal set; }

    /// <summary>
    ///     Called by the particle effect when it is triggered.
    /// </summary>
    /// <param name="position">The desired position of the trigger.</param>
    /// <remarks>
    ///     This method should not be called directly, the ParticleEffect class
    ///     will defer control to this method when the controller is assigned.
    /// </remarks>
    internal protected virtual void Trigger(ref Vector2 position)
    {
        if (ParticleEffect != null)
        {
            for (int i = 0; i < ParticleEffect.Count; i++)
            {
                ParticleEffect[i].Trigger(ref position);
            }
        }
    }

    /// <summary>
    ///     Called by the particle effect when it is updated.
    /// </summary>
    /// <param name="deltaSeconds">Elapsed time in whole and fractional seconds.</param>
    /// <remarks>
    ///     This method should not be called directly, the ParticleEffect class
    ///     will defer control to this method when the controller is assigned.
    /// </remarks>
    internal protected virtual void Update(float deltaSeconds)
    {
        if (ParticleEffect != null)
        {
            for (int i = 0; i < ParticleEffect.Count; i++)
            {
                ParticleEffect[i].Update(deltaSeconds);
            }
        }
    }
}