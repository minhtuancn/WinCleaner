using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using WinCleaner.Models;
using WinCleaner.Services;
using Xunit;

namespace WinCleaner.Tests.Unit;

public class PathSafetyValidatorTests
{
    private readonly IPathSafetyValidator _validator;
    private readonly Mock<ILogger<PathSafetyValidator>> _loggerMock;

    public PathSafetyValidatorTests()
    {
        _loggerMock = new Mock<ILogger<PathSafetyValidator>>();
        _validator = new PathSafetyValidator(_loggerMock.Object);
    }

    [Theory]
    [InlineData(@"C:\Windows\System32\drivers\etc\hosts", PathSafetyLevel.Critical)]
    [InlineData(@"C:\Windows\WinSxS\manifests\x86_test.manifest", PathSafetyLevel.Critical)]
    [InlineData(@"C:\System Volume Information\file", PathSafetyLevel.Critical)]
    [InlineData(@"C:\pagefile.sys", PathSafetyLevel.Critical)]
    [InlineData(@"C:\hiberfil.sys", PathSafetyLevel.Critical)]
    public void ValidatePath_CriticalSystemPaths_ReturnsCritical(string path, PathSafetyLevel expected)
    {
        var result = _validator.ValidatePath(path, CleanCategory.SystemTemp, ItemRiskLevel.Safe);
        
        Assert.Equal(expected, result.SafetyLevel);
        Assert.False(result.IsSafeToDelete);
        Assert.True(result.RequiresConfirmation);
    }

    [Theory]
    [InlineData(@"C:\Windows\Temp\test.tmp", PathSafetyLevel.Protected)]
    [InlineData(@"C:\Program Files\SomeApp\cache.dat", PathSafetyLevel.Protected)]
    [InlineData(@"C:\ProgramData\Microsoft\file", PathSafetyLevel.Protected)]
    public void ValidatePath_ProtectedSystemPaths_ReturnsProtected(string path, PathSafetyLevel expected)
    {
        var result = _validator.ValidatePath(path, CleanCategory.SystemTemp, ItemRiskLevel.Safe);
        
        Assert.Equal(expected, result.SafetyLevel);
        Assert.False(result.IsSafeToDelete);
        Assert.True(result.RequiresConfirmation);
    }

    [Theory]
    [InlineData(@"C:\Users\Test\AppData\Local\Temp\test.tmp", PathSafetyLevel.Safe)]
    [InlineData(@"C:\Users\Test\AppData\Local\Microsoft\Windows\INetCache\file", PathSafetyLevel.Safe)]
    [InlineData(@"C:\Users\Test\AppData\Local\Google\Chrome\User Data\Default\Cache\data", PathSafetyLevel.Safe)]
    [InlineData(@"C:\Temp\test.log", PathSafetyLevel.Safe)]
    public void ValidatePath_KnownSafeCachePaths_ReturnsSafe(string path, PathSafetyLevel expected)
    {
        var result = _validator.ValidatePath(path, CleanCategory.SystemTemp, ItemRiskLevel.Safe);
        
        Assert.Equal(expected, result.SafetyLevel);
        Assert.True(result.IsSafeToDelete);
        Assert.False(result.RequiresConfirmation);
    }

    [Fact]
    public void ValidatePath_UserDocuments_ReturnsCaution()
    {
        var result = _validator.ValidatePath(@"C:\Users\Test\Documents\important.docx", CleanCategory.UserPrograms, ItemRiskLevel.Low);
        
        Assert.Equal(PathSafetyLevel.Caution, result.SafetyLevel);
        Assert.True(result.RequiresConfirmation);
    }

    [Fact]
    public void ValidatePath_RiskLevelElevatesSafety()
    {
        var safePath = @"C:\Temp\safe.tmp";
        
        var resultSafe = _validator.ValidatePath(safePath, CleanCategory.SystemTemp, ItemRiskLevel.Safe);
        var resultCritical = _validator.ValidatePath(safePath, CleanCategory.SystemTemp, ItemRiskLevel.Critical);
        
        Assert.Equal(PathSafetyLevel.Safe, resultSafe.SafetyLevel);
        Assert.Equal(PathSafetyLevel.Critical, resultCritical.SafetyLevel);
    }

    [Fact]
    public void ValidatePath_CategoryElevatesSafety()
    {
        var path = @"C:\Temp\test.tmp";
        
        var resultSystemRestore = _validator.ValidatePath(path, CleanCategory.SystemRestore, ItemRiskLevel.Safe);
        var resultBrowserCache = _validator.ValidatePath(path, CleanCategory.BrowserCache, ItemRiskLevel.Safe);
        
        Assert.Equal(PathSafetyLevel.Critical, resultSystemRestore.SafetyLevel);
        Assert.Equal(PathSafetyLevel.Safe, resultBrowserCache.SafetyLevel);
    }

    [Fact]
    public void ValidatePath_EmptyPath_ReturnsCritical()
    {
        var result = _validator.ValidatePath("", CleanCategory.SystemTemp, ItemRiskLevel.Safe);
        
        Assert.Equal(PathSafetyLevel.Critical, result.SafetyLevel);
        Assert.False(result.IsSafeToDelete);
    }

    [Fact]
    public void IsPathProtected_CriticalPath_ReturnsTrue()
    {
        Assert.True(_validator.IsPathProtected(@"C:\Windows\System32\config\SAM"));
        Assert.True(_validator.IsPathProtected(@"C:\pagefile.sys"));
    }

    [Fact]
    public void IsPathProtected_SafePath_ReturnsFalse()
    {
        Assert.False(_validator.IsPathProtected(@"C:\Temp\test.tmp"));
        Assert.False(_validator.IsPathProtected(@"C:\Users\Test\AppData\Local\Temp\cache"));
    }
}