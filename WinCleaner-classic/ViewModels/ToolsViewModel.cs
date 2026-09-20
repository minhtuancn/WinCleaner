using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using WinCleaner.Design;

namespace WinCleaner.ViewModels;

public partial class ToolsViewModel : BaseViewModel, INavigableViewModel
{
    public string Title => "Tools";
    public Geometry Icon => Icons.Folder;
    public bool IsSelected { get; set; }

    public ToolsViewModel()
    {
    }
}