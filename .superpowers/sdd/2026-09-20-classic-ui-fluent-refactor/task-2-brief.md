### Task 2: Shared Theme Resources (Common Margins, Padding, CornerRadius, Shadows)

**Files:**
- Create: `WinCleaner-classic/Resources/SharedThemeResources.xaml`
- Modify: `WinCleaner-classic/App.xaml` (insert SharedThemeResources before Styles.xaml)

**Interfaces:**
- Produces: ResourceDictionary with keys `MarginXs`, `MarginSm`, `MarginMd`, `MarginLg`, `MarginXl`, `PaddingXs`…`CornerRadiusSmall`…`ShadowSmall`…

- [ ] **Step 1: Write failing test** – load Application resources and assert keys exist.

```csharp
[Fact]
public void SharedThemeResources_ContainsRequiredKeys()
{
    var app = new App();
    app.InitializeComponent();
    var rd = app.Resources.MergedDictionaries
        .First(d => d.Source?.OriginalString.Contains("SharedThemeResources") == true);
    Assert.Contains("MarginXs", rd.Keys.Cast<string>());
    Assert.Contains("CornerRadiusNormal", rd.Keys.Cast<string>());
    Assert.Contains("ShadowMedium", rd.Keys.Cast<string>());
}
```

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement SharedThemeResources.xaml** (copy from Modern, ensure all keys used in Styles.xaml)

```xml
<ResourceDictionary ...>
  <Thickness x:Key="MarginXs">4</Thickness>
  <Thickness x:Key="MarginSm">8</Thickness>
  <Thickness x:Key="MarginMd">12</Thickness>
  <Thickness x:Key="MarginLg">16</Thickness>
  <Thickness x:Key="MarginXl">24</Thickness>
  <CornerRadius x:Key="CornerRadiusSmall">2</CornerRadius>
  <CornerRadius x:Key="CornerRadiusNormal">4</CornerRadius>
  <CornerRadius x:Key="CornerRadiusMedium">6</CornerRadius>
  <CornerRadius x:Key="CornerRadiusLarge">8</CornerRadius>
  <DropShadowEffect x:Key="ShadowSmall" BlurRadius="4" ShadowDepth="1" Opacity="0.1" Color="Black"/>
  <DropShadowEffect x:Key="ShadowMedium" BlurRadius="8" ShadowDepth="2" Opacity="0.15" Color="Black"/>
  <DropShadowEffect x:Key="ShadowLarge" BlurRadius="16" ShadowDepth="4" Opacity="0.2" Color="Black"/>
</ResourceDictionary>
```

- [ ] **Step 4: Update App.xaml merge order**  

```xml
<ResourceDictionary.MergedDictionaries>
  <ResourceDictionary Source="Resources/Converters.xaml"/>
  <ResourceDictionary Source="Resources/SharedThemeResources.xaml"/>
  <ResourceDictionary Source="Resources/Styles.xaml"/>
  <ResourceDictionary Source="Resources/Themes/Light.xaml"/>
  <ResourceDictionary Source="Resources/Themes/Dark.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

- [ ] **Step 5: Run test → PASS**

- [ ] **Step 6: Commit**