﻿using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Errors;
using Contentful.Core.Models;
using Contentful.Core.Models.Management;
using Contentful.Net.CMA;
using Newtonsoft.Json;
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
        private string _roleId = "";
        private string _environmentId = "some-env";

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
            var space = await _client.GetSpace(_spaceId);

            Assert.Equal("dotnet-test-space", space.Name);
        }

        [Fact]
        [Order(10)]
        public async Task GetAllSpaces()
        {
            var spaces = await _client.GetSpaces();

            Assert.Contains(spaces, c => c.Name == "blog space");
        }

        [Fact]
        [Order(20)]
        public async Task UpdateSpaceName()
        {
            var space = await _client.UpdateSpaceName(_spaceId, "knuckleburger", 1);
            Assert.Equal("knuckleburger", space.Name);
            space.Name = "dotnet-test-space";
            space = await _client.UpdateSpaceName(space);
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

            var contenttype = await _client.CreateOrUpdateContentType(contentType, _spaceId);

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

            var updatedContentType = await _client.CreateOrUpdateContentType(contentType, _spaceId, version: 1);

            Assert.Equal(2, updatedContentType.Fields.Count);
            Assert.Equal("Cool content changed", updatedContentType.Name);
        }

        [Fact]
        [Order(40)]
        public async Task GetContentType()
        {
            var contentType = await _client.GetContentType(_contentTypeId, _spaceId);

            Assert.Equal(_contentTypeId, contentType.SystemProperties.Id);
        }

        [Fact]
        [Order(50)]
        public async Task GetAllContentTypes()
        {
            //It seems we need to give the API a chance to catch up...
            Thread.Sleep(5000);
            var contentTypes = await _client.GetContentTypes();

            Assert.Single(contentTypes);
        }

        [Fact]
        [Order(60)]
        public async Task PublishContentType()
        {
            var contentType = await _client.ActivateContentType(_contentTypeId, 2);

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

            entry = await _client.CreateOrUpdateEntry(entry, contentTypeId: _contentTypeId);

            Assert.Equal("bla", entry.Fields.field1["en-US"].ToString());
        }

        [Fact]
        [Order(85)]
        public async Task CreateEntryWithoutId()
        {
            var entry = new Entry<dynamic>();

            entry.Fields = new JObject
            (
                new JProperty("field1", new JObject(new JProperty("en-US", "bla"))),
                new JProperty("field2", new JObject(new JProperty("en-US", "blue")))
            );

            entry = await _client.CreateEntry(entry, contentTypeId: _contentTypeId);

            Assert.Equal("bla", entry.Fields.field1["en-US"].ToString());

            await _client.DeleteEntry(entry.SystemProperties.Id, entry.SystemProperties.Version.Value);
        }

        [Fact]
        [Order(90)]
        public async Task GetAllEntries()
        {
            //It seems we need to give the API a chance to catch up...
            Thread.Sleep(10000);
            var entries = await _client.GetEntriesCollection<dynamic>();

            Assert.Single(entries);
        }

        [Fact]
        [Order(100)]
        public async Task GetEntry()
        {
            var entry = await _client.GetEntry("entry1");

            Assert.Equal(1, entry.SystemProperties.Version);
            Assert.Equal("bla", entry.Fields.field1["en-US"].ToString());
        }

        [Fact]
        [Order(110)]
        public async Task PublishEntry()
        {
            var entry = await _client.PublishEntry("entry1", 1);

            Assert.Equal(2, entry.SystemProperties.Version);
            Assert.Equal("bla", entry.Fields.field1["en-US"].ToString());
        }

        [Fact]
        [Order(120)]
        public async Task UnpublishEntry()
        {
            var entry = await _client.UnpublishEntry("entry1", 2);

            Assert.Equal(3, entry.SystemProperties.Version);
            Assert.Equal("bla", entry.Fields.field1["en-US"].ToString());
        }

        [Fact]
        [Order(130)]
        public async Task ArchiveEntry()
        {
            var entry = await _client.ArchiveEntry("entry1", 3);

            Assert.Equal(4, entry.SystemProperties.Version);
            Assert.Equal("bla", entry.Fields.field1["en-US"].ToString());
        }

        [Fact]
        [Order(140)]
        public async Task UnarchiveEntry()
        {
            var entry = await _client.UnarchiveEntry("entry1", 4);

            Assert.Equal(5, entry.SystemProperties.Version);
            Assert.Equal("bla", entry.Fields.field1["en-US"].ToString());
        }

        [Fact]
        [Order(145)]
        public async Task GetAssets()
        {
            var assets = await _client.GetAssetsCollection();

            Assert.Empty(assets);

            var publishedAssets = await _client.GetPublishedAssetsCollection();

            Assert.Empty(publishedAssets);
        }

        [Fact]
        [Order(150)]
        public async Task CreateAsset()
        {
            var asset = new ManagementAsset();
            asset.SystemProperties = new SystemProperties()
            {
                Id = "asset1"
            };
            asset.Title = new Dictionary<string, string>()
            {
                { "en-US", "AssetMaster" }
            };
            asset.Files = new Dictionary<string, File>
            {
                { "en-US", new File()
                    {
                        ContentType ="image/jpeg",
                        FileName = "moby.png",
                        UploadUrl = "https://robertlinde.se/assets/top/moby-top.png"
                    }
                }
            };

            var createdAsset = await _client.CreateOrUpdateAsset(asset);

            Assert.Equal("AssetMaster", createdAsset.Title["en-US"]);
        }

        [Fact]
        [Order(155)]
        public async Task CreateAssetWithoutId()
        {
            var asset = new ManagementAsset();

            asset.Title = new Dictionary<string, string>()
            {
                { "en-US", "AssetMaster" }
            };
            asset.Files = new Dictionary<string, File>
            {
                { "en-US", new File()
                    {
                        ContentType ="image/jpeg",
                        FileName = "moby.png",
                        UploadUrl = "https://robertlinde.se/assets/top/moby-top.png"
                    }
                }
            };

            var createdAsset = await _client.CreateAsset(asset);

            Assert.Equal("AssetMaster", createdAsset.Title["en-US"]);
        }

        [Fact]
        [Order(160)]
        public async Task GetAsset()
        {
            var asset = await _client.GetAsset("asset1");

            Assert.Equal("AssetMaster", asset.Title["en-US"]);
        }

        [Fact]
        [Order(170)]
        public async Task ProcessAsset()
        {
            await _client.ProcessAsset("asset1",1 , "en-US");

            Assert.True(true);
        }

        [Fact]
        [Order(180)]
        public async Task PublishAsset()
        {
            //Give processing a chance to finish...
            Thread.Sleep(5000);
            ManagementAsset asset = null;

            asset = await _client.PublishAsset("asset1", 2);

            Assert.Equal("AssetMaster", asset.Title["en-US"]);
        }

        [Fact]
        [Order(190)]
        public async Task UnpublishAsset()
        {
            var asset = await _client.UnpublishAsset("asset1", 3);

            Assert.Equal("AssetMaster", asset.Title["en-US"]);
        }

        [Fact]
        [Order(200)]
        public async Task ArchiveAsset()
        {
            var asset = await _client.ArchiveAsset("asset1", 4);

            Assert.Equal("AssetMaster", asset.Title["en-US"]);
        }

        [Fact]
        [Order(210)]
        public async Task UnarchiveAsset()
        {
            var asset = await _client.UnarchiveAsset("asset1", 5);

            Assert.Equal("AssetMaster", asset.Title["en-US"]);
        }

        [Fact]
        [Order(220)]
        public async Task GetLocales()
        {
            var locales = await _client.GetLocalesCollection();

            Assert.Equal(1, locales.Total);
        }

        [Fact]
        [Order(230)]
        public async Task CreateGetUpdateDeleteLocale()
        {
            var locale = new Locale();
            locale.Code = "c-sharp";
            locale.Name = "See Sharp";
            locale.FallbackCode = "en-US";
            locale.Optional = true;
            locale.ContentDeliveryApi = true;
            locale.ContentManagementApi = true;

            var createdLocale = await _client.CreateLocale(locale);

            Assert.Equal("c-sharp", createdLocale.Code);

            locale = await _client.GetLocale(createdLocale.SystemProperties.Id);

            Assert.Equal("See Sharp", locale.Name);

            locale.Name = "c#";

            locale = await _client.UpdateLocale(locale);

            Assert.Equal("c#", locale.Name);

            await _client.DeleteLocale(locale.SystemProperties.Id);
        }

        [Fact]
        [Order(240)]
        public async Task GetAllWebHooks()
        {
            var hooks = await _client.GetWebhooksCollection();

            Assert.Equal(0, hooks.Total);
        }

        [Fact]
        [Order(250)]
        public async Task CreateGetUpdateDeleteWebHook()
        {
            var webHook = new Webhook();
            webHook.Name = "Captain Hook";
            webHook.Url = "https://robertlinde.se";
            webHook.HttpBasicPassword = "Pass";
            webHook.HttpBasicUsername = "User";
            webHook.Headers = new List<KeyValuePair<string, string>>();
            webHook.Headers.Add(new KeyValuePair<string, string>("ben", "long"));
            webHook.Topics = new List<string>()
            {
                "Entry.*"
            };

            var createdHook = await _client.CreateWebhook(webHook);

            Assert.Equal("Captain Hook", createdHook.Name);

            webHook.Name = "Dustin Hoffman";
            webHook.SystemProperties = new SystemProperties()
            {
                Id = createdHook.SystemProperties.Id
            };

            var updatedHook = await _client.CreateOrUpdateWebhook(webHook);

            Assert.Equal("Dustin Hoffman", updatedHook.Name);

            webHook = await _client.GetWebhook(updatedHook.SystemProperties.Id);

            Assert.Equal("Dustin Hoffman", webHook.Name);

            var calls = await _client.GetWebhookCallDetailsCollection(webHook.SystemProperties.Id);

            Assert.Empty(calls);

            await Assert.ThrowsAsync<ContentfulException>(async () => await _client.GetWebhookCallDetails("XXX", webHook.SystemProperties.Id));

            var health = await _client.GetWebhookHealth(webHook.SystemProperties.Id);

            await _client.DeleteWebhook(updatedHook.SystemProperties.Id);
        }

        [Fact]
        [Order(610)]
        public async Task GetApiKeys()
        {
            var keys = await _client.GetAllApiKeys();

            Assert.Equal(0, keys.Total);
        }

        [Fact]
        [Order(620)]
        public async Task CreateApiKey()
        {
            var createdKey = await _client.CreateApiKey("Keyport", "blahblah");

            Assert.Equal("Keyport", createdKey.Name);
            Assert.NotNull(createdKey.AccessToken);
        }

        [Fact]
        [Order(600)]
        public async Task DeleteEntry()
        {
            await _client.DeleteEntry("entry1", 1);
        }

        [Fact]
        [Order(610)]
        public async Task DeleteAsset()
        {
            await _client.DeleteAsset("asset1", 5);
        }

        [Fact]
        [Order(620)]
        public async Task GetExtensions()
        {
            var res = await _client.GetAllExtensions();

            Assert.Empty(res);
        }

        [Fact]
        [Order(630)]
        public async Task CreateExtension()
        {
            var extension = new UiExtension
            {
                Name = "Test",
                FieldTypes = new List<string> { SystemFieldTypes.Boolean },
                Sidebar = false,
                Src = "https://robertlinde.se"
            };

            var res = await _client.CreateExtension(extension);

            Assert.Equal("Test", res.Name);
        }

        [Fact]
        [Order(640)]
        public async Task UpdateExtension()
        {
            var extension = new UiExtension
            {
                Name = "Test2",
                FieldTypes = new List<string> { SystemFieldTypes.Boolean },
                Sidebar = false,
                Src = "https://robertlinde.se",
                SystemProperties = new SystemProperties
                {
                    Id = "test"
                }
            };

            var res = await _client.CreateOrUpdateExtension(extension);

            Assert.Equal("Test2", res.Name);
        }

        [Fact]
        [Order(650)]
        public async Task GetExtension()
        {
            var res = await _client.GetExtension("test");

            Assert.Equal("Test2", res.Name);
        }

        [Fact]
        [Order(660)]
        public async Task DeleteExtension()
        {
           await _client.DeleteExtension("test");
        }

        [Fact]
        [Order(660)]
        public async Task GetOrganizations()
        {
           var res = await _client.GetOrganizations();

            Assert.NotEmpty(res);
        }

        [Fact]
        [Order(670)]
        public async Task GetEditorInterfaces()
        {
            var res = await _client.GetEditorInterface(_contentTypeId);

            Assert.Equal(2, res.Controls.Count);
        }

        [Fact]
        [Order(680)]
        public async Task UpdateEditorInterfaces()
        {
            var ei = await _client.GetEditorInterface(_contentTypeId);

            var res = await _client.UpdateEditorInterface(ei, _contentTypeId, ei.SystemProperties.Version.Value);
            Assert.Equal(2, res.Controls.Count);
        }

        [Fact]
        [Order(690)]
        public async Task GetRoles()
        {
            var res = await _client.GetAllRoles();
            _roleId = res.First().SystemProperties.Id;
            Assert.Equal(7, res.Count());
        }

        [Fact]
        [Order(700)]
        public async Task CreateRole()
        {
            var role = new Role
            {
                Name = "Some name",
                Description = "Some description",
            };

            await Assert.ThrowsAsync<ContentfulException>(async () => { await _client.CreateRole(role); });
        }

        [Fact]
        [Order(710)]
        public async Task GetRole()
        {
            var allroles = await _client.GetAllRoles();
            _roleId = allroles.First().SystemProperties.Id;
            var res = await _client.GetRole(_roleId);

            Assert.Equal(allroles.First().Name, res.Name);
            res.Name = "Author2";

            var updated = await _client.UpdateRole(res);

            await _client.DeleteRole(updated.SystemProperties.Id);
        }

        [Fact]
        [Order(720)]
        public async Task GetSnapshotsForContentType()
        {
            var snapshots = await _client.GetAllSnapshotsForContentType(_contentTypeId);

            var snapshot = await _client.GetSnapshotForContentType(snapshots.First().SystemProperties.Id, _contentTypeId);

            Assert.NotNull(snapshot);
        }

        [Fact]
        [Order(720)]
        public async Task GetSnapshotsForEntry()
        {

            var entry = new Entry<dynamic>();

            entry.Fields = new JObject
            (
                new JProperty("field1", new JObject(new JProperty("en-US", "bla"))),
                new JProperty("field2", new JObject(new JProperty("en-US", "blue")))
            );

            entry = await _client.CreateEntry(entry, contentTypeId: _contentTypeId);

            entry.Fields = new JObject
            (
                new JProperty("field1", new JObject(new JProperty("en-US", "bla2"))),
                new JProperty("field2", new JObject(new JProperty("en-US", "blue")))
            );

            entry = await _client.PublishEntry(entry.SystemProperties.Id, version: entry.SystemProperties.Version.Value);

            var snapshots = await _client.GetAllSnapshotsForEntry(entry.SystemProperties.Id);

            var snapshot = await _client.GetSnapshotForEntry(snapshots.First().SystemProperties.Id, entry.SystemProperties.Id);

            Assert.NotNull(snapshot);

            await _client.UnpublishEntry(entry.SystemProperties.Id, entry.SystemProperties.Version.Value);

            await _client.DeleteEntry(entry.SystemProperties.Id, entry.SystemProperties.Version.Value);
        }

        [Fact]
        [Order(720)]
        public async Task GetSpaceMemberships()
        {
            var sm = await _client.GetSpaceMemberships();

            Assert.NotEmpty(sm);
        }

        [Fact]
        [Order(730)]
        public async Task CreateSpaceMembership()
        {
            var newMembership = new SpaceMembership();

            newMembership.Admin = true;

            newMembership.User = new User()
            {
                SystemProperties = new SystemProperties()
                {
                    Id = "4OyBmDxSpgUr9WWv0csvXG",
                    LinkType = "User",
                    Type = "Link"
                }
            };

            await Assert.ThrowsAsync<ContentfulException>(async () => await _client.CreateSpaceMembership(newMembership));
        }

        [Fact]
        [Order(720)]
        public async Task GetSpaceMembership()
        {
            var allMemberships = await _client.GetSpaceMemberships();
            var allroles = await _client.GetAllRoles();

            var singleMembership = await _client.GetSpaceMembership(allMemberships.First().SystemProperties.Id);

            singleMembership = await _client.UpdateSpaceMembership(singleMembership);

            await Assert.ThrowsAsync<ContentfulException>(async () => await _client.DeleteSpaceMembership("XXX"));

            Assert.NotNull(singleMembership);
        }

        [Fact]
        [Order(730)]
        public async Task GetAccessTokens()
        {
            var accessTokens = await _client.GetAllManagementTokens();

            Assert.NotEmpty(accessTokens);
        }

        [Fact]
        [Order(740)]
        public async Task CreateAccessToken()
        {
            var token = new ManagementToken
            {
                Name = "Token name",
                Scopes = new List<string> {
                    SystemManagementScopes.Manage
                }
            };

            var accessToken = await _client.CreateManagementToken(token);

            Assert.Equal("Token name", accessToken.Name);

            token = await _client.GetManagementToken(accessToken.SystemProperties.Id);

            await _client.RevokeManagementToken(accessToken.SystemProperties.Id);
        }

        [Fact]
        [Order(750)]
        public async Task GetCurrentUser()
        {
            var user = await _client.GetCurrentUser();

            Assert.NotNull(user);
        }

        [Fact]
        [Order(740)]
        public async Task GetEnvironments()
        {
            var envs = await _client.GetEnvironments();

            Assert.NotEmpty(envs);
        }

        [Fact]
        [Order(750)]
        public async Task CreateEnvironment()
        {
            var env = await _client.CreateEnvironment("useless");

            Assert.NotNull(env);
        }

        [Fact]
        [Order(760)]
        public async Task CreateEnvironmentById()
        {
            var env = await _client.CreateOrUpdateEnvironment(_environmentId, _environmentId);

            Assert.NotNull(env);
        }

        [Fact]
        [Order(770)]
        public async Task GetEnvironment()
        {
            var env = await _client.GetEnvironment(_environmentId);

            Assert.NotNull(env);
        }

        [Fact]
        [Order(780)]
        public async Task DeleteEnvironment()
        {
            var env = await _client.GetEnvironment(_environmentId);
            var count = 0;
            while(env.SystemProperties.Status.SystemProperties.Id != "ready" && count < 20)
            {
                Thread.Sleep(3000);
                env = await _client.GetEnvironment(_environmentId);
                count++;
            }

            await _client.DeleteEnvironment(_environmentId);
        }

        [Fact]
        [Order(900)]
        public async Task UnpublishContentType()
        {
            //It seems we need to give the API a chance to catch up...
            Thread.Sleep(2000);
            var contentTypes = await _client.GetActivatedContentTypes();

            Assert.Single(contentTypes);
            await _client.DeactivateContentType(_contentTypeId);

            contentTypes = await _client.GetActivatedContentTypes();
            Assert.Empty(contentTypes);
        }

        [Fact]
        [Order(920)]
        public async Task DeleteContentType()
        {
            await _client.DeleteContentType(_contentTypeId);
        }

        [Fact]
        [Order(1000)]
        public async Task DeleteSpace()
        {
            await _client.DeleteSpace(_spaceId);
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

            var space = _client.CreateSpace("dotnet-test-space", "en-US", organisation: "0w9KPTOuMqrBnj9GpkDAwF").Result;

            SpaceId = space.SystemProperties.Id;
        }

        public void Dispose()
        {
            try
            {
                _client.DeleteSpace(SpaceId);
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
