### Task 8: Tools View (Scheduler, Cookie, Shred, AppX tabs)

**Files:**
- Create: `WinCleaner-classic/ViewModels/ToolsViewModel.cs`
- Create: `WinCleaner-classic/Views/ToolsView.xaml`
- Create sub-UserControls: `SchedulerTab.xaml`, `CookieTab.xaml`, `ShredTab.xaml`, `AppXTab.xaml`

**Interfaces:**
- ToolsViewModel aggregates four child VMs (SchedulerVM, CookieVM, ShredVM, AppXVM) each implementing `INavigableViewModel` (for tab header).

- [ ] **Step 1: Write failing test** – verify ToolsViewModel exposes four tabs with correct titles/icons.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement each tab VM** (wrap existing services: `TaskSchedulerService`, `CookieService`, `SecureDeleteService`, `AppxService`).

- [ ] **Step 4: Build ToolsView.xaml** – `TabControl` with `ItemTemplate` for icons, content via `ContentTemplateSelector`.

- [ ] **Step 5: Run test → PASS**

- [ ] **Step 6: Commit**