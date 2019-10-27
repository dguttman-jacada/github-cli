using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using github_cli.Workloads;
using github_cli.Workloads.Communication;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Extensions.Logging;

namespace github_cli
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var builder = Host.CreateDefaultBuilder(args)
                .UseConfigRoot()
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ConfigRoot>(hostContext.Configuration);
                    var logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(hostContext.Configuration)
                        .Enrich.FromLogContext()
                        .CreateLogger();

                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddProvider(new SerilogLoggerProvider(logger, true));
                    });

                    services.UseWorkloads();
                    services.UseCommandLineApp();
                    services.AddHostedService<App>();
                    services.AddSingleton<CsvFactory>();
                    services.AddSingleton(cts);
                });

            try
            {
                await builder.RunConsoleAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
