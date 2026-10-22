// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright © 2010 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

using System.Globalization;

namespace ProjectMercury;

/// <summary>
///     Defines a range (or interval) of floating point values.
/// </summary>
#if WINDOWS
    [Serializable]
#endif
public struct Range : IEquatable<Range>, IFormattable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Range" /> struct.
    /// </summary>
    /// <param name="minimum">The inclusive minimum value.</param>
    /// <param name="maximum">The inclusive maximum value.</param>
    public Range(float minimum, float maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    ///     Gets or sets the inclusive minimum value in the range.
    /// </summary>
    public float Minimum;

    /// <summary>
    ///     Gets or sets the inclusive maximum value in the range.
    /// </summary>
    public float Maximum;

    /// <summary>
    ///     Gets the size of the range.
    /// </summary>
    public float Size => Calculator.Abs(Maximum - Minimum);

    /// <summary>
    ///     Returns true if the specified range is completely contained within this range.
    /// </summary>
    public bool Contains(Range range)
        => Minimum <= range.Minimum &&
           Maximum >= range.Maximum;

    /// <summary>
    ///     Returns true if the specified value falls within the range.
    /// </summary>
    public bool Contains(float value)
        => Minimum <= value &&
           Maximum >= value;

    /// <summary>
    ///     Merges the specifed range into this range with a boolean union.
    /// </summary>
    public void Merge(Range value)
    {
        Minimum = Minimum < value.Minimum ? Minimum : value.Minimum;
        Maximum = Maximum > value.Maximum ? Maximum : value.Maximum;
    }

    /// <summary>
    ///     Intersects the specified range with this range with a boolean intersection.
    /// </summary>
    public void Intersect(Range value)
    {
        Minimum = Minimum > value.Minimum ? Minimum : value.Minimum;
        Maximum = Maximum < value.Maximum ? Maximum : value.Maximum;
    }

    /// <summary>
    ///     Subtracts the specified range from this range with a boolean difference.
    /// </summary>
    public void Subtract(Range value)
    {
        Range intersection = Intersect(this, value);

        if (intersection.Minimum > Minimum)
        {
            Maximum = intersection.Minimum;
        }

        else if (intersection.Maximum > Minimum)
        {
            Minimum = intersection.Maximum;
        }
    }

    /// <summary>
    ///     Creates a new range which is the boolean union of two specified ranges.
    /// </summary>
    /// <param name="x">Input range.</param>
    /// <param name="y">Input range.</param>
    public static Range Union(Range x, Range y)
        => new()
        {
            Minimum = x.Minimum < y.Minimum ? x.Minimum : y.Minimum,
            Maximum = x.Maximum > y.Maximum ? x.Maximum : y.Maximum
        };

    /// <summary>
    ///     Creates a new range which is the boolean intersection of two specified ranges.
    /// </summary>
    /// <param name="x">Input range.</param>
    /// <param name="y">Input range.</param>
    public static Range Intersect(Range x, Range y)
        => new()
        {
            Minimum = x.Minimum > y.Minimum ? x.Minimum : y.Minimum,
            Maximum = x.Maximum < y.Maximum ? x.Maximum : y.Maximum
        };

    /// <summary>
    ///     Creates a new range which is the boolean difference between two specified ranges.
    /// </summary>
    /// <param name="x">Input range.</param>
    /// <param name="y">Input range.</param>
    public static Range Subtract(Range x, Range y)
    {
        Range intersection = Intersect(x, y);

        var value = new Range();

        if (intersection.Minimum > x.Minimum)
        {
            value.Maximum = intersection.Minimum;
        }

        else if (intersection.Maximum > x.Maximum)
        {
            value.Minimum = intersection.Maximum;
        }

        return value;
    }

    /// <summary>
    ///     Creates a new range by parsing an ISO 31-11 string representation of a closed interval.
    /// </summary>
    /// <param name="value">Input string value.</param>
    /// <exception cref="FormatException">Thrown if the input string is not in a valid ISO 31-11 closed interval format.</exception>
    /// <remarks>Example of a well formed ISO 31-11 closed interval: <i>"[0,1]"</i>. Open intervals are not supported.</remarks>
    public static Range Parse(string value)
        => Parse(value, CultureInfo.InvariantCulture);

    /// <summary>
    ///     Creates a new range by parsing an ISO 31-11 string representation of a closed interval.
    /// </summary>
    /// <param name="value">Input stirng value.</param>
    /// <param name="format">The format provider.</param>
    /// <remarks>Example of a well formed ISO 31-11 closed interval: <i>"[0,1]"</i>. Open intervals are not supported.</remarks>
    public static Range Parse(string value, IFormatProvider format)
    {
        Guard.ArgumentNull("value", value);
        Guard.ArgumentNull("format", format);

        if (!value.StartsWith("[") || !value.EndsWith("]"))
        {
            goto badformat;
        }

        var numberFormat = NumberFormatInfo.GetInstance(format);

        char[] groupSeperator = numberFormat.NumberGroupSeparator.ToCharArray();

        string[] endpoints = value.Trim('[', ']').Split(groupSeperator);

        if (endpoints.Length != 2)
        {
            goto badformat;
        }

        return new Range
        {
            Minimum = float.Parse(endpoints[0], NumberStyles.Float, numberFormat),
            Maximum = float.Parse(endpoints[1], NumberStyles.Float, numberFormat)
        };

        badformat:
        throw new FormatException("Value is not in ISO 31-11 format for a closed interval.");
    }

    /// <summary>
    ///     Determines whether the specified <see cref="System.Object" /> is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object obj)
    {
        if (obj != null)
        {
            if (obj is Range)
            {
                return Equals((Range)obj);
            }
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the specified <see cref="Range" /> is equal to this instance.
    /// </summary>
    /// <param name="value">The <see cref="Range" /> to compare with this instance.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="Range" /> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(Range value)
        => Minimum.Equals(value.Minimum) &&
           Maximum.Equals(value.Maximum);

    /// <summary>
    ///     Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode()
        => Minimum.GetHashCode() ^ Maximum.GetHashCode();

    /// <summary>
    ///     Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString()
        => ToString("G", CultureInfo.InvariantCulture);

    /// <summary>
    ///     Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>
    ///     A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public string ToString(IFormatProvider formatProvider)
        => ToString("G", formatProvider);

    /// <summary>
    ///     Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>
    ///     A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public string ToString(string format, IFormatProvider formatProvider)
    {
        var numberFormat = NumberFormatInfo.GetInstance(formatProvider);

        string minimum = Minimum.ToString(format, numberFormat);
        string maximum = Maximum.ToString(format, numberFormat);
        string seperator = numberFormat.NumberGroupSeparator;

        return string.Format(formatProvider, "[{0}{1}{2}]", minimum, seperator, maximum);
    }

    /// <summary>
    ///     Implements the operator +.
    /// </summary>
    /// <param name="x">The lvalue.</param>
    /// <param name="y">The rvalue.</param>
    /// <returns>The boolean union of the lvalue and rvalue.</returns>
    public static Range operator +(Range x, Range y)
        => Union(x, y);

    /// <summary>
    ///     Implements the operator -.
    /// </summary>
    /// <param name="x">The lvalue.</param>
    /// <param name="y">the rvalue.</param>
    /// <returns>The boolean difference of the lvalue and rvalue.</returns>
    public static Range operator -(Range x, Range y)
        => Subtract(x, y);

    /// <summary>
    ///     Implements the operator |.
    /// </summary>
    /// <param name="x">The lvalue.</param>
    /// <param name="y">The rvalue.</param>
    /// <returns>The boolean intersection of the lvalue and rvalue.</returns>
    public static Range operator |(Range x, Range y)
        => Intersect(x, y);

    /// <summary>
    ///     Implements the operator ==.
    /// </summary>
    /// <param name="x">The lvalue.</param>
    /// <param name="y">The rvalue.</param>
    /// <returns>
    ///     <c>true</c> if the lvalue <see cref="Range" /> is equal to the rvalue; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(Range x, Range y)
        => x.Equals(y);

    /// <summary>
    ///     Implements the operator !=.
    /// </summary>
    /// <param name="x">The lvalue.</param>
    /// <param name="y">The rvalue.</param>
    /// <returns>
    ///     <c>true</c> if the lvalue <see cref="Range" /> is not equal to the rvalue; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(Range x, Range y)
        => !x.Equals(y);
}