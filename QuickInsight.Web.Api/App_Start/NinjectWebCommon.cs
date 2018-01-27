using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Ninject;
using Ninject.Components;
using Ninject.Extensions.Conventions;
using Ninject.Infrastructure;
using Ninject.Planning.Bindings;
using Ninject.Planning.Bindings.Resolvers;
using Ninject.Syntax;
using Ninject.Web.Common;
using Ninject.Web.Common.WebHost;
using Ninject.Web.Mvc.FilterBindingSyntax;
using QuickInsight.Web.Api;
using QuickInsight.Web.Api.Filter;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(NinjectWebCommon), "Stop")]


namespace QuickInsight.Web.Api
{
    public static class NinjectWebCommon
    {
        private static readonly Bootstrapper Bootstrapper = new Bootstrapper();

        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            Bootstrapper.Initialize(CreateKernel);
        }

        public static void Stop()
        {
            Bootstrapper.ShutDown();
        }

        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                RegisterServices(kernel);
                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }
        private static void RegisterServices(IKernel kernel)
        {
            //kernel.BindFilter<AuthenticationFilter>(FilterScope.First, 0).InRequestScope();
            kernel.BindFilter<ExceptionFilter>(FilterScope.Global, 1);

            kernel.Bind(x =>
            {
                x.FromThisAssembly()
                    .SelectAllClasses() 
                    .BindDefaultInterface();
            });

            //x.FromAssemblyContaining(
            //    typeof(Business.AgencyBusiness),

            //MediatR
            kernel.Components.Add<IBindingResolver, ContravariantBindingResolver>();
            kernel.Bind(scan => 
                scan.FromAssemblyContaining<IMediator>()
                    .SelectAllClasses()
                    .BindDefaultInterface());
            //kernel.Bind<TextWriter>().ToConstant(writer);

            kernel.Bind(scan => scan.FromAssemblyContaining<Ping>().SelectAllClasses().InheritedFrom(typeof(IRequestHandler<,>)).BindAllInterfaces());
            kernel.Bind(scan => scan.FromAssemblyContaining<Ping>().SelectAllClasses().InheritedFrom(typeof(IRequestHandler<>)).BindAllInterfaces());
            kernel.Bind(scan => scan.FromAssemblyContaining<Ping>().SelectAllClasses().InheritedFrom(typeof(INotificationHandler<>)).BindAllInterfaces());

            //Pipeline
            kernel.Bind(typeof(IPipelineBehavior<,>)).To(typeof(RequestPreProcessorBehavior<,>));
            kernel.Bind(typeof(IPipelineBehavior<,>)).To(typeof(RequestPostProcessorBehavior<,>));
            //kernel.Bind(typeof(IPipelineBehavior<,>)).To(typeof(GenericPipelineBehavior<,>));
            //kernel.Bind(typeof(IRequestPreProcessor<>)).To(typeof(GenericRequestPreProcessor<>));
            //kernel.Bind(typeof(IRequestPostProcessor<,>)).To(typeof(GenericRequestPostProcessor<,>));
            //kernel.Bind(typeof(IRequestPostProcessor<,>)).To(typeof(ConstrainedRequestPostProcessor<,>));
            //kernel.Bind(typeof(INotificationHandler<>)).To(typeof(ConstrainedPingedHandler<>)).WhenNotificationMatchesType<Pinged>();

            kernel.Bind<SingleInstanceFactory>().ToMethod(ctx => t => ctx.Kernel.TryGet(t));
            kernel.Bind<MultiInstanceFactory>().ToMethod(ctx => t =>
            {
                try
                {
                    return ctx.Kernel.GetAll(t).ToList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    return new object[0];
                }
            });

            //kernel.Bind<IFilterProvider>().To<NinjectWebApiFilterProvider>();
        }
    }

    public static class BindingExtensions
    {
        public static IBindingInNamedWithOrOnSyntax<object> WhenNotificationMatchesType<TNotification>(this IBindingWhenSyntax<object> syntax)
            where TNotification : INotification
        {
            return syntax.When(request => typeof(TNotification).IsAssignableFrom(request.Service.GenericTypeArguments.Single()));
        }
    }

    public class ContravariantBindingResolver : NinjectComponent, IBindingResolver
    {
        public IEnumerable<IBinding> Resolve(Multimap<Type, IBinding> bindings, Type service)
        {
            if (service.IsGenericType)
            {
                var genericType = service.GetGenericTypeDefinition();
                var genericArguments = genericType.GetGenericArguments();
                if (genericArguments.Count() == 1
                    && genericArguments.Single().GenericParameterAttributes.HasFlag(GenericParameterAttributes.Contravariant))
                {
                    var argument = service.GetGenericArguments().Single();
                    var matches = bindings.Where(kvp => kvp.Key.IsGenericType
                                                        && kvp.Key.GetGenericTypeDefinition().Equals(genericType)
                                                        && kvp.Key.GetGenericArguments().Single() != argument
                                                        && kvp.Key.GetGenericArguments().Single().IsAssignableFrom(argument))
                        .SelectMany(kvp => kvp.Value);
                    return matches;
                }
            }

            return Enumerable.Empty<IBinding>();
        }
    }

    //public class NinjectWebApiFilterProvider : IFilterProvider
    //{
    //    private IKernel _kernel;

    //    public NinjectWebApiFilterProvider(IKernel kernel)
    //    {
    //        _kernel = kernel;
    //    }

    //    public IEnumerable<FilterInfo> GetFilters(HttpConfiguration configuration, HttpActionDescriptor actionDescriptor)
    //    {
    //        var controllerFilters = actionDescriptor.ControllerDescriptor.GetFilters().Select(instance => new FilterInfo(instance, FilterScope.Controller));
    //        var actionFilters = actionDescriptor.GetFilters().Select(instance => new FilterInfo(instance, FilterScope.Action));

    //        var filters = controllerFilters.Concat(actionFilters);

    //        foreach (var filter in filters)
    //        {
    //            _kernel.Inject(filter.Instance);
    //        }

    //        return filters;
    //    }
    //}
}