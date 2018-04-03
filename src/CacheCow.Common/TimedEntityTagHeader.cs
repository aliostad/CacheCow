using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Common
{
    /// <summary>
    /// A construct representing two options of Cache Validation: ETag and LastModified
    /// </summary>
	public class TimedEntityTagHeaderValue
	{
        /// <summary>
        /// Either this or ETag is null. I could have used Scala's Either but pattern matching is new in c#
        /// </summary>
		public DateTimeOffset? LastModified { get; set; }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="tag">Opaque string representing the version of the resource</param>
        /// <param name="isWeak">Whether it is weak</param>
		public TimedEntityTagHeaderValue(string tag, bool isWeak = false)
		{
            if (tag == null)
                throw new ArgumentNullException("tag");
            ETag = new EntityTagHeaderValue(tag, isWeak);
		}

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="entityTagHeaderValue">ETag</param>
        public TimedEntityTagHeaderValue(EntityTagHeaderValue entityTagHeaderValue)
        {
            ETag = entityTagHeaderValue;
        }

        /// <summary>
        /// .ctor so the Cache validation is done using LastModified
        /// Beware! Use this option if you do not care about milliseconds. Sadly, HTTP time does not have millisecond accuracy.
        /// </summary>
        /// <param name="lastModified">Last modified of the resource</param>
        public TimedEntityTagHeaderValue(DateTimeOffset lastModified)
            : this((string)null)
        {
            LastModified = lastModified;
        }

        /// <summary>
        /// Either this or LastModified is null. I could have used Scala's Either but pattern matching is new in c#
        /// </summary>
        public EntityTagHeaderValue ETag { get; }

	}
}
