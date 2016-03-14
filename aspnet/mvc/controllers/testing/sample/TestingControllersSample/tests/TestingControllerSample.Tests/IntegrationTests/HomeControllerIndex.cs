using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using TestingControllersSample;
using Xunit;

namespace TestingControllerSample.Tests.IntegrationTests
{
    public class HomeControllerIndex
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public HomeControllerIndex()
        {
            _server = new TestServer(TestServer.CreateBuilder()
                .UseEnvironment("Development")
                .UseServices(services =>
                {
                    var env = new TestApplicationEnvironment();
                    env.ApplicationBasePath =
                        Path.GetFullPath(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "..",
                            "..", "src", "TestingControllersSample"));
                    env.ApplicationName = "TestingControllersSample";
                    services.AddInstance<IApplicationEnvironment>(env);
                })
                .UseStartup<Startup>());
            _client = _server.CreateClient();
        }

        [Fact]
        public async Task ReturnsInitialListOfBrainstormSessions()
        {
            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            Assert.True(responseString.Contains("Test Session 1"));
        }

    }
}