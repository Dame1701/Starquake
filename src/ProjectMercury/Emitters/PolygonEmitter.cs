// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace ProjectMercury.Emitters;

/// <summary>
///     Emits particles in the shape of a polygon defined with the Points property.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Emitters.PolygonEmitterTypeConverter, ProjectMercury.Design")]
#endif
public class PolygonEmitter : Emitter
{
    /// <summary>
    ///     Gets or sets a value indicating wether or not the polygon should be closed.
    /// </summary>
    public bool Close;

    /// <summary>
    ///     Polygon points.
    /// </summary>
    public PolygonPointCollection Points { get; set; }

    /// <summary>
    ///     Gets or sets the origin of the point collection.
    /// </summary>
    /// <value>The origin.</value>
    public PolygonOrigin Origin
    {
        get => Points.Origin;
        set => Points.Origin = value;
    }

    /// <summary>
    ///     Polygon rotation.
    /// </summary>
    public float Rotation
    {
        get => Calculator.Atan2(AngleSin, AngleCos);
        set
        {
            Guard.ArgumentNotFinite("Rotation", value);

            AngleCos = Calculator.Cos(value);
            AngleSin = Calculator.Sin(value);
        }
    }

    private float AngleCos = 1f;
    private float AngleSin;

    private float _scale;

    /// <summary>
    ///     Polygon scale.
    /// </summary>
    public float Scale
    {
        get => _scale;
        set
        {
            Guard.ArgumentNotFinite("Scale", value);
            Guard.ArgumentLessThan("Scale", value, float.Epsilon);

            _scale = value;
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PolygonEmitter" /> class.
    /// </summary>
    public PolygonEmitter()
    {
        Close = true;
        Points = new PolygonPointCollection();
        Scale = 1.0f;
    }

    /// <summary>
    ///     Returns an unitialised deep copy of the Emitter.
    /// </summary>
    /// <returns>A deep copy of the Emitter.</returns>
    public override Emitter DeepCopy()
    {
        var copy = new PolygonEmitter
        {
            Close = Close,
            Origin = Origin,
            Points = new PolygonPointCollection(),
            Rotation = Rotation,
            Scale = Scale
        };

        copy.Points.AddRange(Points);

        CopyBaseFields(copy);

        return copy;
    }

    /// <summary>
    ///     Generates an offset vector and force vector for a Particle when it is released.
    /// </summary>
    /// <param name="offset">The offset of the Particle from the trigger location.</param>
    /// <param name="force">A unit vector defining the initial force of the Particle.</param>
    protected override void GenerateOffsetAndForce(out Vector2 offset, out Vector2 force)
    {
#if XBOX
            offset = new Vector2();
#endif
        if (Points.Count == 0)
        {
            offset = Vector2.Zero;
        }

        else if (Points.Count == 1)
        {
            offset = Points[0];
        }

        else if (Points.Count == 2)
        {
            Vector2 a = Points[0],
                b = Points[1];

            float amount = RandomHelper.NextFloat();

            offset.X = a.X + (b.X - a.X) * amount;
            offset.Y = a.Y + (b.Y - a.Y) * amount;
        }

        else
        {
            int i = Close
                ? RandomHelper.NextInt(0, Points.Count)
                : RandomHelper.NextInt(0, Points.Count - 1);

            Vector2 a = Points[i],
                b = Points[(i + 1) % Points.Count];

            float amount = RandomHelper.NextFloat();

            offset.X = a.X + (b.X - a.X) * amount;
            offset.Y = a.Y + (b.Y - a.Y) * amount;
        }

        offset.X *= Scale;
        offset.Y *= Scale;

        Vector2 originalOffset = offset;

        offset.X = originalOffset.X * AngleCos + originalOffset.Y * -AngleSin;
        offset.Y = originalOffset.X * AngleSin + originalOffset.Y * AngleCos;

        offset.X += Points.TranslationOffset.X;
        offset.Y += Points.TranslationOffset.Y;

        force = RandomHelper.NextUnitVector();
    }
}

/// <summary>
///     Collection of points to generate a polygon.
/// </summary>
/// <remarks>
///     By implementing the IList interface explicitly, we can effectively override certain methods of the
///     base class without them being declared as virtual.
/// </remarks>
public class PolygonPointCollection : List<Vector2>, IList
{
    private PolygonOrigin _origin;

    /// <summary>
    ///     Gets or sets an offset vector for the shape.
    /// </summary>
    [ContentSerializerIgnore]
    public Vector2 TranslationOffset;

    /// <summary>
    ///     Gets or sets the origin of the shape defined by the points in the collection.
    /// </summary>
    [ContentSerializerIgnore]
    public PolygonOrigin Origin
    {
        get => _origin;
        set
        {
            if (Origin != value)
            {
                _origin = value;

                RecalculateTranslation();
            }
        }
    }

    int IList.Add(object value)
    {
        var v = (Vector2)value;

        base.Add(v);

        RecalculateTranslation();

        return IndexOf(v);
    }

    void IList.Clear()
    {
        base.Clear();
    }

    void IList.Remove(object value)
    {
        var v = (Vector2)value;

        if (Contains(v))
        {
            base.Remove(v);
        }

        RecalculateTranslation();
    }

    void IList.RemoveAt(int index)
    {
        base.RemoveAt(index);

        RecalculateTranslation();
    }

    object IList.this[int index]
    {
        get => base[index];
        set
        {
            var v = (Vector2)value;

            base[index] = v;

            RecalculateTranslation();
        }
    }

    /// <summary>
    ///     Recalculats the translation offset vector based on the origin type.
    /// </summary>
    private void RecalculateTranslation()
    {
        switch (Origin)
        {
            case PolygonOrigin.Default:
            {
                GetDefaultTranslation(out TranslationOffset);

                break;
            }
            case PolygonOrigin.Center:
            {
                GetCenterTranslation(out TranslationOffset);

                break;
            }
            case PolygonOrigin.Origin:
            {
                GetOriginTranslation(out TranslationOffset);

                break;
            }
        }
    }

    private void GetCenterTranslation(out Vector2 offset)
    {
        // Creates a rectangle around the polygon and use its center as the polygon center.
        float left = base[0].X,
            right = base[0].X,
            top = base[0].Y,
            bottom = base[0].Y;

        // Check all the points to make the rectangle surround the entire polygon.
        for (int i = 1; i < Count; i++)
        {
            left = base[i].X < left ? base[i].X : left;
            right = base[i].X > right ? base[i].X : right;
            top = base[i].Y < top ? base[i].Y : top;
            bottom = base[i].Y > bottom ? base[i].Y : bottom;
        }

        offset = new Vector2
        {
            X = -((right - left) / 2f) + left,
            Y = -((bottom - top) / 2f) + top
        };
    }

    private void GetOriginTranslation(out Vector2 offset)
    {
        offset = base[0];
    }

    private void GetDefaultTranslation(out Vector2 offset)
    {
        offset = Vector2.Zero;
    }
}

/// <summary>
///     Enumerates the origin options for a polygon shape.
/// </summary>
public enum PolygonOrigin : byte
{
    /// <summary>
    ///     No origin is specified, the translation vector will not be set.
    /// </summary>
    Default = 0,

    /// <summary>
    ///     The translation vector will be set to move the origin into the centre of the shape.
    /// </summary>
    Center = 1,

    /// <summary>
    ///     The translation vector will be set to move the origin to the first point in this shape.
    /// </summary>
    Origin = 2
}