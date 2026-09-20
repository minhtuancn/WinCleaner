using System.Linq;
using System.Reflection;
using System.Windows;
using Xunit;

namespace WinCleaner.Tests.Unit;

public class SharedThemeResourcesTests
{
    [Fact]
    public void SharedThemeResources_ContainsRequiredKeys()
    {
        var asm = Assembly.Load("WinCleaner");
        var appType = asm.GetType("WinCleaner.App");
        Assert.NotNull(appType);
        
        var app = (Application)Activator.CreateInstance(appType)!;
        appType.GetMethod("InitializeComponent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(app, null);
        var rd = app.Resources.MergedDictionaries
            .First(d => d.Source?.OriginalString.Contains("SharedThemeResources") == true);
        Assert.Contains("MarginXs", rd.Keys.Cast<string>());
        Assert.Contains("CornerRadiusNormal", rd.Keys.Cast<string>());
        Assert.Contains("ShadowMedium", rd.Keys.Cast<string>());
    }
}