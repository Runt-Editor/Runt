using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Ninject;
using Ninject;
using Runt.ViewModels;

namespace Runt
{
    public class Bootstrapper : BootstrapperBase
    {
        private readonly IKernel _container;

        public Bootstrapper()
        {
            _container = new StandardKernel();
            Start();
        }

        protected override void Configure()
        {
            var config = CreateConfig(Environment.GetCommandLineArgs());
            var serviceCollection = CreateServiceCollection(config);
            NinjectRegistration.Populate(_container, serviceCollection);

            AugmentViewTypeLocator(ref ViewLocator.LocateTypeForModelType, FindProxy);
            base.Configure();
        }

        static Type FindProxy(Func<Type, DependencyObject, object, Type> fallback,
            Type modelType, DependencyObject displayLocation, object context)
        {
            var proxy = modelType.GetCustomAttribute<ProxyModelAttribute>();
            if (proxy != null)
                return fallback(proxy.Type, displayLocation, context);
            return fallback(modelType, displayLocation, context);
        }

        static void AugmentViewTypeLocator(ref Func<Type, DependencyObject, object, Type> lookup, 
            Func<Func<Type, DependencyObject, object, Type>, Type, DependencyObject, object, Type> augmentation)
        {
            var orig = lookup;
            lookup = (modelType, displayLocation, context) => 
                augmentation(orig, modelType, displayLocation, context);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ShellViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return _container.Get(service, key);
        }

        private static IConfiguration CreateConfig(string[] args)
        {
            var config = new Configuration();
            config.AddEnvironmentVariables();
            return config;
        }

        private static IServiceCollection CreateServiceCollection(IConfiguration config)
        {
            var collection = new ServiceCollection(config);
            collection.Add(GetDefaultServices(config));
            return collection;
        }

        private static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration config)
        {
            var describer = new ServiceDescriber(config);

            yield return describer.Singleton<IWindowManager, MetroWindowManager>();
        }
    }
}
