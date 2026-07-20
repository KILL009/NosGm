using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using NosTale.Module.Bazaar.Queries.GetRcbList;
using System;
using System.Reflection;

namespace NosTale.Module.Bazaar.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBazaarCore(this IServiceCollection services)
        {
            Console.WriteLine("Initializing bazaar module");
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));
            services.AddTransient<IPipelineBehavior<GetRcbListQuery, string>, GetRcbListFallbackBehavior>();
            return services;
        }
    }
}
