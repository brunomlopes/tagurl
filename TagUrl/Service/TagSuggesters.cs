using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace TagUrl.Service
{
    public class TagSuggesters
    {
        private readonly ITagSuggester[] _suggesters;

        public TagSuggesters()
        {
            _suggesters = new ITagSuggester[] { new RedditTagSuggester(), new GithubTagSuggester() };
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

}

