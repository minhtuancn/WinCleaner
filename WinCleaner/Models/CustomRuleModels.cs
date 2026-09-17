using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public enum CustomRuleType
    {
        File,
        Registry,
        Directory
    }

    public enum CustomRuleAction
    {
        DeleteFile,
        DeleteDirectory,
        DeleteRegistryKey,
        DeleteRegistryValue,
        EmptyDirectory
    }

    public enum CustomRuleCondition
    {
        Always,
        FileExists,
        DirectoryExists,
        RegistryKeyExists,
        RegistryValueExists,
        FileOlderThan,
        FileLargerThan,
        CustomScript
    }

    public class CustomCleanerRule : ObservableObject
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private string _description = "";
        private string _category = "Custom";
        private CustomRuleType _type = CustomRuleType.File;
        private CustomRuleAction _action = CustomRuleAction.DeleteFile;
        private string _path = "";
        private string _pattern = "*.*";
        private bool _recursive = false;
        private string _exclude = "";
        private CustomRuleCondition _condition = CustomRuleCondition.Always;
        private string _conditionValue = "";
        private bool _enabled = true;
        private ItemRiskLevel _riskLevel = ItemRiskLevel.Medium;
        private string _author = "";
        private DateTime _createdDate = DateTime.Now;
        private DateTime _modifiedDate = DateTime.Now;
        private int _version = 1;
        private List<string> _tags = new();

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public CustomRuleType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public CustomRuleAction Action
        {
            get => _action;
            set => SetProperty(ref _action, value);
        }

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public string Pattern
        {
            get => _pattern;
            set => SetProperty(ref _pattern, value);
        }

        public bool Recursive
        {
            get => _recursive;
            set => SetProperty(ref _recursive, value);
        }

        public string Exclude
        {
            get => _exclude;
            set => SetProperty(ref _exclude, value);
        }

        public CustomRuleCondition Condition
        {
            get => _condition;
            set => SetProperty(ref _condition, value);
        }

        public string ConditionValue
        {
            get => _conditionValue;
            set => SetProperty(ref _conditionValue, value);
        }

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        public ItemRiskLevel RiskLevel
        {
            get => _riskLevel;
            set => SetProperty(ref _riskLevel, value);
        }

        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public DateTime ModifiedDate
        {
            get => _modifiedDate;
            set => SetProperty(ref _modifiedDate, value);
        }

        public int Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public List<string> Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        public string TypeDisplayName => Type switch
        {
            CustomRuleType.File => "File",
            CustomRuleType.Registry => "Registry",
            CustomRuleType.Directory => "Directory",
            _ => Type.ToString()
        };

        public string ActionDisplayName => Action switch
        {
            CustomRuleAction.DeleteFile => "Delete File",
            CustomRuleAction.DeleteDirectory => "Delete Directory",
            CustomRuleAction.DeleteRegistryKey => "Delete Registry Key",
            CustomRuleAction.DeleteRegistryValue => "Delete Registry Value",
            CustomRuleAction.EmptyDirectory => "Empty Directory",
            _ => Action.ToString()
        };
    }

    public class CustomRuleCollection : ObservableObject
    {
        private List<CustomCleanerRule> _rules = new();
        private string _name = "Custom Rules";
        private string _description = "";
        private string _author = "";
        private DateTime _createdDate = DateTime.Now;
        private DateTime _modifiedDate = DateTime.Now;
        private int _version = 1;

        public List<CustomCleanerRule> Rules
        {
            get => _rules;
            set => SetProperty(ref _rules, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public DateTime ModifiedDate
        {
            get => _modifiedDate;
            set => SetProperty(ref _modifiedDate, value);
        }

        public int Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public int RuleCount => _rules.Count;
        public int EnabledRuleCount => _rules.Count(r => r.Enabled);
    }

    public class CustomRuleTemplate
    {
        public static CustomCleanerRule CreateBrowserCacheTemplate()
        {
            return new CustomCleanerRule
            {
                Name = "Browser Cache",
                Description = "Clears browser cache files",
                Category = "Browser",
                Type = CustomRuleType.File,
                Action = CustomRuleAction.DeleteFile,
                Path = "%LocalAppData%\\Google\\Chrome\\User Data\\Default\\Cache",
                Pattern = "*.*",
                Recursive = true,
                RiskLevel = ItemRiskLevel.Safe,
                Tags = new List<string> { "browser", "cache", "chrome" }
            };
        }

        public static CustomCleanerRule CreateTempFilesTemplate()
        {
            return new CustomCleanerRule
            {
                Name = "Temporary Files",
                Description = "Clears temporary files older than 7 days",
                Category = "System",
                Type = CustomRuleType.File,
                Action = CustomRuleAction.DeleteFile,
                Path = "%TEMP%",
                Pattern = "*.tmp",
                Recursive = true,
                Condition = CustomRuleCondition.FileOlderThan,
                ConditionValue = "7",
                RiskLevel = ItemRiskLevel.Safe,
                Tags = new List<string> { "temp", "system", "cleanup" }
            };
        }

        public static CustomCleanerRule CreateLogFilesTemplate()
        {
            return new CustomCleanerRule
            {
                Name = "Log Files",
                Description = "Clears log files older than 30 days",
                Category = "Logs",
                Type = CustomRuleType.File,
                Action = CustomRuleAction.DeleteFile,
                Path = "%LocalAppData%\\Logs",
                Pattern = "*.log",
                Recursive = true,
                Condition = CustomRuleCondition.FileOlderThan,
                ConditionValue = "30",
                RiskLevel = ItemRiskLevel.Low,
                Tags = new List<string> { "logs", "cleanup" }
            };
        }

        public static CustomCleanerRule CreateRegistryCleanupTemplate()
        {
            return new CustomCleanerRule
            {
                Name = "Registry Cleanup",
                Description = "Removes empty registry keys",
                Category = "Registry",
                Type = CustomRuleType.Registry,
                Action = CustomRuleAction.DeleteRegistryKey,
                Path = "HKCU\\Software\\MyApp",
                Condition = CustomRuleCondition.RegistryKeyExists,
                RiskLevel = ItemRiskLevel.Medium,
                Tags = new List<string> { "registry", "cleanup" }
            };
        }
    }
}