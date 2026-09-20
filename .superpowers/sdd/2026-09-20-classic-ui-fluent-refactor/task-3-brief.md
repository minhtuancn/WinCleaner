### Task 3: Light / Dark Theme Dictionaries (Color + Brush only)

**Files:**
- Modify: `WinCleaner-classic/Resources/Themes/Light.xaml`
- Modify: `WinCleaner-classic/Resources/Themes/Dark.xaml`

**Interfaces:**
- Each dict defines only `Color` and `SolidColorBrush` keys (PrimaryColor, BackgroundBrush, TextPrimaryBrush, etc.) – **no** spacing/corner/shadow keys.

- [ ] **Step 1: Write failing test** – verify Light.xaml has `PrimaryColor` and no `MarginXs`.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Strip spacing/corner/shadow from both theme files** (keep only colors/brushes/fonts).

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**