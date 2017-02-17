using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using CacheCow.Server.ETagGeneration;

namespace CacheCow.Tests.Server.Integration.MiniServer
{
    public class ItemController : ApiController
    {
        public IEnumerable<Item> Get()
        {
            return Item.Items.Values;
        }

        [ContentHashETag]
        public Item Get(int id)
        {
            return Item.Items[id];
        }

        public HttpResponseMessage Post(string name)
        {
            int id = Item.Items.Count + 1;
            Item.Items.Add(id, new Item()
            {
                Name = name,
                Id = id
            });

            var response = Request.CreateResponse(HttpStatusCode.Created);
            response.Headers.Location = new Uri(Request.RequestUri,
                                                id.ToString());
            return response;
        }

        public void Put(int id, string name)
        {
            Item.Items[id].Name = name;
        }

        public void Delete(int id)
        {
            Item.Items.Remove(id);
        }

    }
}
