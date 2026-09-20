### Task 7: Cleaner View (Virtualized TreeView)

**Files:**
- Create: `WinCleaner-classic/ViewModels/CleanerViewModel.cs`
- Create: `WinCleaner-classic/Views/CleanerView.xaml`
- Modify: `WinCleaner-classic/ViewModels/MainViewModel.cs` (extract scan/clean logic to CleanerViewModel)

**Interfaces:**
- `CleanerViewModel` : `ObservableCollection<CleanCategoryGroup> Categories`, `CleanProfile SelectedProfile`, `ICommand ScanCommand`, `ICommand CleanCommand`, `ICommand SelectSafeCommand`.
- TreeView uses `VirtualizingStackPanel.IsVirtualizing="True"` and `VirtualizingStackPanel.VirtualizationMode="Recycling"`.

- [ ] **Step 1: Write failing test** – verify CleanerViewModel loads profiles, ScanCommand populates Categories.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement CleanerViewModel** (reuse `ISystemScanner`, `ICleanerService` from DI).

- [ ] **Step 4: Build CleanerView.xaml** – Toolbar (Profile ComboBox, SearchBox, Select buttons) + TreeView with `HierarchicalDataTemplate` for groups/items, checkbox binding, risk badge.

- [ ] **Step 6: Run test → PASS**

- [ ] **Step 7: Commit**