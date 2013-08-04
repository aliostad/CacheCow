using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheCow.Common;
using NUnit.Framework;

namespace CacheCow.Client.FileCacheStore.Tests
{
    [Ignore]
	[TestFixture]
	public class ConcurrencyTests
	{

		private string _rootPath;
		private Random _random = new Random();
		private const int ConcurrencyLevel = 100;
		private const int WaitTimeOut = 20000; // 20 seconds   
		private RandomResponseBuilder _responseBuilder = new RandomResponseBuilder(ConcurrencyLevel);

		[SetUp]
		public void Setup()
		{
			ThreadPool.SetMinThreads(100, 1000);
			_rootPath = Path.Combine(Path.GetTempPath(), _random.Next().ToString());
			Directory.CreateDirectory(_rootPath);
		}

		[TearDown]
		public void TearDown()
		{
			int retry = 0;
			while (retry<3)
			{
				try
				{
					Directory.Delete(_rootPath, true);
					break;
				}
				catch(Exception e)
				{
					Console.WriteLine(e.Message);
					retry++;
					Thread.Sleep(500);
				}
				
			}
		}

		[Test]
		public void Test()
		{
			var store = new FileStore(_rootPath);

			List<Task> tasks = new List<Task>();
			HttpResponseMessage responseMessage = null;
			for (int i = 0; i < ConcurrencyLevel; i++)
			{
				var message = GetMessage(i % ConcurrencyLevel);
				var cacheKey = new CacheKey(message.RequestUri.ToString(), new string[0]);


				tasks.Add(new Task(
						() => store.AddOrUpdate(cacheKey, _responseBuilder.Send(message))));

				tasks.Add(new Task(
						() => store.TryGetValue(cacheKey, out responseMessage)));

				tasks.Add(new Task(
				        () => store.TryRemove(cacheKey)));


			}

			var randomisedList = new List<Task>();
			//while (tasks.Count>0)
			//{
			//    var i = _random.Next(tasks.Count);
			//    randomisedList.Add(tasks[i]);
			//    tasks.RemoveAt(i);
			//}
			
			//tasks = randomisedList;

			foreach (var task in tasks)
			{
				task.ContinueWith(t =>
				                  	{
				                  		if (t.IsFaulted)
				                  			Assert.Fail(t.Exception.ToString());  
				                  	});
				task. Start();
			}

			DateTime tt = DateTime.Now;
			var waited = Task.WaitAll(tasks.ToArray(), WaitTimeOut); //
			Console.WriteLine("Total milliseconds " + (DateTime.Now - tt).TotalMilliseconds);
			if(!waited)
				Assert.Fail("Timed out");
		}

		private HttpRequestMessage GetMessage(int number)
		{
			return new HttpRequestMessage(HttpMethod.Get,
				"http://carmanager.softxnet.co.uk/api/car/" + number);
		}
	}
}
