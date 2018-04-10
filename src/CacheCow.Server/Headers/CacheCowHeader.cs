using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CacheCow.Server.Headers
{
    public class CacheCowHeader
    {
        public const string Name = "x-cachecow-server";
        private const string Pattern = "ValidationApplied=(True|False);ValidationMatched=(True|False);ShortCircuited=(True|False)";
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
        /// For GET it means resulted in 304 and for PUT resulted in 409
        /// </summary>
        public bool ValidationMatched { get; set; }

        public override string ToString()
        {
            return $"ValidationApplied={ValidationApplied};ValidationMatched={ValidationMatched};ShortCircuited={ShortCircuited}";
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
                    ValidationMatched = bool.Parse(m.Groups[2].Value)
                };

                return true;
            }

            return false;
        }
    }
}
