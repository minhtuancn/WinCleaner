### Task 9: Settings View (WPF ColorPicker, Localization, Theme, Updates)

**Files:**
- Create: `WinCleaner-classic/ViewModels/SettingsViewModel.cs` (replace WinForms ColorDialog)
- Create: `WinCleaner-classic/Views/SettingsView.xaml`
- Modify: `WinCleaner-classic/Services/SettingsService.cs` (persist new settings)

**Interfaces:**
- SettingsViewModel: `ICommand PickAccentColorCommand` (opens WPF ColorPicker dialog), `ObservableCollection<string> Languages`, `string SelectedLanguage`, `bool AutoCheckUpdates`, `UpdateChannel SelectedChannel`.

- [ ] **Step 1: Write failing test** – verify SettingsViewModel loads/saves accent color, language, update channel.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement SettingsViewModel** – use `Microsoft.Toolkit.Wpf.UI.Controls.ColorPicker` (NuGet) or custom HSV picker.

- [ ] **Step 4: Build SettingsView.xaml** – grouped sections (Appearance, Updates, Advanced) with Expander, bind all to DynamicResources.

- [ ] **Step 5: Run test → PASS**

- [ ] **Step 6: Commit**