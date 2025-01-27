using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ScreenEditor.Views;

public partial class TileCanvas : UserControl
{
    private Point _mouseCoords;

    public int SelectedTile { get; set; }

    private Bitmap? _tileSet;

    public Bitmap? TileSet
    {
        get => _tileSet;
        set
        {
            if (value == null)
            {
                return;
            }

            _tileSet = value;
            _tileCount = (int)(_tileSet.Size.Width / TileSize.Width * (_tileSet.Size.Height / TileSize.Height));
            SelectedTile = 0;
            InvalidateVisual();
        }
    }

    public Size TileSize { get; set; }

    private int _scrollValue;

    public int ScrollValue
    {
        get => _scrollValue;
        set
        {
            _scrollValue = value;
            InvalidateVisual();
        }
    }

    private bool _showGrid;

    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            _showGrid = value;
            InvalidateVisual();
        }
    }

    public int TilesDown { get; set; }

    private int _tileCount;

    public TileCanvas()
    {
        InitializeComponent();

        TileSize = new Size(128, 96);
    }

    private int _tilesToDrawAcross;
    private int _tilesToDrawDown;
    private bool _mouseOnCanvas;

    public override void Render(DrawingContext context)
    {
        HighlightSelectedTile(context);
        DrawTiles(context);
        DrawGrid(context);

        base.Render(context);
    }
    
    private void InputElement_OnPointerEntered(object? Sender, PointerEventArgs E)
    {
        _mouseOnCanvas = false;
        InvalidateVisual();
    }

    private void InputElement_OnPointerMoved(object? Sender, PointerEventArgs E)
    {
        _mouseCoords = E.GetPosition(this);
        InvalidateVisual();

        base.OnPointerMoved(E);
    }

    private void InputElement_OnPointerPressed(object? Sender, PointerPressedEventArgs E)
    {
        var point = E.GetCurrentPoint(Sender as Control);

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var tileIndexAcross = (int)(point.Position.X / TileSize.Width);
        var tileIndexDown = (int)(point.Position.Y / TileSize.Height);

        SelectedTile = (tileIndexDown + ScrollValue) * _tilesToDrawAcross + tileIndexAcross;

        //if (OnTileChange != null)
        //{
        //    OnTileChange(selectedTile);
        //}
    }

    private void InputElement_OnPointerExited(object? Sender, PointerEventArgs E)
    {
        _mouseOnCanvas = true;
        InvalidateVisual();
    }

    private void Control_OnSizeChanged(object? Sender, SizeChangedEventArgs E)
    {
        InvalidateVisual();
    }
    
    private void HighlightSelectedTile(DrawingContext context)
    {
        if (!_mouseOnCanvas)
        {
            return;
        }
        
        if (_mouseCoords.X >= 0 && _mouseCoords.X <= Bounds.Size.Width &&
            _mouseCoords.Y >= 0 && _mouseCoords.Y <= Bounds.Size.Height)
        {
            context.FillRectangle(new SolidColorBrush(Colors.White),
                new Rect(
                    (int)(_mouseCoords.X / TileSize.Width) * TileSize.Width,
                    (int)(_mouseCoords.Y / TileSize.Height) * TileSize.Height,
                    TileSize.Width,
                    TileSize.Height));
        }
    }

    private void DrawTiles(DrawingContext context)
    {
        _tilesToDrawAcross = (int)(Bounds.Size.Width / TileSize.Width);
        _tilesToDrawDown = (int)(Bounds.Size.Height / TileSize.Height);

        TilesDown = _tileCount / _tilesToDrawAcross;

        if (TileSet != null)
        {
            var tileCounter = ScrollValue * _tilesToDrawAcross;

            for (var y = 0; y < _tilesToDrawDown; y++)
            {
                for (var x = 0; x < _tilesToDrawAcross; x++)
                {
                    if (tileCounter < _tileCount)
                    {
                        if (SelectedTile == tileCounter)
                        {
                            context.FillRectangle(
                                new SolidColorBrush(Colors.White),
                                new Rect(
                                    x * TileSize.Width,
                                    y * TileSize.Height,
                                    TileSize.Width,
                                    TileSize.Height));
                        }

                        var drawPosition = GetCoordinatesForTile(tileCounter);

                        context.DrawImage(TileSet,
                            new Rect(
                                x * TileSize.Width,
                                y * TileSize.Height,
                                TileSize.Width,
                                TileSize.Height),
                            new Rect(
                                drawPosition.X,
                                drawPosition.Y,
                                TileSize.Width,
                                TileSize.Height));

                        tileCounter++;
                    }
                }
            }
        }
    }

    private void DrawGrid(DrawingContext context)
    {
        for (var y = 0; y < _tilesToDrawDown + 1; y++)
        {
            context.DrawLine(
                new Pen(new SolidColorBrush(Colors.White)),
                new Point(0, y * TileSize.Height),
                new Point(Bounds.Size.Width, y * TileSize.Height));
        }

        for (var x = 0; x < _tilesToDrawAcross + 1; x++)
        {
            context.DrawLine(
                new Pen(new SolidColorBrush(Colors.White)),
                new Point(x * TileSize.Width, 0),
                new Point(x * TileSize.Width, Bounds.Size.Height));
        }
    }
    
    private Point GetCoordinatesForTile(int tileNumber)
    {
        if (TileSet == null)
        {
            return new Point(0, 0);
        }

        var tilesAcross = (int)(TileSet.Size.Width / TileSize.Width);

        var rowsDown = tileNumber / tilesAcross;
        var rowsAcross = tileNumber - rowsDown * tilesAcross;

        return new Point(rowsAcross * TileSize.Width, rowsDown * TileSize.Height);
    }
}

/*
 * public event RaiseTileChange OnTileChange;
       private Point mouseCoords;

       private int selectedTile;
       public int SelectedTile
       {
           get { return selectedTile; }
           set { selectedTile = value; }
       }

       Bitmap tileSet;
       public Bitmap TileSet
       {
           get { return tileSet; }
           set
           {
               if (value != null)
               {
                   tileSet = value;
                   tileCount = (tileSet.Width / tileSize.Width) * (tileSet.Height / tileSize.Height);
                   SelectedTile = 0;
               }

           }
       }

       Size tileSize;
       public Size TileSize
       {
           get { return tileSize; }
           set
           {
               tileSize = value;
           }
       }

       int scrollValue;
       public int ScrollValue
       {
           get { return scrollValue; }
           set
           {
               scrollValue = value;
               Invalidate();
           }
       }

       bool showGrid;
       public bool ShowGrid
       {
           get { return showGrid; }
           set
           {
               showGrid = value;
               Invalidate();
           }
       }

       private int tilesDown;
       public int TilesDown
       {
           get { return tilesDown; }
           set
           {
               tilesDown = value;
           }
       }

       int tileCount;
       public TileCanvas()
       {
           InitializeComponent();
           DoubleBuffered = true;
           BackColor = Color.Black;
       }

       int tilesToDrawAcross;
       int tilesToDrawDown;
       bool mouseLeft = true;

       private Point GetCoordinatesForTile(int TileNumber)
       {
           int tilesAcross = tileSet.Width / tileSize.Width;
           int tilesDown = tileSet.Height / tileSize.Height;

           int rowsDown = TileNumber / tilesAcross;
           int rowsAcross = TileNumber - (rowsDown * tilesAcross);

           return new Point(rowsAcross * tileSize.Width, rowsDown * tileSize.Height);
       }

       protected override void OnPaint(PaintEventArgs e)
       {
           if (!mouseLeft)
           {
               if (mouseCoords.X >= 0 && mouseCoords.X <= Width &&
                             mouseCoords.Y >= 0 && mouseCoords.Y <= Height)
               {
                   using (Brush b = new SolidBrush(Color.FromArgb(255, 255, 255, 0)))
                   {
                       e.Graphics.FillRectangle(b, new Rectangle((mouseCoords.X / tileSize.Width) * tileSize.Width, (mouseCoords.Y / tileSize.Height) * tileSize.Height, tileSize.Width, tileSize.Height));
                   }
               }
           }

           // Draw tiles
           tilesToDrawAcross = this.Width / tileSize.Width;
           tilesToDrawDown = this.Height / tileSize.Height;

           tilesDown = tileCount / tilesToDrawAcross;

           int tileCounter = 0;
           tileCounter = ScrollValue * tilesToDrawAcross;

           for (int y = 0; y < tilesToDrawDown; y++)
           {
               for (int x = 0; x < tilesToDrawAcross; x++)
               {
                   if (tileCounter < tileCount)
                   {

                       if (selectedTile == tileCounter)
                       {
                           using (Brush b = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
                           {
                               e.Graphics.FillRectangle(b, new Rectangle(x * tileSize.Width, y * tileSize.Height, tileSize.Width, tileSize.Height));
                           }

                       }

                       Point drawPosition = GetCoordinatesForTile(tileCounter);
                       e.Graphics.DrawImage(TileSet,
                                   new Rectangle(x * tileSize.Width, y * tileSize.Height, tileSize.Width, tileSize.Height),
                                   drawPosition.X,
                                   drawPosition.Y,
                                   tileSize.Width,
                                   tileSize.Height,
                                   GraphicsUnit.Pixel);


                       tileCounter++;
                       }
               }
           }


           // Draw Gridlines
           for (int y = 0; y < tilesToDrawDown + 1; y++)
           {
               e.Graphics.DrawLine(Pens.White, new Point(0, y * tileSize.Height), new Point(Width, y * tileSize.Height));
           }

           for (int x = 0; x < tilesToDrawAcross + 1; x++)
           {
               e.Graphics.DrawLine(Pens.White, new Point(x * tileSize.Width, 0), new Point(x * tileSize.Width, Height));
           }


           base.OnPaint(e);
       }

       private void TileCanvas_Resize(object sender, EventArgs e)
       {
           Invalidate();
       }

       private void TileCanvas_MouseMove(object sender, MouseEventArgs e)
       {
           mouseCoords = e.Location;
           Invalidate();
       }

       private void TileCanvas_MouseClick(object sender, MouseEventArgs e)
       {
           if (e.Button == MouseButtons.Left)
           {
               int tileIndexAcross = e.Location.X / tileSize.Width;
               int tileIndexDown = e.Location.Y / tileSize.Height;

               selectedTile = ((tileIndexDown + scrollValue) * tilesToDrawAcross) + tileIndexAcross;

               if (OnTileChange != null)
               {
                   OnTileChange(selectedTile);
               }
           }
       }

       private void TileCanvas_MouseEnter(object sender, EventArgs e)
       {
           mouseLeft = false;
           Invalidate();
       }

       private void TileCanvas_MouseLeave(object sender, EventArgs e)
       {
           mouseLeft = true;
           Invalidate();
       }

*/