using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Models;
using Contentful.Net.CMA;
using Newtonsoft.Json.Linq;
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
    [TestCaseOrderer("Contentful.Net.CMA.CustomTestCaseOrderer", "Contentful.Net.CMA")]
    public class ContentfulCMATests : IClassFixture<SpaceFixture>
    {
        private ContentfulManagementClient _client;
        private string _spaceId = "";
        private string _contentTypeId = "contenttype";

        public ContentfulCMATests(SpaceFixture fixture)
        {

            var httpClient = new HttpClient(new TestEndpointMessageHandler());
            var managementToken = Environment.GetEnvironmentVariable("CONTENTFUL_ACCESS_TOKEN");
            _spaceId = fixture.SpaceId;
            _client = new ContentfulManagementClient(httpClient, new ContentfulOptions()
            {
                DeliveryApiKey = "123",
                ManagementApiKey = managementToken,
                SpaceId = fixture.SpaceId,
                UsePreviewApi = false
            });

        }

        [Fact]
        [Order(5)]
        public async Task GetASpace()
        {
            var space = await _client.GetSpaceAsync(_spaceId);

            Assert.Equal("dotnet-test-space", space.Name);
        }

        [Fact]
        [Order(10)]
        public async Task GetAllSpaces()
        {
            var spaces = await _client.GetSpacesAsync();

            Assert.Contains(spaces, c => c.Name == "dotnet-test-space");
        }

        [Fact]
        [Order(20)]
        public async Task UpdateSpaceName()
        {
            var space = await _client.UpdateSpaceNameAsync(_spaceId, "knuckleburger", 1);
            Assert.Equal("knuckleburger", space.Name);
            space.Name = "dotnet-test-space";
            space = await _client.UpdateSpaceNameAsync(space);
            Assert.Equal("dotnet-test-space", space.Name);
        }

        [Fact]
        [Order(25)]
        public async Task CreateContentType()
        {
            var contentType = new ContentType();
            contentType.SystemProperties = new SystemProperties()
            {
                Id = _contentTypeId
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

            var contenttype = await _client.CreateOrUpdateContentTypeAsync(contentType, _spaceId);

            Assert.Equal(2, contenttype.Fields.Count);
        }

        [Fact]
        [Order(30)]
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

            var updatedContentType = await _client.CreateOrUpdateContentTypeAsync(contentType, _spaceId, version: 1);

            Assert.Equal(2, updatedContentType.Fields.Count);
            Assert.Equal("Cool content changed", updatedContentType.Name);
        }

        [Fact]
        [Order(40)]
        public async Task GetContentType()
        {
            var contentType = await _client.GetContentTypeAsync(_contentTypeId, _spaceId);

            Assert.Equal(_contentTypeId, contentType.SystemProperties.Id);
        }

        [Fact]
        [Order(50)]
        public async Task GetAllContentTypes()
        {
            //It seems we need to give the API a chance to catch up...
            Thread.Sleep(1000);
            var contentTypes = await _client.GetContentTypesAsync();

            Assert.Equal(1, contentTypes.Count());
        }

        [Fact]
        [Order(60)]
        public async Task PublishContentType()
        {
            var contentType = await _client.ActivateContentTypeAsync(_contentTypeId, 2);

            Assert.Equal(3, contentType.SystemProperties.Version);
        }

        [Fact]
        [Order(80)]
        public async Task CreateEntry()
        {
            var entry = new Entry<dynamic>();

            entry.SystemProperties = new SystemProperties()
            {
                Id = "entry1"
            };

            entry.Fields = new JObject
            (
                new JProperty("field1", new JObject(new JProperty("en-US", "bla"))),
                new JProperty("field2", new JObject(new JProperty("en-US", "blue")))
            );

            entry = await _client.CreateOrUpdateEntryAsync(entry, contentTypeId: _contentTypeId);

            Assert.Equal("bla", entry.Fields.field1["en-US"].ToString());
        }


        [Fact]
        [Order(700)]
        public async Task UnpublishContentType()
        {
            var contentTypes = await _client.GetActivatedContentTypesAsync();

            Assert.Equal(1, contentTypes.Count());
            await _client.DeactivateContentTypeAsync(_contentTypeId);

            contentTypes = await _client.GetActivatedContentTypesAsync();
            Assert.Empty(contentTypes);
        }

        [Fact]
        [Order(1000)]
        public async Task DeleteSpace()
        {
            await _client.DeleteSpaceAsync(_spaceId);
        }

    }

    public class SpaceFixture : IDisposable
    {
        public string SpaceId { get; set; }
        private readonly ContentfulManagementClient _client;
        public SpaceFixture()
        {
            var httpClient = new HttpClient(new TestEndpointMessageHandler());
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
            try
            {
                _client.DeleteSpaceAsync(SpaceId);
            }
            catch(Exception)
            {
                //We don't really wanna do anything here, 
                //just try to delete the space if it's left over for some reason
            }
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
