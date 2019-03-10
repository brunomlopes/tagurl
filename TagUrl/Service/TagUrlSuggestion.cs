namespace TagUrl.Service
{
    public class TagUrlSuggestion
    {
        public TagUrlSuggestion(string tag, string[] sources)
        {
            Tag = tag;
            Sources = sources;
        }

        public string Tag { get; set; }
        public string[] Sources { get; set; }
    }
}