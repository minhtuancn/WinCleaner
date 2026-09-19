using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using WinCleaner.Models;
using WinCleaner.Services;
using Xunit;

namespace WinCleaner.Tests.Unit;

public class CleanupPlanServiceTests
{
    private readonly CleanupPlanService _service;
    private readonly Mock<ILogger<CleanupPlanService>> _loggerMock;
    private readonly Mock<IPathSafetyValidator> _pathValidatorMock;
    private readonly Mock<IAppRunningGuard> _appGuardMock;
    private readonly Mock<IResilienceService> _resilienceMock;

    public CleanupPlanServiceTests()
    {
        _loggerMock = new Mock<ILogger<CleanupPlanService>>();
        _pathValidatorMock = new Mock<IPathSafetyValidator>();
        _appGuardMock = new Mock<IAppRunningGuard>();
        _resilienceMock = new Mock<IResilienceService>();

        _service = new CleanupPlanService(
            _loggerMock.Object,
            _pathValidatorMock.Object,
            _appGuardMock.Object,
            _resilienceMock.Object);
    }

    [Fact]
    public void CreatePlanFromItems_CreatesPlanWithCorrectSteps()
    {
        var items = new List<CleanItem>
        {
            new CleanItem { Id = "1", Name = "Temp File 1", Path = @"C:\Temp\1.tmp", SizeBytes = 1024, Category = CleanCategory.SystemTemp, RiskLevel = ItemRiskLevel.Safe },
            new CleanItem { Id = "2", Name = "Cache File", Path = @"C:\Cache\cache.dat", SizeBytes = 2048, Category = CleanCategory.BrowserCache, RiskLevel = ItemRiskLevel.Low }
        };

        var plan = _service.CreatePlanFromItems(items, CleanProfile.Safe);

        Assert.Equal(2, plan.TotalSteps);
        Assert.Equal(3072, plan.TotalSizeBytes);
        Assert.Equal(2, plan.Steps.Count);
        Assert.Equal("1", plan.Steps[0].CandidateId);
        Assert.Equal("2", plan.Steps[1].CandidateId);
    }

    [Fact]
    public async Task ValidatePlanAsync_SafePaths_MarksAllReady()
    {
        var plan = CreateTestPlan();
        
        _pathValidatorMock.Setup(v => v.ValidatePath(It.IsAny<string>(), It.IsAny<CleanCategory>(), It.IsAny<ItemRiskLevel>()))
            .Returns(new PathSafetyResult { SafetyLevel = PathSafetyLevel.Safe });
        _appGuardMock.Setup(g => g.GetRunningProcessesForPath(It.IsAny<string>())).Returns(new List<string>());
        _appGuardMock.Setup(g => g.GetRunningProcessesForCategory(It.IsAny<CleanCategory>())).Returns(new List<string>());

        var validatedPlan = await _service.ValidatePlanAsync(plan);

        Assert.Equal(2, validatedPlan.ValidatedSteps);
        Assert.Equal(2, validatedPlan.ReadySteps);
        Assert.Equal(3072, validatedPlan.SafeSizeBytes);
        Assert.True(validatedPlan.IsValid);
        Assert.Empty(validatedPlan.ValidationErrors);
    }

    [Fact]
    public async Task ValidatePlanAsync_CriticalPath_AddsError()
    {
        var plan = CreateTestPlan();
        
        _pathValidatorMock.Setup(v => v.ValidatePath(It.IsAny<string>(), It.IsAny<CleanCategory>(), It.IsAny<ItemRiskLevel>()))
            .Returns(new PathSafetyResult { SafetyLevel = PathSafetyLevel.Critical, Reasons = new List<string> { "Critical system path" } });
        _appGuardMock.Setup(g => g.GetRunningProcessesForPath(It.IsAny<string>())).Returns(new List<string>());
        _appGuardMock.Setup(g => g.GetRunningProcessesForCategory(It.IsAny<CleanCategory>())).Returns(new List<string>());

        var validatedPlan = await _service.ValidatePlanAsync(plan);

        Assert.Equal(0, validatedPlan.ReadySteps);
        Assert.Equal(PathSafetyLevel.Critical, validatedPlan.Steps[0].SafetyResult.SafetyLevel);
        Assert.False(validatedPlan.IsValid);
        Assert.NotEmpty(validatedPlan.ValidationErrors);
    }

    [Fact]
    public async Task ValidatePlanAsync_RunningApps_AddsWarning()
    {
        var plan = CreateTestPlan();
        
        _pathValidatorMock.Setup(v => v.ValidatePath(It.IsAny<string>(), It.IsAny<CleanCategory>(), It.IsAny<ItemRiskLevel>()))
            .Returns(new PathSafetyResult { SafetyLevel = PathSafetyLevel.Safe });
        _appGuardMock.Setup(g => g.GetRunningProcessesForPath(It.IsAny<string>())).Returns(new List<string> { "chrome" });
        _appGuardMock.Setup(g => g.GetRecommendedAction(It.IsAny<List<string>>(), It.IsAny<ItemRiskLevel>())).Returns(AppRunningAction.Warn);

        var validatedPlan = await _service.ValidatePlanAsync(plan);

        Assert.True(validatedPlan.HasWarnings);
        Assert.NotEmpty(validatedPlan.ValidationWarnings);
        Assert.Equal(AppRunningAction.Warn, validatedPlan.Steps[0].AppAction);
        Assert.True(validatedPlan.Steps[0].RequiresAppClose);
    }

    [Fact]
    public async Task ExecutePlanAsync_SuccessfulExecution_ReturnsCompletedResult()
    {
        var plan = CreateTestPlan();
        plan.OverallStatus = CleanupOverallStatus.Pending;
        
        _pathValidatorMock.Setup(v => v.ValidatePath(It.IsAny<string>(), It.IsAny<CleanCategory>(), It.IsAny<ItemRiskLevel>()))
            .Returns(new PathSafetyResult { SafetyLevel = PathSafetyLevel.Safe });
        _appGuardMock.Setup(g => g.GetRunningProcessesForPath(It.IsAny<string>())).Returns(new List<string>());
        _appGuardMock.Setup(g => g.GetRunningProcessesForCategory(It.IsAny<CleanCategory>())).Returns(new List<string>());
        
        // First validate the plan
        plan = await _service.ValidatePlanAsync(plan);
        
        var successResult = new CleanupOperationResult
        {
            Status = CleanupOperationStatus.Success,
            CandidateId = "1",
            SizeBytes = 1024
        };
        _resilienceMock.Setup(r => r.ExecuteWithIsolationAsync(
            It.IsAny<Func<System.Threading.CancellationToken, Task>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CleanCategory>(), It.IsAny<ItemRiskLevel>(), It.IsAny<long>(),
            It.IsAny<TimeSpan?>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(successResult);

        var result = await _service.ExecutePlanAsync(plan, new Progress<string>(), new Progress<LogEntry>(), default);

        Assert.Equal(CleanupOverallStatus.Completed, result.OverallStatus);
        Assert.Equal(2, result.SuccessfulOperations);
        Assert.Equal(3072, result.TotalCleanedBytes);
    }

    private CleanupPlan CreateTestPlan()
    {
        return new CleanupPlan
        {
            PlanId = "test-plan",
            Name = "Test Plan",
            Profile = CleanProfile.Safe,
            Steps = new System.Collections.ObjectModel.ObservableCollection<CleanupPlanStep>
            {
                new CleanupPlanStep { CandidateId = "1", Path = @"C:\Temp\1.tmp", SizeBytes = 1024, Category = CleanCategory.SystemTemp, RiskLevel = ItemRiskLevel.Safe },
                new CleanupPlanStep { CandidateId = "2", Path = @"C:\Cache\2.dat", SizeBytes = 2048, Category = CleanCategory.BrowserCache, RiskLevel = ItemRiskLevel.Low }
            },
            TotalSteps = 2,
            TotalSizeBytes = 3072
        };
    }
}