using System;
using System.IO;
using System.Xml;

class Program
{
    static void Main()
    {
        var filePath = @"D:\dev\setup-ai\WinCleaner\Version.props";
        
        var doc = new XmlDocument();
        var project = doc.CreateElement("Project");
        doc.AppendChild(project);
        
        var propertyGroup = doc.CreateElement("PropertyGroup");
        project.AppendChild(propertyGroup);
        
        AddProperty(doc, propertyGroup, "VersionPrefix", "2.0.0");
        AddProperty(doc, propertyGroup, "VersionSuffix", "beta.1");
        AddProperty(doc, propertyGroup, "Version", "$(VersionPrefix)-$(VersionSuffix)");
        AddProperty(doc, propertyGroup, "AssemblyVersion", "2.0.0.0");
        AddProperty(doc, propertyGroup, "FileVersion", "2.0.0.1");
        AddProperty(doc, propertyGroup, "InformationalVersion", "2.0.0-beta.1+$(GitCommitSha)");
        AddProperty(doc, propertyGroup, "Product", "WinCleaner");
        AddProperty(doc, propertyGroup, "Authors", "WinCleaner Team");
        AddProperty(doc, propertyGroup, "Company", "WinCleaner");
        AddProperty(doc, propertyGroup, "Copyright", "Copyright © 2026 WinCleaner Team");
        AddProperty(doc, propertyGroup, "Description", "Professional, Safe & Free Windows Cleaner");
        AddProperty(doc, propertyGroup, "PackageProjectUrl", "https://github.com/minhtuancn/WinCleaner");
        AddProperty(doc, propertyGroup, "RepositoryUrl", "https://github.com/minhtuancn/WinCleaner.git");
        AddProperty(doc, propertyGroup, "RepositoryType", "git");
        AddProperty(doc, propertyGroup, "PackageTags", "cleaner;windows;optimization;maintenance");
        AddProperty(doc, propertyGroup, "PackageLicenseExpression", "MIT");
        AddProperty(doc, propertyGroup, "PublishSingleFile", "true");
        AddProperty(doc, propertyGroup, "SelfContained", "false");
        AddProperty(doc, propertyGroup, "RuntimeIdentifier", "win-x64");
        AddProperty(doc, propertyGroup, "PlatformTarget", "x64");
        
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = System.Text.Encoding.UTF8
        };
        
        using (var writer = XmlWriter.Create(filePath, settings))
        {
            doc.Save(writer);
        }
        
        Console.WriteLine("Version.props written with proper XML escaping");
    }
    
    static void AddProperty(XmlDocument doc, XmlElement parent, string name, string value)
    {
        var element = doc.CreateElement(name);
        element.InnerText = value;
        parent.AppendChild(element);
    }
}