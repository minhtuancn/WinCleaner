### Task 6: Dashboard View (MainViewModel refactor)

**Files:**
- Modify: `WinCleaner-classic/ViewModels/MainViewModel.cs` (implement `INavigableViewModel`, keep scan/clean logic)
- Create: `WinCleaner-classic/Views/DashboardView.xaml` (card grid: Health, Disk, Top Junk, Quick Actions)

**Interfaces:**
- `MainViewModel` implements `INavigableViewModel` (Title="Dashboard", Icon=Icons.Dashboard).
- DashboardView binds to MainViewModel properties.

- [ ] **Step 1: Write failing test** – verify MainViewModel.Title == "Dashboard" and has `ScanCommand`.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Add `INavigableViewModel` implementation**; move UI-only properties (Drives, LogEntries) to DashboardView; keep core Scan/Clean logic.

- [ ] **Step 4: Create DashboardView.xaml** with `UniformGrid` of `Card` controls using `DynamicResource` for margins.

- [ ] **Step 5: Run test → PASS**

- [ ] **Step 6: Commit**