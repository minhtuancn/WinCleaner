using System.Reflection;
using System.Windows.Input;
using Xunit;

namespace WinCleaner.Tests.Unit;

[Collection("WpfTests")]
public class CleanerViewModelTests
{
    private readonly WpfTestFixture _fixture;
    private readonly Assembly _classicAssembly;
    private readonly Type _cleanerViewModelType;

    public CleanerViewModelTests(WpfTestFixture fixture)
    {
        _fixture = fixture;
        _classicAssembly = Assembly.Load("WinCleaner");
        _cleanerViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.CleanerViewModel")!;
    }

    [Fact]
    public void CleanerViewModel_Should_Have_Cleaner_Title()
    {
        var titleProperty = _cleanerViewModelType.GetProperty("Title");
        Assert.NotNull(titleProperty);
        Assert.Equal(typeof(string), titleProperty.PropertyType);
    }

    [Fact]
    public void CleanerViewModel_Should_Have_Categories_Property()
    {
        var categoriesProperty = _cleanerViewModelType.GetProperty("Categories");
        Assert.NotNull(categoriesProperty);
        var collectionType = typeof(System.Collections.ObjectModel.ObservableCollection<>).MakeGenericType(
            _classicAssembly.GetType("WinCleaner.Models.CleanCategoryGroup")!);
        Assert.True(collectionType.IsAssignableFrom(categoriesProperty.PropertyType));
    }

    [Fact]
    public void CleanerViewModel_Should_Have_SelectedProfile_Property()
    {
        var profileProperty = _cleanerViewModelType.GetProperty("SelectedProfile");
        Assert.NotNull(profileProperty);
        var cleanProfileType = _classicAssembly.GetType("WinCleaner.Models.CleanProfile")!;
        Assert.Equal(cleanProfileType, profileProperty.PropertyType);
    }

    [Fact]
    public void CleanerViewModel_Should_Have_ScanCommand()
    {
        var scanCommandProperty = _cleanerViewModelType.GetProperty("ScanCommand");
        Assert.NotNull(scanCommandProperty);
        Assert.True(typeof(ICommand).IsAssignableFrom(scanCommandProperty.PropertyType));
    }

    [Fact]
    public void CleanerViewModel_Should_Have_CleanCommand()
    {
        var cleanCommandProperty = _cleanerViewModelType.GetProperty("CleanCommand");
        Assert.NotNull(cleanCommandProperty);
        Assert.True(typeof(ICommand).IsAssignableFrom(cleanCommandProperty.PropertyType));
    }

    [Fact]
    public void CleanerViewModel_Should_Have_SelectSafeCommand()
    {
        var selectSafeCommandProperty = _cleanerViewModelType.GetProperty("SelectSafeCommand");
        Assert.NotNull(selectSafeCommandProperty);
        Assert.True(typeof(ICommand).IsAssignableFrom(selectSafeCommandProperty.PropertyType));
    }

    [Fact]
    public void CleanerViewModel_Should_Implement_INavigableViewModel()
    {
        var iNavigableType = _classicAssembly.GetType("WinCleaner.ViewModels.INavigableViewModel")!;
        Assert.True(iNavigableType.IsAssignableFrom(_cleanerViewModelType));
    }
}