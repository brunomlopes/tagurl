using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace TagUrl.Service
{
    public class SuggestTagsController
    {
        private readonly MemoryCache _suggestionsCache;
        private readonly TagUrlService _tagUrlService;

        public SuggestTagsController(MemoryCache suggestionsCache, TagUrlService tagUrlService)
        {
            _suggestionsCache = suggestionsCache;
            _tagUrlService = tagUrlService;
        }

        public async Task SuggestTags(HttpContext context)
        {
            string url = "";
            string title = "";
            string body = "";
            string[] existingTags = { };

            if (HttpMethods.IsPost(context.Request.Method))
            {
                if (context.Request.ContentType != MediaTypeNames.Application.Json
                    && context.Request.ContentType != MediaTypeNames.Application.Json+ ";charset=utf-8")
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync($"Content type {context.Request.ContentType} not acceptable. Use {MediaTypeNames.Application.Json}");
                    return;
                }

                var requestBody = await JsonDocument.ParseAsync(context.Request.Body);
                if (!requestBody.RootElement.TryGetProperty("url", out var urlProp))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync("Missing property 'url'");
                    return;
                }

                if(requestBody.RootElement.TryGetProperty("body", out var bodyProp))
                    body = bodyProp.GetString() ?? "";
                if(requestBody.RootElement.TryGetProperty("title", out var titleProp))
                    title = titleProp.GetString() ?? "";

                if(requestBody.RootElement.TryGetProperty("existingTags", out var existingTagsProp))
                    existingTags = existingTagsProp.EnumerateArray().Select(e => e.GetString()).ToArray();
                url = urlProp.GetString();
                
            }
            else
            {
                if (!context.Request.Query.ContainsKey("url"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync("Missing query string parameter 'url'");
                    return;
                }

                url = context.Request.Query["url"].First();
            }

            var key = ("tagSuggestions", url, title, body);
            var suggestions =
                await _suggestionsCache.GetOrCreateAsync(key,
                    async cacheEntry => await _tagUrlService
                        .SuggestionsForAsync(url, title, body, existingTags));


            var response = new TagUrlResponse
            {
                Url = url,
                Title = title,
                Body = body,
                ExistingTags = existingTags,
                Suggestions = suggestions
            };


            context.Response.ContentType = MediaTypeNames.Application.Json;
            WriteToOutput(context.Response.BodyPipe, response);
            // Question: Do I really need this flushasync here? or should that happen somewhere else?
            await context.Response.BodyPipe.FlushAsync();
        }


        public void WriteToOutput(PipeWriter bodyPipe, TagUrlResponse response)
        {
            var jsonWriter = new Utf8JsonWriter(bodyPipe, state: default);
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("url", response.Url);
            jsonWriter.WriteString("title", response.Title);
            jsonWriter.WriteString("body", response.Body);
            jsonWriter.WriteStartArray("existingTags");
            foreach (var tag in response.ExistingTags)
            {
                jsonWriter.WriteStringValue(tag);
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteStartArray("suggestions");
            foreach (var suggestion in response.Suggestions)
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("tag", suggestion.Tag);
                jsonWriter.WriteStartArray("sources");
                foreach (var source in suggestion.Sources)
                {
                    jsonWriter.WriteStringValue(source);
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteEndObject();

            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
            jsonWriter.Flush(true);
        }
    }
}