using Microsoft.Extensions.Logging;
using Moq;
using WinCleaner.Models;
using WinCleaner.Services;
using Xunit;

namespace WinCleaner.Tests.Unit;

public class AppRunningGuardTests
{
    private readonly IAppRunningGuard _guard;
    private readonly Mock<ILogger<AppRunningGuard>> _loggerMock;

    public AppRunningGuardTests()
    {
        _loggerMock = new Mock<ILogger<AppRunningGuard>>();
        _guard = new AppRunningGuard(_loggerMock.Object);
    }

    [Fact]
    public void GetRunningProcessesForCategory_BrowserCache_ReturnsKnownBrowserProcesses()
    {
        var processes = _guard.GetRunningProcessesForCategory(CleanCategory.BrowserCache);
        
        Assert.NotNull(processes);
    }

    [Fact]
    public void GetRunningProcessesForCategory_UnknownCategory_ReturnsEmpty()
    {
        var processes = _guard.GetRunningProcessesForCategory((CleanCategory)999);
        
        Assert.Empty(processes);
    }

    [Theory]
    [InlineData(AppRunningAction.Warn, ItemRiskLevel.Safe)]
    [InlineData(AppRunningAction.Warn, ItemRiskLevel.Low)]
    [InlineData(AppRunningAction.Warn, ItemRiskLevel.Medium)]
    [InlineData(AppRunningAction.RequireClose, ItemRiskLevel.High)]
    [InlineData(AppRunningAction.RequireClose, ItemRiskLevel.Critical)]
    public void GetRecommendedAction_ReturnsExpectedAction(AppRunningAction expected, ItemRiskLevel riskLevel)
    {
        var runningProcesses = new List<string> { "chrome", "firefox" };
        var action = _guard.GetRecommendedAction(runningProcesses, riskLevel);
        
        Assert.Equal(expected, action);
    }

    [Fact]
    public void GetRecommendedAction_NoRunningProcesses_ReturnsSkip()
    {
        var action = _guard.GetRecommendedAction(new List<string>(), ItemRiskLevel.Critical);
        
        Assert.Equal(AppRunningAction.Skip, action);
    }
}