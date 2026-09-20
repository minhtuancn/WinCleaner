### Task 11: Accessibility & Localization Polish

**Files:**
- Modify: all Views (add `AutomationProperties.Name`, `AutomationProperties.HelpText`)
- Modify: ViewModels (move any hard-coded strings to `IResourceService`)

**Interfaces:**
- Consumes: `IResourceService.GetString(key)`.

- [ ] **Step 1: Write failing test** – scan XAML for missing `AutomationProperties.Name` on interactive elements.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Add automation properties**; replace Vietnamese literals with resource keys.

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**