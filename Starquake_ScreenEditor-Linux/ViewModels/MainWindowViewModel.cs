using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ScreenEditor.ViewModels;

using ReactiveUI;

public class MainWindowViewModel : ViewModelBase
{
    public ICommand NewMapDataCommand { get; }

    public ICommand SaveMapDataAsyncCommand { get; }
    
    public MainWindowViewModel()
    {
        NewMapDataCommand = ReactiveCommand.Create(NewMapDataAction);

        SaveMapDataAsyncCommand = ReactiveCommand.CreateFromTask(SaveMapDataAsyncAction);
    }

    private void NewMapDataAction()
    {
        
    }

    private async Task SaveMapDataAsyncAction()
    {
        
    }
}