using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using github_cli.Extensions;
using github_cli.Workloads.Communication;
using Microsoft.Extensions.DependencyInjection;

namespace github_cli.Workloads
{
    internal static class WorkloadsHelper
    {
        public static IServiceCollection UseWorkloads(this IServiceCollection services) => UseWorkloads(services, typeof(IWorkload).Assembly);

        public static IServiceCollection UseWorkloads(this IServiceCollection services, Assembly assembly)
        {
            services.AddSingleton<ClientFactory>();
            foreach (var type in assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IWorkload).IsAssignableFrom(t)))
            {
                services.AddTransient(typeof(IWorkload), type);
            }
            return services;
        }

        public static IServiceCollection UseCommandLineApp(this IServiceCollection services) => UseCommandLineApp(services, typeof(IWorkload).Assembly);

        public static IServiceCollection UseCommandLineApp(this IServiceCollection services, Assembly assembly)
        {
            var rootCommand = new RootCommand(nameof(github_cli));
            services.Where(s => s.ServiceType == typeof(IWorkload))
                .Select(s => new Command(s.ImplementationType.Name, s.ImplementationType.Name))
                .ForEach(c => rootCommand.Add(c));
            services.AddSingleton(rootCommand);
            return services;
        }

        private class Commander : ICommandHandler
        {
            public Task<int> InvokeAsync(InvocationContext context)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
