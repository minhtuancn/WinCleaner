using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ICustomRuleService
    {
        Task<List<CustomCleanerRule>> GetRulesAsync();
        Task<CustomCleanerRule?> GetRuleAsync(string id);
        Task<bool> AddRuleAsync(CustomCleanerRule rule);
        Task<bool> UpdateRuleAsync(CustomCleanerRule rule);
        Task<bool> DeleteRuleAsync(string id);
        Task<bool> EnableRuleAsync(string id, bool enabled);
        Task<bool> ImportRulesAsync(string filePath);
        Task<bool> ExportRulesAsync(string filePath, List<CustomCleanerRule>? rules = null);
        Task<List<CustomCleanerRule>> GetTemplatesAsync();
        Task<CustomCleanerRule> CreateFromTemplateAsync(string templateName);
        List<CustomCleanerRule> ConvertToCleanItems(List<CustomCleanerRule> rules);
    }

    public class CustomRuleService : ICustomRuleService
    {
        private readonly string _rulesDirectory;
        private readonly string _rulesFilePath;
        private List<CustomCleanerRule> _rules = new();
        private readonly object _lock = new();

        public CustomRuleService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _rulesDirectory = Path.Combine(appData, "WinCleaner", "CustomRules");
            _rulesFilePath = Path.Combine(_rulesDirectory, "custom_rules.json");
            Directory.CreateDirectory(_rulesDirectory);
            
            _ = LoadRulesAsync();
        }

        private async Task LoadRulesAsync()
        {
            if (File.Exists(_rulesFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(_rulesFilePath);
                    var rules = JsonSerializer.Deserialize<List<CustomCleanerRule>>(json, GetJsonOptions());
                    lock (_lock)
                    {
                        _rules = rules ?? new List<CustomCleanerRule>();
                    }
                }
                catch
                {
                    lock (_lock)
                    {
                        _rules = new List<CustomCleanerRule>();
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    _rules = new List<CustomCleanerRule>();
                }
            }
        }

        private async Task SaveRulesAsync()
        {
            List<CustomCleanerRule> rulesCopy;
            lock (_lock)
            {
                rulesCopy = _rules.ToList();
            }
            
            string json = JsonSerializer.Serialize(rulesCopy, GetJsonOptions());
            await File.WriteAllTextAsync(_rulesFilePath, json);
        }

        public async Task<List<CustomCleanerRule>> GetRulesAsync()
        {
            List<CustomCleanerRule> rulesCopy;
            lock (_lock)
            {
                rulesCopy = _rules.OrderBy(r => r.Category).ThenBy(r => r.Name).ToList();
            }
            return rulesCopy;
        }

        public async Task<CustomCleanerRule?> GetRuleAsync(string id)
        {
            lock (_lock)
            {
                return _rules.FirstOrDefault(r => r.Id == id);
            }
        }

        public async Task<bool> AddRuleAsync(CustomCleanerRule rule)
        {
            rule.Id = Guid.NewGuid().ToString();
            rule.CreatedDate = DateTime.Now;
            rule.ModifiedDate = DateTime.Now;
            rule.Version = 1;
            
            lock (_lock)
            {
                _rules.Add(rule);
            }
            
            await SaveRulesAsync();
            return true;
        }

        public async Task<bool> UpdateRuleAsync(CustomCleanerRule rule)
        {
            bool found = false;
            lock (_lock)
            {
                var existing = _rules.FirstOrDefault(r => r.Id == rule.Id);
                if (existing == null) return false;

                existing.Name = rule.Name;
                existing.Description = rule.Description;
                existing.Category = rule.Category;
                existing.Type = rule.Type;
                existing.Action = rule.Action;
                existing.Path = rule.Path;
                existing.Pattern = rule.Pattern;
                existing.Recursive = rule.Recursive;
                existing.Exclude = rule.Exclude;
                existing.Condition = rule.Condition;
                existing.ConditionValue = rule.ConditionValue;
                existing.Enabled = rule.Enabled;
                existing.RiskLevel = rule.RiskLevel;
                existing.Author = rule.Author;
                existing.Tags = rule.Tags;
                existing.ModifiedDate = DateTime.Now;
                existing.Version++;
                found = true;
            }
            
            if (found)
                await SaveRulesAsync();
            return found;
        }

        public async Task<bool> DeleteRuleAsync(string id)
        {
            bool removed = false;
            lock (_lock)
            {
                var rule = _rules.FirstOrDefault(r => r.Id == id);
                if (rule == null) return false;

                _rules.Remove(rule);
                removed = true;
            }
            
            if (removed)
                await SaveRulesAsync();
            return removed;
        }

        public async Task<bool> EnableRuleAsync(string id, bool enabled)
        {
            bool found = false;
            lock (_lock)
            {
                var rule = _rules.FirstOrDefault(r => r.Id == id);
                if (rule == null) return false;

                rule.Enabled = enabled;
                rule.ModifiedDate = DateTime.Now;
                rule.Version++;
                found = true;
            }
            
            if (found)
                await SaveRulesAsync();
            return found;
        }

        public async Task<bool> ImportRulesAsync(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var imported = JsonSerializer.Deserialize<List<CustomCleanerRule>>(json, GetJsonOptions());
                
                if (imported != null && imported.Count > 0)
                {
                    lock (_lock)
                    {
                        foreach (var rule in imported)
                        {
                            rule.Id = Guid.NewGuid().ToString();
                            rule.CreatedDate = DateTime.Now;
                            rule.ModifiedDate = DateTime.Now;
                            rule.Version = 1;
                            _rules.Add(rule);
                        }
                    }
                    await SaveRulesAsync();
                    return true;
                }
            }
            catch { }
            return false;
        }

        public async Task<bool> ExportRulesAsync(string filePath, List<CustomCleanerRule>? rules = null)
        {
            try
            {
                var rulesToExport = rules ?? _rules;
                var json = JsonSerializer.Serialize(rulesToExport, GetJsonOptions());
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch { return false; }
        }

        public async Task<List<CustomCleanerRule>> GetTemplatesAsync()
        {
            return new List<CustomCleanerRule>
            {
                CustomRuleTemplate.CreateBrowserCacheTemplate(),
                CustomRuleTemplate.CreateTempFilesTemplate(),
                CustomRuleTemplate.CreateLogFilesTemplate(),
                CustomRuleTemplate.CreateRegistryCleanupTemplate(),
                new CustomCleanerRule
                {
                    Name = "Visual Studio Cache",
                    Description = "Clears Visual Studio build cache",
                    Category = "Development",
                    Type = CustomRuleType.Directory,
                    Action = CustomRuleAction.DeleteDirectory,
                    Path = "%LocalAppData%\\Microsoft\\VisualStudio",
                    Pattern = "*.cache",
                    Recursive = true,
                    RiskLevel = ItemRiskLevel.Low,
                    Tags = new List<string> { "visualstudio", "cache", "development" }
                },
                new CustomCleanerRule
                {
                    Name = "Node.js Cache",
                    Description = "Clears npm and node-gyp cache",
                    Category = "Development",
                    Type = CustomRuleType.Directory,
                    Action = CustomRuleAction.DeleteDirectory,
                    Path = "%LocalAppData%\\npm-cache",
                    Pattern = "*.*",
                    Recursive = true,
                    RiskLevel = ItemRiskLevel.Safe,
                    Tags = new List<string> { "nodejs", "npm", "cache", "development" }
                },
                new CustomCleanerRule
                {
                    Name = "Docker Cleanup",
                    Description = "Removes unused Docker images and containers",
                    Category = "Development",
                    Type = CustomRuleType.File,
                    Action = CustomRuleAction.DeleteFile,
                    Path = "%ProgramData%\\Docker",
                    Pattern = "*.log",
                    Recursive = true,
                    RiskLevel = ItemRiskLevel.Medium,
                    Tags = new List<string> { "docker", "cleanup", "development" }
                },
                new CustomCleanerRule
                {
                    Name = "Game Cache",
                    Description = "Clears game launcher cache",
                    Category = "Games",
                    Type = CustomRuleType.Directory,
                    Action = CustomRuleAction.DeleteDirectory,
                    Path = "%LocalAppData%\\Steam\\appcache",
                    Pattern = "*.*",
                    Recursive = true,
                    RiskLevel = ItemRiskLevel.Low,
                    Tags = new List<string> { "steam", "games", "cache" }
                }
            };
        }

        public async Task<CustomCleanerRule> CreateFromTemplateAsync(string templateName)
        {
            var templates = await GetTemplatesAsync();
            var template = templates.FirstOrDefault(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
            
            if (template == null)
                throw new ArgumentException($"Template not found: {templateName}");

            var rule = new CustomCleanerRule
            {
                Id = Guid.NewGuid().ToString(),
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                Type = template.Type,
                Action = template.Action,
                Path = template.Path,
                Pattern = template.Pattern,
                Recursive = template.Recursive,
                Exclude = template.Exclude,
                Condition = template.Condition,
                ConditionValue = template.ConditionValue,
                RiskLevel = template.RiskLevel,
                Tags = new List<string>(template.Tags),
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                Version = 1,
                Enabled = true
            };

            return rule;
        }

        public List<CustomCleanerRule> ConvertToCleanItems(List<CustomCleanerRule> rules)
        {
            return rules.Where(r => r.Enabled).ToList();
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
        }
    }
}