using Contentful.Core;
using Contentful.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Contentful.Net.Integration
{
    public class ContentfulCMATests
    {
        private ContentfulManagementClient _client;

        public ContentfulCMATests()
        {

            var httpClient = new HttpClient(new TestEndpointMessageHandler());

            _client = new ContentfulManagementClient(httpClient, new ContentfulOptions()
            {
                DeliveryApiKey = "b4c0n73n7fu1",
                ManagementApiKey = "",
                SpaceId = "cfexampleapi",
                UsePreviewApi = false
            });
        }
    }
}
