using Contentful.Core;
using Contentful.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Contentful.Net.Integration
{
    public class ContentfulCMATests : IClassFixture<SpaceFixture>
    {
        private ContentfulManagementClient _client;
        private string _spaceId = "";

        public ContentfulCMATests(SpaceFixture fixture)
        {

            var httpClient = new HttpClient(new TestEndpointMessageHandler());
            var managementToken = Environment.GetEnvironmentVariable("CONTENTFUL_ACCESS_TOKEN");
            _spaceId = fixture.SpaceId;
            _client = new ContentfulManagementClient(httpClient, new ContentfulOptions()
            {
                DeliveryApiKey = "123",
                ManagementApiKey = managementToken,
                SpaceId = _spaceId,
                UsePreviewApi = false
            });

        }

        [Fact]
        public async Task GetASpace()
        {
            var space = await _client.GetSpaceAsync(_spaceId);

            Assert.Equal("dotnet-test-space", space.Name);
        }

        [Fact]
        public async Task GetAllSpaces()
        {
            var spaces = await _client.GetSpacesAsync();

            Assert.Contains(spaces, c => c.Name == "dotnet-test-space");
        }

        [Fact]
        public async Task UpdateSpaceName()
        {
            var space = await _client.UpdateSpaceNameAsync(_spaceId, "knuckleburger", 1);
            Assert.Equal("knuckleburger", space.Name);
            space.Name = "dotnet-test-space";
            space = await _client.UpdateSpaceNameAsync(space);
            Assert.Equal("dotnet-test-space", space.Name);
        }
    }

    public class SpaceFixture : IDisposable
    {
        public string SpaceId { get; set; }
        private readonly ContentfulManagementClient _client;
        public  SpaceFixture()
        {
            var httpClient = new HttpClient();
            var managementToken = Environment.GetEnvironmentVariable("CONTENTFUL_ACCESS_TOKEN");
            _client = new ContentfulManagementClient(httpClient, new ContentfulOptions()
            {
                DeliveryApiKey = "123",
                ManagementApiKey = managementToken,
                SpaceId = "123",
                UsePreviewApi = false
            });

            var space = _client.CreateSpaceAsync("dotnet-test-space", "en-US").Result;

            SpaceId = space.SystemProperties.Id;
        }

        public void Dispose()
        {
            _client.DeleteSpaceAsync(SpaceId);
        }
    }
}
