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
            var entries = await _client.GetEntries<dynamic>();

            Assert.Equal(10, entries.Count());
        }

        [Fact]
        public async Task GetContentTypes()
        {
            var contentTypes = await _client.GetContentTypes();

            Assert.Equal(4, contentTypes.Count());
        }

        [Theory]
        [InlineData("cat", 8, "Meow.", "name")]
        [InlineData("1t9IbcfdCk6m04uISSsaIK", 2, null, "name")]
        [InlineData("dog", 3, "Bark!", "name")]
        public async Task GetContentTypeById(string id, int expectedFieldLength, string expectedDescription, string expectedDisplayField)
        {
            var contentType = await _client.GetContentType(id);

            Assert.Equal(expectedFieldLength, contentType.Fields.Count);
            Assert.Equal(expectedDescription, contentType.Description);
            Assert.Equal(expectedDisplayField, contentType.DisplayField);
        }

        [Theory]
        [InlineData("nyancat", "Nyan Cat", "rainbow")]
        [InlineData("happycat", "Happy Cat", "gray")]
        public async Task GetSpecificEntries(string id, string expectedName, string expectedColor)
        {
            var entry = await _client.GetEntry<dynamic>(id);

            Assert.Equal(expectedName, entry.name.ToString());
            Assert.Equal(expectedColor, entry.color.ToString());
        }

        [Fact]
        public async Task GetSpace()
        {
            var space = await _client.GetSpace();

            Assert.Equal("Contentful Example API", space.Name);
            Assert.Equal("cfexampleapi", space.SystemProperties.Id);
            Assert.Equal(2, space.Locales.Count);
            Assert.Equal("en-US", space.Locales.Single(c => c.Default).Code);
        }
        
        [Fact]
        public async Task GetAssets()
        {
            var assets = await _client.GetAssets();

            Assert.Equal(4, assets.Count());
            Assert.Contains(assets, c => c.Title == "Doge");
            Assert.Contains(assets, c => c.Title == "Happy Cat");
            Assert.Contains(assets, c => c.Title == "Nyan Cat");
            Assert.Contains(assets, c => c.Title == "Jake");
        }

        [Theory]
        [InlineData("1x0xpXu4pSGS4OukSyWGUK", "Doge" ,"nice picture")]
        [InlineData("happycat", "Happy Cat", null)]
        [InlineData("nyancat", "Nyan Cat", null)]
        [InlineData("jake", "Jake", null)]
        public async Task GetSpecificAsset(string id, string expectedTitle, string expectedDescription)
        {
            var asset = await _client.GetAsset(id);

            Assert.Equal(expectedTitle, asset.Title);
            Assert.Equal(expectedDescription, asset.Description);
        }

        [Fact]
        public async Task SyncInitial()
        {
            var res = await _client.SyncInitial(SyncType.Asset);

            Assert.Equal(4, res.Assets.Count());
        }

        [Fact]
        public async Task SyncInitialWithContentType()
        {
            var res = await _client.SyncInitial(SyncType.Entry, "cat");

            Assert.Equal(3, res.Entries.Count());
        }

        [Fact]
        public async Task SyncNextResult()
        {
            var res = await _client.SyncInitial(SyncType.Entry);

            var nextResult = await _client.SyncNextResult(res.NextSyncUrl);

            Assert.Empty(nextResult.Entries);
        }

        [Fact]
        public async Task GetEntriesByContentType()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.ContentTypeIs("cat"));

            Assert.Equal(3, res.Count());
        }

        [Fact]
        public async Task GetEntriesByField()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.FieldEquals("sys.id", "nyancat"));

            Assert.Single(res);
            Assert.Equal("rainbow", res.First().color.ToString());
        }

        [Fact]
        public async Task GetEntriesByFieldNotEquals()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.FieldDoesNotEqual("sys.id", "nyancat"));

            Assert.Equal(9, res.Count());
        }

        [Fact]
        public async Task GetEntriesByFieldAndType()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.ContentTypeIs("cat").FieldEquals("fields.color", "rainbow"));

            Assert.Single(res);
            Assert.Equal("rainbow", res.First().color.ToString());
            Assert.Equal("nyancat", res.First().sys.id.ToString());
        }

        [Fact]
        public async Task GetEntriesByFieldIncludes()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.FieldIncludes("sys.id", new[] { "nyancat", "jake" }));

            Assert.Equal(2, res.Count());
            Assert.Contains(res, c => c.color?.ToString() == "rainbow");
            Assert.Contains(res, c => c.name?.ToString() == "Jake");
        }

        [Fact]
        public async Task GetEntriesByFieldIncludesAndType()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New
                .ContentTypeIs("cat").FieldIncludes("fields.color", new[] { "rainbow", "gray" }));

            Assert.Equal(2, res.Count());
            Assert.Equal("rainbow", res.Last().color.ToString());
            Assert.Equal("nyancat", res.Last().sys.id.ToString());
            Assert.Equal("gray", res.First().color.ToString());
            Assert.Equal("happycat", res.First().sys.id.ToString());
        }

        [Fact]
        public async Task GetEntriesByFieldExcludesAndType()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New
                .ContentTypeIs("cat").FieldExcludes("fields.color", new[] { "rainbow", "gray" }));

            Assert.Single(res);
            Assert.Equal("orange", res.Last().color.ToString());
            Assert.Equal("garfield", res.Last().sys.id.ToString());
        }

        [Fact]
        public async Task GetEntriesByFieldExistsAndType()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New
                .ContentTypeIs("cat").FieldExists("fields.color"));

            Assert.Equal(3, res.Count());
        }

        [Fact]
        public async Task GetEntriesByFieldLessThanOrEqualTo()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.FieldLessThanOrEqualTo("sys.createdAt", "2013-08-28"));

            Assert.Equal(5, res.Count());
            Assert.Contains(res, c => c.sys.id.ToString() == "nyancat");
        }

        [Fact]
        public async Task GetEntriesByFieldGreaterThanOrEqualTo()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.FieldGreaterThanOrEqualTo("sys.createdAt", "2013-08-28"));

            Assert.Equal(5, res.Count());
            Assert.DoesNotContain(res, c => c.sys.id.ToString() == "nyancat");
        }

        [Fact]
        public async Task GetEntriesByQuery()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.FullTextSearch("nyan"));

            Assert.Single(res);
            Assert.Contains(res, c => c.sys.id.ToString() == "nyancat");
        }

        [Fact]
        public async Task GetEntriesByFieldMatchesAndType()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New
                .ContentTypeIs("cat").FieldMatches("fields.color", "rain"));

            Assert.Single(res);
            Assert.Equal("rainbow", res.Last().color.ToString());
            Assert.Equal("nyancat", res.Last().sys.id.ToString());
        }

        [Fact]
        public async Task GetEntriesByNearByAndType()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New
                .ContentTypeIs("1t9IbcfdCk6m04uISSsaIK").InProximityOf("fields.center", "38,-122"));

            Assert.Equal(4, res.Count());

            Assert.Collection(res, 
                (c) => Assert.Equal("San Francisco", c.name.ToString()),
                (c) => Assert.Equal("London", c.name.ToString()),
                (c) => Assert.Equal("Paris", c.name.ToString()),
                (c) => Assert.Equal("Berlin", c.name.ToString()));
        }

        [Fact]
        public async Task GetEntriesByWithinAreaAndType()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New
                .ContentTypeIs("1t9IbcfdCk6m04uISSsaIK").WithinArea("fields.center", "40", "-124", "36", "-120"));

            Assert.Single(res);

            Assert.Collection(res,
                (c) => Assert.Equal("San Francisco", c.name.ToString()));
        }

        [Fact]
        public async Task GetEntriesAndOrder()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New
                .OrderBy(SortOrderBuilder<dynamic>.New("sys.createdAt").Build()));

            Assert.Equal(10, res.Count());
            Assert.Equal("Nyan Cat", res.First().name.ToString());
        }

        [Fact]
        public async Task GetEntriesAndOrderReversed()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New
                .OrderBy(SortOrderBuilder<dynamic>.New("sys.createdAt", SortOrder.Reversed).Build()));

            Assert.Equal(10, res.Count());
            Assert.Equal("San Francisco", res.First().name.ToString());
        }

        [Fact]
        public async Task GetEntriesAndOrderSeveral()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New
                .OrderBy(SortOrderBuilder<dynamic>.New("sys.createdAt").ThenBy("sys.updatedAt").Build()));

            Assert.Equal(10, res.Count());
            Assert.Equal("Nyan Cat", res.First().name.ToString());
        }

        [Fact]
        public async Task GetEntriesAndLimit()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.Limit(3));

            Assert.Equal(3, res.Count());
            Assert.Equal(10, res.Total);
        }

        [Fact]
        public async Task GetEntriesAndSkip()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.Skip(3));

            Assert.Equal(7, res.Count());
            Assert.Equal(10, res.Total);
        }

        [Fact]
        public async Task GetEntriesAndInclude()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.Include(3));

            Assert.Equal(10, res.Count());
            Assert.Equal(10, res.Total);
            Assert.Equal(4, res.IncludedAssets.Count());
        }

        [Fact]
        public async Task GetEntriesByFieldLinking()
        {
            var res = await _client.GetEntries(QueryBuilder<dynamic>.New.ContentTypeIs("cat").FieldEquals("fields.bestFriend.sys.id", "nyancat"));

            Assert.Single(res);
            Assert.Equal("gray", res.First().color.ToString());
        }

        [Theory]
        [InlineData(MimeTypeRestriction.Image, 4)]
        [InlineData(MimeTypeRestriction.Archive, 0)]
        [InlineData(MimeTypeRestriction.Presentation, 0)]
        public async Task GetAssetsByMimeType(MimeTypeRestriction restriction, int expectedAssets)
        {
            var res = await _client.GetAssets(QueryBuilder<Asset>.New.MimeTypeIs(restriction));

            Assert.Equal(expectedAssets, res.Count());
        }

        [Theory]
        [InlineData("en-US", "Nyan Cat")]
        [InlineData("tlh", "Nyan vIghro'")]
        public async Task GetEntryByLocale(string locale, string expectedName)
        {
            var res = await _client.GetEntry("nyancat", QueryBuilder<dynamic>.New.LocaleIs(locale));

            Assert.Equal(expectedName, res.name.ToString());
            Assert.Equal("rainbow", res.color.ToString());
        }
    }

    public class TestEndpointMessageHandler : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if(Environment.GetEnvironmentVariable("CONTENTFUL_RUN_WITHOUT_PROXY") == "true")
            {
                //return await base.SendAsync(request, cancellationToken);
            }

            var requestUrl = request.RequestUri.ToString();

            requestUrl = requestUrl
                .Replace("https://cdn.contentful.com/", "http://localhost:62933/");

            request.RequestUri = new Uri(requestUrl);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
