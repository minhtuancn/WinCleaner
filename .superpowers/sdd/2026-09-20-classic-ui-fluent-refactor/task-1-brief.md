### Task 1: Design System Tokens

**Files:**
- Create: `WinCleaner-classic/Design/ColorPalette.cs`
- Create: `WinCleaner-classic/Design/Typography.cs`
- Create: `WinCleaner-classic/Design/Spacing.cs`
- Create: `WinCleaner-classic/Design/Icons.cs` (vector Geometry from FluentCleaner)
- Modify: `WinCleaner-classic/WinCleaner.csproj` (add Design folder as compile)

**Interfaces:**
- Produces: static classes with `public static readonly` fields used by ResourceDictionaries.

- [ ] **Step 1: Write failing test** – create a test that loads the assembly and verifies each token class exists and has expected members.

```csharp
[Fact]
public void DesignTokens_Exist()
{
    var asm = Assembly.Load("WinCleaner");
    Assert.NotNull(asm.GetType("WinCleaner.Design.ColorPalette"));
    Assert.NotNull(asm.GetType("WinCleaner.Design.Typography"));
    Assert.NotNull(asm.GetType("WinCleaner.Design.Spacing"));
    Assert.NotNull(asm.GetType("WinCleaner.Design.Icons"));
}
```

- [ ] **Step 2: Run test to verify it fails**  
  `dotnet test tests/WinCleaner.Tests.Unit --filter "DesignTokens_Exist"` → FAIL (types missing)

- [ ] **Step 3: Implement token classes** (copy from Modern `src/WinCleaner.App/Design/` and adapt namespace to `WinCleaner.Design`)

```csharp
// ColorPalette.cs
namespace WinCleaner.Design;
public static class ColorPalette
{
    public static readonly Color Primary = Color.FromRgb(0x00, 0x78, 0xD4);
    public static readonly Color PrimaryDark = Color.FromRgb(0x00, 0x5A, 0x9E);
    // ... all Fluent colors
}
```

- [ ] **Step 4: Run test to verify it passes** → PASS

- [ ] **Step 5: Commit**  
  `git add WinCleaner-classic/Design/ tests/WinCleaner.Tests.Unit/DesignTokensTests.cs`  
  `git commit -m "feat: add Fluent design system tokens for Classic"`