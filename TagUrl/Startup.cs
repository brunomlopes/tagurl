using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TagUrl.Service;

namespace TagUrl
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<TagUrlService>();
            services.AddTransient<TagSuggesters>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting(routes =>
            {
                routes.Map("/tag", async context =>
                {
                    string url = "";
                    string title = "";
                    string body = "";
                    string[] existingTags = { };

                    if (HttpMethods.IsPost(context.Request.Method))
                    {
                        if (context.Request.ContentType != MediaTypeNames.Application.Json)
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
                        _ = requestBody.RootElement.TryGetProperty("body", out var bodyProp);
                        _ = requestBody.RootElement.TryGetProperty("title", out var titleProp);
                        _ = requestBody.RootElement.TryGetProperty("existingTags", out var existingTagsProp);
                        url = urlProp.GetString();
                        title = titleProp.GetString() ?? "";
                        body = bodyProp.GetString() ?? "";
                        existingTags = existingTagsProp.EnumerateArray().Select(e => e.GetString()).ToArray();
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

                    var suggestions = await context.RequestServices.GetRequiredService<TagUrlService>()
                    .SuggestionsForAsync(url, title, body, existingTags);

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
                    await context.Response.BodyPipe.FlushAsync();
                });
                routes.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }

        public void WriteToOutput(PipeWriter bodyPipe, TagUrlResponse response)
        {
            var jsonWriter = new Utf8JsonWriter(bodyPipe, state: default);
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("url", response.Url);
            jsonWriter.WriteString("title", response.Title);
            jsonWriter.WriteString("body", response.Body);
            jsonWriter.WriteStartArray("existingTags");
            foreach(var tag in response.ExistingTags)
            {
                jsonWriter.WriteStringValue(tag);
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteStartArray("suggestions");
            foreach(var suggestion in response.Suggestions)
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("tag", suggestion.Tag);
                jsonWriter.WriteStartArray("sources");
                foreach(var source in suggestion.Sources)
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
