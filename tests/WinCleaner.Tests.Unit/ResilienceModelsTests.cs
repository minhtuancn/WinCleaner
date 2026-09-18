using WinCleaner.Models;
using Xunit;

namespace WinCleaner.Tests.Unit;

public class ResilienceModelsTests
{
    [Fact]
    public void CleanupOperationResult_IsSuccess_ReturnsTrueForSuccess()
    {
        var result = new CleanupOperationResult { Status = CleanupOperationStatus.Success };
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CleanupOperationResult_IsSuccess_ReturnsFalseForFailed()
    {
        var result = new CleanupOperationResult { Status = CleanupOperationStatus.Failed };
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(CleanupOperationStatus.AccessDenied, true)]
    [InlineData(CleanupOperationStatus.Locked, true)]
    [InlineData(CleanupOperationStatus.NotFound, true)]
    [InlineData(CleanupOperationStatus.ChangedSinceScan, true)]
    [InlineData(CleanupOperationStatus.IoError, true)]
    [InlineData(CleanupOperationStatus.DeviceUnavailable, true)]
    [InlineData(CleanupOperationStatus.Timeout, true)]
    [InlineData(CleanupOperationStatus.Cancelled, true)]
    [InlineData(CleanupOperationStatus.UnsafePath, false)]
    [InlineData(CleanupOperationStatus.RequiresElevation, false)]
    [InlineData(CleanupOperationStatus.Unsupported, false)]
    [InlineData(CleanupOperationStatus.Failed, false)]
    public void CleanupOperationResult_IsRecoverableFailure_ReturnsExpected(CleanupOperationStatus status, bool expected)
    {
        var result = new CleanupOperationResult { Status = status };
        Assert.Equal(expected, result.IsRecoverableFailure);
    }

    [Fact]
    public void CleanupOperationResult_GetUserMessage_ReturnsCustomMessage()
    {
        var result = new CleanupOperationResult { UserSafeMessage = "Custom message" };
        Assert.Equal("Custom message", result.GetUserMessage());
    }

    [Fact]
    public void CleanupOperationResult_GetUserMessage_ReturnsDefaultForSuccess()
    {
        var result = new CleanupOperationResult { Status = CleanupOperationStatus.Success };
        Assert.Equal("Deleted successfully", result.GetUserMessage());
    }

    [Fact]
    public void ThemeSettings_Clone_CreatesIndependentCopy()
    {
        var original = new ThemeSettings
        {
            CurrentTheme = AppTheme.Dark,
            AccentColor = "#FF0000",
            EnableAnimations = false,
            EnableTransparency = true,
            UiScale = 1.25,
            UseSystemTheme = false
        };

        var clone = original.Clone();

        Assert.Equal(original.CurrentTheme, clone.CurrentTheme);
        Assert.Equal(original.AccentColor, clone.AccentColor);
        Assert.Equal(original.EnableAnimations, clone.EnableAnimations);
        Assert.Equal(original.EnableTransparency, clone.EnableTransparency);
        Assert.Equal(original.UiScale, clone.UiScale);
        Assert.Equal(original.UseSystemTheme, clone.UseSystemTheme);
        
        // Verify independence
        clone.CurrentTheme = AppTheme.Light;
        Assert.Equal(AppTheme.Dark, original.CurrentTheme);
        Assert.Equal(AppTheme.Light, clone.CurrentTheme);
    }
}