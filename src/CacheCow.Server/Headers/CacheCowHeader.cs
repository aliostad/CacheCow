using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CacheCow.Server.Headers
{
    public class CacheCowHeader
    {
        public const string Name = "x-cachecow-server";
        private const string Pattern = "validation-applied=(True|False);validation-matched=(True|False);short-circuited=(True|False);query-made=(True|False)";
        private static Regex _regex = new Regex(Pattern);

        /// <summary>
        /// Whether validation resulted in short-circuiting and the call to depper layers and the controller was bypassed
        /// </summary>
        public bool ShortCircuited { get; set; }

        /// <summary>
        /// Whether validation requested and applied to the request regardless of the result
        /// </summary>
        public bool ValidationApplied { get; set; }

        /// <summary>
        /// Whether the condition requested met
        /// For GET it means resulted in 304 and for PUT resulted in 412
        /// </summary>
        public bool ValidationMatched { get; set; }

        /// <summary>
        /// Whether a Query was made and returned non-null
        /// </summary>
        public bool QueryMadeAndSuccessful { get; set; }

        public override string ToString()
        {
            return $"validation-applied={ValidationApplied};validation-matched={ValidationMatched};short-circuited={ShortCircuited};query-made={QueryMadeAndSuccessful}";
        }

        public static bool TryParse(string value, out CacheCowHeader header)
        {
            header = null;
            var m = _regex.Match(value);
            if(m.Success)
            {
                header = new CacheCowHeader()
                {
                    ShortCircuited = bool.Parse(m.Groups[3].Value),
                    ValidationApplied = bool.Parse(m.Groups[1].Value),
                    ValidationMatched = bool.Parse(m.Groups[2].Value),
                    QueryMadeAndSuccessful = bool.Parse(m.Groups[4].Value)
                };

                return true;
            }

            return false;
        }
    }
}
