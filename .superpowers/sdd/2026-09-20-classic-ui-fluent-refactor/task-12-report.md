# Task 12: Window State Persistence - Implementation Report

## Summary
Implemented window state persistence for sidebar width, selected navigation tab, and sidebar open/closed state.

## Changes Made

### 1. Extended `WindowStateData` (WinCleaner-classic/Services/WindowStateService.cs + src/WinCleaner.Infrastructure.Windows/Services/WindowStateService.cs)
Added three new properties:
- `double SidebarWidth` (default: 280)
- `string SelectedNavItem` (default: "Dashboard")
- `bool IsSidebarOpen` (default: true)

Updated `GetDefaultStateAsync()` to include defaults for new fields.

### 2. Updated `ShellWindow.xaml.cs` (WinCleaner-classic/Views/ShellWindow.xaml.cs)
- Added constructor accepting `IWindowStateService` for DI
- Hooked `Loaded` event to load and apply saved state
- Hooked `Closing` event to capture and save current state
- Implemented `ApplyWindowState()` to restore:
  - Window size/position/state
  - Sidebar width (via `SidebarColumn.Width`)
  - Sidebar open/closed state
  - Selected navigation tab (via `Navigate()`)
- Implemented `CaptureWindowState()` to save current state

### 3. Made `ShellViewModel.Navigate()` public
Changed from `private` to `public` to allow state restoration from `ShellWindow`.

### 4. Updated DI Registration (WinCleaner-classic/App.xaml.cs)
Modified `ShellWindow` registration to inject `IWindowStateService`.

### 5. Unit Tests (tests/WinCleaner.Tests.Unit/WindowStateServiceTests.cs)
Added 3 tests following TDD:
- `LoadStateAsync_DefaultState_ContainsNewProperties` - verifies defaults
- `SaveAndLoadStateAsync_RoundTrip_PreservesNewProperties` - verifies serialization round-trip
- `GetDefaultStateAsync_ContainsValidDefaultsForNewProperties` - verifies default values are valid

## Test Results
- **RED phase**: Tests failed initially (new properties didn't exist)
- **GREEN phase**: All 3 new tests + 99 existing tests pass (102 total)
- Test command: `dotnet test tests/WinCleaner.Tests.Unit --configuration Release --no-build`
- Output: Pristine, no warnings/errors

## Files Changed
1. `WinCleaner-classic/Services/WindowStateService.cs` - Extended WindowStateData & GetDefaultStateAsync
2. `src/WinCleaner.Infrastructure.Windows/Services/WindowStateService.cs` - Same changes for Infrastructure project
3. `WinCleaner-classic/Views/ShellWindow.xaml.cs` - Load/Save state on Loaded/Closing events
4. `WinCleaner-classic/ViewModels/ShellViewModel.cs` - Made Navigate() public
5. `WinCleaner-classic/App.xaml.cs` - Updated DI registration
6. `tests/WinCleaner.Tests.Unit/WindowStateServiceTests.cs` - New unit tests (3 tests)

## Self-Review Findings
- ✅ Fully implements all requirements from task brief
- ✅ Follows TDD approach (RED → GREEN)
- ✅ All tests pass (102/102)
- ✅ Code follows existing patterns (DI, async/await, JSON serialization)
- ✅ No overbuilding - only added what was specified
- ✅ Edge cases handled (null checks, fallback defaults)

## Concerns
- The `SidebarColumn.ActualWidth` in `CaptureWindowState()` might return 0 if called before layout. Consider using the saved `SidebarWidth` as fallback or deferring capture.
- The `SelectedNavItem` restoration uses `CurrentView?.GetType().Name.Replace("ViewModel", "")` which assumes naming convention. Could be made more robust with a dedicated property.