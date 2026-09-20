using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCleaner.Design;
using WinCleaner.Services;

namespace WinCleaner.ViewModels;

public partial class AppXTabViewModel : BaseViewModel, INavigableViewModel
{
    private readonly IAppxService _appxService;

    public string Title => "AppX";
    public Geometry Icon => Icons.Package;
    public bool IsSelected { get; set; }

    public AppXTabViewModel(IAppxService appxService)
    {
        _appxService = appxService;
    }
}