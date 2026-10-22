// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using ProjectMercury.Controllers;
using ProjectMercury.Emitters;

namespace ProjectMercury;

/// <summary>
///     Defines the root of a particle effect hierarchy.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.ParticleEffectTypeConverter, Projectmercury.Design")]
#endif
public class ParticleEffect : EmitterCollection
{
    private string _name;

    /// <summary>
    ///     Gets or sets the name of the ParticleEffect.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
        get => _name;
        set
        {
            Guard.ArgumentNullOrEmpty("Name", value);

            if (Name != value)
            {
                _name = value;

                OnNameChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    ///     Occurs when name of the ParticleEffect has been changed.
    /// </summary>
    public event EventHandler NameChanged;

    /// <summary>
    ///     Raises the <see cref="E:NameChanged" /> event.
    /// </summary>
    /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
    protected virtual void OnNameChanged(EventArgs e)
    {
        if (NameChanged != null)
        {
            NameChanged(this, e);
        }
    }

    /// <summary>
    ///     Gets or sets the author of the ParticleEffect.
    /// </summary>
    public string Author;

    /// <summary>
    ///     Gets or sets the description of the ParticleEffect.
    /// </summary>
    public string Description;

    /// <summary>
    ///     Gets or sets the controller which is assigned to the ParticleEffect.
    /// </summary>
    [ContentSerializerIgnore]
    public ControllerCollection Controllers { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ParticleEffect" /> class.
    /// </summary>
    public ParticleEffect()
    {
        Name = "Particle Effect";
        Controllers = new ControllerCollection
        {
            Owner = this
        };
    }

    /// <summary>
    ///     Returns a deep copy of the ParticleEffect.
    /// </summary>
    public virtual ParticleEffect DeepCopy()
    {
        var effect = new ParticleEffect
        {
            Author = Author,
            Description = Description,
            Name = Name
        };

        foreach (Emitter emitter in this)
        {
            effect.Add(emitter.DeepCopy());
        }

        return effect;
    }

    /// <summary>
    ///     Triggers the ParticleEffect at the specified position.
    /// </summary>
    public virtual void Trigger(Vector2 position)
    {
        if (Controllers.Count > 0)
        {
            for (int i = 0; i < Controllers.Count; i++)
            {
                Controllers[i].Trigger(ref position);
            }
        }

        else
        {
            for (int i = 0; i < Count; i++)
            {
                this[i].Trigger(ref position);
            }
        }
    }

    /// <summary>
    ///     Triggers the ParticleEffect at the specified position.
    /// </summary>
    public virtual void Trigger(ref Vector2 position)
    {
        if (Controllers.Count > 0)
        {
            for (int i = 0; i < Controllers.Count; i++)
            {
                Controllers[i].Trigger(ref position);
            }
        }

        else
        {
            for (int i = 0; i < Count; i++)
            {
                this[i].Trigger(ref position);
            }
        }
    }

    /// <summary>
    ///     Initialises all Emitters within the ParticleEffect.
    /// </summary>
    public virtual void Initialise()
    {
        for (int i = 0; i < Count; i++)
        {
            this[i].Initialise();
        }
    }

    /// <summary>
    ///     Terminates all Emitters within the ParticleEffect with immediate effect.
    /// </summary>
    public virtual void Terminate()
    {
        for (int i = 0; i < Count; i++)
        {
            this[i].Terminate();
        }
    }

    /// <summary>
    ///     Loads content required by Emitters within the ParticleEffect.
    /// </summary>
    public virtual void LoadContent(ContentManager content)
    {
        for (int i = 0; i < Count; i++)
        {
            this[i].LoadContent(content);
        }
    }

    /// <summary>
    ///     Updates all Emitters within the ParticleEffect.
    /// </summary>
    /// <param name="deltaSeconds">Elapsed frame time in whole and fractional seconds.</param>
    public virtual void Update(float deltaSeconds)
    {
        if (Controllers.Count > 0)
        {
            for (int i = 0; i < Controllers.Count; i++)
            {
                Controllers[i].Update(deltaSeconds);
            }
        }

        else
        {
            for (int i = 0; i < Count; i++)
            {
                this[i].Update(deltaSeconds);
            }
        }
    }

    /// <summary>
    ///     Updates all Emitters within the ParticleEffect.
    /// </summary>
    /// <param name="totalSeconds">Total game time in whole and fractional seconds.</param>
    /// <param name="deltaSeconds">Elapsed frame time in whole and fractional seconds.</param>
    [Obsolete("Use Update(deltaSeconds) instead.", false)]
    public virtual void Update(float totalSeconds, float deltaSeconds)
    {
        Update(deltaSeconds);
    }

    /// <summary>
    ///     Gets the total number of active Particles in the ParticleEffect.
    /// </summary>
    public int ActiveParticlesCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < Count; i++)
            {
                count += base[i].ActiveParticlesCount;
            }

            return count;
        }
    }
}