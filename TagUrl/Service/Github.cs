using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TagUrl.Service
{
    public class GithubTagSuggester : ITagSuggester
    {
        private static readonly HttpClient client = new HttpClient();

        static GithubTagSuggester()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "tagurl.brunomlopes.com");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.mercy-preview+json");
        }
        public string SourceName => "github";

        private static string[] Empty = { };
        public async Task<IReadOnlyCollection<string>> Suggest(string url, string title, string body, string[] skipTags)
        {
            if (!url.StartsWith("https://github.com/")) return Empty;
            
            var uri = new Uri(url);
            var parts = uri.AbsolutePath.TrimStart('/').Split("/");
            if(parts.Length < 2) return Empty;

            var (owner,repo) = (parts[0],parts[1]);

            var response = await client.GetStreamAsync($"https://api.github.com/repos/{owner}/{repo}/topics");

            var doc = await JsonDocument.ParseAsync(response);
            return doc.RootElement.GetProperty("names").EnumerateArray().Select(topic => topic.GetString()).ToArray();
        }
    }
}
