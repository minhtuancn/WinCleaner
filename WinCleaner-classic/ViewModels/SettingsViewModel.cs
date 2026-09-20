using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using WinCleaner.Design;

namespace WinCleaner.ViewModels;

public partial class SettingsViewModel : BaseViewModel, INavigableViewModel
{
    public string Title => "Settings";
    public Geometry Icon => Icons.Settings;
    public bool IsSelected { get; set; }

    public SettingsViewModel()
    {
    }
}