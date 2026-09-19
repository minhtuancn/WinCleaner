using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ICleanupPlanService
    {
        Task<CleanupPlan> CreatePlanAsync(List<CleanCategoryGroup> groups, CleanProfile profile, CancellationToken cancellationToken = default);
        Task<CleanupPlan> ValidatePlanAsync(CleanupPlan plan, CancellationToken cancellationToken = default);
        Task<CleanupExecutionResult> ExecutePlanAsync(CleanupPlan plan, IProgress<string> progress, IProgress<LogEntry> logProgress, CancellationToken cancellationToken = default);
        CleanupPlan CreatePlanFromItems(IEnumerable<CleanItem> items, CleanProfile profile);
    }

    public class CleanupPlanService : ICleanupPlanService
    {
        private readonly ILogger<CleanupPlanService> _logger;
        private readonly IPathSafetyValidator _pathSafetyValidator;
        private readonly IAppRunningGuard _appRunningGuard;
        private readonly IResilienceService _resilienceService;

        public CleanupPlanService(
            ILogger<CleanupPlanService> logger,
            IPathSafetyValidator pathSafetyValidator,
            IAppRunningGuard appRunningGuard,
            IResilienceService resilienceService)
        {
            _logger = logger;
            _pathSafetyValidator = pathSafetyValidator;
            _appRunningGuard = appRunningGuard;
            _resilienceService = resilienceService;
        }

        public Task<CleanupPlan> CreatePlanAsync(List<CleanCategoryGroup> groups, CleanProfile profile, CancellationToken cancellationToken = default)
        {
            var selectedItems = groups.SelectMany(g => g.Items.Where(i => i.IsSelected)).ToList();
            return Task.FromResult(CreatePlanFromItems(selectedItems, profile));
        }

        public CleanupPlan CreatePlanFromItems(IEnumerable<CleanItem> items, CleanProfile profile)
        {
            var plan = new CleanupPlan
            {
                Name = $"Cleanup Plan - {profile}",
                Profile = profile,
            };

            foreach (var item in items)
            {
                var step = new CleanupPlanStep
                {
                    CandidateId = item.Id,
                    RuleId = item.Id, // Could be enhanced to track actual rule ID
                    Path = item.Path,
                    DisplayName = item.Name,
                    Category = item.Category,
                    RiskLevel = item.RiskLevel,
                    SizeBytes = item.SizeBytes,
                    RequiresAdmin = item.RequiresAdmin,
                };

                plan.Steps.Add(step);
                plan.TotalSteps++;
                plan.TotalSizeBytes += item.SizeBytes;
            }

            return plan;
        }

        public async Task<CleanupPlan> ValidatePlanAsync(CleanupPlan plan, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating cleanup plan {PlanId} with {StepCount} steps", plan.PlanId, plan.Steps.Count);

            plan.ValidationWarnings.Clear();
            plan.ValidationErrors.Clear();
            plan.ValidatedSteps = 0;
            plan.ReadySteps = 0;
            plan.SafeSizeBytes = 0;
            plan.CautionSizeBytes = 0;
            plan.ProtectedSizeBytes = 0;

            foreach (var step in plan.Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Path safety validation
                step.SafetyResult = _pathSafetyValidator.ValidatePath(step.Path, step.Category, step.RiskLevel);
                step.IsValidated = true;
                plan.ValidatedSteps++;

                // Accumulate sizes by safety level
                switch (step.SafetyResult.SafetyLevel)
                {
                    case PathSafetyLevel.Safe:
                        plan.SafeSizeBytes += step.SizeBytes;
                        break;
                    case PathSafetyLevel.Caution:
                        plan.CautionSizeBytes += step.SizeBytes;
                        break;
                    case PathSafetyLevel.Protected:
                    case PathSafetyLevel.Critical:
                        plan.ProtectedSizeBytes += step.SizeBytes;
                        break;
                }

                // Check app running status
                var runningProcesses = _appRunningGuard.GetRunningProcessesForPath(step.Path);
                if (runningProcesses.Count == 0)
                {
                    runningProcesses = _appRunningGuard.GetRunningProcessesForCategory(step.Category);
                }
                step.RunningProcesses = runningProcesses;

                if (runningProcesses.Count > 0)
                {
                    step.RequiresAppClose = true;
                    step.AppAction = _appRunningGuard.GetRecommendedAction(runningProcesses, step.RiskLevel);
                    step.UserMessage = $"Running applications detected: {string.Join(", ", runningProcesses)}";
                    plan.ValidationWarnings.Add($"Step '{step.DisplayName}': {step.UserMessage}");
                }

                // Determine if step is ready to execute
                if (step.SafetyResult.IsSafeToDelete && step.Status == CleanupOperationStatus.Pending)
                {
                    step.Status = CleanupOperationStatus.Pending; // Ready
                    plan.ReadySteps++;
                }
                else if (step.SafetyResult.SafetyLevel >= PathSafetyLevel.Critical)
                {
                    step.Status = CleanupOperationStatus.UnsafePath;
                    plan.ValidationErrors.Add($"Step '{step.DisplayName}': Critical path - {step.SafetyResult.Reasons.FirstOrDefault()}");
                }
            }

            plan.ValidatedAt = DateTime.UtcNow;
            plan.OverallStatus = plan.ValidationErrors.Count > 0 ? CleanupOverallStatus.Failed : CleanupOverallStatus.Pending;

            _logger.LogInformation("Plan validation complete: {ReadySteps}/{TotalSteps} ready, {WarningCount} warnings, {ErrorCount} errors",
                plan.ReadySteps, plan.TotalSteps, plan.ValidationWarnings.Count, plan.ValidationErrors.Count);

            return plan;
        }

        public async Task<CleanupExecutionResult> ExecutePlanAsync(CleanupPlan plan, IProgress<string> progress, IProgress<LogEntry> logProgress, CancellationToken cancellationToken = default)
        {
            var result = new CleanupExecutionResult
            {
                PlanId = plan.PlanId,
                StartedAt = DateTime.UtcNow,
            };

            var stopwatch = Stopwatch.StartNew();
            plan.OverallStatus = CleanupOverallStatus.Running;
            plan.ExecutedAt = DateTime.UtcNow;

            _logger.LogInformation("Executing cleanup plan {PlanId} with {ReadySteps} ready steps", plan.PlanId, plan.ReadySteps);

            Log(logProgress, Models.LogLevel.Info, $"=== Starting Cleanup Plan: {plan.Name} ===", "CleanupPlan");
            Log(logProgress, Models.LogLevel.Info, $"Total steps: {plan.TotalSteps}, Ready: {plan.ReadySteps}, Size: {plan.TotalSizeFormatted}", "CleanupPlan");

            foreach (var step in plan.Steps.Where(s => s.IsReadyToExecute).ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                step.Status = CleanupOperationStatus.Pending; // Executing
                step.ExecutedAt = DateTime.UtcNow;
                var stepStopwatch = Stopwatch.StartNew();

                progress?.Report($"Executing: {step.DisplayName} ({step.SizeBytes} bytes)");
                Log(logProgress, Models.LogLevel.Info, $"Executing: {step.DisplayName}", "CleanupPlan");

                // Execute with resilience isolation
                var operationResult = await _resilienceService.ExecuteWithIsolationAsync(
                    async (ct) => await ExecuteStepAsync(step, ct),
                    step.CandidateId,
                    step.RuleId,
                    step.Path,
                    step.Category,
                    step.RiskLevel,
                    step.SizeBytes,
                    timeout: TimeSpan.FromMinutes(5),
                    cancellationToken: cancellationToken);

                stepStopwatch.Stop();
                step.Duration = stepStopwatch.Elapsed;
                step.ExecutionResult = operationResult;
                step.Status = operationResult.Status;

                result.OperationResults.Add(operationResult);

                if (operationResult.IsSuccess)
                {
                    result.SuccessfulOperations++;
                    result.TotalCleanedBytes += step.SizeBytes;
                    step.UserMessage = "Completed successfully";
                    Log(logProgress, Models.LogLevel.Success, $"✓ {step.DisplayName} - {step.SizeBytes} bytes", "CleanupPlan");
                }
                else if (operationResult.IsRecoverableFailure)
                {
                    result.FailedOperations++;
                    step.UserMessage = operationResult.GetUserMessage();
                    Log(logProgress, Models.LogLevel.Warning, $"⚠ {step.DisplayName}: {operationResult.GetUserMessage()}", "CleanupPlan");
                }
                else
                {
                    result.FailedOperations++;
                    step.UserMessage = operationResult.GetUserMessage();
                    Log(logProgress, Models.LogLevel.Error, $"✗ {step.DisplayName}: {operationResult.GetUserMessage()}", "CleanupPlan");
                }

                // Update plan aggregates
                plan.SafeSizeBytes = plan.Steps.Where(s => s.ExecutionResult?.IsSuccess == true).Sum(s => s.SizeBytes);
            }

            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
            plan.TotalDuration = stopwatch.Elapsed;

            // Determine overall status
            if (result.WasCancelled || cancellationToken.IsCancellationRequested)
            {
                result.OverallStatus = CleanupOverallStatus.Cancelled;
                plan.OverallStatus = CleanupOverallStatus.Cancelled;
            }
            else if (result.FailedOperations > 0 && result.SuccessfulOperations > 0)
            {
                result.OverallStatus = CleanupOverallStatus.PartialFailure;
                plan.OverallStatus = CleanupOverallStatus.PartialFailure;
            }
            else if (result.FailedOperations > 0 && result.SuccessfulOperations == 0)
            {
                result.OverallStatus = CleanupOverallStatus.Failed;
                plan.OverallStatus = CleanupOverallStatus.Failed;
            }
            else
            {
                result.OverallStatus = CleanupOverallStatus.Completed;
                plan.OverallStatus = CleanupOverallStatus.Completed;
            }

            Log(logProgress, Models.LogLevel.Success, $"=== Cleanup Plan Complete: {result.OverallStatus} ===", "CleanupPlan");
            Log(logProgress, Models.LogLevel.Success, $"Cleaned: {result.TotalCleanedFormatted} ({result.SuccessfulOperations}/{result.TotalSteps} steps)", "CleanupPlan");

            return result;
        }

        private async Task ExecuteStepAsync(CleanupPlanStep step, CancellationToken cancellationToken)
        {
            // This is a placeholder - actual execution would call the CleanItem.CleanAction
            // For now, simulate the operation
            await Task.Delay(100, cancellationToken);
            
            // In real implementation, this would call the actual cleaner logic
            // The CleanerService.CleanItemAsync would be called here
        }

        private void Log(IProgress<LogEntry> logProgress, Models.LogLevel level, string message, string source)
        {
            logProgress?.Report(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = source
            });
        }
    }
}