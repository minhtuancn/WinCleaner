using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace WinCleaner.Tests.Unit;

[Collection("WpfTests")]
public class AccessibilityTests
{
    private readonly string _viewsPath = FindViewsPath();

    private static string FindViewsPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        
        // Navigate up to solution root
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WinCleaner.sln")))
        {
            dir = dir.Parent;
        }
        
        if (dir == null)
            throw new DirectoryNotFoundException("Could not find solution root");
        
        var viewsPath = Path.Combine(dir.FullName, "src", "WinCleaner.App", "Views");
        if (!Directory.Exists(viewsPath))
            throw new DirectoryNotFoundException($"Views directory not found: {viewsPath}");
        
        return viewsPath;
    }

    private static readonly XNamespace AutomationProps = "http://schemas.microsoft.com/winfx/2006/xaml/presentation/AutomationProperties";

    private static bool HasAutomationName(XElement element)
    {
        // Check for AutomationProperties.Name attribute (with namespace)
        if (element.Attribute(AutomationProps + "Name") != null)
            return true;
        
        // Check for AutomationProperties.Name attribute (with prefix, as written in XAML)
        foreach (var attr in element.Attributes())
        {
            if (attr.Name.LocalName == "Name" && attr.Name.NamespaceName == AutomationProps.NamespaceName)
                return true;
            if (attr.Name.ToString().EndsWith("AutomationProperties.Name", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        // Check for x:Name or Name
        if (element.Attribute("Name") != null || element.Attribute(XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml")) != null)
            return true;
        
        return false;
    }

    private static bool HasAutomationHelpText(XElement element)
    {
        // Check for AutomationProperties.HelpText attribute (with namespace)
        if (element.Attribute(AutomationProps + "HelpText") != null)
            return true;
        
        // Check for AutomationProperties.HelpText attribute (with prefix, as written in XAML)
        foreach (var attr in element.Attributes())
        {
            if (attr.Name.LocalName == "HelpText" && attr.Name.NamespaceName == AutomationProps.NamespaceName)
                return true;
            if (attr.Name.ToString().EndsWith("AutomationProperties.HelpText", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }

    [Fact]
    public void All_Interactive_Elements_Should_Have_AutomationProperties_Name()
    {
        var xamlFiles = Directory.GetFiles(_viewsPath, "*.xaml");
        var missingAutomationProperties = new List<string>();

        var interactiveElementTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Button", "CheckBox", "ComboBox", "TextBox", "Slider", "RadioButton",
            "TabItem", "ListViewItem", "TreeViewItem", "MenuItem", "ToggleButton",
            "RepeatButton", "Hyperlink", "Expander", "GroupBox", "ListBox", "ListView", "TreeView", "TabControl"
        };

        foreach (var file in xamlFiles)
        {
            var fileName = Path.GetFileName(file);
            var content = File.ReadAllText(file);
            var doc = XDocument.Parse(content);

            var interactiveElements = doc.Descendants()
                .Where(e => interactiveElementTypes.Contains(e.Name.LocalName))
                .ToList();

            foreach (var element in interactiveElements)
            {
                if (!HasAutomationName(element))
                {
                    var lineInfo = element as IXmlLineInfo;
                    var line = lineInfo?.LineNumber ?? 0;
                    missingAutomationProperties.Add($"{fileName}: Line {line} - <{element.Name.LocalName}> missing AutomationProperties.Name");
                }
            }
        }

        Assert.Empty(missingAutomationProperties);
    }

    [Fact]
    public void All_Interactive_Elements_Should_Have_AutomationProperties_HelpText()
    {
        var xamlFiles = Directory.GetFiles(_viewsPath, "*.xaml");
        var missingHelpText = new List<string>();

        var interactiveElementTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Button", "CheckBox", "ComboBox", "TextBox", "Slider", "RadioButton",
            "TabItem", "ListViewItem", "TreeViewItem", "MenuItem", "ToggleButton",
            "RepeatButton", "Hyperlink", "Expander", "GroupBox", "ListBox", "ListView", "TreeView", "TabControl"
        };

        foreach (var file in xamlFiles)
        {
            var fileName = Path.GetFileName(file);
            var content = File.ReadAllText(file);
            var doc = XDocument.Parse(content);

            var interactiveElements = doc.Descendants()
                .Where(e => interactiveElementTypes.Contains(e.Name.LocalName))
                .ToList();

            foreach (var element in interactiveElements)
            {
                if (!HasAutomationHelpText(element))
                {
                    var lineInfo = element as IXmlLineInfo;
                    var line = lineInfo?.LineNumber ?? 0;
                    missingHelpText.Add($"{fileName}: Line {line} - <{element.Name.LocalName}> missing AutomationProperties.HelpText");
                }
            }
        }

        Assert.Empty(missingHelpText);
    }
}