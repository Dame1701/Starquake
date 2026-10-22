// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

using System.Globalization;
using Microsoft.Xna.Framework;

namespace ProjectMercury;

/// <summary>
///     Defines a Vector3 object which has a definable random variation.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.VariableFloat3TypeConverter, ProjectMercury.Design")]
#endif
public struct VariableFloat3 : IEquatable<VariableFloat3>
{
    /// <summary>
    ///     The base value of the VariableFloat3.
    /// </summary>
    public Vector3 Value;

    /// <summary>
    ///     The range of the random variation around the base value.
    /// </summary>
    public Vector3 Variation;

    /// <summary>
    ///     Samples the VariableFloat3.
    /// </summary>
    /// <returns>A randomised Vector3 value.</returns>
    public Vector3 Sample()
        => new()
        {
            X = RandomHelper.Variation(Value.X, Variation.X),
            Y = RandomHelper.Variation(Value.Y, Variation.Y),
            Z = RandomHelper.Variation(Value.Z, Variation.Z)
        };

    /// <summary>
    ///     Performs an implicit conversion from <see cref="Microsoft.Xna.Framework.Vector3" /> to
    ///     <see cref="ProjectMercury.VariableFloat3" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator VariableFloat3(Vector3 value)
        => new()
        {
            Value = value,
            Variation = Vector3.Zero
        };

    /// <summary>
    ///     Performs an implicit conversion from <see cref="ProjectMercury.VariableFloat3" /> to
    ///     <see cref="Microsoft.Xna.Framework.Vector3" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator Vector3(VariableFloat3 value)
        => value.Sample();

    /// <summary>
    ///     Indicates whether this instance and a specified object are equal.
    /// </summary>
    /// <param name="obj">Another object to compare to.</param>
    /// <returns>
    ///     true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false.
    /// </returns>
    public override bool Equals(object obj)
    {
        if (obj is VariableFloat3)
        {
            return Equals((VariableFloat3)obj);
        }

        return false;
    }

    /// <summary>
    ///     Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    ///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
    /// </returns>
    public bool Equals(VariableFloat3 other)
        => Value == other.Value &&
           Variation == other.Variation;

    /// <summary>
    ///     Returns the hash code for this instance.
    /// </summary>
    /// <returns>
    ///     A 32-bit signed integer that is the hash code for this instance.
    /// </returns>
    public override int GetHashCode()
        => Value.GetHashCode() + Variation.GetHashCode();

    /// <summary>
    ///     Returns the fully qualified type name of this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.String" /> containing a fully qualified type name.
    /// </returns>
    public override string ToString()
    {
        CultureInfo culture = CultureInfo.CurrentCulture;

        return string.Format("{{Value:{0}, Variation:{1}}}", Value, Variation);
    }
}