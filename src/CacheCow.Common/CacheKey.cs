using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CacheCow.Common
{
	public class CacheKey
	{
		private readonly string _resourceUri;
		private readonly string _toString;
		private readonly string _routePattern;
		private readonly byte[] _hash;
		private readonly string _hashBase64;
		private string _domain = null;

		private const string CacheKeyFormat = "{0}-{1}";

		/// <summary>
		/// constructor for CacheKey
		/// </summary>
		/// <param name="resourceUri">URI of the resource</param>
		/// <param name="headerValues">value of the headers as in the request. Only those values whose named defined in VaryByHeader
		/// must be passed
		/// </param>
		public CacheKey(string resourceUri, IEnumerable<string> headerValues = null)
		{
			_toString = string.Format(CacheKeyFormat, resourceUri, string.Join("-", headerValues));
			using (var sha1 = new SHA1CryptoServiceProvider())
			{
				_hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(_toString));
			}

			_hashBase64 = Convert.ToBase64String(_hash);
            _resourceUri = resourceUri;
		}

        public string ResourceUri
		{
			get { return _resourceUri; }
		}

		public byte[] Hash
		{
			get { return _hash; }
		}

		public string HashBase64
		{
			get { return _hashBase64; }
		}

		public override string ToString()
		{
			return _toString;
		}


		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			var eTagKey = obj as CacheKey;
			if (eTagKey == null)
				return false;
			return ToString() == eTagKey.ToString();
		}

		public override int GetHashCode()
		{
			return _toString.GetHashCode();
		}

	}
}
