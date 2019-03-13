using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using TagUrl.Service;
using Xunit;

namespace Tests
{
    public class TextAnalysisTests
    {
        [Fact]
        public async Task CanSerializeBodyRequestCorrectly()
        {
            var stream = new MemoryStream();

            using (var streamPipeWriter = new StreamPipeWriter(stream))
            {
                TextAnalysis.WriteRequest(streamPipeWriter, "This is a body of text\n with a new line");

                await streamPipeWriter.FlushAsync();
            }
            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            var text = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
            text.ShouldBe(@"{""documents"":[{""language"":""en"",""id"":""doc"",""text"":""This is a body of text\n with a new line""}]}");

        }
        
    }
}