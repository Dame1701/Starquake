// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury;

/// <summary>
///     Encapsulates common mathematical functions.
/// </summary>
public static class Calculator
{
    /// <summary>
    ///     Represents the value of pi.
    /// </summary>
    public const float Pi = 3.141593f;

    /// <summary>
    ///     Represents the value of pi times two.
    /// </summary>
    public const float TwoPi = 6.283185f;

    /// <summary>
    ///     Represents the value of pi divided by two.
    /// </summary>
    public const float PiOver2 = 1.570796f;

    /// <summary>
    ///     Represents the value of pi divided by four.
    /// </summary>
    public const float PiOver4 = 0.7853982f;

    /// <summary>
    ///     Restricts a value to be within a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value. If value is less than min, min will be returned.</param>
    /// <param name="max">The maximum value. If value is greater than max, max will be returned.</param>
    /// <returns>The clamped value.</returns>
    public static float Clamp(float value, float min, float max)
    {
        value = value > max ? max : value;
        value = value < min ? min : value;

        return value;
    }

    /// <summary>
    ///     Restricts a value to be within the specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="range">The range of allowable values.</param>
    /// <returns>The clamped value.</returns>
    public static float Clamp(float value, Range range)
    {
        value = value > range.Maximum ? range.Maximum : value;
        value = value < range.Minimum ? range.Minimum : value;

        return value;
    }

    /// <summary>
    ///     Restricts a value to be within a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value. If value is less than min, value will be assigned min.</param>
    /// <param name="max">The maximum value. If value is greater than max, value will be assigned max.</param>
    public static void Clamp(ref float value, float min, float max)
    {
        value = value > max ? max : value;
        value = value < min ? min : value;
    }

    /// <summary>
    ///     Restricts a value to be within a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="range">The range of allowable values.</param>
    public static void Clamp(ref float value, Range range)
    {
        value = value > range.Maximum ? range.Maximum : value;
        value = value < range.Minimum ? range.Minimum : value;
    }

    /// <summary>
    ///     Restricts a value to be within a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value. If value is less than min, min will be returned.</param>
    /// <param name="max">The maximum value. If value is greater than max, max will be returned.</param>
    /// <returns>The clamped value.</returns>
    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
    {
        value = value.CompareTo(max) > 0 ? max : value;
        value = value.CompareTo(min) < 0 ? min : value;

        return value;
    }

    /// <summary>
    ///     Wraps the specified value to be within the specified range.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The wrapped value.</returns>
    public static float Wrap(float value, float min, float max)
    {
        float range = max - min;

        if (value < min)
        {
            do
            {
                value += range;
            } while (value < min);
        }

        else if (value > max)
        {
            do
            {
                value -= range;
            } while (value > max);
        }

        return value;
    }

    /// <summary>
    ///     Wraps the specified value to be within the specified range.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="range">The range of allowable values.</param>
    /// <returns>The wrapped value.</returns>
    public static float Wrap(float value, Range range)
    {
        if (value < range.Minimum)
        {
            do
            {
                value += range.Size;
            } while (value < range.Minimum);
        }

        else if (value > range.Maximum)
        {
            do
            {
                value -= range.Size;
            } while (value > range.Maximum);
        }

        return value;
    }

    /// <summary>
    ///     Wraps the specified value to be within the specified range.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    public static void Wrap(ref float value, float min, float max)
    {
        float range = max - min;

        if (value < min)
        {
            do
            {
                value += range;
            } while (value < min);
        }

        else if (value > max)
        {
            do
            {
                value -= range;
            } while (value > max);
        }
    }

    /// <summary>
    ///     Wraps the specified value to be within the specified range.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="range">The allowable range of values.</param>
    public static void Wrap(ref float value, Range range)
    {
        if (value < range.Minimum)
        {
            do
            {
                value += range.Size;
            } while (value < range.Minimum);
        }

        else if (value > range.Maximum)
        {
            do
            {
                value -= range.Size;
            } while (value > range.Maximum);
        }
    }

    /// <summary>
    ///     Linearly interpolates between two values.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
    /// <returns>Interpolated value.</returns>
    public static float LinearInterpolate(float value1, float value2, float amount)
        => value1 + (value2 - value1) * amount;

    /// <summary>
    ///     Linearly interpolates between two values.
    /// </summary>
    /// <param name="value">The output value.</param>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
    public static void LinearInterpolate(ref float value, float value1, float value2, float amount)
    {
        value = value1 + (value2 - value1) * amount;
    }

    /// <summary>
    ///     Linearly interpolates between three values, where the position of the middle value is variable.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="value2Position">The position of the second source value between 0 and 1.</param>
    /// <param name="value3">Source value.</param>
    /// <param name="amount">Value between 0 and 1 indicating the position in the curve to evaluate.</param>
    /// <returns>Interpolated value.</returns>
    public static float LinearInterpolate(float value1, float value2, float value2Position, float value3, float amount)
    {
        if (amount < value2Position)
        {
            return LinearInterpolate(value1, value2, amount / value2Position);
        }

        return LinearInterpolate(value2, value3, (amount - value2Position) / (1f - value2Position));
    }

    /// <summary>
    ///     Interpolates between two values using a cubic equation.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="amount">Weighting value.</param>
    /// <returns>Interpolated value.</returns>
    public static float CubicInterpolate(float value1, float value2, float amount)
    {
        Clamp(ref amount, 0f, 1f);

        return LinearInterpolate(value1, value2, amount * amount * (3f - 2f * amount));
    }

    /// <summary>
    ///     Interpolates between two values using a cubic equation.
    /// </summary>
    /// <param name="value">The output value.</param>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="amount">Weighting value.</param>
    public static void CubicInterpolate(ref float value, float value1, float value2, float amount)
    {
        Clamp(ref amount, 0f, 1f);

        LinearInterpolate(ref value, value1, value2, amount * amount * (3f - 2f * amount));
    }

    /// <summary>
    ///     Returns the greater of two values.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <returns>The greater value.</returns>
    public static float Max(float value1, float value2)
        => value1 >= value2 ? value1 : value2;

    /// <summary>
    ///     Returns the greater of three values.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="value3">Source value.</param>
    /// <returns>The greater value.</returns>
    public static float Max(float value1, float value2, float value3)
        => value2 >= value3 ? value1 >= value2 ? value1 : value2 : value1 >= value3 ? value1 : value3;

    /// <summary>
    ///     Sets value to be the greater of two values.
    /// </summary>
    /// <param name="value">The output value.</param>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    public static void Max(ref float value, float value1, float value2)
    {
        value = value1 >= value2 ? value1 : value2;
    }

    /// <summary>
    ///     Sets value to the the greater of three values.
    /// </summary>
    /// <param name="value">The output value.</param>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="value3">Source value.</param>
    public static void Max(ref float value, float value1, float value2, float value3)
    {
        value = value2 >= value3 ? value1 >= value2 ? value1 : value2 : value1 >= value3 ? value1 : value3;
    }

    /// <summary>
    ///     Returns the greater of two values.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <returns>The greater value, or value1 if the values are equal.</returns>
    public static T Max<T>(T value1, T value2) where T : IComparable<T>
        => value1.CompareTo(value2) >= 0 ? value1 : value2;

    /// <summary>
    ///     Returns the greater of three values.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="value3">Source value.</param>
    /// <returns>The greater value, or value1 if the values are equal.</returns>
    public static T Max<T>(T value1, T value2, T value3) where T : IComparable<T>
        => value2.CompareTo(value3) >= 0 ? value1.CompareTo(value2) >= 0 ? value1 : value2 : value1.CompareTo(value3) >= 0 ? value1 : value3;

    /// <summary>
    ///     Returns the lesser of two values.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <returns>The lesser value.</returns>
    public static float Min(float value1, float value2)
        => value1 <= value2 ? value1 : value2;

    /// <summary>
    ///     Returns the lesser of three values.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="value3">Source value.</param>
    /// <returns>The lesser value.</returns>
    public static float Min(float value1, float value2, float value3)
        => value2 <= value3 ? value1 <= value2 ? value1 : value2 : value1 <= value3 ? value1 : value3;

    /// <summary>
    ///     Sets value to be the lesser of two values.
    /// </summary>
    /// <param name="value">The output value.</param>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    public static void Min(ref float value, float value1, float value2)
    {
        value = value1 <= value2 ? value1 : value2;
    }

    /// <summary>
    ///     Sets value to be the lesser of three values.
    /// </summary>
    /// <param name="value">The output value.</param>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="value3">Source value.</param>
    public static void Min(ref float value, float value1, float value2, float value3)
    {
        value = value2 <= value3 ? value1 <= value2 ? value1 : value2 : value1 <= value3 ? value1 : value3;
    }

    /// <summary>
    ///     Returns the lesser of two values.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <returns>The lesser value, or value1 if the values are equal.</returns>
    public static T Min<T>(T value1, T value2) where T : IComparable<T>
        => value1.CompareTo(value2) <= 0 ? value1 : value2;

    /// <summary>
    ///     Returns the lesser of three values.
    /// </summary>
    /// <param name="value1">Source value.</param>
    /// <param name="value2">Source value.</param>
    /// <param name="value3">Source value.</param>
    /// <returns>The lesser value, or value1 if the values are equal.</returns>
    public static T Min<T>(T value1, T value2, T value3) where T : IComparable<T>
        => value2.CompareTo(value3) <= 0 ? value1.CompareTo(value2) <= 0 ? value1 : value2 : value1.CompareTo(value3) <= 0 ? value1 : value3;

    /// <summary>
    ///     Returns the absolute value of a single precision floating point number.
    /// </summary>
    /// <param name="value">Source value.</param>
    /// <returns>The absolute vaue of the source value.</returns>
    public static float Abs(float value)
        => value >= 0 ? value : -value;

    /// <summary>
    ///     Assigns the absolute value of a single precision floating point number.
    /// </summary>
    /// <param name="value">Source value.</param>
    public static void Abs(ref float value)
    {
        value = value >= 0 ? value : -value;
    }

    /// <summary>
    ///     Returns the angle whose cosine is the specified value.
    /// </summary>
    /// <param name="value">A number representing a cosine.</param>
    /// <returns>The angle whose cosine is the specified value.</returns>
    public static float Acos(float value)
        => (float)Math.Acos(value);

    /// <summary>
    ///     Returns the angle whose sine is the specified value.
    /// </summary>
    /// <param name="value">A number representing a sine.</param>
    /// <returns>The angle whose sine is the specified value.</returns>
    public static float Asin(float value)
        => (float)Math.Asin(value);

    /// <summary>
    ///     Returns the angle whos tangent is the speicified number.
    /// </summary>
    /// <param name="value">A number representing a tangent.</param>
    /// <returns>The angle whos tangent is the speicified number.</returns>
    public static float Atan(float value)
        => (float)Math.Atan(value);

    /// <summary>
    ///     Returns the angle whose tangent is the quotient of the two specified numbers.
    /// </summary>
    /// <param name="y">The y coordinate of a point.</param>
    /// <param name="x">The x coordinate of a point.</param>
    /// <returns>The angle whose tangent is the quotient of the two specified numbers.</returns>
    public static float Atan2(float y, float x)
        => (float)Math.Atan2(y, x);

    /// <summary>
    ///     Returns the sine of the specified angle.
    /// </summary>
    /// <param name="value">An angle specified in radians.</param>
    /// <returns>The sine of the specified angle.</returns>
    public static float Sin(float value)
        => (float)Math.Sin(value);

    /// <summary>
    ///     Returns the hyperbolic sine of the specified angle.
    /// </summary>
    /// <param name="value">An angle specified in radians.</param>
    /// <returns>The hyperbolic sine of the specified angle.</returns>
    public static float Sinh(float value)
        => (float)Math.Sinh(value);

    /// <summary>
    ///     Returns the cosine of the specified angle.
    /// </summary>
    /// <param name="value">An angle specified in radians.</param>
    /// <returns>The cosine of the specified angle.</returns>
    public static float Cos(float value)
        => (float)Math.Cos(value);

    /// <summary>
    ///     Returns the hyperbolic cosine of the specified angle.
    /// </summary>
    /// <param name="value">An angle specified in radians.</param>
    /// <returns>The hyperbolic cosine of the specified angle.</returns>
    public static float Cosh(float value)
        => (float)Math.Cosh(value);

    /// <summary>
    ///     Returns the tangent of the specified angle.
    /// </summary>
    /// <param name="value">An angle specified in radians.</param>
    /// <returns>The tangent of the specified angle.</returns>
    public static float Tan(float value)
        => (float)Math.Tan(value);

    /// <summary>
    ///     Returns the hyperbolic tangent of the specified angle.
    /// </summary>
    /// <param name="value">An angle specified in radians.</param>
    /// <returns>The hyperbolic tangent of the specified angle.</returns>
    public static float Tanh(float value)
        => (float)Math.Tanh(value);

    /// <summary>
    ///     Returns the natural (base e) logarithm of the specified value.
    /// </summary>
    /// <param name="value">A number whose logarithm is to be found.</param>
    /// <returns>The natural (base e) logarithm of the specified value.</returns>
    public static float Log(float value)
        => (float)Math.Log(value);

    /// <summary>
    ///     Returns the specified value raised to the specified power.
    /// </summary>
    /// <param name="value">Source value.</param>
    /// <param name="power">A single precision floating point number that specifies a power.</param>
    /// <returns>The specified value raised to the specified power.</returns>
    public static float Pow(float value, float power)
        => (float)Math.Pow(value, power);

    /// <summary>
    ///     Returns the square root of the specified value.
    /// </summary>
    /// <param name="value">Source value.</param>
    /// <returns>The square root of the specified value.</returns>
    public static float Sqrt(float value)
        => (float)Math.Sqrt(value);
}