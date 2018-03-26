using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Common
{
	public class TimedEntityTagHeaderValue : EntityTagHeaderValue
	{

		public DateTimeOffset LastModified { get; set; }

		public TimedEntityTagHeaderValue(string tag)
			: this(tag, false)
		{

		}

		public TimedEntityTagHeaderValue(EntityTagHeaderValue entityTagHeaderValue)
			: this(entityTagHeaderValue.Tag, entityTagHeaderValue.IsWeak)
		{

		}

		public TimedEntityTagHeaderValue(string tag, bool isWeak)
			: base(tag, isWeak)
		{
			LastModified = DateTimeOffset.Parse(DateTimeOffset.UtcNow.ToString("r")); // to remove milliseconds
		}


		public override string ToString()
		{
			return base.ToString() + "\r\n" + LastModified.ToString("r");
		}

		public static bool TryParse(string timedETagValue, out TimedEntityTagHeaderValue value)
		{
			value = null;
			if (timedETagValue == null)
				return false;

			var strings = timedETagValue.Split(new[] { "\r\n" }, StringSplitOptions.None);
			if (strings.Length != 2)
				return false;

			EntityTagHeaderValue etag = null;
			DateTimeOffset lastModified;
			if (!EntityTagHeaderValue.TryParse(strings[0], out etag))
				return false;

			if (!DateTimeOffset.TryParse(strings[1], out lastModified))
				return false;

			value = new TimedEntityTagHeaderValue(etag.Tag, etag.IsWeak)
			{
				LastModified = lastModified
			};
			return true;

		}

		public EntityTagHeaderValue ToEntityTagHeaderValue()
		{
			return new EntityTagHeaderValue(this.Tag, this.IsWeak);
		}

	}
}
