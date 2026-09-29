// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace ScreenEditor.Views;

public partial class TileSelector : UserControl
{
    public static readonly StyledProperty<Bitmap?> TileSetProperty =
        AvaloniaProperty.Register<TileSelector, Bitmap?>(nameof(TileSet), null);

    public TileSelector()
    {
        InitializeComponent();

        TileSetProperty.Changed
            .AddClassHandler<TileSelector, Bitmap?>((x, e)
                => x.OnTileSetPropertyChanged(e));
    }

    public Bitmap? TileSet
    {
        get => GetValue(TileSetProperty);
        set => SetValue(TileSetProperty, value);
    }

    private void OnTileSetPropertyChanged(AvaloniaPropertyChangedEventArgs<Bitmap?> e)
    {
        TileCanvas.TileSet = e.NewValue.GetValueOrDefault();
    }
}