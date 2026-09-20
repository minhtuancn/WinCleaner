### Task 10: Animations & Micro-interactions

**Files:**
- Modify: `WinCleaner-classic/Resources/Styles.xaml` (add `Storyboard` resources for FadeIn, SlideIn)
- Modify: ShellWindow.xaml (sidebar slide, content cross-fade)

**Interfaces:**
- Consumes: `EnableAnimations` bool from ThemeService.

- [ ] **Step 1: Write failing test** – UI automation: click nav item, verify content opacity animation runs (use `FlaUI`).

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Define `FadeIn`/`FadeOut` Storyboards** targeting `ContentControl.Opacity`; trigger via `DataTrigger` on `CurrentView` change.

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**