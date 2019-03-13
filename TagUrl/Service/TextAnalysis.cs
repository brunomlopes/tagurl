using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace TagUrl.Service
{
    public class TextAnalysis : ITagSuggester
    {
        private const string _singleDocId = "doc";
        private static readonly HttpClient client = new HttpClient();

        private string _key;

        static TextAnalysis()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);

        }

        public TextAnalysis(string key)
        {
            _key = key;
            if (!client.DefaultRequestHeaders.Contains("Ocp-Apim-Subscription-Key"))
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _key);
        }

        public string SourceName => "test-analysis";
        public async Task<IReadOnlyCollection<string>> Suggest(string url, string title, string body, string[] skipTags)
        {
            var document = $"{title}\n{body}".Trim();

            if (string.IsNullOrWhiteSpace(document)) return new string[] { };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://westeurope.api.cognitive.microsoft.com/text/analytics/v2.0/entities");
            var bodyStream = new MemoryStream();
            using (var streamPipeWriter = new StreamPipeWriter(bodyStream))
            {
                WriteRequest(streamPipeWriter, document);
                await streamPipeWriter.FlushAsync();
            }
            bodyStream.Seek(0, SeekOrigin.Begin);
            request.Content = new StreamContent(bodyStream)
            {
                Headers = { { "Content-Type", MediaTypeNames.Application.Json } }
            };

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(response.ReasonPhrase + "\n"+ await response.Content.ReadAsStringAsync());
                return new string[] { };
            }

            var responseDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            var doc = responseDoc.RootElement.GetProperty("documents")
                .EnumerateArray()
                .First(d => d.GetProperty("id").GetString() == _singleDocId);

            return doc.GetProperty("entities")
                .EnumerateArray()
                .Select(entity => (name: entity.GetProperty("name").GetString(),
                    matchCount: entity.GetProperty("matches").EnumerateArray().Count()))
                .OrderByDescending(k => k.matchCount)
                .Select(k => k.name.ToLowerInvariant().Replace(" ", "-"))
                .ToArray();
        }

        public static void WriteRequest(IBufferWriter<byte> streamPipeWriter, string text)
        {
            var jsonWriter = new Utf8JsonWriter(streamPipeWriter);

            jsonWriter.WriteStartObject();
            jsonWriter.WriteStartArray("documents");
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("language", "en");
            jsonWriter.WriteString("id", _singleDocId);
            jsonWriter.WriteString("text", text);
            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();

            jsonWriter.Flush(true);
        }
    }
}