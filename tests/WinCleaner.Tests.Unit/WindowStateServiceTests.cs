using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using FluentAssertions;
using WinCleaner.Models;
using WinCleaner.Services;
using Xunit;

namespace WinCleaner.Tests.Unit;

public class WindowStateServiceTests
{
    [Fact]
    public async Task LoadStateAsync_DefaultState_ContainsNewProperties()
    {
        var service = new WindowStateService();
        await service.ResetToDefaultAsync();
        var state = await service.LoadStateAsync();

        state.Should().NotBeNull();
        state.SidebarWidth.Should().BeGreaterThan(0);
        state.SelectedNavItem.Should().NotBeNullOrEmpty();
        state.IsSidebarOpen.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAndLoadStateAsync_RoundTrip_PreservesNewProperties()
    {
        var service = new WindowStateService();
        
        var originalState = new WindowStateData
        {
            Width = 1200,
            Height = 800,
            Left = 50,
            Top = 50,
            WindowState = WindowState.Normal,
            IsMaximized = false,
            SelectedProfile = CleanProfile.Deep,
            DryRunMode = true,
            AutoScrollLog = false,
            TreeViewExpandedStates = new Dictionary<string, bool> { ["test"] = true },
            ColumnWidths = new Dictionary<string, double> { ["col1"] = 100 },
            LastActiveTab = "Settings",
            SplitterDistance = 800,
            LogScrollPosition = 50,
            LastUpdated = DateTime.Now,
            SidebarWidth = 320,
            SelectedNavItem = "Tools",
            IsSidebarOpen = false
        };

        await service.SaveStateAsync(originalState);
        var loadedState = await service.LoadStateAsync();

        loadedState.SidebarWidth.Should().Be(originalState.SidebarWidth);
        loadedState.SelectedNavItem.Should().Be(originalState.SelectedNavItem);
        loadedState.IsSidebarOpen.Should().Be(originalState.IsSidebarOpen);
        loadedState.Width.Should().Be(originalState.Width);
        loadedState.Height.Should().Be(originalState.Height);
        loadedState.LastActiveTab.Should().Be(originalState.LastActiveTab);
    }

    [Fact]
    public async Task GetDefaultStateAsync_ContainsValidDefaultsForNewProperties()
    {
        var service = new WindowStateService();
        var state = await service.GetDefaultStateAsync();

        state.SidebarWidth.Should().BeGreaterThan(0);
        state.SelectedNavItem.Should().BeOneOf("Dashboard", "Cleaner", "Tools", "Settings");
        state.IsSidebarOpen.Should().BeTrue();
    }
}