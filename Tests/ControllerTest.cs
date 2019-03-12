using System.IO;
using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TagUrl.Service;
using Xunit;

namespace Tests
{
    public class ControllerTest
    {
        private SuggestTagsController _controller;
        private TagUrlService _tagUrlService;

        public ControllerTest()
        {
            _tagUrlService = new TagUrlService(new TagSuggesters());

            _controller = new SuggestTagsController(new MemoryCache(new MemoryCacheOptions()), _tagUrlService);
        }

        [Fact]
        public async Task CanDeSerializeBodyWithJustUrl()
        {
            var context = new DefaultHttpContext()
            {
                Request =
                {
                    
                    ContentType = MediaTypeNames.Application.Json,
                    Method = "POST",
                    Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(@"
{
	""url"":""https://mithril.js.org/""
}
"))
                }
            };
            
            await _controller.SuggestTags(context);
            
            context.Response.StatusCode.ShouldBe(200, new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEnd());
        }
    }
}