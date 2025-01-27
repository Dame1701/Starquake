using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace ScreenEditor.Views;

public partial class TileSelector : UserControl
{
    public static readonly StyledProperty<Bitmap?> TileSetProperty =
        AvaloniaProperty.Register<TileSelector, Bitmap?>(nameof(TileSet), defaultValue: null);

    public Bitmap? TileSet
    {
        get => GetValue(TileSetProperty);
        set => SetValue(TileSetProperty, value);
    }

    public TileSelector()
    {
        InitializeComponent();
        
        TileSetProperty.Changed
            .AddClassHandler<TileSelector, Bitmap?>((x, e)
                => x.OnTileSetPropertyChanged(e));
    }

    private void OnTileSetPropertyChanged(AvaloniaPropertyChangedEventArgs<Bitmap?> e)
    {
        //var oldValue = e.OldValue?.ToString();
        //var newValue = e.NewValue?.ToString();

        TileCanvas.TileSet = e.NewValue.GetValueOrDefault();
    }
}