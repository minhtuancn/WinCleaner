using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Media;
using Xunit;

namespace WinCleaner.Tests.Unit;

[Collection("WpfTests")]
public class ShellViewModelTests
{
    private readonly WpfTestFixture _fixture;
    private readonly Assembly _classicAssembly;
    private readonly Type _shellViewModelType;
    private readonly Type _navItemType;
    private readonly Type _iNavigableViewModelType;
    private readonly Type _mainViewModelType;
    private readonly Type _cleanerViewModelType;
    private readonly Type _toolsViewModelType;
    private readonly Type _settingsViewModelType;

    public ShellViewModelTests(WpfTestFixture fixture)
    {
        _fixture = fixture;
        _classicAssembly = Assembly.Load("WinCleaner");
        _shellViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.ShellViewModel")!;
        _navItemType = _classicAssembly.GetType("WinCleaner.ViewModels.NavItem")!;
        _iNavigableViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.INavigableViewModel")!;
        _mainViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.MainViewModel")!;
        _cleanerViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.CleanerViewModel")!;
        _toolsViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.ToolsViewModel")!;
        _settingsViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.SettingsViewModel")!;
    }

    [Fact]
    public void ShellViewModel_Type_Exists_With_Required_Members()
    {
        // Verify ShellViewModel has all required properties and commands
        Assert.NotNull(_shellViewModelType.GetProperty("NavigationItems"));
        Assert.NotNull(_shellViewModelType.GetProperty("CurrentView"));
        Assert.NotNull(_shellViewModelType.GetProperty("IsSidebarOpen"));
        Assert.NotNull(_shellViewModelType.GetProperty("WindowTitle"));
        Assert.NotNull(_shellViewModelType.GetProperty("IsDarkTheme"));
        
        Assert.NotNull(_shellViewModelType.GetProperty("NavigateCommand"));
        Assert.NotNull(_shellViewModelType.GetProperty("ToggleSidebarCommand"));
        Assert.NotNull(_shellViewModelType.GetProperty("ToggleThemeCommand"));
    }

    [Fact]
    public void NavItem_Should_Have_Label_Icon_And_ViewModelType()
    {
        // Arrange
        var geometry = Geometry.Parse("M0,0");
        geometry.Freeze();
        
        // Act
        var navItem = Activator.CreateInstance(_navItemType, "Test", geometry, typeof(object));
        
        // Assert
        Assert.Equal("Test", _navItemType.GetProperty("Label")!.GetValue(navItem));
        Assert.Equal(geometry, _navItemType.GetProperty("Icon")!.GetValue(navItem));
        Assert.Equal(typeof(object), _navItemType.GetProperty("ViewModelType")!.GetValue(navItem));
    }

    [Fact]
    public void INavigableViewModel_Should_Have_Title_Icon_IsSelected()
    {
        // This test verifies the interface exists and has required members
        Assert.NotNull(_iNavigableViewModelType.GetProperty("Title"));
        Assert.NotNull(_iNavigableViewModelType.GetProperty("Icon"));
        Assert.NotNull(_iNavigableViewModelType.GetProperty("IsSelected"));
    }

    [Fact]
    public void ViewModels_Implement_INavigableViewModel()
    {
        // Verify all four view models implement INavigableViewModel
        Assert.True(_iNavigableViewModelType.IsAssignableFrom(_mainViewModelType));
        Assert.True(_iNavigableViewModelType.IsAssignableFrom(_cleanerViewModelType));
        Assert.True(_iNavigableViewModelType.IsAssignableFrom(_toolsViewModelType));
        Assert.True(_iNavigableViewModelType.IsAssignableFrom(_settingsViewModelType));
    }

    [Fact]
    public void MainViewModel_Has_INavigableViewModel_Properties()
    {
        // Verify MainViewModel has the INavigableViewModel properties
        Assert.NotNull(_mainViewModelType.GetProperty("Title"));
        Assert.NotNull(_mainViewModelType.GetProperty("Icon"));
        Assert.NotNull(_mainViewModelType.GetProperty("IsSelected"));
    }
}