namespace CacheCow.Server.EntityTagStore.MongoDb.Tests.Embedded
{
	using System;
	using System.IO;
	using System.Reflection;

	public interface IResource
	{
		Stream GetStream(string resourceName);
		void CopyStream(Stream input, Stream outputStream);
		void CopyStream(string resourceName, Stream outputStream);
	}

	public class Resource : IResource
	{
		public virtual Stream GetStream(string resourceName)
		{
			var assembly = Assembly.GetExecutingAssembly();
			//foreach (var assembly in assemblies)
			//{
			try
			{
				var actualResourceName = "";
				var resources = assembly.GetManifestResourceNames();
				foreach (var resource in resources)
					if (resource.ToLower().EndsWith(resourceName.ToLower()))
						actualResourceName = resource;

				if (actualResourceName != "")
					return assembly.GetManifestResourceStream(actualResourceName);
			}
			catch
			{
			}
			//}
			return null;
		}

		public virtual void CopyStream(string resourceName, Stream outputStream)
		{
			using (var inputStream = GetStream(resourceName))
				CopyStream(inputStream, outputStream);
		}

		public virtual void CopyStream(Stream input, Stream outputStream)
		{
			var buffer = new byte[8 * 1024];
			int len;
			while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
				outputStream.Write(buffer, 0, len);
		}
	}
}
