using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CacheCow.Client.Tests
{
	public class DeepComparer
	{

		public static IEnumerable<string> Compare<T>(T a, T b)
		{
			var errors = new List<string>();
			RecursiveCompare(string.Empty, a, b, errors);
			return errors;
		}

		private static void RecursiveCompare(string name, object a, object b, List<string> errors)
		{

			if (name.Split('.').Count() >= 10)
				return;

			if (a == b)
				return;

			if (a == null)
			{
				errors.AddError(name, "null", b);
				return;
			}
			else if (b == null)
			{
				errors.AddError(name, a, "null");
				return;
			}

			if (a.Equals(b))
				return;

			if (a.GetType() != b.GetType())
				throw new InvalidOperationException("Comparing objects with different type");

			var type = a.GetType();
			if (type.IsValueType && a is IConvertible)
			{
				RecursiveCompare(name, Convert.ChangeType(a, typeof(string)),
					Convert.ChangeType(b, typeof(string)), errors);
				return;
			}

			if (type == typeof(string))
			{
				errors.AddError(name, a, b);
				return;
			}

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				CompareNullableTypeValues(type.GetGenericArguments()[0], name, a, b, errors);
				return;
			}

			int propCount = 0;
			foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				var methodInfo = propertyInfo.GetGetMethod();
				if (methodInfo.GetParameters().Length == 0)
					RecursiveCompare(name + "." + propertyInfo.Name,
						methodInfo.Invoke(a, null),
						methodInfo.Invoke(b, null),
						errors
						);
				propCount++;
			}

			if (propCount == 0)
				errors.AddError(name, a, b);

		}

		private static void CompareNullableTypeValues(Type type, string name, object a, object b, List<string> errors)
		{
			var methodInfo = typeof(Nullable<>).GetProperty("Value")
				.GetGetMethod().MakeGenericMethod(type);
			RecursiveCompare(name,
				methodInfo.Invoke(a, null),
				methodInfo.Invoke(b, null),
				errors);
		}

		private static bool IsPrimitive(Type t)
		{
			return t == typeof(string) ||
				   t == typeof(float) ||
				   t == typeof(byte) ||
				   t == typeof(Int16) ||
				   t == typeof(Int32) ||
				   t == typeof(Int64) ||
				   t == typeof(double) ||
				   t == typeof(decimal) ||
				   t == typeof(DateTime) ||
				   t == typeof(DateTimeOffset) ||
				   t == typeof(bool) ||
				   t == typeof(char);
		}
	}



	public static class ListOfStringExtensions
	{
		public static void AddError(this List<string> list, string name, object a, object b)
		{
			list.Add(string.Format("{0} -> a:{1} b:{2}", name, a, b));
		}
	}

}
