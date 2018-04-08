using CacheCow.Samples.Common;
using CacheCow.Server.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheCow.Samples.MvcCore
{
    public class TimedETagQueryCarRepository : InMemoryCarRepository, ITimedETagQueryCarRepository
    {
        public void Dispose()
        {
            // nothing
        }

        public Task<TimedEntityTagHeaderValue> QueryAsync(ResourceExecutingContext context)
        {
            int? id = 0;
            if (context.RouteData.Values.ContainsKey("id"))
                id = (int)context.RouteData.Values["id"];
            
            if(id.HasValue) // Get one car
            {
                if (_cars.ContainsKey(id.Value))
                    return Task.FromResult(new TimedEntityTagHeaderValue(TurnDatetimeOffsetToETag(_cars[id.Value].LastModified)));
                else
                    return Task.FromResult((TimedEntityTagHeaderValue)null);
            }
            else // all cars
            {               
                var maxDatetimeoffset = _cars.Values.Aggregate(DateTimeOffset.MinValue, (seed, car) => car.LastModified > seed ? car.LastModified : seed);
                return Task.FromResult(new TimedEntityTagHeaderValue(TurnDatetimeOffsetToETag(maxDatetimeoffset)));
            }
        }

        private string TurnDatetimeOffsetToETag(DateTimeOffset dateTimeOffset)
        {
            var dateBytes = BitConverter.GetBytes(dateTimeOffset.UtcDateTime.Ticks);
            var offsetBytes = BitConverter.GetBytes((Int16)dateTimeOffset.Offset.TotalHours);
            return Convert.ToBase64String(dateBytes.Concat(offsetBytes).ToArray());
        }
    }

   
}
