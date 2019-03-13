using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TagUrl.Service
{
    public class TagSuggesters : ITagSuggesters
    {
        private readonly ITagSuggester[] _suggesters;

        public TagSuggesters(IConfiguration configuration)
        {
            _suggesters = new ITagSuggester[] { new RedditTagSuggester(), new GithubTagSuggester(), new TextAnalysis(configuration["TextAnalysis:Key"]) };
        }

        public async Task<IReadOnlyCollection<TagUrlSuggestion>> Suggest(string url, string title, string body, string[] skipTags)
        {
            var results = _suggesters.Select(s =>
                    (source: s.SourceName, suggestions: s.Suggest(url, title, body, skipTags)))
                .ToArray();

            var suggestions = await Task.WhenAll(results.Select(selector: async t =>
            {
                var s = await t.suggestions;
                return s.Select(tag => (t.source, suggestion: tag.ToLowerInvariant())).ToImmutableArray();
            }).ToArray());

            return suggestions
                .SelectMany(t => t)
                .GroupBy(t => t.suggestion)
                .Where(t => !skipTags.Contains(t.Key))
                .Select(s =>
            {
                return new TagUrlSuggestion(s.Key, s.Select(i => i.source).Distinct().ToArray());
            }).ToImmutableArray();
        }
    }

    public interface ITagSuggesters
    {
        Task<IReadOnlyCollection<TagUrlSuggestion>> Suggest(string url, string title, string body, string[] skipTags);
    }
}

