using System.Reflection;
using System.Windows;
using Xunit;

namespace WinCleaner.Tests.Unit;

[CollectionDefinition("WpfTests")]
public class WpfTestCollection : ICollectionFixture<WpfTestFixture>
{
}

public class WpfTestFixture : IDisposable
{
    public Application App { get; }

    public WpfTestFixture()
    {
        var asm = Assembly.Load("WinCleaner");
        var appType = asm.GetType("WinCleaner.App")!;
        App = (Application)Activator.CreateInstance(appType)!;
        appType.GetMethod("InitializeComponent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(App, null);
    }

    public void Dispose()
    {
        App?.Shutdown();
    }
}