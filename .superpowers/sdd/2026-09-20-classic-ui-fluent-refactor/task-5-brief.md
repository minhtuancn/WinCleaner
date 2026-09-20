### Task 5: ShellViewModel & ShellWindow (Navigation Host)

**Files:**
- Create: `WinCleaner-classic/ViewModels/ShellViewModel.cs`
- Create: `WinCleaner-classic/Views/ShellWindow.xaml` + `.cs`
- Modify: `WinCleaner-classic/App.xaml.cs` (startup → show ShellWindow, init ThemeService)

**Interfaces:**
- `ShellViewModel` exposes `ObservableCollection<NavItem> NavigationItems`, `INavigableViewModel CurrentView`, `ICommand NavigateCommand`, `ICommand ToggleSidebarCommand`, `ICommand ToggleThemeCommand`.
- `NavItem` : `Label`, `Icon` (Geometry), `ViewModelType`.
- `INavigableViewModel` : `string Title { get; }`, `Geometry Icon { get; }`, `bool IsSelected { get; set; }`.

- [ ] **Step 1: Write failing test** – instantiate ShellViewModel, verify NavigationItems count = 4 (Dashboard, Cleaner, Tools, Settings), verify NavigateCommand changes CurrentView.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement ShellViewModel** (mirror Modern `ShellViewModel.cs` but with Classic view models). Register views via `DataTemplate` in ShellWindow.Resources.

- [ ] **Step 4: Implement ShellWindow.xaml** – Grid with Sidebar (ListBox bound to NavigationItems) + ContentControl bound to CurrentView.

- [ ] **Step 5: Update App.xaml.cs** – resolve `IThemeService`, `await InitializeAsync()`, then `new ShellWindow { DataContext = shellVm }.Show();`

- [ ] **Step 6: Run test → PASS**

- [ ] **Step 7: Commit**