using CacheCow.Samples.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CacheCow.Samples.Common
{
    public static class Extensions
    {
        public static string ToETagString(this DateTimeOffset dateTimeOffset)
        {
            return TurnDatetimeOffsetToETag(dateTimeOffset);
        }

        private static string TurnDatetimeOffsetToETag(DateTimeOffset dateTimeOffset)
        {
            var dateBytes = BitConverter.GetBytes(dateTimeOffset.UtcDateTime.Ticks);
            var offsetBytes = BitConverter.GetBytes((Int16)dateTimeOffset.Offset.TotalHours);
            return Convert.ToBase64String(dateBytes.Concat(offsetBytes).ToArray());
        }

        public static DateTimeOffset GetMaxLastModified(this IEnumerable<Car> cars)
        {
            return cars.Aggregate(DateTimeOffset.MinValue, (seed, car) => seed > car.LastModified ? seed : car.LastModified);
        }
    }
}
