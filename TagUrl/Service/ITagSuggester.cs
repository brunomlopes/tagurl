using System.Collections.Generic;
using System.Threading.Tasks;

namespace TagUrl.Service
{
    public interface ITagSuggester
    {
        string SourceName { get; }
        Task<IReadOnlyCollection<string>> Suggest(string url, string title, string body, string[] skipTags);
    }
}