namespace CacheCow.Server.EntityTagStore.MongoDb.Tests.Embedded
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public class MongoTargets
	{
		public virtual IEnumerable<string> Files
		{
			get
			{
				return new[]
                           {
                               "bsondump.exe",
                               "mongo.exe",
                               "mongod.exe",
                               "mongodump.exe",
                               "mongoexport.exe",
                               "mongofiles.exe",
                               "mongoimport.exe",
                               "mongorestore.exe",
                               "mongos.exe",
                               "mongostat.exe"
                           };
			}
		}

		public virtual IEnumerable<string> FilePaths
		{
			get { return Files.Select(f => Path.Combine(Directory, f)); }
		}

		public virtual string Executable
		{
			get { return Path.Combine(Directory, "mongod.exe"); }
		}

		public virtual string Arguments
		{
			get { return string.Format("--dbpath \"{0}\" --rest --noauth --port {1}", DataDirectory, Port); }
		}

		protected virtual int Port
		{
			get { return 27020; }
		}

		public bool CreateNoWindow
		{
			get { return true; }
		}

		public virtual string ConnectionString
		{
			get { return string.Format("mongodb://localhost:{0}", Port); }
		}

		public virtual string DatabaseName
		{
			get { return "EntityTagStore"; }
		}

		public virtual string Directory
		{
			get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Embedded\\MongoDB"); }
		}

		public virtual string DataDirectory
		{
			get { return Path.Combine(Directory, "Data"); }
		}
	}
}
