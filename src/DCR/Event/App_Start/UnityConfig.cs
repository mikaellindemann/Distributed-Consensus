using Microsoft.Practices.Unity;
using System.Web.Http;
using Common.Tools;
using Event.Communicators;
using Event.Interfaces;
using Event.Logic;
using Event.Storage;
using Unity.WebApi;

namespace Event
{
    public static class UnityConfig
    {
        public static void RegisterComponents(HttpConfiguration config)
        {
			var container = new UnityContainer();
            
            // register all your components with the container here
            // it is NOT necessary to register your controllers
            
            // e.g. container.RegisterType<ITestService, TestService>();
            container
                .RegisterType<IEventHistoryLogic, EventHistoryLogic>(new HierarchicalLifetimeManager())
                .RegisterType<ILifecycleLogic, LifecycleLogic>(new HierarchicalLifetimeManager())
                .RegisterType<IAuthLogic, AuthLogic>(new HierarchicalLifetimeManager())
                .RegisterType<ILockingLogic, LockingLogic>(new HierarchicalLifetimeManager())
                .RegisterType<IStateLogic, StateLogic>(new HierarchicalLifetimeManager())

                .RegisterType<IEventStorage, EventStorage>(new HierarchicalLifetimeManager())
                .RegisterType<IEventStorageForReset, EventStorageForReset>(new HierarchicalLifetimeManager())
                .RegisterType<IEventHistoryStorage, EventStorage>(new HierarchicalLifetimeManager())

                .RegisterType<IEventFromEvent, EventCommunicator>(new HierarchicalLifetimeManager())
                .RegisterType<IServerFromEvent, ServerCommunicator>(new HierarchicalLifetimeManager())

                .RegisterType<IEventContext, EventContext>(new HierarchicalLifetimeManager())
                
                .RegisterType<HttpClientToolbox>(new HierarchicalLifetimeManager(), new InjectionConstructor());
            
            config.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}