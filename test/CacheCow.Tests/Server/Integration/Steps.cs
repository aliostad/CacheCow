using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using CacheCow.Common;
using CacheCow.Server;
using CacheCow.Server.ETagGeneration;
using CacheCow.Tests.Server.Integration.MiniServer;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace CacheCow.Tests.Server.Integration
{


    [Binding]
    public class Steps
    {
        public class Keys
        {
            public const string Client = "Client";
            public const string ItemId = "Id";
            public const string CacheHandler = "CacheHandler";
        }

        private const string ServerUrl = "http://gypsylife/api/";

        [Given(@"I have an API running CacheCow Server and using (.*) storage")]
        public void GivenIHaveAnAPIRunningCacheCowServerAndUsingStorage(string storage)
        {
            IEntityTagStore store;
            var configuration = new HttpConfiguration();
            switch (storage)
            {
                case "InMemory":
                    store = new InMemoryEntityTagStore();
                    break;
                default:
                    throw new ArgumentException("Store unknown: " + storage);
            }

            configuration.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            var inMemoryServer = new InMemoryServer(configuration);
            var cachingHandler = new CachingHandler(configuration, store, "Accept")
            {
                InnerHandler = inMemoryServer
            };
            var client = new HttpClient(cachingHandler);

            ScenarioContext.Current[Keys.Client] = client;
            ScenarioContext.Current[Keys.CacheHandler] = cachingHandler;
        }

        [Given(@"I Create a new item")]
        public void GivenICreateANewItem()
        {
            var client = (HttpClient)ScenarioContext.Current[Keys.Client];
            var result = client.PostAsync(ServerUrl + "Item?name=Chipish", null).Result;
            ScenarioContext.Current[Keys.ItemId] = result.Headers.Location.Segments.Last();
        }

        [Given(@"Get the collection ETag as (.*)")]
        public void GivenGetTheCollectionETagAsEtag(string etagName)
        {
            WhenGetTheCollectionETagAsEtag(etagName);
        }

        [When(@"I create a new item")]
        public void WhenICreateANewItem()
        {
            GivenICreateANewItem();
        }

        [When(@"Get the collection ETag as (.*)")]
        public void WhenGetTheCollectionETagAsEtag(string etagName)
        {
            var client = (HttpClient)ScenarioContext.Current[Keys.Client];
            var result = client.GetAsync(ServerUrl + "Item").Result;
            ScenarioContext.Current[etagName] = result.Headers.ETag.Tag;
        }

        [Then(@"I expect (.*) to be different from (.*)")]
        public void ThenIExpectETagToBeDifferentFromETag(string first, string second)
        {
            Assert.AreNotEqual(ScenarioContext.Current[first], ScenarioContext.Current[second]);
        }

        [Given(@"Get the instance ETag as (.*)")]
        public void GivenGetTheInstanceETagAsETag(string etagName)
        {
            WhenGetTheInstanceETagAsETag(etagName);
        }


        [When(@"I update the item")]
        public void WhenIUpdateTheItem()
        {
            var client = (HttpClient)ScenarioContext.Current[Keys.Client];
            var result = client.PutAsync(ServerUrl + "Item/" + 
                ScenarioContext.Current[Keys.ItemId] + "?name=newName", null).Result;
        }

        [When(@"Get the instance ETag as (.*)")]
        public void WhenGetTheInstanceETagAsETag(string etagName)
        {
            var client = (HttpClient)ScenarioContext.Current[Keys.Client];
            var result = client.GetAsync(ServerUrl + "Item/" + ScenarioContext.Current[Keys.ItemId]).Result;
            ScenarioContext.Current[etagName] = result.Headers.ETag.Tag;
        }

        [Given(@"I use Content Based Hash Generation")]
        public void GivenIUseContentBasedHashGeneration()
        {
            var handler = (CachingHandler)ScenarioContext.Current[Keys.CacheHandler];
            handler.ETagValueGenerator = new ContentHashETagGenerator().Generate;
        }



    }
}
