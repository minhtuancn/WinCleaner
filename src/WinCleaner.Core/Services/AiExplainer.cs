using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IAiExplainer
    {
        Task<string> ExplainRuleAsync(Winapp2Entry entry, CancellationToken cancellationToken = default);
        Task<string> SummarizePlanAsync(List<CleanupPlanStep> steps, CancellationToken cancellationToken = default);
    }

    public class AiExplainer : IAiExplainer
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _apiKey;

        public AiExplainer(HttpClient httpClient, string endpoint = "", string apiKey = "")
        {
            _httpClient = httpClient;
            _endpoint = endpoint;
            _apiKey = apiKey;
        }

        public async Task<string> ExplainRuleAsync(Winapp2Entry entry, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_apiKey))
            {
                return GenerateLocalExplanation(entry);
            }

            try
            {
                var prompt = BuildExplanationPrompt(entry);
                var response = await CallLlmAsync(prompt, cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI explainer failed: {ex.Message}");
                return GenerateLocalExplanation(entry);
            }
        }

        public async Task<string> SummarizePlanAsync(List<CleanupPlanStep> steps, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_apiKey))
            {
                return GenerateLocalSummary(steps);
            }

            try
            {
                var prompt = BuildSummaryPrompt(steps);
                var response = await CallLlmAsync(prompt, cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI summarizer failed: {ex.Message}");
                return GenerateLocalSummary(steps);
            }
        }

        private string GenerateLocalExplanation(Winapp2Entry entry)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{entry.Name}** ({entry.Section})");
            sb.AppendLine();
            sb.AppendLine(entry.Warning);
            sb.AppendLine();
            sb.AppendLine("**What will be cleaned:**");
            foreach (var fk in entry.FileKeys)
            {
                sb.AppendLine($"- Files: {fk.Path} ({fk.Pattern}) {(fk.Recurse ? "[recursive]" : "")}");
            }
            foreach (var rk in entry.RegKeys)
            {
                sb.AppendLine($"- Registry: {rk.Key} ({rk.Action})");
            }
            if (entry.ExcludeKeys.Count > 0)
            {
                sb.AppendLine("**Exclusions:**");
                foreach (var ek in entry.ExcludeKeys)
                {
                    sb.AppendLine($"- {ek.Path} ({ek.Pattern})");
                }
            }
            sb.AppendLine();
            sb.AppendLine($"**Risk level:** {entry.RiskLevel}");
            sb.AppendLine($"**Default:** {(entry.Default ? "Enabled" : "Disabled")}");
            return sb.ToString();
        }

        private string GenerateLocalSummary(List<CleanupPlanStep> steps)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Cleanup plan contains {steps.Count} step(s):");
            sb.AppendLine();
            foreach (var step in steps)
            {
                sb.AppendLine($"- **{step.Category}**: 1 item(s), ~{FormatBytes(step.SizeBytes)}");
            }
            var totalSize = steps.Sum(s => s.SizeBytes);
            sb.AppendLine();
            sb.AppendLine($"**Total estimated space to free:** {FormatBytes(totalSize)}");
            return sb.ToString();
        }

        private string BuildExplanationPrompt(Winapp2Entry entry)
        {
            return $"Explain in simple terms what the following WinCleaner rule does, what files/registry keys it touches, and any risks. Rule: {entry.Name}. Section: {entry.Section}. Warning: {entry.Warning}. FileKeys: {string.Join(", ", entry.FileKeys.Select(f => f.Path))}. RegKeys: {string.Join(", ", entry.RegKeys.Select(r => r.Key))}. ExcludeKeys: {string.Join(", ", entry.ExcludeKeys.Select(e => e.Path))}. Risk: {entry.RiskLevel}.";
        }

        private string BuildSummaryPrompt(List<CleanupPlanStep> steps)
        {
            var desc = string.Join("; ", steps.Select(s => $"{s.Category}: 1 item"));
            return $"Summarize this cleanup plan for a non-technical user: {desc}. Total estimated size: {FormatBytes(steps.Sum(s => s.SizeBytes))}.";
        }

        private async Task<string> CallLlmAsync(string prompt, CancellationToken cancellationToken)
        {
            // Placeholder for actual HTTP call to LLM endpoint
            // Example using OpenAI-compatible API
            var request = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant explaining Windows cleanup rules." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 300,
                temperature = 0.3
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var respJson = await response.Content.ReadAsStringAsync(cancellationToken);
            // parse response (simplified)
            using var doc = System.Text.Json.JsonDocument.Parse(respJson);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblBytes = bytes;
            while (dblBytes >= 1024 && i < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                i++;
            }
            return $"{dblBytes:0.##} {suffixes[i]}";
        }
    }
}