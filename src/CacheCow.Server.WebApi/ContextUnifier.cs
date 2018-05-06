using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace CacheCow.Server.WebApi
{
    public class ContextUnifier
    {
        private readonly HttpActionContext _actionContext;
        private readonly HttpActionExecutedContext _actionExecutedContext;

        public ContextUnifier(HttpActionContext actionContext)
        {
            this._actionContext = actionContext;
        }

        public ContextUnifier(HttpActionExecutedContext actionExecutedContext)
        {
            this._actionExecutedContext = actionExecutedContext;
        }


        public HttpRequestMessage Request
        {
            get
            {
                return _actionContext == null ? _actionExecutedContext.Request : _actionContext.Request;
            }
        }

        public HttpResponseMessage Response
        {
            get
            {
                return _actionContext == null ? _actionExecutedContext.Response : _actionContext.Response;
            }
            set
            {
                if (_actionContext == null)
                    _actionExecutedContext.Response = value;
                else
                    _actionContext.Response = value;

            }
        }

    }
}
