using System.Reflection;
using System.Windows.Media;
using Xunit;

namespace WinCleaner.Tests.Unit;

[Collection("WpfTests")]
public class ToolsViewModelTests
{
    private readonly WpfTestFixture _fixture;
    private readonly Assembly _classicAssembly;
    private readonly Type _toolsViewModelType;
    private readonly Type _iNavigableViewModelType;
    private readonly Type _schedulerTabVmType;
    private readonly Type _cookieTabVmType;
    private readonly Type _shredTabVmType;
    private readonly Type _appXTabVmType;

    public ToolsViewModelTests(WpfTestFixture fixture)
    {
        _fixture = fixture;
        _classicAssembly = Assembly.Load("WinCleaner");
        _toolsViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.ToolsViewModel")!;
        _iNavigableViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.INavigableViewModel")!;
        _schedulerTabVmType = _classicAssembly.GetType("WinCleaner.ViewModels.SchedulerTabViewModel")!;
        _cookieTabVmType = _classicAssembly.GetType("WinCleaner.ViewModels.CookieTabViewModel")!;
        _shredTabVmType = _classicAssembly.GetType("WinCleaner.ViewModels.ShredTabViewModel")!;
        _appXTabVmType = _classicAssembly.GetType("WinCleaner.ViewModels.AppXTabViewModel")!;
    }

    [Fact]
    public void ToolsViewModel_Should_Implement_INavigableViewModel()
    {
        Assert.True(_iNavigableViewModelType.IsAssignableFrom(_toolsViewModelType));
    }

    [Fact]
    public void ToolsViewModel_Should_Have_Title_And_Icon()
    {
        Assert.NotNull(_toolsViewModelType.GetProperty("Title"));
        Assert.NotNull(_toolsViewModelType.GetProperty("Icon"));
    }

    [Fact]
    public void ToolsViewModel_Should_Have_Four_Child_ViewModels()
    {
        // Verify ToolsViewModel exposes four child VMs as properties
        Assert.NotNull(_toolsViewModelType.GetProperty("SchedulerVM"));
        Assert.NotNull(_toolsViewModelType.GetProperty("CookieVM"));
        Assert.NotNull(_toolsViewModelType.GetProperty("ShredVM"));
        Assert.NotNull(_toolsViewModelType.GetProperty("AppXVM"));
    }

    [Fact]
    public void ChildViewModels_Should_Implement_INavigableViewModel()
    {
        Assert.True(_iNavigableViewModelType.IsAssignableFrom(_schedulerTabVmType));
        Assert.True(_iNavigableViewModelType.IsAssignableFrom(_cookieTabVmType));
        Assert.True(_iNavigableViewModelType.IsAssignableFrom(_shredTabVmType));
        Assert.True(_iNavigableViewModelType.IsAssignableFrom(_appXTabVmType));
    }

    [Fact]
    public void SchedulerTabViewModel_Should_Have_Correct_Title_And_Icon()
    {
        var titleProperty = _schedulerTabVmType.GetProperty("Title");
        var iconProperty = _schedulerTabVmType.GetProperty("Icon");
        
        Assert.NotNull(titleProperty);
        Assert.NotNull(iconProperty);
        Assert.Equal(typeof(string), titleProperty.PropertyType);
        Assert.Equal(typeof(Geometry), iconProperty.PropertyType);
    }

    [Fact]
    public void CookieTabViewModel_Should_Have_Correct_Title_And_Icon()
    {
        var titleProperty = _cookieTabVmType.GetProperty("Title");
        var iconProperty = _cookieTabVmType.GetProperty("Icon");
        
        Assert.NotNull(titleProperty);
        Assert.NotNull(iconProperty);
        Assert.Equal(typeof(string), titleProperty.PropertyType);
        Assert.Equal(typeof(Geometry), iconProperty.PropertyType);
    }

    [Fact]
    public void ShredTabViewModel_Should_Have_Correct_Title_And_Icon()
    {
        var titleProperty = _shredTabVmType.GetProperty("Title");
        var iconProperty = _shredTabVmType.GetProperty("Icon");
        
        Assert.NotNull(titleProperty);
        Assert.NotNull(iconProperty);
        Assert.Equal(typeof(string), titleProperty.PropertyType);
        Assert.Equal(typeof(Geometry), iconProperty.PropertyType);
    }

    [Fact]
    public void AppXTabViewModel_Should_Have_Correct_Title_And_Icon()
    {
        var titleProperty = _appXTabVmType.GetProperty("Title");
        var iconProperty = _appXTabVmType.GetProperty("Icon");
        
        Assert.NotNull(titleProperty);
        Assert.NotNull(iconProperty);
        Assert.Equal(typeof(string), titleProperty.PropertyType);
        Assert.Equal(typeof(Geometry), iconProperty.PropertyType);
    }
}