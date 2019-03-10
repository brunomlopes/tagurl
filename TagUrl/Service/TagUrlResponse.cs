using System.Collections.Generic;

namespace TagUrl.Service
{
    public class TagUrlResponse
    {
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Body { get; set; } = "";
        public string[] ExistingTags { get; set; } = { };

        public IReadOnlyCollection<TagUrlSuggestion> Suggestions { get; set; } = new TagUrlSuggestion[] { };
    }
}
