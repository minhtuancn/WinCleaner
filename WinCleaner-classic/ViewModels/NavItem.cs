using System;
using System.Windows.Media;

namespace WinCleaner.ViewModels;

public sealed class NavItem
{
    public string Label { get; }
    public Geometry Icon { get; }
    public Type ViewModelType { get; }

    public NavItem(string label, Geometry icon, Type viewModelType)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        ViewModelType = viewModelType ?? throw new ArgumentNullException(nameof(viewModelType));
    }
}