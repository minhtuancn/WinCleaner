using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ICookieService
    {
        Task<List<CookieInfo>> ScanCookiesAsync(CancellationToken cancellationToken = default);
        Task<bool> CleanCookiesAsync(List<CookieInfo> cookies, CancellationToken cancellationToken = default);
    }

    public class CookieInfo
    {
        public string Browser { get; set; } = string.Empty;
        public string Profile { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
        public bool IsSecure { get; set; }
        public bool IsHttpOnly { get; set; }
        public long SizeBytes { get; set; }
    }

    public class CookieService : ICookieService
    {
        private static readonly Dictionary<string, string[]> BrowserCookiePaths = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Chrome", new[] { @"Google\Chrome\User Data\Default\Network\Cookies", @"Google\Chrome\User Data\Profile *\Network\Cookies" } },
            { "Edge", new[] { @"Microsoft\Edge\User Data\Default\Network\Cookies", @"Microsoft\Edge\User Data\Profile *\Network\Cookies" } },
            { "Brave", new[] { @"BraveSoftware\Brave-Browser\User Data\Default\Network\Cookies", @"BraveSoftware\Brave-Browser\User Data\Profile *\Network\Cookies" } },
            { "Opera", new[] { @"Opera Software\Opera Stable\Network\Cookies", @"Opera Software\Opera Stable\Profile *\Network\Cookies" } },
            { "Vivaldi", new[] { @"Vivaldi\User Data\Default\Network\Cookies", @"Vivaldi\User Data\Profile *\Network\Cookies" } }
        };

        public async Task<List<CookieInfo>> ScanCookiesAsync(CancellationToken cancellationToken = default)
        {
            var cookies = new List<CookieInfo>();

            foreach (var (browser, patterns) in BrowserCookiePaths)
            {
                foreach (var pattern in patterns)
                {
                    var expandedPaths = ExpandPaths(pattern);
                    foreach (var cookieDb in expandedPaths)
                    {
                        if (!File.Exists(cookieDb)) continue;

                        try
                        {
                            var browserCookies = await ReadChromeCookiesAsync(cookieDb, browser, cancellationToken);
                            cookies.AddRange(browserCookies);
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue scanning other browsers
                            System.Diagnostics.Debug.WriteLine($"Failed to read cookies from {cookieDb}: {ex.Message}");
                        }
                    }
                }
            }

            return cookies;
        }

        public async Task<bool> CleanCookiesAsync(List<CookieInfo> cookies, CancellationToken cancellationToken = default)
        {
            // Group by database file to avoid multiple connections
            var grouped = cookies.GroupBy(c => GetCookieDbPath(c.Browser, c.Profile));
            foreach (var group in grouped)
            {
                var dbPath = group.Key;
                if (!File.Exists(dbPath)) continue;

                var cookiesToDelete = group.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                try
                {
                    await DeleteCookiesFromDbAsync(dbPath, cookiesToDelete, cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete cookies from {dbPath}: {ex.Message}");
                    return false;
                }
            }
            return true;
        }

        private string GetCookieDbPath(string browser, string profile)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var pattern = BrowserCookiePaths[browser].FirstOrDefault(p => p.Contains(profile)) ?? BrowserCookiePaths[browser].First();
            return Path.Combine(basePath, pattern);
        }

        private IEnumerable<string> ExpandPaths(string pattern)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var fullPattern = Path.Combine(basePath, pattern);
            if (fullPattern.Contains('*'))
            {
                var dir = Path.GetDirectoryName(fullPattern);
                var searchPattern = Path.GetFileName(fullPattern);
                if (Directory.Exists(dir))
                {
                    foreach (var file in Directory.GetFiles(dir, searchPattern))
                        yield return file;
                }
            }
            else
            {
                yield return fullPattern;
            }
        }

        private async Task<List<CookieInfo>> ReadChromeCookiesAsync(string dbPath, string browser, CancellationToken cancellationToken)
        {
            var result = new List<CookieInfo>();
            var connectionString = $"Data Source={dbPath};Mode=ReadOnly;Cache=Shared";
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = "SELECT host_key, name, path, expires_utc, is_secure, is_httponly FROM cookies";
            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var cookie = new CookieInfo
                {
                    Browser = browser,
                    Domain = reader.GetString(0),
                    Name = reader.GetString(1),
                    Path = reader.GetString(2),
                    ExpiresUtc = DateTime.FromFileTimeUtc(reader.GetInt64(3)),
                    IsSecure = reader.GetBoolean(4),
                    IsHttpOnly = reader.GetBoolean(5)
                };
                result.Add(cookie);
            }
            return result;
        }

        private async Task DeleteCookiesFromDbAsync(string dbPath, HashSet<string> cookieNames, CancellationToken cancellationToken)
        {
            var connectionString = $"Data Source={dbPath}";
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var transaction = connection.BeginTransaction();
            foreach (var name in cookieNames)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = "DELETE FROM cookies WHERE name = @name";
                cmd.Parameters.AddWithValue("@name", name);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            transaction.Commit();
        }
    }
}