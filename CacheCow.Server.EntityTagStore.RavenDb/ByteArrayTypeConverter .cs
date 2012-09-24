using System;
using System.Text;
using System.Web;
using Raven.Client.Converters;

namespace CacheCow.Server.EntityTagStore.RavenDb {
	public class ByteArrayTypeConverter : ITypeConverter {
		public bool CanConvertFrom(Type sourceType) {
			return sourceType.Equals(typeof(byte[]));
		}

		public string ConvertFrom(string tag, object value, bool allowNull) {
			var enc = new ASCIIEncoding();
			var convertFrom = enc.GetString(value as byte[]);
			return convertFrom;
		}

		public object ConvertTo(string value) {
			var enc = new ASCIIEncoding();
			return enc.GetBytes(value);
		}
	}
}