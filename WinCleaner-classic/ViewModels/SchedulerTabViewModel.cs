using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCleaner.Design;
using WinCleaner.Services;

namespace WinCleaner.ViewModels;

public partial class SchedulerTabViewModel : BaseViewModel, INavigableViewModel
{
    private readonly ITaskSchedulerService _taskSchedulerService;

    public string Title => "Scheduler";
    public Geometry Icon => Icons.Clock;
    public bool IsSelected { get; set; }

    public SchedulerTabViewModel(ITaskSchedulerService taskSchedulerService)
    {
        _taskSchedulerService = taskSchedulerService;
    }
}