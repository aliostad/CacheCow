using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CacheCow.Client.Headers
{
	public class CacheCowHeader
	{
		// NOTE: public const has problems such as 
		// if changed, all dependent libraries have to be recompiled
		// but it has the advantage of using at compile-time
		// so conscious decision to use const instead of static readonly

		public const string Name = "x-cachecow";

		public static class ExtensionNames
		{
			public const string WasStale = "was-stale";
			public const string DidNotExist = "did-not-exist";
			public const string NotCacheable = "not-cacheable";
			public const string CacheValidationApplied = "cache-validation-applied";
			public const string RetrievedFromCache = "retrieved-from-cache";
		}

		public bool? WasStale { get; set; }
		public bool? DidNotExist { get; set; }
		public bool? NotCacheable { get; set; }
		public bool? CacheValidationApplied { get; set; }
		public bool? RetrievedFromCache { get; set; }

		public CacheCowHeader()
		{
			Version = Assembly.GetExecutingAssembly()
				.GetName().Version.ToString();
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(Version);
			AddToStringBuilder(sb, WasStale, ExtensionNames.WasStale);
			AddToStringBuilder(sb, NotCacheable, ExtensionNames.NotCacheable);
			AddToStringBuilder(sb, DidNotExist, ExtensionNames.DidNotExist);
			AddToStringBuilder(sb, CacheValidationApplied, ExtensionNames.CacheValidationApplied);
			AddToStringBuilder(sb, RetrievedFromCache, ExtensionNames.RetrievedFromCache);
			return sb.ToString();
		}

		private void AddToStringBuilder(StringBuilder sb, bool? property, string extensionName)
		{
			if (property != null)
			{
				sb.Append(';');
				sb.Append(extensionName);
				sb.Append('=');
				sb.Append(property.Value.ToString().ToLower());
			}
		}

		public static bool TryParse(string value, out CacheCowHeader cacheCowHeader)
		{
			cacheCowHeader = null;

			if (value == null)
				return false;

			if (value == string.Empty)
				return false;

			cacheCowHeader = new CacheCowHeader();
			var chunks = value.Split(new []{";"}, StringSplitOptions.None);
			cacheCowHeader.Version = chunks[0];

			for (int i = 1; i < chunks.Length; i++)
			{
				cacheCowHeader.WasStale = cacheCowHeader.WasStale ?? ParseNameValue(chunks[i], ExtensionNames.WasStale);
				cacheCowHeader.CacheValidationApplied = cacheCowHeader.CacheValidationApplied ?? ParseNameValue(chunks[i], ExtensionNames.CacheValidationApplied);
				cacheCowHeader.NotCacheable = cacheCowHeader.NotCacheable ?? ParseNameValue(chunks[i], ExtensionNames.NotCacheable);
				cacheCowHeader.DidNotExist = cacheCowHeader.DidNotExist ?? ParseNameValue(chunks[i], ExtensionNames.DidNotExist);
				cacheCowHeader.RetrievedFromCache = cacheCowHeader.RetrievedFromCache ?? ParseNameValue(chunks[i], ExtensionNames.RetrievedFromCache);
			}

			return true;
		}

		private static bool? ParseNameValue(string entry, string name)
		{
			if (string.IsNullOrEmpty(entry))
				return null;
			
			var chunks = entry.Split('=');
			if (chunks.Length != 2)
				return null;

			chunks[0] = chunks[0].Trim();
			chunks[1] = chunks[1].Trim();

			if (chunks[0].ToLower() != name)
				return null;

			bool result = false;
			if (!bool.TryParse(chunks[1], out result))
				return null;

			return result;
		}

		public string Version { get; private set; }
	}
}
