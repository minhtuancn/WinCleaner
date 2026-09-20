using System.Reflection;
using Xunit;

namespace WinCleaner.Tests.Unit;

public class DesignTokensTests
{
    [Fact]
    public void DesignTokens_Exist()
    {
        var asm = Assembly.Load("WinCleaner");
        Assert.NotNull(asm.GetType("WinCleaner.Design.ColorPalette"));
        Assert.NotNull(asm.GetType("WinCleaner.Design.Typography"));
        Assert.NotNull(asm.GetType("WinCleaner.Design.Spacing"));
        Assert.NotNull(asm.GetType("WinCleaner.Design.Icons"));
    }
}