using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Client
{
	/// <summary>
	/// Default implementation of IHttpResponseMessageSerializer using proprietry format
	/// Does not close the stream since the stream can be used to store other objects
	/// so it has to be closed in the client
	/// </summary>
	public class DefaultHttpResponseMessageSerializer : IHttpResponseMessageSerializer
	{

		private const int FourByteId = 0x73BAB140;

		public void Serialize(HttpResponseMessage response, Stream stream)
		{
			var binaryWriter = new BinaryWriter(stream, Encoding.UTF8);
			var bodyBuffer = response.Content.ReadAsByteArrayAsync().Result;
			binaryWriter.Write(FourByteId);
			binaryWriter.Write(bodyBuffer.Length);
			binaryWriter.Write((int)response.StatusCode);
			binaryWriter.Write(response.ReasonPhrase);
			binaryWriter.Write(FourByteId);
			binaryWriter.Write(response.Headers.ToString());			
			binaryWriter.Write(bodyBuffer);
		}

		public HttpResponseMessage Deserialize(Stream stream)
		{
			var binaryReader = new BinaryReader(stream, Encoding.UTF8);
			var id = binaryReader.ReadInt32();
			if (id != FourByteId)
				throw new InvalidDataException("DefaultHttpResponseMessageSerializer Id is not present");
			var bodySize = binaryReader.ReadInt32();
			var statusCode = binaryReader.ReadInt32();
			var reasonPhrase = binaryReader.ReadString();
			binaryReader.ReadInt32();
			var headers = binaryReader.ReadString();
			var response = new HttpResponseMessage((HttpStatusCode)statusCode);
			response.ReasonPhrase = reasonPhrase;
			foreach (var header in headers.Split(new []{"\r\n"}, StringSplitOptions.RemoveEmptyEntries))
			{
				var indexOfColon = header.IndexOf(":");
				response.Headers.TryAddWithoutValidation(header.Substring(0, indexOfColon), header.Substring(indexOfColon + 1).Trim());
			}

			response.Content = new ByteArrayContent(binaryReader.ReadBytes(bodySize));
			return response;
		}
	}
}
