using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Contentful.Net.Integration
{
    public class ContentfulCMATests : IClassFixture<SpaceFixture>
    {
        private ContentfulManagementClient _client;
        private string _spaceId = "";
        private string _contentTypeId = "";

        public ContentfulCMATests(SpaceFixture fixture)
        {

            var httpClient = new HttpClient(new TestEndpointMessageHandler());
            var managementToken = Environment.GetEnvironmentVariable("CONTENTFUL_ACCESS_TOKEN");
            _spaceId = fixture.SpaceId;
            _contentTypeId = fixture.ContentTypeId;
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

        [Fact]
        public async Task UpdateContentType()
        {
            var contentType = new ContentType();
            contentType.SystemProperties = new SystemProperties()
            {
                Id = _contentTypeId
            };
            contentType.Name = "Cool content changed";
            contentType.Fields = new List<Field>()
            {
                new Field()
                {
                    Name = "Field1",
                    Id = "field1",
                    @Type = "Text"
                },
                new Field()
                {
                    Name = "Field2",
                    Id = "field2",
                    @Type = "Text"
                }
            };

            var updatedContentType = await _client.CreateOrUpdateContentTypeAsync(contentType, version: 1);

            Assert.Equal(2, updatedContentType.Fields.Count);
            Assert.Equal("Cool content changed", updatedContentType.Name);
        }
    }

    public class SpaceFixture : IDisposable
    {
        public string SpaceId { get; set; }
        public string ContentTypeId => "contenttype";
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

            var contentType = new ContentType();
            contentType.SystemProperties = new SystemProperties()
            {
                Id = ContentTypeId
            };
            contentType.Name = "Cool content";
            contentType.Fields = new List<Field>()
            {
                new Field()
                {
                    Name = "Field1",
                    Id = "field1",
                    @Type = "Text"
                },
                new Field()
                {
                    Name = "Field2",
                    Id = "field2",
                    @Type = "Text"
                }
            };

            var contenttype = _client.CreateOrUpdateContentTypeAsync(contentType, SpaceId).Result;
        }

        public void Dispose()
        {
            _client.DeleteSpaceAsync(SpaceId);
        }
    }

    public class TestEndpointMessageHandler : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Environment.GetEnvironmentVariable("CONTENTFUL_RUN_WITHOUT_PROXY") == "true")
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var requestUrl = request.RequestUri.ToString();

            requestUrl = requestUrl
                .Replace("https://api.contentful.com/", "http://127.0.0.1:5000/");

            request.RequestUri = new Uri(requestUrl);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
