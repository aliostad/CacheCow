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

        public static string ToETagString(this DateTimeOffset dateTimeOffset, int count)
        {
            return TurnDatetimeOffsetAndCountToETag(dateTimeOffset, count);
        }

        private static string TurnDatetimeOffsetToETag(DateTimeOffset dateTimeOffset)
        {
            var dateBytes = BitConverter.GetBytes(dateTimeOffset.UtcDateTime.Ticks);
            var offsetBytes = BitConverter.GetBytes((Int16)dateTimeOffset.Offset.TotalHours);
            return Convert.ToBase64String(dateBytes.Concat(offsetBytes).ToArray());
        }

        private static string TurnDatetimeOffsetAndCountToETag(DateTimeOffset dateTimeOffset, int count)
        {
            var dateBytes = BitConverter.GetBytes(dateTimeOffset.UtcDateTime.Ticks);
            var offsetBytes = BitConverter.GetBytes((Int16)dateTimeOffset.Offset.TotalHours);
            var allBytes = dateBytes.Concat(offsetBytes).Concat(BitConverter.GetBytes(count)).ToArray();
            return Convert.ToBase64String(allBytes);
        }

        public static DateTimeOffset GetMaxLastModified(this IEnumerable<Car> cars)
        {
            return cars.Aggregate(DateTimeOffset.MinValue, (seed, car) => seed > car.LastModified ? seed : car.LastModified);
        }
    }
}
