using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNet.TestHost;
using TestingControllersSample;
using Xunit;

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
}