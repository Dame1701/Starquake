using Avalonia.Media.Imaging;

namespace ScreenEditor.ViewModels;

public class TileSelectorViewModel
{
    public Bitmap? TileSet
    {
        get; set;
    }
    
    //public event RaiseTileChange OnTileChange;

    //public int SelectedTile
    //{
    //    get { return tileCanvas1.SelectedTile; }
    //}

   // public TileSelectorViewModel()
    //{
        //();
    //}

    /*
    private void btnClose_Click(object sender, EventArgs e)
    {
        this.Hide();
    }

    private void tileCanvas1_Resize(object sender, EventArgs e)
    {
        TileScroller.Maximum = tileCanvas1.TilesDown;
    }

    private void TileScroller_Scroll(object sender, ScrollEventArgs e)
    {
        tileCanvas1.ScrollValue = e.NewValue;
    }

    private void tileCanvas1_OnTileChange(int TileNumber)
    {
        if (OnTileChange != null)
        {
            OnTileChange(tileCanvas1.SelectedTile);
        }
    }*/
}

