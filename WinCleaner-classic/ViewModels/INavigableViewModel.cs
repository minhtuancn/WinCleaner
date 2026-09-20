using System.Windows.Media;

namespace WinCleaner.ViewModels;

public interface INavigableViewModel
{
    string Title { get; }
    Geometry Icon { get; }
    bool IsSelected { get; set; }
}