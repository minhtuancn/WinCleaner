using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCleaner.Design;

namespace WinCleaner.ViewModels;

public partial class ToolsViewModel : BaseViewModel, INavigableViewModel
{
    public string Title => "Tools";
    public Geometry Icon => Icons.Folder;
    public bool IsSelected { get; set; }

    public SchedulerTabViewModel SchedulerVM { get; }
    public CookieTabViewModel CookieVM { get; }
    public ShredTabViewModel ShredVM { get; }
    public AppXTabViewModel AppXVM { get; }

    public ObservableCollection<INavigableViewModel> Tabs { get; }

    public ToolsViewModel(
        SchedulerTabViewModel schedulerVM,
        CookieTabViewModel cookieVM,
        ShredTabViewModel shredVM,
        AppXTabViewModel appXVM)
    {
        SchedulerVM = schedulerVM;
        CookieVM = cookieVM;
        ShredVM = shredVM;
        AppXVM = appXVM;

        Tabs = new ObservableCollection<INavigableViewModel>
        {
            SchedulerVM,
            CookieVM,
            ShredVM,
            AppXVM
        };
    }
}