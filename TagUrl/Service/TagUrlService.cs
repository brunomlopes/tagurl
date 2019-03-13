using System.Collections.Generic;
using System.Threading.Tasks;

namespace TagUrl.Service
{
    public class TagUrlService
    {
        private readonly ITagSuggesters _suggesters;

        public TagUrlService(ITagSuggesters suggesters)
        {
            this._suggesters = suggesters;
        }
        internal async Task<IReadOnlyCollection<TagUrlSuggestion>> SuggestionsForAsync(
            string url, string title, string body, string[] existingTags)
        {
            return await _suggesters.Suggest(url, title, body, existingTags);
        }
    }
}

