// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace ProjectMercury.Controllers;

/// <summary>
///     Defines a collection of controller objects.
/// </summary>
public class ControllerCollection : List<Controller>
{
    /// <summary>
    ///     Gets or sets the particle effect which owns the collection.
    /// </summary>
    public ParticleEffect Owner { get; internal set; }

    /// <summary>
    ///     Adds a controller to the collection.
    /// </summary>
    /// <param name="controller">The controller to add.</param>
    public new void Add(Controller controller)
    {
        if (!Contains(controller))
        {
            controller.ParticleEffect = Owner;

            base.Add(controller);
        }
    }

    /// <summary>
    ///     Removes a controller from the collection.
    /// </summary>
    /// <param name="controller">The controller to remove.</param>
    public new void Remove(Controller controller)
    {
        if (Contains(controller))
        {
            controller.ParticleEffect = null;

            base.Remove(controller);
        }
    }
}