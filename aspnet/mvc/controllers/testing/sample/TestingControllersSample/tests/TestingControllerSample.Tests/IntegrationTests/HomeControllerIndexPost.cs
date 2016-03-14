using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.TestHost;
using TestingControllersSample;
using Xunit;

namespace TestingControllerSample.Tests.IntegrationTests
{
    public class HomeControllerIndexPost
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public HomeControllerIndexPost()
        {
            _server = new TestServer(TestServer.CreateBuilder()
                .UseEnvironment("Development")
                .UseStartup<Startup>());
            _client = _server.CreateClient();
        }

        [Fact]
        public async Task AddsNewBrainstormSession()
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "/");
            var data = new Dictionary<string, string>();
            string testSessionName = Guid.NewGuid().ToString();
            data.Add("SessionName", testSessionName);

            message.Content = new FormUrlEncodedContent(data);

            var response = await _client.SendAsync(message);

            Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
            Assert.Equal("/", response.Headers.Location.ToString());
        }
    }
}