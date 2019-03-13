using System;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TagUrl.Service;

namespace TagUrl
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_configuration);
            services.AddTransient<TagUrlService>();
            services.AddTransient<ITagSuggesters, TagSuggesters>();
            services.AddTransient<SuggestTagsController>();
            services.AddSingleton(new MemoryCache(new MemoryCacheOptions()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var favicon = new Lazy<byte[]>(() =>
            {
                var icoBase64 =
                    "AAABAAEAEBAAAAEAIABoBAAAFgAAACgAAAAQAAAAIAAAAAEAIAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgICAEoCAgNeAgICjAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgICAF4CAgOiAgID/gICA/4CAgLkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgICAGICAgOmAgID/gICA/4CAgP+AgID/gICAuwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgICAGICAgOmAgID/gICA/4CAgP+AgID/gICA/4CAgP+AgIC6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgICAE4CAgOmAgID/gICA/4CAgP+AgID/gICA/4CAgP+AgID/gICA/4CAgLoAAAAAAAAAAAAAAAAAAAAAAAAAAICAgNeAgID/gICA/4CAgP+AgID/gICA/4CAgP+AgID/gICA/4CAgP+AgID/gICAuAAAAAAAAAAAAAAAAAAAAACAgICfgICA/4CAgP+AgID/gICA/4CAgP+AgID/gICA/4CAgP+AgID/gICA/4CAgP+AgIC0AAAAAAAAAAAAAAAAAAAAAICAgLWAgID/gICA/4CAgP+AgID/gICA/4CAgP+AgID/gICA/4CAgP+AgID/gICA/4CAgFAAAAAAAAAAAAAAAAAAAAAAgICAtoCAgP+AgID/gICA/4CAgP+AgID/gICA/4CAgP+AgID/gICA/4CAgP+AgIB+gICAagAAAAAAAAAAAAAAAAAAAACAgIC2gICA/4CAgP+AgID/gICA/4CAgP+AgID/gICA0YCAgPGAgID/QEBAdoCAgN5AQEApAAAAAAAAAAAAAAAAAAAAAICAgLaAgID/gICA/4CAgP+AgID/gICA04CAgFCAgIBZgICA6EBAQGWAgIBOgICArgAAAAAAAAAAAAAAAAAAAAAAAAAAgICAtoCAgP+AgID/gICA/4CAgO6AgIB7gICAfYCAgKdAQEBOQEBAEoCAgOEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAgICygICA/4CAgP+AgIDcgICAnYCAgJyAgID9QEBAMgAAAACAgID1AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEBAQExAQEBtQEBAa4CAgK6AgIB/QEBAMQAAAABAQEAggICA2gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAgIA1gICA90BAQAZAQEAVgICAgYCAgIIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAICAgFyAgIDwgICA4ICAgIUAAAAA+f8AAPD/AADgfwAAwD8AAIAfAAAADwAAAAcAAIAHAADABwAA4AUAAPA2AAD4NgAA/AYAAP/eAAD/7AAA//EAAA==";
                return Convert.FromBase64String(icoBase64);
            });
            app.UseRouting(routes =>
            {
                routes.Map("/suggest", async context => await context.RequestServices.GetRequiredService<SuggestTagsController>().SuggestTags(context));
                routes.MapGet("/favicon.ico", async context =>
                {
                    context.Response.Headers["Content-Type"] = "image/x-icon";
                    await context.Response.Body.WriteAsync(favicon.Value);
                });
                routes.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
