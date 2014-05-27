using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
            AugmentViewLocator(ref ViewLocator.LocateForModel, CreateEditor);
            base.Configure();
        }

        static Type FindProxy(Func<Type, DependencyObject, object, Type> fallback,
            Type modelType, DependencyObject displayLocation, object context)
        {
            var proxy = modelType.GetCustomAttribute<ProxyModelAttribute>();
            if (proxy != null && proxy.Type != modelType)
                return ViewLocator.LocateTypeForModelType(proxy.Type, displayLocation, context);
            var ret = fallback(modelType, displayLocation, context);
            return ret;
        }

        static UIElement CreateEditor(Func<object, DependencyObject, object, UIElement> fallback,
            object model, DependencyObject displayLocation, object context)
        {
            var editorModel = model as EditorViewModel;
            if(editorModel != null)
                return new RuntTextEditor(editorModel.Language, editorModel.File);

            return fallback(model, displayLocation, context);
        }

        static void AugmentViewTypeLocator(ref Func<Type, DependencyObject, object, Type> lookup, 
            Func<Func<Type, DependencyObject, object, Type>, Type, DependencyObject, object, Type> augmentation)
        {
            var orig = lookup;
            lookup = (modelType, displayLocation, context) => 
                augmentation(orig, modelType, displayLocation, context);
        }

        static void AugmentViewLocator(ref Func<object, DependencyObject, object, UIElement> lookup,
            Func<Func<object, DependencyObject, object, UIElement>, object, DependencyObject, object, UIElement> augmentation)
        {
            var orig = lookup;
            lookup = (model, displayLocation, context) =>
                augmentation(orig, model, displayLocation, context);
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

        private static IEnumerable<Assembly> GetAllAssemblies()
        {
            var binDir = Assembly.GetExecutingAssembly().GetName().CodeBase;
            binDir = binDir.Substring("File:///".Length).Replace('/', Path.DirectorySeparatorChar);
            binDir = Path.GetDirectoryName(binDir);
            var assemblies = Directory.EnumerateFiles(binDir, "*.dll", SearchOption.TopDirectoryOnly);
            foreach(var asm in assemblies)
            {
                Assembly a = null;
                try
                {
                    a = Assembly.LoadFile(asm);
                }
                catch(Exception e)
                {
                    // ignore
                }

                if (a != null)
                    yield return a;
            }
        }
    }
}
