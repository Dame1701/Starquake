using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ScreenEditor.Views;

namespace ScreenEditor.ViewModels;

using ReactiveUI;

public class MainWindowViewModel : ViewModelBase
{
    private Window _mainWindow;
    
    public ICommand NewMapDataCommand { get; }

    public ICommand LoadMapDataAsyncCommand { get; }
    public ICommand SaveMapDataAsyncCommand { get; }
    
    public ICommand LoadTileSetAsyncCommand { get; }
    
    public ICommand ShowGridToggleCommand { get; }
    
    public MainWindowViewModel()
    {
        NewMapDataCommand = ReactiveCommand.Create(NewMapDataAction);
        
        LoadMapDataAsyncCommand = ReactiveCommand.CreateFromTask(LoadMapDataAsyncAction);
        
        SaveMapDataAsyncCommand = ReactiveCommand.CreateFromTask(SaveMapDataAsyncAction);
        
        LoadTileSetAsyncCommand = ReactiveCommand.CreateFromTask(LoadTileSetAsyncAction);
        
        ShowGridToggleCommand = ReactiveCommand.Create<bool>(ShowGridToggleAction);
    }

    private void NewMapDataAction()
    {
        
    }

    private async Task LoadMapDataAsyncAction()
    {
        
    }
    
    private async Task SaveMapDataAsyncAction()
    {
        
    }
    
    private async Task LoadTileSetAsyncAction()
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var applicationLifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (applicationLifetime?.MainWindow is null)
        {
            throw new Exception("Main Window is null");
        }
        
        // Start async operation to open the dialog.
        var files = await applicationLifetime.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PNG File",
            AllowMultiple = false, 
            FileTypeFilter = [FilePickerFileTypes.ImagePng]
        });

        if (files.Count >= 1)
        {
            var tiles = new Avalonia.Media.Imaging.Bitmap(files[0].Path.ToString());
            
        }
    }
    
    private void ShowGridToggleAction(bool isChecked)
    {
        
    }
}

/*
  private void loadTileSetToolStripMenuItem_Click(object sender, EventArgs e)
          {
              if (String.IsNullOrEmpty(tileSetFileName))
              {
                  using (OpenFileDialog ofd = new OpenFileDialog())
                  {
                      ofd.Filter = "PNG Graphics Files (.png)|*.png";
                      ofd.Multiselect = false;
                      ofd.ShowDialog();

                      if (!String.IsNullOrEmpty(ofd.FileName))
                      {
                          LoadTileSet(ofd.FileName);
                          tileSetFileName = ofd.FileName;
                      }
                  }
              }
              else
              {
                  LoadTileSet(tileSetFileName);
              }
          }

          private void LoadTileSet(string FileName)
          {
              // Load in the tileset
              using (Stream s = new FileStream(FileName, FileMode.Open, FileAccess.Read))
              {
                  using (Bitmap b = (Bitmap)Bitmap.FromStream(s))
                  {
                      tileSet = (Bitmap)b.Clone();

                      if (tileSet != null)
                      {
                          // update the picturebox
                          pbxTileSet.BackgroundImageLayout = ImageLayout.Stretch;
                          pbxTileSet.BackgroundImage = tileSet;

                          // update the editcanvas
                          editCanvas1.TileSet = tileSet;
                          tileSelectedDisplay1.TileSet = tileSet;

                          // Calculate tile count
                          tileCount = (tileSet.Width / tileSize.Width) * (tileSet.Height / tileSize.Height);
                         
                          if (screenCollisionDataArray !=null)
                          {
                              int CollisionMapsToAdd = (tileSize.Width / 32) * (tileSize.Height / 32) * ((tileCount - 1) - screenCollisionDataArray.GetUpperBound(0));
                          }
                      }
                  }
              }
          }
*/