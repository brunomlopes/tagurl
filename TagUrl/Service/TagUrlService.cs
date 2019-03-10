using System.Collections.Generic;
using System.Threading.Tasks;

namespace TagUrl.Service
{
    public class TagUrlService
    {
        private readonly TagSuggesters suggesters;

        public TagUrlService(TagSuggesters suggesters)
        {
            this.suggesters = suggesters;
        }
        internal async Task<IReadOnlyCollection<TagUrlSuggestion>> SuggestionsForAsync(
            string url, string title, string body, string[] existingTags)
        {
            return await suggesters.Suggest(url, title, body, existingTags);
        }
    }
}

