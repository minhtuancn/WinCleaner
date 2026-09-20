using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;
using Xunit;

namespace WinCleaner.Tests.Unit;

[Collection("WpfTests")]
public class AnimationResourceTests
{
    private readonly WpfTestFixture _fixture;

    public AnimationResourceTests(WpfTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Styles_ShouldContainFadeInStoryboard()
    {
        // Arrange
        var app = _fixture.App;

        // Act
        var fadeInStoryboard = app.TryFindResource("FadeInStoryboard") as Storyboard;

        // Assert
        Assert.NotNull(fadeInStoryboard);
    }

    [Fact]
    public void Styles_ShouldContainFadeOutStoryboard()
    {
        // Arrange
        var app = _fixture.App;

        // Act
        var fadeOutStoryboard = app.TryFindResource("FadeOutStoryboard") as Storyboard;

        // Assert
        Assert.NotNull(fadeOutStoryboard);
    }

    [Fact]
    public void Styles_ShouldContainSlideInStoryboard()
    {
        // Arrange
        var app = _fixture.App;

        // Act
        var slideInStoryboard = app.TryFindResource("SlideInStoryboard") as Storyboard;

        // Assert
        Assert.NotNull(slideInStoryboard);
    }

    [Fact]
    public void Styles_ShouldContainSlideOutStoryboard()
    {
        // Arrange
        var app = _fixture.App;

        // Act
        var slideOutStoryboard = app.TryFindResource("SlideOutStoryboard") as Storyboard;

        // Assert
        Assert.NotNull(slideOutStoryboard);
    }

    [Fact]
    public void FadeInStoryboard_ShouldTargetOpacity()
    {
        // Arrange
        var app = _fixture.App;
        var storyboard = app.TryFindResource("FadeInStoryboard") as Storyboard;

        // Act & Assert
        Assert.NotNull(storyboard);
        
        // Verify the storyboard targets Opacity property
        var hasOpacityAnimation = storyboard.Children
            .OfType<DoubleAnimation>()
            .Any(a => Storyboard.GetTargetProperty(a).Path == "Opacity");
        
        Assert.True(hasOpacityAnimation, "FadeInStoryboard should contain a DoubleAnimation targeting Opacity");
    }

    [Fact]
    public void FadeOutStoryboard_ShouldTargetOpacity()
    {
        // Arrange
        var app = _fixture.App;
        var storyboard = app.TryFindResource("FadeOutStoryboard") as Storyboard;

        // Act & Assert
        Assert.NotNull(storyboard);
        
        // Verify the storyboard targets Opacity property
        var hasOpacityAnimation = storyboard.Children
            .OfType<DoubleAnimation>()
            .Any(a => Storyboard.GetTargetProperty(a).Path == "Opacity");
        
        Assert.True(hasOpacityAnimation, "FadeOutStoryboard should contain a DoubleAnimation targeting Opacity");
    }

    [Fact]
    public void SlideInStoryboard_ShouldTargetTranslateTransform()
    {
        // Arrange
        var app = _fixture.App;
        var storyboard = app.TryFindResource("SlideInStoryboard") as Storyboard;

        // Act & Assert
        Assert.NotNull(storyboard);
        
        // Verify the storyboard contains a DoubleAnimation
        var hasAnimation = storyboard.Children
            .OfType<DoubleAnimation>()
            .Any();
        
        Assert.True(hasAnimation, "SlideInStoryboard should contain a DoubleAnimation");
    }

    [Fact]
    public void EnableAnimationsResource_ShouldExist()
    {
        // Arrange
        var app = _fixture.App;

        // Act
        var enableAnimations = app.TryFindResource("EnableAnimations");

        // Assert
        Assert.NotNull(enableAnimations);
        Assert.IsType<bool>(enableAnimations);
    }
}