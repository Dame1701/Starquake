// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Starquake.ScreenEditor.Converters;

/// <summary>
///     Binds a group of toggles to a single enum property: each toggle is checked when the property
///     equals its parameter, and checking one sets the property to it.
/// </summary>
/// <remarks>
///     Unchecking is ignored on purpose. The toggles are one choice out of several, so the only thing
///     that unchecks one is another being checked, and acting on that would undo the change that
///     caused it.
/// </remarks>
internal sealed class EnumMatchConverter : IValueConverter
{

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && parameter is not null && value.ToString() == parameter.ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true || parameter is null)
        {
            return BindingOperations.DoNothing;
        }

        Type enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return Enum.Parse(enumType, parameter.ToString()!);
    }
}
