using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace KenshiModManager.Services
{
    /// <summary>
    /// Service for fetching release information from GitHub API
    /// </summary>
    public class GitHubReleaseService
    {
        private const string OwnerRepo = "nonniks/KenshiModManager";
        private static readonly HttpClient _httpClient = new HttpClient();

        static GitHubReleaseService()
        {
            // GitHub API requires User-Agent header
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "KenshiModManager/1.1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Fetches release notes for a specific version tag
        /// </summary>
        /// <param name="versionTag">Version tag (e.g., "v1.1.0")</param>
        /// <returns>Release notes body (Markdown format) or null if not found</returns>
        public static async Task<GitHubRelease?> GetReleaseByTagAsync(string versionTag)
        {
            try
            {
                string url = $"https://api.github.com/repos/{OwnerRepo}/releases/tags/{versionTag}";
                Console.WriteLine($"[GitHubReleaseService] Fetching release info from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[GitHubReleaseService] GitHub API returned status: {response.StatusCode}");
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine($"[GitHubReleaseService] Successfully fetched release: {release?.Name}");
                return release;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHubReleaseService] Error fetching release: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetches the latest release information
        /// </summary>
        /// <returns>Latest release info or null if not found</returns>
        public static async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            try
            {
                string url = $"https://api.github.com/repos/{OwnerRepo}/releases/latest";
                Console.WriteLine($"[GitHubReleaseService] Fetching latest release from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[GitHubReleaseService] GitHub API returned status: {response.StatusCode}");
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine($"[GitHubReleaseService] Successfully fetched latest release: {release?.Name}");
                return release;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHubReleaseService] Error fetching latest release: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetches multiple releases (for changelog history)
        /// </summary>
        /// <param name="count">Number of releases to fetch (default: 5)</param>
        /// <returns>List of releases or null if error occurred</returns>
        public static async Task<List<GitHubRelease>?> GetReleasesAsync(int count = 5)
        {
            try
            {
                string url = $"https://api.github.com/repos/{OwnerRepo}/releases?per_page={count}";
                Console.WriteLine($"[GitHubReleaseService] Fetching {count} releases from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[GitHubReleaseService] GitHub API returned status: {response.StatusCode}");
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine($"[GitHubReleaseService] Successfully fetched {releases?.Count ?? 0} releases");
                return releases;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHubReleaseService] Error fetching releases: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts version string to GitHub tag format
        /// </summary>
        /// <param name="version">Version string (e.g., "1.1.0.0" or "1.1.0")</param>
        /// <returns>Tag format (e.g., "v1.1.0")</returns>
        public static string VersionToTag(string version)
        {
            if (string.IsNullOrEmpty(version))
                return "v0.0.0";

            // Remove .0 suffix if present (1.1.0.0 -> 1.1.0)
            if (version.EndsWith(".0"))
            {
                var parts = version.Split('.');
                if (parts.Length == 4 && parts[3] == "0")
                {
                    version = $"{parts[0]}.{parts[1]}.{parts[2]}";
                }
            }

            // Add 'v' prefix if not present
            return version.StartsWith("v") ? version : $"v{version}";
        }
    }

    /// <summary>
    /// Represents a GitHub release from API response
    /// </summary>
    public class GitHubRelease
    {
        public string? Name { get; set; }
        public string? TagName { get; set; }
        public string? Body { get; set; }
        public string? HtmlUrl { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
