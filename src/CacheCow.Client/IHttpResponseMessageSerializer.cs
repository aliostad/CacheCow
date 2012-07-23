using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace CacheCow.Client
{
	public interface IHttpResponseMessageSerializer
	{
		void Serialize(HttpResponseMessage response, Stream stream);
		HttpResponseMessage Deserialize(Stream stream);
	}
}
