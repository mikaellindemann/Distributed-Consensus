using Microsoft.Practices.Unity;
using System.Web.Http;
using Common.Tools;
using Server.Interfaces;
using Server.Logic;
using Server.Storage;
using Unity.WebApi;

namespace Server
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
                .RegisterType<IWorkflowHistoryLogic, WorkflowHistoryLogic>(new HierarchicalLifetimeManager())
                .RegisterType<IServerLogic, ServerLogic>(new HierarchicalLifetimeManager())

                .RegisterType<IServerStorage, ServerStorage>(new HierarchicalLifetimeManager())
                .RegisterType<IServerHistoryStorage, ServerStorage>(new HierarchicalLifetimeManager())

                .RegisterType<IServerContext, StorageContext>(new HierarchicalLifetimeManager())

                .RegisterType<HttpClientToolbox>(new HierarchicalLifetimeManager(), new InjectionConstructor());
            
            config.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}