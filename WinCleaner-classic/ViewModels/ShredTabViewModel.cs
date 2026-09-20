using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCleaner.Design;
using WinCleaner.Services;

namespace WinCleaner.ViewModels;

public partial class ShredTabViewModel : BaseViewModel, INavigableViewModel
{
    private readonly ISecureDeleteService _secureDeleteService;

    public string Title => "Shred";
    public Geometry Icon => Icons.Delete;
    public bool IsSelected { get; set; }

    public ShredTabViewModel(ISecureDeleteService secureDeleteService)
    {
        _secureDeleteService = secureDeleteService;
    }
}