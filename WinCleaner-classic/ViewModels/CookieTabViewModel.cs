using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCleaner.Design;
using WinCleaner.Services;

namespace WinCleaner.ViewModels;

public partial class CookieTabViewModel : BaseViewModel, INavigableViewModel
{
    private readonly ICookieService _cookieService;

    public string Title => "Cookie";
    public Geometry Icon => Icons.Browser;
    public bool IsSelected { get; set; }

    public CookieTabViewModel(ICookieService cookieService)
    {
        _cookieService = cookieService;
    }
}