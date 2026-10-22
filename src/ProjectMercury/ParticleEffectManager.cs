// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright © 2010 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

using ProjectMercury.Renderers;
using Parallel = ProjectMercury.Threading.Parallel;

namespace ProjectMercury;

public class ParticleEffectManager : List<ParticleEffect>
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="ParticleEffectManager" /> class.
    /// </summary>
    /// <param name="renderer">The renderer which will be used to render particles.</param>
    public ParticleEffectManager(Renderer renderer) : base(20)
    {
        Guard.ArgumentNull("renderer", renderer);

        Renderer = renderer;
    }
    /// <summary>
    ///     Gets or sets the renderer which is used to render the particle effects.
    /// </summary>
    public Renderer Renderer { get; set; }

    /// <summary>
    ///     Gets the number of active particles in each particle effect.
    /// </summary>
    public int ActiveParticlesCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < Count; i++)
            {
                count += this[i].ActiveParticlesCount;
            }

            return count;
        }
    }

    /// <summary>
    ///     Updates all the particle effects which are managed by the manager.
    /// </summary>
    /// <param name="deltaSeconds">The elapsed time in whole and fractional seconds.</param>
    /// <param name="multithreaded">True to update particle effects asynchronously.</param>
    public void Update(float deltaSeconds, bool multithreaded)
    {
        if (!multithreaded)
        {
            for (int i = 0; i < Count; i++)
            {
                this[i].Update(deltaSeconds);
            }
        }
        else
        {
            if (Count > 0)
            {
                Parallel.For(0, Count, delegate(int i)
                {
                    this[i].Update(deltaSeconds);
                });
            }
        }
    }

    /// <summary>
    ///     Draws each particle effect.
    /// </summary>
    public void Draw()
    {
        for (int i = 0; i < Count; i++)
        {
            Renderer.RenderEffect(this[i]);
        }
    }
}