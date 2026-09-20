### Task 4: Styles.xaml – Switch StaticResource → DynamicResource for themeable values

**Files:**
- Modify: `WinCleaner-classic/Resources/Styles.xaml` (lines referencing Margin, Padding, Brushes)

**Interfaces:**
- Consumes: keys from SharedThemeResources + Light/Dark.
- Produces: styles that update at runtime when theme changes.

- [ ] **Step 1: Write failing test** – create a Window with a styled Button, switch theme via `ThemeService`, assert button Margin updates.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Replace all `StaticResource Margin*` / `Padding*` / `Brush*` with `DynamicResource`** in Styles.xaml (search/replace).

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**