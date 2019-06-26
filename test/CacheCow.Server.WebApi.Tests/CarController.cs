using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace CacheCow.Server.WebApi.Tests
{
    public class CarController : ApiController
    {

        [HttpGet]
        [HttpCache(DefaultExpirySeconds = 10)]
        public IHttpActionResult Get(int id)
        {
            if (id == 42)
                throw new MeaningOfLifeException();

            if (id == 999)
                return new NotFoundResult(this);
            else
                return Ok(new {id});
        }

        
    }

    public class MeaningOfLifeException : Exception
    {
        
    }

}
