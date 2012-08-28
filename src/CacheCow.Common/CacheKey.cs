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
		private readonly IEnumerable<string> _headerValues;
		private readonly string _toString;
		private readonly string _routePattern;
		private readonly byte[] _hash;
		private readonly string _hashBase64;
		private string _domain = null;

		private const string CacheKeyFormat = "{0}-{1}";

		public CacheKey(string resourceUri, IEnumerable<string> headerValues)
			: this(resourceUri, headerValues, resourceUri)
		{

		}

		/// <summary>
		/// constructor for CacheKey
		/// </summary>
		/// <param name="resourceUri">URI of the resource</param>
		/// <param name="headerValues">value of the headers as in the request. Only those values whose named defined in VaryByHeader
		/// must be passed
		/// </param>
		/// <param name="routePattern">route pattern for the URI. by default it is the same
		/// but in some cases it could be different.
		/// For example /api/cars/fastest and /api/cars/mostExpensive can share tha pattern /api/cars/*
		/// This will be used at the time of cache invalidation. 
		/// </param>
		public CacheKey(string resourceUri, IEnumerable<string> headerValues, string routePattern)
		{
			_routePattern = routePattern;
			_headerValues = headerValues.ToList();
			_resourceUri = resourceUri;
			_toString = string.Format(CacheKeyFormat, resourceUri, string.Join("-", headerValues));
			using (var sha1 = new SHA1CryptoServiceProvider())
			{
				_hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(_toString));
			}
			_hashBase64 = Convert.ToBase64String(_hash);

		}

		public string ResourceUri
		{
			get { return _resourceUri; }
		}

		public string RoutePattern
		{
			get { return _routePattern; }
		}

		public byte[] Hash
		{
			get { return _hash; }
		}

		public string HashBase64
		{
			get { return _hashBase64; }
		}

		public string Domain
		{
			get
			{
				if(_domain == null)
				{
					_domain = new Uri(_resourceUri).Host;
				}
				return _domain;
			}
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
