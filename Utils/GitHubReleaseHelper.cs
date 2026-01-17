using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MetaMystia;

namespace SgrYuki.Utils;

[AutoLog]
public static partial class GitHubReleaseHelper
{
    private static readonly System.Net.Http.HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.github.com/")
    };

    static GitHubReleaseHelper()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MetaMystia/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public static async Task<string> GetLatestReleaseTagUsingGithubAsync(
        string owner,
        string repo)
    {
        var response = await _http.GetAsync(
            $"repos/{owner}/{repo}/releases/latest");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        // 200: {"tag_name":"v0.11.0"}
        if (dict != null && dict.TryGetValue("tag_name", out var tag))
        {
            return tag.ToString();
        }
        return null;
    }

    public static async Task<string> GetLatestReleaseTagAsync()
    {
        var c = new System.Net.Http.HttpClient();
        c.DefaultRequestHeaders.UserAgent.ParseAdd("MetaMystia/1.0");
        
        var response = await c.GetAsync("https://api.izakaya.cc/version/meta-mystia");

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        // 200: {"dll":"0.11.0","zip":"0.4.5"}
        if (dict != null && dict.TryGetValue("dll", out var ver))
        {
            return ver.ToString();
        }
        return null;
    }

    public static string GetPluginLatestTag(bool useGithub = false)
    {
        if (useGithub)
        {
            return GetLatestReleaseTagUsingGithubAsync("MetaMikuAI", "MetaMystia").GetAwaiter().GetResult();
        }
        return GetLatestReleaseTagAsync().GetAwaiter().GetResult();
    }
}

