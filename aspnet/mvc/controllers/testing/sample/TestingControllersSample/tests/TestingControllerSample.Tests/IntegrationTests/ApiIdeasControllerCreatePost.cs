using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.TestHost;
using TestingControllersSample;
using Xunit;
using System.Linq;
using System.Net.Http.Headers;
using TestingControllersSample.Core.Model;

namespace TestingControllerSample.Tests.IntegrationTests
{
    public class ApiIdeasControllerCreatePost
    { 
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public ApiIdeasControllerCreatePost()
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

        internal class NewIdeaDto
        {
            public NewIdeaDto(string name, string description, int sessionId)
            {
                Name = name;
                Description = description;
                SessionId = sessionId;
            }

            public string Name { get; set; }
            public string Description { get; set; }
            public int SessionId { get; set; }
        }

        [Fact]
        public async Task ReturnsBadRequestForMissingNameValue()
        {
            var newIdea = new NewIdeaDto("", "Description", 1);
            var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ReturnsBadRequestForMissingDescriptionValue()
        {
            var newIdea = new NewIdeaDto("Name", "", 1);
            var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ReturnsBadRequestForMissingSessionIdValue()
        {
            var newIdea = new NewIdeaDto("Name", "Description", 0);
            var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ReturnsNotFoundForInvalidSession()
        {
            var newIdea = new NewIdeaDto("Name", "Description", 123);
            var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ReturnsCreatedIdeaWithCorrectInputs()
        {
            var testIdeaName = Guid.NewGuid().ToString();
            var newIdea = new NewIdeaDto(testIdeaName, "Description", 1);
           
            var response = await _client.PostAsJsonAsync("/api/ideas/create", newIdea);
            response.EnsureSuccessStatusCode();

            var returnedSession = await response.Content.ReadAsJsonAsync<BrainStormSession>();
            Assert.Equal(2, returnedSession.Ideas.Count);
            Assert.True(returnedSession.Ideas.Any(i => i.Name == testIdeaName));
        }
    }
}