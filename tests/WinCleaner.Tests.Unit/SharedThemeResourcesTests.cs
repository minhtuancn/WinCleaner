using System.Linq;
using Xunit;

namespace WinCleaner.Tests.Unit;

[Collection("WpfTests")]
public class SharedThemeResourcesTests
{
    private readonly WpfTestFixture _fixture;

    public SharedThemeResourcesTests(WpfTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SharedThemeResources_ContainsRequiredKeys()
    {
        var rd = _fixture.App.Resources.MergedDictionaries
            .First(d => d.Source?.OriginalString.Contains("SharedThemeResources") == true);
        Assert.Contains("MarginXs", rd.Keys.Cast<string>());
        Assert.Contains("CornerRadiusNormal", rd.Keys.Cast<string>());
        Assert.Contains("ShadowMedium", rd.Keys.Cast<string>());
    }
}