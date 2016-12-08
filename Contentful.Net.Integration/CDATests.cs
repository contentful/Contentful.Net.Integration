using System;
using Xunit;
using Contentful.Core;
using System.Net.Http;
using Contentful.Core.Configuration;
using System.Threading.Tasks;
using Contentful.Core.Models;
using System.Linq;
using Contentful.Core.Search;
using System.Threading;

namespace Contentful.Net.Integration
{
    public class ContentfulCDATests
    {
        private ContentfulClient _client;


        public ContentfulCDATests()
        {

            var httpClient = new HttpClient(new TestEndpointMessageHandler());

            _client = new ContentfulClient(httpClient, new ContentfulOptions()
            {
                DeliveryApiKey = "b4c0n73n7fu1",
                ManagementApiKey = "",
                SpaceId = "cfexampleapi",
                UsePreviewApi = false
            });
        }

        [Fact]
        public async Task GetAllEntries()
        {
            var entries = await _client.GetEntriesAsync<Entry<dynamic>>();

            Assert.Equal(10, entries.Count());
        }

        [Fact]
        public async Task GetContentTypes()
        {
            var contentTypes = await _client.GetContentTypesAsync();

            Assert.Equal(4, contentTypes.Count());
        }

        [Theory]
        [InlineData("cat", 8, "Meow.", "name")]
        [InlineData("1t9IbcfdCk6m04uISSsaIK", 2, null, "name")]
        [InlineData("dog", 3, "Bark!", "name")]
        public async Task GetContentTypeById(string id, int expectedFieldLength, string expectedDescription, string expectedDisplayField)
        {
            var contentType = await _client.GetContentTypeAsync(id);

            Assert.Equal(expectedFieldLength, contentType.Fields.Count);
            Assert.Equal(expectedDescription, contentType.Description);
            Assert.Equal(expectedDisplayField, contentType.DisplayField);
        }

        [Theory]
        [InlineData("nyancat", "Nyan Cat", "rainbow")]
        [InlineData("happycat", "Happy Cat", "gray")]
        public async Task GetSpecificEntries(string id, string expectedName, string expectedColor)
        {
            var entry = await _client.GetEntryAsync<Entry<dynamic>>(id);

            Assert.Equal(expectedName, entry.Fields.name.ToString());
            Assert.Equal(expectedColor, entry.Fields.color.ToString());
        }

        [Fact]
        public async Task GetSpace()
        {
            var space = await _client.GetSpaceAsync();

            Assert.Equal("Contentful Example API", space.Name);
            Assert.Equal("cfexampleapi", space.SystemProperties.Id);
            Assert.Equal(2, space.Locales.Count);
            Assert.Equal("en-US", space.Locales.Single(c => c.Default).Code);
        }
        
        [Fact]
        public async Task GetAssets()
        {
            var assets = await _client.GetAssetsAsync();

            Assert.Equal(4, assets.Count());

            Assert.Collection(assets, 
                (a) => Assert.Equal("Doge", a.Title),
                (a) => Assert.Equal("Happy Cat", a.Title),
                (a) => Assert.Equal("Nyan Cat", a.Title),
                (a) => Assert.Equal("Jake", a.Title));
        }

        [Theory]
        [InlineData("1x0xpXu4pSGS4OukSyWGUK", "Doge" ,"nice picture")]
        [InlineData("happycat", "Happy Cat", null)]
        [InlineData("nyancat", "Nyan Cat", null)]
        [InlineData("jake", "Jake", null)]
        public async Task GetSpecificAsset(string id, string expectedTitle, string expectedDescription)
        {
            var asset = await _client.GetAssetAsync(id);

            Assert.Equal(expectedTitle, asset.Title);
            Assert.Equal(expectedDescription, asset.Description);
        }

        [Fact]
        public async Task SyncInitial()
        {
            var res = await _client.SyncInitialAsync(SyncType.Asset);

            Assert.Equal(4, res.Assets.Count());
        }

        [Fact]
        public async Task SyncInitialWithContentType()
        {
            var res = await _client.SyncInitialAsync(SyncType.Entry, "cat");

            Assert.Equal(3, res.Entries.Count());
        }

        [Fact]
        public async Task SyncNextResult()
        {
            var res = await _client.SyncInitialAsync(SyncType.Entry);

            var nextResult = await _client.SyncNextResultAsync(res.NextSyncUrl);

            Assert.Equal(0, nextResult.Entries.Count());
        }

        [Fact]
        public async Task GetEntriesByContentType()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().ContentTypeIs("cat"));

            Assert.Equal(3, res.Count());
        }

        [Fact]
        public async Task GetEntriesByField()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().FieldEquals("sys.id", "nyancat"));

            Assert.Equal(1, res.Count());
            Assert.Equal("rainbow", res.First().Fields.color.ToString());
        }

        [Fact]
        public async Task GetEntriesByFieldNotEquals()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().FieldDoesNotEqual("sys.id", "nyancat"));

            Assert.Equal(9, res.Count());
        }

        [Fact]
        public async Task GetEntriesByFieldAndType()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().ContentTypeIs("cat").FieldEquals("fields.color", "rainbow"));

            Assert.Equal(1, res.Count());
            Assert.Equal("rainbow", res.First().Fields.color.ToString());
            Assert.Equal("nyancat", res.First().SystemProperties.Id);
        }

        [Fact]
        public async Task GetEntriesByFieldIncludes()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().FieldIncludes("sys.id", new[] { "nyancat", "jake" }));

            Assert.Equal(2, res.Count());
            Assert.Equal("rainbow", res.First().Fields.color.ToString());
            Assert.Equal("Jake", res.Last().Fields.name.ToString());
        }

        [Fact]
        public async Task GetEntriesByFieldIncludesAndType()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New()
                .ContentTypeIs("cat").FieldIncludes("fields.color", new[] { "rainbow", "gray" }));

            Assert.Equal(2, res.Count());
            Assert.Equal("rainbow", res.Last().Fields.color.ToString());
            Assert.Equal("nyancat", res.Last().SystemProperties.Id);
            Assert.Equal("gray", res.First().Fields.color.ToString());
            Assert.Equal("happycat", res.First().SystemProperties.Id);
        }

        [Fact]
        public async Task GetEntriesByFieldExcludesAndType()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New()
                .ContentTypeIs("cat").FieldExcludes("fields.color", new[] { "rainbow", "gray" }));

            Assert.Equal(1, res.Count());
            Assert.Equal("orange", res.Last().Fields.color.ToString());
            Assert.Equal("garfield", res.Last().SystemProperties.Id);
        }

        [Fact]
        public async Task GetEntriesByFieldExistsAndType()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New()
                .ContentTypeIs("cat").FieldExists("fields.color"));

            Assert.Equal(3, res.Count());
        }

        [Fact]
        public async Task GetEntriesByFieldLessThanOrEqualTo()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().FieldLessThanOrEqualTo("sys.createdAt", "2013-08-28"));

            Assert.Equal(5, res.Count());
            Assert.Contains(res, c => c.SystemProperties.Id == "nyancat");
        }

        [Fact]
        public async Task GetEntriesByFieldGreaterThanOrEqualTo()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().FieldGreaterThanOrEqualTo("sys.createdAt", "2013-08-28"));

            Assert.Equal(5, res.Count());
            Assert.DoesNotContain(res, c => c.SystemProperties.Id == "nyancat");
        }

        [Fact]
        public async Task GetEntriesByQuery()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().FullTextSearch("nyan"));

            Assert.Equal(1, res.Count());
            Assert.Contains(res, c => c.SystemProperties.Id == "nyancat");
        }

        [Fact]
        public async Task GetEntriesByFieldMatchesAndType()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New()
                .ContentTypeIs("cat").FieldMatches("fields.color", "rain"));

            Assert.Equal(1, res.Count());
            Assert.Equal("rainbow", res.Last().Fields.color.ToString());
            Assert.Equal("nyancat", res.Last().SystemProperties.Id);
        }

        [Fact]
        public async Task GetEntriesByNearByAndType()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New()
                .ContentTypeIs("1t9IbcfdCk6m04uISSsaIK").InProximityOf("fields.center", "38, -122"));

            Assert.Equal(4, res.Count());

            Assert.Collection(res, 
                (c) => Assert.Equal("San Francisco", c.Fields.name.ToString()),
                (c) => Assert.Equal("London", c.Fields.name.ToString()),
                (c) => Assert.Equal("Paris", c.Fields.name.ToString()),
                (c) => Assert.Equal("Berlin", c.Fields.name.ToString()));
        }

        [Fact]
        public async Task GetEntriesByWithinAreaAndType()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New()
                .ContentTypeIs("1t9IbcfdCk6m04uISSsaIK").WithinArea("fields.center", "40", "-124", "36", "-120"));

            Assert.Equal(1, res.Count());

            Assert.Collection(res,
                (c) => Assert.Equal("San Francisco", c.Fields.name.ToString()));
        }

        [Fact]
        public async Task GetEntriesAndOrder()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New()
                .OrderBy(new SortOrderBuilder("sys.createdAt").Build()));

            Assert.Equal(10, res.Count());
            Assert.Equal("Nyan Cat", res.First().Fields.name.ToString());
        }

        [Fact]
        public async Task GetEntriesAndOrderReversed()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New()
                .OrderBy(new SortOrderBuilder("sys.createdAt", SortOrder.Reversed).Build()));

            Assert.Equal(10, res.Count());
            Assert.Equal("San Francisco", res.First().Fields.name.ToString());
        }

        [Fact]
        public async Task GetEntriesAndLimit()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().Limit(3));

            Assert.Equal(3, res.Count());
            Assert.Equal(10, res.Total);
        }

        [Fact]
        public async Task GetEntriesAndSkip()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().Skip(3));

            Assert.Equal(7, res.Count());
            Assert.Equal(10, res.Total);
        }

        [Fact]
        public async Task GetEntriesAndInclude()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().Include(3));

            Assert.Equal(10, res.Count());
            Assert.Equal(10, res.Total);
            Assert.Equal(4, res.IncludedAssets.Count());
        }

        [Fact]
        public async Task GetEntriesByFieldLinking()
        {
            var res = await _client.GetEntriesCollectionAsync<Entry<dynamic>>(QueryBuilder.New().ContentTypeIs("cat").FieldEquals("fields.bestFriend.sys.id", "nyancat"));

            Assert.Equal(1, res.Count());
            Assert.Equal("gray", res.First().Fields.color.ToString());
        }

        [Theory]
        [InlineData(MimeTypeRestriction.Image, 4)]
        [InlineData(MimeTypeRestriction.Archive, 0)]
        [InlineData(MimeTypeRestriction.Presentation, 0)]
        public async Task GetAssetsByMimeType(MimeTypeRestriction restriction, int expectedAssets)
        {
            var res = await _client.GetAssetsAsync(QueryBuilder.New().MimeTypeIs(restriction));

            Assert.Equal(expectedAssets, res.Count());
        }

        [Fact]
        public async Task GetEntryByLocale()
        {
            var res = await _client.GetEntriesAsync<Entry<dynamic>>("/nyancat?locale=en-US");

            Assert.Equal(0, res.Count());
        }
    }

    public class TestEndpointMessageHandler : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestUrl = request.RequestUri.ToString();

            requestUrl = requestUrl.Replace("https://cdn.contentful.com/", "http://127.0.0.1:5000/");

            request.RequestUri = new Uri(requestUrl);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
