// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Starquake.ScreenEditor.Content;
using Starquake.ScreenEditor.Dialogs;
using Starquake.ScreenEditor.Editing;

namespace Starquake.ScreenEditor.ViewModels;

/// <summary>
///     The window's commands and the state that only the window cares about. The editing state
///     itself lives in <see cref="EditorSession" />, which the drawn surfaces share.
/// </summary>
internal class MainWindowViewModel : ViewModelBase
{

    private readonly List<RelayCommand> _commands = [];

    private string _lastError = string.Empty;

    private Window? _owner;

    private TileSheetChoice? _selectedTileSheet;

    public MainWindowViewModel()
    {
        NewMapCommand = Register(new RelayCommand(NewMapAsync));
        OpenMapCommand = Register(new RelayCommand(OpenMapAsync));
        SaveMapCommand = Register(new RelayCommand(SaveMapAsync));
        SaveMapAsCommand = Register(new RelayCommand(SaveMapAsAsync));
        OpenTileSheetCommand = Register(new RelayCommand(OpenTileSheetAsync));
        UseTileSheetCommand = Register(new RelayCommand(parameter => UseTileSheet(parameter as string)));
        ExitCommand = Register(new RelayCommand(() => _owner?.Close()));
        ShowShortcutsCommand = Register(new RelayCommand(ShowShortcutsAsync));

        UndoCommand = Register(new RelayCommand(
            () => Session.Undo.Undo(Session.Document),
            () => Session.Undo.CanUndo));

        RedoCommand = Register(new RelayCommand(
            () => Session.Undo.Redo(Session.Document),
            () => Session.Undo.CanRedo));

        CopyScreenCommand = Register(new RelayCommand(Session.CopyScreen));
        PasteScreenCommand = Register(new RelayCommand(Session.PasteScreen, () => Session.CanPaste));
        ClearScreenCommand = Register(new RelayCommand(Session.ClearScreen));
        FillScreenCommand = Register(new RelayCommand(Session.FloodScreen));

        PreviousScreenCommand = Register(new RelayCommand(() => Session.CurrentScreen--));
        NextScreenCommand = Register(new RelayCommand(() => Session.CurrentScreen++));

        Session.Invalidated += (_, _) => OnSessionChanged();
        Session.Undo.Changed += (_, _) => OnSessionChanged();
        Session.PropertyChanged += (_, _) => OnSessionChanged();
    }

    public EditorSession Session { get; } = new();

    public RelayCommand NewMapCommand { get; }

    public RelayCommand OpenMapCommand { get; }

    public RelayCommand SaveMapCommand { get; }

    public RelayCommand SaveMapAsCommand { get; }

    public RelayCommand OpenTileSheetCommand { get; }

    public RelayCommand UseTileSheetCommand { get; }

    public RelayCommand ExitCommand { get; }

    public RelayCommand ShowShortcutsCommand { get; }

    public RelayCommand UndoCommand { get; }

    public RelayCommand RedoCommand { get; }

    public RelayCommand CopyScreenCommand { get; }

    public RelayCommand PasteScreenCommand { get; }

    public RelayCommand ClearScreenCommand { get; }

    public RelayCommand FillScreenCommand { get; }

    public RelayCommand PreviousScreenCommand { get; }

    public RelayCommand NextScreenCommand { get; }

    /// <summary>
    ///     The tile sheets found in the checkout, offered on the File menu so that switching between
    ///     the redrawn artwork and the Spectrum original is two clicks rather than a file dialog.
    /// </summary>
    public IReadOnlyList<TileSheetChoice> AvailableTileSheets { get; } =
        GameContent.TileSheetPaths
            .Select(path => new TileSheetChoice(Path.GetFileNameWithoutExtension(path), path))
            .ToArray();

    /// <summary>
    ///     Which of the found sheets the map is being drawn with. Setting it loads that sheet, so the
    ///     toolbar's picker needs no command of its own.
    /// </summary>
    public TileSheetChoice? SelectedTileSheet
    {
        get => _selectedTileSheet;
        set
        {
            if (ReferenceEquals(value, _selectedTileSheet))
            {
                return;
            }

            SetField(ref _selectedTileSheet, value);

            if (value is not null)
            {
                UseTileSheet(value.Path);
            }
        }
    }

    public string Title
    {
        get
        {
            string name = Session.Document.FilePath is null
                ? "Untitled map"
                : Path.GetFileName(Session.Document.FilePath);

            return "Starquake Screen Editor  —  " + name + (Session.Document.IsDirty ? " •" : string.Empty);
        }
    }

    public string UndoDescription => Session.Undo.UndoDescription;

    public string RedoDescription => Session.Undo.RedoDescription;

    /// <summary>
    ///     Where the map came from, or what went wrong trying to open it. Shown in the status bar,
    ///     which is the right amount of noise for something that is usually just reassurance.
    /// </summary>
    public string DocumentStatus
    {
        get
        {
            if (_lastError.Length > 0)
            {
                return _lastError;
            }

            return Session.Document.FilePath is null
                ? "No map file"
                : Session.Document.FilePath +
                  (Session.Document.IsDirty ? "  ·  unsaved changes" : "  ·  saved");
        }
    }

    private static FilePickerFileType ScreenDataFileType => new("Starquake screen data")
    {
        Patterns = ["*.dat"]
    };

    /// <summary>
    ///     Opens whatever the checkout has to offer, so that the editor comes up ready to work rather
    ///     than empty. Anything missing is simply not loaded; the File menu covers that case.
    /// </summary>
    public void LoadStartingContent()
    {
        SelectedTileSheet = AvailableTileSheets.FirstOrDefault();

        string? mapPath = GameContent.MapPath;
        if (mapPath is not null)
        {
            LoadMap(mapPath);
        }
    }

    public void AttachOwner(Window owner) => _owner = owner;

    /// <summary>
    ///     Asks about unsaved work before the window goes away. Returns whether closing should go
    ///     ahead.
    /// </summary>
    public async Task<bool> ConfirmCloseAsync()
    {
        if (!Session.Document.IsDirty || _owner is null)
        {
            return true;
        }

        ConfirmChoice choice = await ConfirmDialog.AskAsync(
            _owner,
            "This map has changes that have not been saved. Save them before closing?");

        return choice switch
        {
            ConfirmChoice.Save => await SaveMapAsync(),
            ConfirmChoice.Discard => true,
            _ => false
        };
    }

    private async Task ShowShortcutsAsync()
    {
        if (_owner is not null)
        {
            await new ShortcutsDialog().ShowDialog(_owner);
        }
    }

    private RelayCommand Register(RelayCommand command)
    {
        _commands.Add(command);

        return command;
    }

    private void OnSessionChanged()
    {
        foreach (RelayCommand command in _commands)
        {
            command.RaiseCanExecuteChanged();
        }

        RaisePropertyChanged(nameof(Title));
        RaisePropertyChanged(nameof(DocumentStatus));
        RaisePropertyChanged(nameof(UndoDescription));
        RaisePropertyChanged(nameof(RedoDescription));
    }

    private async Task NewMapAsync()
    {
        if (!await ConfirmCloseAsync())
        {
            return;
        }

        _lastError = string.Empty;
        Session.SetDocument(EditorDocument.CreateEmpty());
    }

    private async Task OpenMapAsync()
    {
        if (!await ConfirmCloseAsync() || _owner is null)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open screen data",
            AllowMultiple = false,
            FileTypeFilter = [ScreenDataFileType]
        });

        string? path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (path is not null)
        {
            LoadMap(path);
        }
    }

    private async Task<bool> SaveMapAsync()
    {
        string? path = Session.Document.FilePath;

        return path is null ? await SaveMapAsAsync() : Save(path);
    }

    private async Task<bool> SaveMapAsAsync()
    {
        if (_owner is null)
        {
            return false;
        }

        IStorageFile? file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save screen data",
            SuggestedFileName = Path.GetFileName(Session.Document.FilePath) ?? ScreenDataFile.DefaultFileName,
            DefaultExtension = "dat",
            FileTypeChoices = [ScreenDataFileType]
        });

        string? path = file?.TryGetLocalPath();

        return path is not null && Save(path);
    }

    private bool Save(string path)
    {
        try
        {
            ScreenDataFile.Save(Session.Document, path);
            _lastError = string.Empty;
        }
        catch (Exception exception)
        {
            _lastError = "Could not save: " + exception.Message;
        }

        OnSessionChanged();

        return _lastError.Length == 0;
    }

    private void LoadMap(string path)
    {
        try
        {
            Session.SetDocument(ScreenDataFile.Load(path));
            _lastError = string.Empty;
        }
        catch (Exception exception)
        {
            _lastError = "Could not open " + Path.GetFileName(path) + ": " + exception.Message;
        }

        OnSessionChanged();
    }

    private async Task OpenTileSheetAsync()
    {
        if (_owner is null)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open tile sheet",
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImagePng]
        });

        string? path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (path is not null)
        {
            UseTileSheet(path);
        }
    }

    private void UseTileSheet(string? path)
    {
        if (path is null)
        {
            return;
        }

        try
        {
            Session.SetSheet(TileSheet.Load(path));
            _lastError = string.Empty;
        }
        catch (Exception exception)
        {
            _lastError = "Could not open " + Path.GetFileName(path) + ": " + exception.Message;
        }

        OnSessionChanged();
    }
}

/// <summary>
///     A tile sheet the editor found for itself, ready to be put on a menu.
/// </summary>
internal sealed record TileSheetChoice(string Name, string Path);
