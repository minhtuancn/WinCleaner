using System;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using Xunit;

namespace WinCleaner.Tests.Unit;

[Collection("WpfTests")]
public class SettingsViewModelTests
{
    private readonly WpfTestFixture _fixture;
    private readonly Assembly _classicAssembly;
    private readonly Type _settingsViewModelType;

    public SettingsViewModelTests(WpfTestFixture fixture)
    {
        _fixture = fixture;
        _classicAssembly = Assembly.Load("WinCleaner");
        _settingsViewModelType = _classicAssembly.GetType("WinCleaner.ViewModels.SettingsViewModel")!;
    }

    [Fact]
    public void SettingsViewModel_Should_Have_Title_Property()
    {
        // Arrange
        var titleProperty = _settingsViewModelType.GetProperty("Title");
        
        // Assert
        Assert.NotNull(titleProperty);
        Assert.Equal(typeof(string), titleProperty.PropertyType);
        var getMethod = titleProperty.GetGetMethod();
        Assert.NotNull(getMethod);
    }

    [Fact]
    public void SettingsViewModel_Should_Have_Icon_Property_Returning_Geometry()
    {
        // Arrange
        var iconProperty = _settingsViewModelType.GetProperty("Icon");
        
        // Assert
        Assert.NotNull(iconProperty);
        Assert.Equal(typeof(Geometry), iconProperty.PropertyType);
    }

    [Fact]
    public void SettingsViewModel_Should_Implement_INavigableViewModel()
    {
        // Arrange
        var iNavigableType = _classicAssembly.GetType("WinCleaner.ViewModels.INavigableViewModel")!;
        
        // Assert
        Assert.True(iNavigableType.IsAssignableFrom(_settingsViewModelType));
    }

    [Fact]
    public void SettingsViewModel_Should_Have_PickAccentColorCommand()
    {
        // Arrange
        var commandProperty = _settingsViewModelType.GetProperty("PickAccentColorCommand");
        
        // Assert
        Assert.NotNull(commandProperty);
        Assert.True(typeof(ICommand).IsAssignableFrom(commandProperty.PropertyType));
    }

    [Fact]
    public void SettingsViewModel_Should_Have_Languages_Collection()
    {
        // Arrange
        var languagesProperty = _settingsViewModelType.GetProperty("Languages");
        
        // Assert
        Assert.NotNull(languagesProperty);
        Assert.True(typeof(System.Collections.ObjectModel.ObservableCollection<string>).IsAssignableFrom(languagesProperty.PropertyType));
    }

    [Fact]
    public void SettingsViewModel_Should_Have_SelectedLanguage_Property()
    {
        // Arrange
        var selectedLanguageProperty = _settingsViewModelType.GetProperty("SelectedLanguage");
        
        // Assert
        Assert.NotNull(selectedLanguageProperty);
        Assert.Equal(typeof(string), selectedLanguageProperty.PropertyType);
    }

    [Fact]
    public void SettingsViewModel_Should_Have_AutoCheckUpdates_Property()
    {
        // Arrange
        var autoCheckUpdatesProperty = _settingsViewModelType.GetProperty("AutoCheckUpdates");
        
        // Assert
        Assert.NotNull(autoCheckUpdatesProperty);
        Assert.Equal(typeof(bool), autoCheckUpdatesProperty.PropertyType);
    }

    [Fact]
    public void SettingsViewModel_Should_Have_SelectedChannel_Property()
    {
        // Arrange
        var updateChannelType = _classicAssembly.GetType("WinCleaner.Models.UpdateChannel")!;
        var selectedChannelProperty = _settingsViewModelType.GetProperty("SelectedChannel");
        
        // Assert
        Assert.NotNull(selectedChannelProperty);
        Assert.Equal(updateChannelType, selectedChannelProperty.PropertyType);
    }

    [Fact]
    public void SettingsViewModel_Should_Have_Theme_Properties()
    {
        // Arrange
        var appThemeType = _classicAssembly.GetType("WinCleaner.Models.AppTheme")!;
        
        // Assert - Theme properties
        var selectedThemeProperty = _settingsViewModelType.GetProperty("SelectedTheme");
        Assert.NotNull(selectedThemeProperty);
        Assert.Equal(appThemeType, selectedThemeProperty.PropertyType);

        var useSystemThemeProperty = _settingsViewModelType.GetProperty("UseSystemTheme");
        Assert.NotNull(useSystemThemeProperty);
        Assert.Equal(typeof(bool), useSystemThemeProperty.PropertyType);

        var accentColorProperty = _settingsViewModelType.GetProperty("AccentColor");
        Assert.NotNull(accentColorProperty);
        Assert.Equal(typeof(string), accentColorProperty.PropertyType);

        var enableAnimationsProperty = _settingsViewModelType.GetProperty("EnableAnimations");
        Assert.NotNull(enableAnimationsProperty);
        Assert.Equal(typeof(bool), enableAnimationsProperty.PropertyType);

        var enableTransparencyProperty = _settingsViewModelType.GetProperty("EnableTransparency");
        Assert.NotNull(enableTransparencyProperty);
        Assert.Equal(typeof(bool), enableTransparencyProperty.PropertyType);

        var uiScaleProperty = _settingsViewModelType.GetProperty("UiScale");
        Assert.NotNull(uiScaleProperty);
        Assert.Equal(typeof(double), uiScaleProperty.PropertyType);
    }

    [Fact]
    public void SettingsViewModel_Should_Have_Advanced_Settings_Properties()
    {
        // Assert - Advanced settings
        var createRestorePointProperty = _settingsViewModelType.GetProperty("CreateRestorePoint");
        Assert.NotNull(createRestorePointProperty);
        Assert.Equal(typeof(bool), createRestorePointProperty.PropertyType);

        var logRetentionDaysProperty = _settingsViewModelType.GetProperty("LogRetentionDays");
        Assert.NotNull(logRetentionDaysProperty);
        Assert.Equal(typeof(int), logRetentionDaysProperty.PropertyType);
    }

    [Fact]
    public void SettingsViewModel_Should_Have_AvailableChannels_Collection()
    {
        // Arrange
        var availableChannelsProperty = _settingsViewModelType.GetProperty("AvailableChannels");
        
        // Assert
        Assert.NotNull(availableChannelsProperty);
        var updateChannelType = _classicAssembly.GetType("WinCleaner.Models.UpdateChannel")!;
        var expectedCollectionType = typeof(System.Collections.ObjectModel.ObservableCollection<>).MakeGenericType(updateChannelType);
        Assert.True(expectedCollectionType.IsAssignableFrom(availableChannelsProperty.PropertyType));
    }
}