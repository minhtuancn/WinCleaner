using System.Linq;
using Xunit;

namespace WinCleaner.Tests.Unit;

[Collection("WpfTests")]
public class ThemeDictionaryTests
{
    private readonly WpfTestFixture _fixture;

    public ThemeDictionaryTests(WpfTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void LightTheme_ContainsColorAndBrushKeys_ButNoSpacingOrCornerRadius()
    {
        var lightDict = _fixture.App.Resources.MergedDictionaries
            .First(d => d.Source?.OriginalString.Contains("Light.xaml") == true);

        var keys = lightDict.Keys.Cast<string>().ToList();

        Assert.Contains("PrimaryColor", keys);
        Assert.Contains("BackgroundBrush", keys);
        Assert.DoesNotContain("MarginXs", keys);
        Assert.DoesNotContain("CornerRadiusNormal", keys);
        Assert.DoesNotContain("ShadowMedium", keys);
    }
}