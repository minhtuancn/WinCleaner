using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using WinCleaner.Design;

namespace WinCleaner.ViewModels;

public partial class CleanerViewModel : BaseViewModel, INavigableViewModel
{
    public string Title => "Cleaner";
    public Geometry Icon => Icons.Broom;
    public bool IsSelected { get; set; }

    public CleanerViewModel()
    {
    }
}