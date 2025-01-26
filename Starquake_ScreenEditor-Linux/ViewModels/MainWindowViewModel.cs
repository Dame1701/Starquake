using System.Threading.Tasks;
using System.Windows.Input;

namespace ScreenEditor.ViewModels;

using ReactiveUI;

public class MainWindowViewModel : ViewModelBase
{
    public ICommand NewMapDataCommand { get; }

    public ICommand LoadMapDataAsyncCommand { get; }
    public ICommand SaveMapDataAsyncCommand { get; }
    
    public ICommand ShowGridToggleCommand { get; }
    
    public MainWindowViewModel()
    {
        NewMapDataCommand = ReactiveCommand.Create(NewMapDataAction);
        
        LoadMapDataAsyncCommand = ReactiveCommand.CreateFromTask(LoadMapDataAsyncAction);
        
        SaveMapDataAsyncCommand = ReactiveCommand.CreateFromTask(SaveMapDataAsyncAction);
        
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
    
    private void ShowGridToggleAction(bool isChecked)
    {
        
    }
}