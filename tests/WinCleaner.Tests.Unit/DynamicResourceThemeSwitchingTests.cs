using System.Windows;
using System.Windows.Media;
using Xunit;

namespace WinCleaner.Tests.Unit;

[Collection("WpfTests")]
public class DynamicResourceThemeSwitchingTests
{
    private readonly WpfTestFixture _fixture;

    public DynamicResourceThemeSwitchingTests(WpfTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Theme_Brushes_Are_DynamicResource_Resolvable()
    {
        // Arrange: Get the application resources
        var resources = _fixture.App.Resources;
        var dictionaries = resources.MergedDictionaries;

        // Save the original Light theme dictionary to restore later
        var lightDict = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains("Light.xaml") == true);

        // Act: Switch to Dark theme manually
        if (lightDict != null)
        {
            dictionaries.Remove(lightDict);
        }
        var darkDict = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/WinCleaner;component/Resources/Themes/Dark.xaml", UriKind.Absolute)
        };
        dictionaries.Add(darkDict);

        try
        {
            // Assert: Themeable brushes should be resolvable from the new theme
            // These are the brushes that should update when theme changes
            var primaryBrush = resources["PrimaryBrush"] as SolidColorBrush;
            var backgroundBrush = resources["BackgroundBrush"] as SolidColorBrush;
            var surfaceBrush = resources["SurfaceBrush"] as SolidColorBrush;
            var borderBrush = resources["BorderBrush"] as SolidColorBrush;
            var textPrimaryBrush = resources["TextPrimaryBrush"] as SolidColorBrush;

            Assert.NotNull(primaryBrush);
            Assert.NotNull(backgroundBrush);
            Assert.NotNull(surfaceBrush);
            Assert.NotNull(borderBrush);
            Assert.NotNull(textPrimaryBrush);

            // Verify Dark theme colors (different from Light)
            Assert.Equal(Color.FromRgb(0x1F, 0x1F, 0x1F), backgroundBrush.Color); // Dark BackgroundColor
            Assert.Equal(Color.FromRgb(0x2D, 0x2D, 0x2D), surfaceBrush.Color); // Dark SurfaceColor
            Assert.Equal(Color.FromRgb(0x48, 0x46, 0x44), borderBrush.Color); // Dark BorderColor
            Assert.Equal(Colors.White, textPrimaryBrush.Color); // Dark TextPrimaryColor
        }
        finally
        {
            // Restore Light theme dictionary for other tests
            dictionaries.Remove(darkDict);
            if (lightDict != null)
            {
                dictionaries.Add(lightDict);
            }
        }
    }

    [Fact]
    public void Theme_Spacing_Resources_Are_Resolvable()
    {
        // Arrange: Get the application resources
        var resources = _fixture.App.Resources;

        // Act & Assert: Spacing resources from Styles.xaml should be resolvable
        // Note: Styles.xaml defines its own Margin/Padding values that override SharedThemeResources
        var marginXs = resources["MarginXs"] as Thickness?;
        var marginSm = resources["MarginSm"] as Thickness?;
        var marginMd = resources["MarginMd"] as Thickness?;
        var paddingXs = resources["PaddingXs"] as Thickness?;
        var paddingSm = resources["PaddingSm"] as Thickness?;
        var paddingMd = resources["PaddingMd"] as Thickness?;

        Assert.NotNull(marginXs);
        Assert.NotNull(marginSm);
        Assert.NotNull(marginMd);
        Assert.NotNull(paddingXs);
        Assert.NotNull(paddingSm);
        Assert.NotNull(paddingMd);

        // Verify values from Styles.xaml (not SharedThemeResources)
        Assert.Equal(new Thickness(4), marginXs);
        Assert.Equal(new Thickness(8), marginSm);
        Assert.Equal(new Thickness(16), marginMd); // Styles.xaml defines MarginMd as 16
        Assert.Equal(new Thickness(4), paddingXs);
        Assert.Equal(new Thickness(8), paddingSm);
        Assert.Equal(new Thickness(12), paddingMd); // Styles.xaml defines PaddingMd as 12
    }
}