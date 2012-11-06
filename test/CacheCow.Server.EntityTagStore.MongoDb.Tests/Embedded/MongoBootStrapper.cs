namespace CacheCow.Server.EntityTagStore.MongoDb.Tests.Embedded
{
	using System.Diagnostics;

	public enum MongoContextType
	{
		Test,
		Live
	}

	public interface IMongoBootstrapper
	{
		void Startup(MongoContextType context = MongoContextType.Live);
		void Shutdown();

		MongoTargets Targets { get; }
	}

	public class MongoBootstrapper : IMongoBootstrapper
	{
		private int pid;
		private readonly IMongoDeployer deployer;
		private readonly MongoTargets targets = new MongoTargets();

		public MongoBootstrapper(IMongoDeployer deployer)
		{
			this.deployer = deployer;
		}

		public MongoTargets Targets
		{
			get
			{
				return this.targets;
			}
		}

		#region IMongoBootstrapper Members

		public virtual void Startup(MongoContextType context = MongoContextType.Live)
		{
			deployer.Deploy(this.pid, context == MongoContextType.Test);
			if (!deployer.IsRunning(this.pid))
			{
				var process = new Process();
				process.StartInfo.FileName = this.Targets.Executable;
				process.StartInfo.Arguments = this.Targets.Arguments;
				process.StartInfo.WorkingDirectory = this.Targets.Directory;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = this.Targets.CreateNoWindow;
				process.Start();

				this.pid = process.Id;
			}
		}

		public virtual void Shutdown()
		{
			deployer.Kill(this.pid);
		}

		#endregion
	}
}
