namespace CacheCow.Server.EntityTagStore.MongoDb.Tests.Embedded
{
	using System.Diagnostics;
	using System.IO;
	using System.Linq;

	public interface IMongoDeployer
	{
		void Deploy(int pid, bool redeploy = false);
		void Kill(int pid);
		bool IsRunning(int pid);
	}

	public class MongoDeployer : IMongoDeployer
	{
		private readonly IResource resource;
		private readonly MongoTargets targets;

		public MongoDeployer(IResource resource, MongoTargets targets)
		{
			this.resource = resource;
			this.targets = targets;
		}

		#region IMongoDeployer Members

		public virtual void Deploy(int pid, bool redeploy = false)
		{
			if (redeploy)
			{
				Kill(pid);
				Delete();
			}

			DeployIfNeeded();
		}

		public virtual void Kill(int pid)
		{
			while (IsRunning(pid))
			{
				var processes = Process.GetProcesses().Where(p => p.Id == pid && p.ProcessName.ToLower().Contains("mongo")).ToList();
				try
				{
					processes.ForEach(p => p.Kill());
				}
				catch
				{
					/*Not interested in any feedback*/
				}
			}
		}

		public virtual bool IsRunning(int pid)
		{
			return Process.GetProcesses().Where(p => p.Id == pid && p.ProcessName.ToLower().Contains("mongo")).Any();
		}

		#endregion

		private void DeployIfNeeded()
		{
			var targetDirectory = new DirectoryInfo(targets.Directory);
			if (!targetDirectory.Exists)
			{
				targetDirectory.Create();
				new DirectoryInfo(targets.DataDirectory).Create();

				targets
					.FilePaths.ToList()
					.ForEach(f =>
					{
						using (var outputStream = new FileStream(f, FileMode.Create))
							resource.CopyStream(Path.GetFileName(f), outputStream);
					});
			}
		}

		protected virtual void Delete()
		{
			var directory = new DirectoryInfo(targets.Directory);
			if (directory.Exists)
				directory.Delete(true);
		}
	}
}
