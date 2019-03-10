using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;

namespace TagUrl.Service
{
    public class RedditTagSuggester : ITagSuggester
    {
        private static readonly HttpClient client = new HttpClient();

        static RedditTagSuggester()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
        }

        public string SourceName => "reddit";

        private static string[] Empty = { };
        public async Task<IReadOnlyCollection<string>> Suggest(string url, string title, string body, string[] skipTags)
        {
            var tags = Empty;

            var response = await client.GetStreamAsync($"https://www.reddit.com/search.json?q=url:{WebUtility.UrlEncode(url)}");

            var doc = await JsonDocument.ParseAsync(response);

            if (!doc.RootElement.TryGetProperty("data", out var data)) return Empty;

            if (!data.TryGetProperty("children", out var children)) return Empty;

            tags = children.EnumerateArray().Select(child =>
            {
                return child.GetProperty("data").GetProperty("subreddit").GetString();
            }).ToArray();

            return await Task.FromResult(tags);
        }
    }
}
