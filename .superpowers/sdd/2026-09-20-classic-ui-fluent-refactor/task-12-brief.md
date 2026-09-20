### Task 12: Window State Persistence (Sidebar width, selected tab, window size)

**Files:**
- Modify: `WinCleaner-classic/Services/WindowStateService.cs` (extend `WindowStateData`)
- Modify: `ShellWindow.xaml.cs` (save/restore on Loaded/Closing)

**Interfaces:**
- `WindowStateData` adds `double SidebarWidth`, `string SelectedNavItem`, `bool IsSidebarOpen`.

- [ ] **Step 1: Write failing test** – serialize/deserialize WindowStateData, verify round-trip.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Extend WindowStateData & persist in ShellWindow**.

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**