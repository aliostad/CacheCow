using CacheCow.Client;
using CacheCow.Samples.Common;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using CacheCow.Server;
using CacheCow.Server.WebApi;
using System.Web.Http.Dependencies;
using System.Collections.Concurrent;

namespace CacheCow.Samples.WebApi.WithQueryAndIoc
{
    class Program
    {
        static void Main(string[] args)
        {
            const string BaseAddress = "http://localhost:18018";
            var config = new HttpSelfHostConfiguration(BaseAddress);
            var container = new WindsorContainer();
            ConfigDi(container);
            CachingRuntime.RegisterFactory((t) => container.Resolve(t),
                (t1, t2) => container.Register(Component.For(t1).ImplementedBy(t2).LifestyleTransient()));

            config.DependencyResolver = new WindsorDependencyResolver(container);

            config.Routes.MapHttpRoute(
                "API Collection", "api/{controller}s",
                new { id = RouteParameter.Optional });

            config.Routes.MapHttpRoute(
                "API Default", "api/{controller}/{id}",
                new { id = RouteParameter.Optional });

            var server = new HttpSelfHostServer(config);
            server.OpenAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var client = new HttpClient(
                new CachingHandler()
                {
                    InnerHandler = new HttpClientHandler()
                });

            client.BaseAddress = new Uri(BaseAddress);

            var menu = new ConsoleMenu(client);
            menu.Menu().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static void ConfigDi(IWindsorContainer container)
        {
            container.Register(
                Component.For<ApiController>().ImplementedBy<ApiController>(),
                Component.For<ITimedETagExtractor>().ImplementedBy<CarAndCollectionETagExtractor>(),
                Component.For<ITimedETagQueryProvider>().ImplementedBy<TimedETagQueryCarRepository>(),
                Component.For<ICarRepository>().Instance(InMemoryCarRepository.Instance)

                );
        }
    }

    public class WindsorDependencyScope : IDependencyScope
    {

        protected readonly IWindsorContainer _container;
        private ConcurrentBag<object> _toBeReleased = new ConcurrentBag<object>();

        public WindsorDependencyScope(IWindsorContainer container)
        {
            _container = container;
        }

        public void Dispose()
        {
            if (_toBeReleased != null)
            {
                foreach (var o in _toBeReleased)
                {
                    _container.Release(o);
                }
            }
            _toBeReleased = null;
        }

        public object GetService(Type serviceType)
        {
            if (!_container.Kernel.HasComponent(serviceType))
                return null;

            var resolved = _container.Resolve(serviceType);
            if (resolved != null)
                _toBeReleased.Add(resolved);
            return resolved;

        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            if (!_container.Kernel.HasComponent(serviceType))
                return new object[0];


            var allResolved = _container.ResolveAll(serviceType).Cast<object>();
            if (allResolved != null)
            {
                allResolved.ToList()
                    .ForEach(x => _toBeReleased.Add(x));
            }
            return allResolved;

        }
    }

    public class WindsorDependencyResolver : IDependencyResolver
    {
        private readonly IWindsorContainer _container;

        public WindsorDependencyResolver(IWindsorContainer container)
        {
            _container = container;
        }

        public IDependencyScope BeginScope()
        {
            return new WindsorDependencyScope(_container);
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public object GetService(Type serviceType)
        {
            if (!_container.Kernel.HasComponent(serviceType))
                return null;

            return _container.Resolve(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            if (!_container.Kernel.HasComponent(serviceType))
                return new object[0];

            return _container.ResolveAll(serviceType).Cast<object>();
        }
    }

}
