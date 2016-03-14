using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.TestHost;
using TestingControllersSample;
using Xunit;
using System.Linq;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace TestingControllerSample.Tests.IntegrationTests
{
    public class ApiIdeasControllerForSession
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public ApiIdeasControllerForSession()
        {
            _server = new TestServer(TestServer.CreateBuilder()
                .UseEnvironment("Development")
                .UseStartup<Startup>());
            _client = _server.CreateClient();

            // client always expects json results
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        [Fact]
        public async Task ReturnsNotFoundForBadSessionId()
        {
            var response = await _client.GetAsync("/api/ideas/forsession/500");
            Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
        }

        public class IdeaDto
        {
            public int id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public DateTime dateCreated { get; set; }
        }

        [Fact]
        public async Task ReturnsIdeasForValidSessionId()
        {
            var response = await _client.GetAsync("/api/ideas/forsession/1");
            response.EnsureSuccessStatusCode();

            var ideaList = await response.Content.ReadAsJsonAsync<List<IdeaDto>>();
            var firstIdea = ideaList.First();
            var testSession = Startup.GetTestSession();
            Assert.Equal(testSession.Ideas.First().Name, firstIdea.name);
        }

    }

    // http://dotnetliberty.com/index.php/2015/12/17/asp-net-5-web-api-integration-testing/
    public static class HttpContentExtensions
    {
        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content)
        {
            // I'm only accepting JSON from the server, and I don't want to add a dependency on
            // System.Runtime.Serialization.Xml which is required when using the default formatters
            return await content.ReadAsAsync<T>(GetJsonFormatters());
        }

        private static IEnumerable<MediaTypeFormatter> GetJsonFormatters()
        {
            yield return new JsonMediaTypeFormatter();
        }

    }
}